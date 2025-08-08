using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;
using System.Text.Json;
using Microsoft.Azure.Databricks.Client;
using Microsoft.Azure.Databricks.Client.Models;

namespace Bicep.Extension.Databricks.Handlers.Compute;

public class ClusterHandler : BaseHandler<Cluster, ClusterIdentifiers>
{
    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var cluster = request.Properties;

        ClusterInfo? existingCluster = null;
        existingCluster = await GetClusterByName(request, cluster.ClusterName, cancellationToken);

        if (existingCluster != null)
        {
            // Update existing cluster using DatabricksClient
            var client = await GetClientAsync(request, cancellationToken);
            var clusterAttributes = ConvertToClusterAttributes(cluster);
            
            Console.WriteLine($"[TRACE] Edit cluster payload: {JsonSerializer.Serialize(clusterAttributes, new JsonSerializerOptions { WriteIndented = true })}");
            
            var wasRunning = existingCluster.State == "RUNNING" || existingCluster.State == "RESIZING";
            
            // Handle different cluster states before editing
            switch (existingCluster.State)
            {
                case "RUNNING":
                case "TERMINATED":
                    // Safe to edit
                    break;
                case "PENDING":
                case "RESIZING":
                case "RESTARTING":
                    // Wait for cluster to reach a stable state before editing
                    if (!string.IsNullOrEmpty(existingCluster.ClusterId))
                    {
                        Console.WriteLine($"[TRACE] Cluster is in {existingCluster.State} state, waiting for RUNNING state before edit");
                        await WaitForClusterState(client, existingCluster.ClusterId, "RUNNING", cancellationToken);
                    }
                    break;
                case "TERMINATING":
                    // Wait for termination to complete before editing
                    if (!string.IsNullOrEmpty(existingCluster.ClusterId))
                    {
                        Console.WriteLine($"[TRACE] Cluster is terminating, waiting for TERMINATED state before edit");
                        await WaitForClusterState(client, existingCluster.ClusterId, "TERMINATED", cancellationToken);
                    }
                    break;
                case "ERROR":
                case "UNKNOWN":
                    Console.WriteLine($"[WARN] Cluster is in {existingCluster.State} state, attempting to edit anyway");
                    break;
            }
            
            await client.Clusters.Edit(existingCluster.ClusterId, clusterAttributes);
            
            // If cluster was running before edit, wait for it to restart and reach RUNNING state
            // Note: Edit operation automatically restarts running clusters, so we don't need to call Start()
            if (wasRunning && !string.IsNullOrEmpty(existingCluster.ClusterId))
            {
                await WaitForClusterState(client, existingCluster.ClusterId, "RUNNING", cancellationToken);
            }

            if (string.IsNullOrEmpty(existingCluster.ClusterId))
            {
                throw new InvalidOperationException("Cluster ID is null or empty after edit operation");
            }

            return await CreateResourceResponse(request, client, existingCluster.ClusterId);
        }
        else
        {
            var client = await GetClientAsync(request, cancellationToken);
            var clusterAttributes = ConvertToClusterAttributes(cluster);

            Console.WriteLine($"[TRACE] Create cluster payload: {JsonSerializer.Serialize(clusterAttributes, new JsonSerializerOptions { WriteIndented = true })}");
            var clusterId = await client.Clusters.Create(clusterAttributes);
            
            await WaitForClusterState(client, clusterId, "RUNNING", cancellationToken);
            return await CreateResourceResponse(request, client, clusterId);
        }
    }

    private ClusterAttributes ConvertToClusterAttributes(Cluster cluster)
    {
        var clusterAttributes = ClusterAttributes.GetNewClusterConfiguration(cluster.ClusterName)
            .WithRuntimeVersion(cluster.SparkVersion);

        // Set worker configuration
        if (cluster.Autoscale != null)
        {
            clusterAttributes.WithAutoScale(cluster.Autoscale.MinWorkers, cluster.Autoscale.MaxWorkers);
        }
        else if (cluster.NumWorkers > 0)
        {
            clusterAttributes.WithNumberOfWorkers(cluster.NumWorkers);
        }

        // Set node types
        if (!string.IsNullOrEmpty(cluster.NodeTypeId))
        {
            clusterAttributes.WithNodeType(cluster.NodeTypeId);
        }

        if (!string.IsNullOrEmpty(cluster.DriverNodeTypeId))
        {
            clusterAttributes.DriverNodeTypeId = cluster.DriverNodeTypeId;
        }

        // Set auto-termination
        if (cluster.AutoterminationMinutes > 0)
        {
            clusterAttributes.WithAutoTermination(cluster.AutoterminationMinutes);
        }

        // Set cluster log configuration
        if (cluster.ClusterLogConf != null)
        {
            if (cluster.ClusterLogConf.Dbfs != null)
            {
                clusterAttributes.WithClusterLogConf(cluster.ClusterLogConf.Dbfs.Destination);
            }
            else if (cluster.ClusterLogConf.Abfss != null)
            {
                clusterAttributes.WithClusterLogConf(cluster.ClusterLogConf.Abfss.Destination);
            }
        }

        // Set additional properties
        if (cluster.EnableElasticDisk)
        {
            clusterAttributes.EnableElasticDisk = true;
        }

        if (cluster.EnableLocalDiskEncryption)
        {
            // Note: EnableLocalDiskEncryption might not be directly available in ClusterAttributes
            // This property may need to be set through other means or may not be supported
        }

        // Set Azure attributes if provided
        if (cluster.AzureAttributes != null && cluster.AzureAttributes.FirstOnDemand > 0)
        {
            clusterAttributes.AzureAttributes = new Microsoft.Azure.Databricks.Client.Models.AzureAttributes
            {
                FirstOnDemand = cluster.AzureAttributes.FirstOnDemand
            };

            if (!string.IsNullOrEmpty(cluster.AzureAttributes.SpotBidMaxPrice))
            {
                if (double.TryParse(cluster.AzureAttributes.SpotBidMaxPrice, out var spotPrice))
                {
                    clusterAttributes.AzureAttributes.SpotBidMaxPrice = spotPrice;
                }
            }
        }

        // Note: Some advanced properties like DataSecurityMode and RuntimeEngine 
        // may need to be set through other means or may use different property names
        
        // Set single user name
        if (!string.IsNullOrEmpty(cluster.SingleUserName))
        {
            clusterAttributes.SingleUserName = cluster.SingleUserName;
        }

        // Set policy ID
        if (!string.IsNullOrEmpty(cluster.PolicyId))
        {
            clusterAttributes.PolicyId = cluster.PolicyId;
        }

        return clusterAttributes;
    }

    private async Task<ResourceResponse> CreateResourceResponse(ResourceRequest request, DatabricksClient client, string clusterId)
    {
        try
        {
            // Get the current cluster information from Databricks
            var clusterInfo = await client.Clusters.Get(clusterId);
            
            // Start with the original request properties to preserve all configuration
            var cluster = request.Properties;
            
            // Update with actual values from Databricks
            cluster.ClusterId = clusterInfo.ClusterId;
            cluster.ClusterName = clusterInfo.ClusterName ?? cluster.ClusterName;
            
            // Update properties that are available in ClusterInfo
            if (!string.IsNullOrEmpty(clusterInfo.NodeTypeId))
                cluster.NodeTypeId = clusterInfo.NodeTypeId;
            
            if (!string.IsNullOrEmpty(clusterInfo.DriverNodeTypeId))
                cluster.DriverNodeTypeId = clusterInfo.DriverNodeTypeId;
                
            if (!string.IsNullOrEmpty(clusterInfo.SingleUserName))
                cluster.SingleUserName = clusterInfo.SingleUserName;
                
            if (!string.IsNullOrEmpty(clusterInfo.PolicyId))
                cluster.PolicyId = clusterInfo.PolicyId;
            
            // Update data security mode and runtime engine if available
            if (clusterInfo.DataSecurityMode != null)
                cluster.DataSecurityMode = clusterInfo.DataSecurityMode.ToString();
                
            if (clusterInfo.RuntimeEngine != null)
                cluster.RuntimeEngine = clusterInfo.RuntimeEngine.ToString();
            
            // Create the resource response
            return new ResourceResponse
            {
                Type = "Cluster",
                Properties = cluster,
                Identifiers = new ClusterIdentifiers { ClusterId = clusterId }
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Failed to get cluster {clusterId} details for response: {ex.Message}");
            
            // Fallback: use the original request properties but update the cluster ID
            var fallbackCluster = request.Properties;
            fallbackCluster.ClusterId = clusterId;
            
            return new ResourceResponse
            {
                Type = "Cluster",
                Properties = fallbackCluster,
                Identifiers = new ClusterIdentifiers { ClusterId = clusterId }
            };
        }
    }

    private async Task WaitForClusterState(DatabricksClient client, string clusterId, string desiredState, CancellationToken cancellationToken)
    {
        const int maxAttempts = 20; // 10 minutes with 30-second intervals
        const int delaySeconds = 30;
        
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Check for cancellation before each attempt
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                var clusterInfo = await client.Clusters.Get(clusterId);
                var currentState = clusterInfo.State?.ToString() ?? "UNKNOWN";
                
                Console.WriteLine($"[TRACE] Cluster {clusterId} is in state: {currentState} (attempt {attempt + 1}/{maxAttempts})");
                
                if (currentState == desiredState)
                {
                    Console.WriteLine($"[TRACE] Cluster {clusterId} reached desired state: {desiredState}");
                    return;
                }
                
                // Check if the cluster can reach the desired state
                if (IsTerminalErrorState(currentState))
                {
                    var errorMsg = !string.IsNullOrEmpty(clusterInfo.StateMessage) 
                        ? clusterInfo.StateMessage 
                        : "Unknown error";
                    throw new InvalidOperationException(
                        $"Cluster {clusterId} is in terminal error state '{currentState}': {errorMsg}");
                }
                
                if (attempt < maxAttempts - 1) // Don't wait after the last attempt
                {
                    Console.WriteLine($"[TRACE] Waiting {delaySeconds} seconds before checking cluster state again...");
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine($"[TRACE] Operation cancelled while waiting for cluster {clusterId}");
                        throw;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"[TRACE] Operation cancelled while checking cluster {clusterId} state");
                throw;
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                Console.WriteLine($"[TRACE] Error checking cluster state (attempt {attempt + 1}): {ex.Message}");
                if (attempt == maxAttempts - 1)
                {
                    throw new InvalidOperationException($"Failed to get cluster {clusterId} state after {maxAttempts} attempts", ex);
                }
                
                if (attempt < maxAttempts - 1)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine($"[TRACE] Operation cancelled while waiting after error for cluster {clusterId}");
                        throw;
                    }
                }
            }
        }
        
        throw new InvalidOperationException(
            $"Cluster {clusterId} did not reach state '{desiredState}' within the timeout period of {maxAttempts * delaySeconds / 60} minutes");
    }

    private static bool IsTerminalErrorState(string state)
    {
        return state switch
        {
            "ERROR" => true,
            "UNKNOWN" => true,
            _ => false
        };
    }

    private async Task<ClusterInfo?> GetClusterByName(ResourceRequest request, string clusterName, CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"[TRACE] Searching for cluster with name: {clusterName}");
            
            var client = await GetClientAsync(request, cancellationToken);
            var clusters = await client.Clusters.List();
            
            var matchingClusters = clusters.Where(c => c.ClusterName == clusterName).ToList();
            
            if (matchingClusters.Count == 0)
            {
                Console.WriteLine($"[TRACE] No cluster found with name: {clusterName}");
                return null;
            }
            else if (matchingClusters.Count == 1)
            {
                var matchingCluster = matchingClusters[0];
                Console.WriteLine($"[TRACE] Found cluster {clusterName} with ID: {matchingCluster.ClusterId}");
                return new ClusterInfo
                {
                    ClusterId = matchingCluster.ClusterId,
                    ClusterName = matchingCluster.ClusterName,
                    State = matchingCluster.State?.ToString()
                };
            }
            else
            {
                Console.WriteLine($"[TRACE] Found {matchingClusters.Count} clusters with name '{clusterName}'. Creating new one.");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TRACE] Error searching for cluster {clusterName}: {ex.Message}");
            throw new InvalidOperationException($"Failed to search for cluster {clusterName}. Error: {ex.Message}", ex);
        }
    }

    protected override ClusterIdentifiers GetIdentifiers(Cluster properties)
        => new()
        {
            ClusterId = properties.ClusterName,
        };
}

public class ClusterInfo
{
    public string? ClusterId { get; set; }
    public string? ClusterName { get; set; }
    public string? State { get; set; }
}

public class ClusterIdentifiers
{
    [TypeProperty("The unique identifier of the cluster.", ObjectTypePropertyFlags.Identifier)]
    public string? ClusterId { get; set; }
}

[ResourceType("Cluster")]
public class Cluster : ClusterIdentifiers
{
    [TypeProperty("The cluster name.", ObjectTypePropertyFlags.Required)]
    public required string ClusterName { get; set; }

    [TypeProperty("The Spark version to use for the cluster.", ObjectTypePropertyFlags.Required)]
    public required string SparkVersion { get; set; }

    [TypeProperty("The number of worker nodes.", ObjectTypePropertyFlags.None)]
    public int NumWorkers { get; set; }

    [TypeProperty("Auto-scaling configuration for the cluster.", ObjectTypePropertyFlags.None)]
    public AutoScale? Autoscale { get; set; }

    [TypeProperty("The node type ID for worker nodes.", ObjectTypePropertyFlags.None)]
    public string? NodeTypeId { get; set; }

    [TypeProperty("The node type ID for the driver node.", ObjectTypePropertyFlags.None)]
    public string? DriverNodeTypeId { get; set; }

    [TypeProperty("Whether to enable elastic disk.", ObjectTypePropertyFlags.None)]
    public bool EnableElasticDisk { get; set; }

    [TypeProperty("Whether to enable local disk encryption.", ObjectTypePropertyFlags.None)]
    public bool EnableLocalDiskEncryption { get; set; }

    [TypeProperty("Azure-specific cluster attributes.", ObjectTypePropertyFlags.None)]
    public AzureAttributes? AzureAttributes { get; set; }

    [TypeProperty("Auto-termination time in minutes.", ObjectTypePropertyFlags.None)]
    public int AutoterminationMinutes { get; set; }

    [TypeProperty("Cluster log configuration.", ObjectTypePropertyFlags.None)]
    public StorageInfo? ClusterLogConf { get; set; }

    [TypeProperty("Data security mode for the cluster.", ObjectTypePropertyFlags.None)]
    public string? DataSecurityMode { get; set; }

    [TypeProperty("Single user name for single-user clusters.", ObjectTypePropertyFlags.None)]
    public string? SingleUserName { get; set; }

    [TypeProperty("Runtime engine for the cluster.", ObjectTypePropertyFlags.None)]
    public string? RuntimeEngine { get; set; }

    [TypeProperty("Policy ID to apply to the cluster.", ObjectTypePropertyFlags.None)]
    public string? PolicyId { get; set; }
}

public class AutoScale
{
    [TypeProperty("Minimum number of workers.", ObjectTypePropertyFlags.Required)]
    public required int MinWorkers { get; set; }

    [TypeProperty("Maximum number of workers.", ObjectTypePropertyFlags.Required)]
    public required int MaxWorkers { get; set; }
}

public class AzureAttributes
{
    [TypeProperty("Number of on-demand instances to use before switching to spot instances.", ObjectTypePropertyFlags.None)]
    public int FirstOnDemand { get; set; }

    [TypeProperty("Azure availability type (ON_DEMAND_AZURE, SPOT_AZURE, SPOT_WITH_FALLBACK_AZURE).", ObjectTypePropertyFlags.None)]
    public string? Availability { get; set; }

    [TypeProperty("Maximum price for spot instances as a percentage of on-demand price.", ObjectTypePropertyFlags.None)]
    public string? SpotBidMaxPrice { get; set; }
}

public class StorageInfo
{
    [TypeProperty("DBFS storage configuration.", ObjectTypePropertyFlags.None)]
    public DbfsStorageInfo? Dbfs { get; set; }

    [TypeProperty("Azure ADLS storage configuration.", ObjectTypePropertyFlags.None)]
    public AbfssStorageInfo? Abfss { get; set; }
}

public class DbfsStorageInfo
{
    [TypeProperty("DBFS destination path.", ObjectTypePropertyFlags.Required)]
    public required string Destination { get; set; }
}

public class AbfssStorageInfo
{
    [TypeProperty("Azure Data Lake Storage destination path.", ObjectTypePropertyFlags.Required)]
    public required string Destination { get; set; }
}

public static class AzureAvailability
{
    public const string OnDemand = "ON_DEMAND_AZURE";
    public const string Spot = "SPOT_AZURE";
    public const string SpotWithFallback = "SPOT_WITH_FALLBACK_AZURE";
}

public static class DataSecurityMode
{
    public const string None = "NONE";
    public const string SingleUser = "SINGLE_USER";
    public const string UserIsolation = "USER_ISOLATION";
    public const string LegacyTableAcl = "LEGACY_TABLE_ACL";
    public const string LegacyPassthrough = "LEGACY_PASSTHROUGH";
    public const string LegacySingleUser = "LEGACY_SINGLE_USER";
}
