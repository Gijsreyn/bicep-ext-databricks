using Microsoft.Azure.Databricks.Client;
using Microsoft.Azure.Databricks.Client.Models;
using Microsoft.Extensions.Logging;
using Bicep.Extension.Databricks.Services;

namespace Bicep.Extension.Databricks.Handlers.Compute;

public class ClusterHandler : BaseHandler<Cluster, ClusterIdentifiers>
{
    private const int TimeoutSeconds = 600; // up to 10 minutes for create/edit + waits
    public ClusterHandler(IDatabricksClientFactory factory, ILogger<ClusterHandler> logger) : base(factory, logger) { }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var desired = request.Properties;
        var client = await GetClientAsync(request.Config.WorkspaceUrl, cancellationToken, TimeoutSeconds);

        _logger.LogInformation("Ensuring cluster '{Name}' (Spark {Spark}) exists", desired.ClusterName, desired.SparkVersion);

        var existing = await FindClusterByName(client, desired.ClusterName, cancellationToken);
        if (existing is not null)
        {
            _logger.LogInformation("Cluster '{Name}' exists (Id {Id}, State {State}) - updating", existing.ClusterName, existing.ClusterId, existing.State);
            await EnsureStableForEdit(client, existing, cancellationToken);
            await client.Clusters.Edit(existing.ClusterId!, BuildAttributes(desired));
            await WaitIfRestarted(client, existing, cancellationToken);
            await PopulateFromServer(client, existing.ClusterId!, request, cancellationToken);
        }
        else
        {
            _logger.LogInformation("Creating cluster '{Name}'", desired.ClusterName);
            var id = await client.Clusters.Create(BuildAttributes(desired));
            await WaitForClusterState(client, id, "RUNNING", cancellationToken);
            await PopulateFromServer(client, id, request, cancellationToken);
        }

        return GetResponse(request);
    }

    private ClusterAttributes BuildAttributes(Cluster cluster)
    {
        var attr = ClusterAttributes.GetNewClusterConfiguration(cluster.ClusterName)
            .WithRuntimeVersion(cluster.SparkVersion);

        if (cluster.Autoscale is not null)
            attr.WithAutoScale(cluster.Autoscale.MinWorkers, cluster.Autoscale.MaxWorkers);
        else if (cluster.NumWorkers > 0)
            attr.WithNumberOfWorkers(cluster.NumWorkers);

        if (!string.IsNullOrEmpty(cluster.NodeTypeId)) attr.WithNodeType(cluster.NodeTypeId);
        if (!string.IsNullOrEmpty(cluster.DriverNodeTypeId)) attr.DriverNodeTypeId = cluster.DriverNodeTypeId;
        if (cluster.AutoTerminationMinutes > 0) attr.WithAutoTermination(cluster.AutoTerminationMinutes);

        if (cluster.ClusterLogConf != null)
        {
            if (cluster.ClusterLogConf.Dbfs != null) attr.WithClusterLogConf(cluster.ClusterLogConf.Dbfs.Destination);
            else if (cluster.ClusterLogConf.Abfss != null) attr.WithClusterLogConf(cluster.ClusterLogConf.Abfss.Destination);
        }

        if (cluster.EnableElasticDisk) attr.EnableElasticDisk = true;

        if (cluster.AzureAttributes != null && cluster.AzureAttributes.FirstOnDemand > 0)
        {
            attr.AzureAttributes = new Microsoft.Azure.Databricks.Client.Models.AzureAttributes { FirstOnDemand = cluster.AzureAttributes.FirstOnDemand };
            if (!string.IsNullOrEmpty(cluster.AzureAttributes.SpotBidMaxPrice) && double.TryParse(cluster.AzureAttributes.SpotBidMaxPrice, out var spot))
                attr.AzureAttributes.SpotBidMaxPrice = spot;
        }

        if (!string.IsNullOrEmpty(cluster.SingleUserName)) attr.SingleUserName = cluster.SingleUserName;
        if (!string.IsNullOrEmpty(cluster.PolicyId)) attr.PolicyId = cluster.PolicyId;
        return attr;
    }

    private async Task EnsureStableForEdit(DatabricksClient client, ClusterInfo existing, CancellationToken ct)
    {
        switch (existing.State)
        {
            case "RUNNING":
            case "TERMINATED":
                return;
            case "PENDING":
            case "RESIZING":
            case "RESTARTING":
                _logger.LogInformation("Waiting for cluster {Id} to reach RUNNING before edit (current {State})", existing.ClusterId, existing.State);
                await WaitForClusterState(client, existing.ClusterId!, "RUNNING", ct); return;
            case "TERMINATING":
                _logger.LogInformation("Waiting for cluster {Id} to terminate before edit", existing.ClusterId);
                await WaitForClusterState(client, existing.ClusterId!, "TERMINATED", ct); return;
            case "ERROR":
            case "UNKNOWN":
                _logger.LogWarning("Cluster {Id} in {State}; attempting edit regardless", existing.ClusterId, existing.State); return;
        }
    }

    private async Task WaitIfRestarted(DatabricksClient client, ClusterInfo existing, CancellationToken ct)
    {
        if (existing.State == "RUNNING" || existing.State == "RESIZING")
        {
            _logger.LogInformation("Waiting for cluster {Id} to return to RUNNING after edit", existing.ClusterId);
            await WaitForClusterState(client, existing.ClusterId!, "RUNNING", ct);
        }
    }

    private async Task PopulateFromServer(DatabricksClient client, string id, ResourceRequest request, CancellationToken ct)
    {
        try
        {
            var info = await client.Clusters.Get(id);
            var props = request.Properties;
            props.ClusterId = info.ClusterId;
            props.ClusterName = info.ClusterName ?? props.ClusterName;
            if (!string.IsNullOrEmpty(info.NodeTypeId)) props.NodeTypeId = info.NodeTypeId;
            if (!string.IsNullOrEmpty(info.DriverNodeTypeId)) props.DriverNodeTypeId = info.DriverNodeTypeId;
            if (!string.IsNullOrEmpty(info.SingleUserName)) props.SingleUserName = info.SingleUserName;
            if (!string.IsNullOrEmpty(info.PolicyId)) props.PolicyId = info.PolicyId;
            if (info.DataSecurityMode.HasValue && Enum.TryParse<DataSecurityMode>(info.DataSecurityMode.Value.ToString(), out var dataSecurityMode))
                props.DataSecurityMode = dataSecurityMode;
            if (info.RuntimeEngine != null) props.RuntimeEngine = info.RuntimeEngine;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to populate cluster {Id} details; returning minimal properties", id);
            request.Properties.ClusterId = id;
        }
    }

    private async Task<ClusterInfo?> FindClusterByName(DatabricksClient client, string name, CancellationToken ct)
    {
        try
        {
            var clusters = await client.Clusters.List();
            var match = clusters.FirstOrDefault(c => c.ClusterName == name);
            if (match == null) return null;
            return new ClusterInfo { ClusterId = match.ClusterId, ClusterName = match.ClusterName, State = match.State?.ToString() };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error listing clusters while searching for {Name}", name);
            throw new InvalidOperationException($"Failed to list clusters searching for {name}: {ex.Message}", ex);
        }
    }

    private async Task WaitForClusterState(DatabricksClient client, string id, string desired, CancellationToken ct)
    {
        const int maxAttempts = 60; // 30 minutes with 30s delay
        const int delaySeconds = 30;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            string state = "UNKNOWN";
            try
            {
                var raw = await client.Clusters.Get(id);
                state = raw.State?.ToString() ?? "UNKNOWN";
            }
            catch (Exception ex)
            {
                if (attempt == maxAttempts - 1)
                    throw new InvalidOperationException($"Failed to get cluster {id} state after {maxAttempts} attempts", ex);
            }
            if (state == desired) return;
            if (state == "ERROR" || state == "UNKNOWN")
                throw new InvalidOperationException($"Cluster {id} entered terminal state {state}");
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), ct);
        }
        throw new InvalidOperationException($"Cluster {id} did not reach state {desired} within timeout");
    }

    protected override ClusterIdentifiers GetIdentifiers(Cluster properties) => new() { ClusterId = properties.ClusterId ?? properties.ClusterName };
}

public class ClusterInfo
{
    public string? ClusterId { get; set; }
    public string? ClusterName { get; set; }
    public string? State { get; set; }
}
