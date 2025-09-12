using System.Text.Json;
using Microsoft.Extensions.Logging;
using DatabricksCluster = Databricks.Models.Compute.Cluster;
using DatabricksClusterIdentifiers = Databricks.Models.Compute.ClusterIdentifiers;
using Configuration = Databricks.Models.Configuration;

namespace Databricks.Handlers.Compute;

public class DatabricksClusterHandler : DatabricksResourceHandlerBase<DatabricksCluster, DatabricksClusterIdentifiers>
{
    private const string ClustersApiEndpoint = "2.0/clusters";
    private const string ClusterCreateApiEndpoint = "2.0/clusters/create";
    private const string ClusterEditApiEndpoint = "2.0/clusters/edit";
    private const string ClusterStartApiEndpoint = "2.0/clusters/start";
    private const string ClusterDeleteApiEndpoint = "2.0/clusters/delete";
    private const string ClusterGetApiEndpoint = "2.0/clusters/get";
    private const string ClustersListApiEndpoint = "2.0/clusters/list";

    // Azure availability constants
    private const string AzureAvailabilitySpot = "SPOT_AZURE";
    private const string AzureAvailabilityOnDemand = "ON_DEMAND_AZURE";
    private const string AzureAvailabilitySpotWithFallback = "SPOT_WITH_FALLBACK_AZURE";

    // Cluster states
    private const string ClusterStatePending = "PENDING";
    private const string ClusterStateRunning = "RUNNING";
    private const string ClusterStateRestarting = "RESTARTING";
    private const string ClusterStateResizing = "RESIZING";
    private const string ClusterStateTerminating = "TERMINATING";
    private const string ClusterStateTerminated = "TERMINATED";
    private const string ClusterStateError = "ERROR";
    private const string ClusterStateUnknown = "UNKNOWN";

    private static readonly HashSet<string> RunningStates = new() { ClusterStateRunning, ClusterStateResizing };
    private static readonly HashSet<string> TerminalStates = new() { ClusterStateTerminated, ClusterStateError };
    private static readonly HashSet<string> TransientStates = new() { ClusterStatePending, ClusterStateRestarting, ClusterStateTerminating };

    public DatabricksClusterHandler(ILogger<DatabricksClusterHandler> logger) : base(logger) { }

    protected override async Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
    {
        var existing = await GetClusterByNameAsync(request.Config, request.Properties.ClusterName, cancellationToken);
        if (existing is not null)
        {
            PopulateClusterProperties(request.Properties, existing);
        }
        return GetResponse(request);
    }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var props = request.Properties;
        
        ValidateClusterProperties(props);
        
        _logger.LogInformation("Ensuring cluster {ClusterName}", props.ClusterName);

        var existing = await GetClusterByNameAsync(request.Config, props.ClusterName, cancellationToken);

        dynamic cluster;
        if (existing is null)
        {
            _logger.LogInformation("Creating new cluster {ClusterName}", props.ClusterName);
            cluster = await CreateClusterAsync(request.Config, props, cancellationToken);
        }
        else
        {
            _logger.LogInformation("Updating existing cluster {ClusterName} (ID: {ClusterId})", props.ClusterName, (string)existing.cluster_id);
            cluster = await UpdateClusterAsync(request.Config, props, existing, cancellationToken);
        }

        // Wait for cluster to be in running state
        _logger.LogInformation("Waiting for cluster {ClusterName} to be running", props.ClusterName);
        cluster = await WaitForClusterStateAsync(request.Config, (string)cluster.cluster_id, ClusterStateRunning, cancellationToken);

        PopulateClusterProperties(props, cluster);
        return GetResponse(request);
    }

    protected override DatabricksClusterIdentifiers GetIdentifiers(DatabricksCluster properties) => new()
    {
        ClusterName = properties.ClusterName
    };

    private async Task<dynamic?> GetClusterByNameAsync(Configuration configuration, string clusterName, CancellationToken ct)
    {
        try
        {
            var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Get, ClustersListApiEndpoint, ct);
            
            if (!response.TryGetProperty("clusters", out var clustersArray))
                return null;

            foreach (var cluster in clustersArray.EnumerateArray())
            {
                if (cluster.TryGetProperty("cluster_name", out var nameProperty) && 
                    nameProperty.GetString() == clusterName)
                {
                    return CreateClusterObject(cluster);
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cluster by name {ClusterName}", clusterName);
            return null;
        }
    }

    private async Task<dynamic> CreateClusterAsync(Configuration configuration, DatabricksCluster props, CancellationToken ct)
    {
        var createPayload = BuildClusterPayload(props);

        var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Post, ClusterCreateApiEndpoint, ct, createPayload);
        
        if (!response.TryGetProperty("cluster_id", out JsonElement clusterIdProperty))
            throw new InvalidOperationException("Cluster creation did not return cluster ID.");

        var clusterId = clusterIdProperty.GetString();
        _logger.LogInformation("Created cluster {ClusterName} with ID {ClusterId}", props.ClusterName, clusterId);

        // Get the full cluster information
        return await GetClusterByIdAsync(configuration, clusterId!, ct)
            ?? throw new InvalidOperationException("Could not retrieve created cluster information.");
    }

    private async Task<dynamic> UpdateClusterAsync(Configuration configuration, DatabricksCluster props, dynamic existing, CancellationToken ct)
    {
        var editPayload = BuildClusterPayloadForEdit(props, (string)existing.cluster_id);

        await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Post, ClusterEditApiEndpoint, ct, editPayload);
        
        _logger.LogInformation("Updated cluster {ClusterName} (ID: {ClusterId})", props.ClusterName, (string)existing.cluster_id);

        // Return the existing cluster with updated information
        return existing;
    }

    private async Task<dynamic> GetClusterByIdAsync(Configuration configuration, string clusterId, CancellationToken ct)
    {
        var endpoint = $"{ClusterGetApiEndpoint}?cluster_id={Uri.EscapeDataString(clusterId)}";
        var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Get, endpoint, ct);
        return CreateClusterObject(response);
    }

    private async Task<dynamic> WaitForClusterStateAsync(Configuration configuration, string clusterId, string desiredState, CancellationToken ct)
    {
        var timeout = TimeSpan.FromMinutes(20); // Reasonable timeout for cluster operations
        var pollInterval = TimeSpan.FromSeconds(30);
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < timeout)
        {
            ct.ThrowIfCancellationRequested();

            var cluster = await GetClusterByIdAsync(configuration, clusterId, ct);
            var currentState = (string)cluster.state;

            _logger.LogInformation("Cluster {ClusterId} current state: {CurrentState}, desired: {DesiredState}", 
                clusterId, currentState, desiredState);

            if (currentState == desiredState)
            {
                _logger.LogInformation("Cluster {ClusterId} reached desired state: {DesiredState}", clusterId, desiredState);
                return cluster;
            }

            if (TerminalStates.Contains(currentState) && currentState != desiredState)
            {
                var stateMessage = cluster.state_message != null ? $" ({cluster.state_message})" : "";
                throw new InvalidOperationException($"Cluster {clusterId} entered terminal state {currentState}{stateMessage} while waiting for {desiredState}");
            }

            // If cluster is terminated and we want it running, start it
            if (currentState == ClusterStateTerminated && desiredState == ClusterStateRunning)
            {
                _logger.LogInformation("Starting terminated cluster {ClusterId}", clusterId);
                await StartClusterAsync(configuration, clusterId, ct);
            }

            await Task.Delay(pollInterval, ct);
        }

        throw new TimeoutException($"Timeout waiting for cluster {clusterId} to reach state {desiredState}");
    }

    private async Task StartClusterAsync(Configuration configuration, string clusterId, CancellationToken ct)
    {
        var payload = new { cluster_id = clusterId };
        await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Post, ClusterStartApiEndpoint, ct, payload);
        _logger.LogInformation("Started cluster {ClusterId}", clusterId);
    }

    private static dynamic BuildClusterPayload(DatabricksCluster props)
    {
        var payload = new
        {
            cluster_name = props.ClusterName,
            spark_version = props.SparkVersion,
            node_type_id = props.NodeTypeId,
            driver_node_type_id = props.DriverNodeTypeId,
            num_workers = props.NumWorkers > 0 ? props.NumWorkers : (int?)null,
            autoscale = props.AutoScale != null ? new
            {
                min_workers = props.AutoScale.MinWorkers,
                max_workers = props.AutoScale.MaxWorkers
            } : null,
            azure_attributes = props.AzureAttributes != null ? new
            {
                first_on_demand = props.AzureAttributes.FirstOnDemand > 0 ? props.AzureAttributes.FirstOnDemand : (int?)null,
                availability = props.AzureAttributes.Availability,
                spot_bid_max_price = props.AzureAttributes.SpotBidMaxPrice > 0 ? props.AzureAttributes.SpotBidMaxPrice : (int?)null
            } : null,
            autotermination_minutes = props.AutoterminationMinutes > 0 ? props.AutoterminationMinutes : (int?)null,
            spark_conf = props.SparkConf,
            spark_env_vars = props.SparkEnvVars,
            custom_tags = props.CustomTags,
            ssh_public_keys = props.SshPublicKeys,
            init_scripts = props.InitScripts,
            data_security_mode = props.DataSecurityMode,
            single_user_name = props.SingleUserName,
            runtime_engine = props.RuntimeEngine
        };

        return payload;
    }

    private static dynamic BuildClusterPayloadForEdit(DatabricksCluster props, string clusterId)
    {
        var payload = new
        {
            cluster_id = clusterId,
            cluster_name = props.ClusterName,
            spark_version = props.SparkVersion,
            node_type_id = props.NodeTypeId,
            driver_node_type_id = props.DriverNodeTypeId,
            num_workers = props.NumWorkers > 0 ? props.NumWorkers : (int?)null,
            autoscale = props.AutoScale != null ? new
            {
                min_workers = props.AutoScale.MinWorkers,
                max_workers = props.AutoScale.MaxWorkers
            } : null,
            azure_attributes = props.AzureAttributes != null ? new
            {
                first_on_demand = props.AzureAttributes.FirstOnDemand > 0 ? props.AzureAttributes.FirstOnDemand : (int?)null,
                availability = props.AzureAttributes.Availability,
                spot_bid_max_price = props.AzureAttributes.SpotBidMaxPrice > 0 ? props.AzureAttributes.SpotBidMaxPrice : (int?)null
            } : null,
            autotermination_minutes = props.AutoterminationMinutes > 0 ? props.AutoterminationMinutes : (int?)null,
            spark_conf = props.SparkConf,
            spark_env_vars = props.SparkEnvVars,
            custom_tags = props.CustomTags,
            ssh_public_keys = props.SshPublicKeys,
            init_scripts = props.InitScripts,
            data_security_mode = props.DataSecurityMode,
            single_user_name = props.SingleUserName,
            runtime_engine = props.RuntimeEngine
        };

        return payload;
    }

    private static dynamic CreateClusterObject(JsonElement cluster)
    {
        return new
        {
            cluster_id = cluster.TryGetProperty("cluster_id", out var clusterId) ? clusterId.GetString() : null,
            cluster_name = cluster.TryGetProperty("cluster_name", out var clusterName) ? clusterName.GetString() : null,
            spark_version = cluster.TryGetProperty("spark_version", out var sparkVersion) ? sparkVersion.GetString() : null,
            node_type_id = cluster.TryGetProperty("node_type_id", out var nodeTypeId) ? nodeTypeId.GetString() : null,
            driver_node_type_id = cluster.TryGetProperty("driver_node_type_id", out var driverNodeTypeId) ? driverNodeTypeId.GetString() : null,
            num_workers = cluster.TryGetProperty("num_workers", out var numWorkers) ? numWorkers.GetInt32() : 0,
            autoscale = cluster.TryGetProperty("autoscale", out var autoscale) ? new
            {
                min_workers = autoscale.TryGetProperty("min_workers", out var minWorkers) ? minWorkers.GetInt32() : 0,
                max_workers = autoscale.TryGetProperty("max_workers", out var maxWorkers) ? maxWorkers.GetInt32() : 0
            } : null,
            state = cluster.TryGetProperty("state", out var state) ? state.GetString() : null,
            state_message = cluster.TryGetProperty("state_message", out var stateMessage) ? stateMessage.GetString() : null,
            jdbc_port = cluster.TryGetProperty("jdbc_port", out var jdbcPort) ? jdbcPort.GetInt32() : 0,
            cluster_cores = cluster.TryGetProperty("cluster_cores", out var clusterCores) ? clusterCores.GetDouble() : 0.0,
            cluster_memory_mb = cluster.TryGetProperty("cluster_memory_mb", out var clusterMemoryMb) ? clusterMemoryMb.GetInt64() : 0L,
            start_time = cluster.TryGetProperty("start_time", out var startTime) ? startTime.GetInt64() : 0L,
            creator_user_name = cluster.TryGetProperty("creator_user_name", out var creatorUserName) ? creatorUserName.GetString() : null,
            autotermination_minutes = cluster.TryGetProperty("autotermination_minutes", out var autoterminationMinutes) ? autoterminationMinutes.GetInt32() : 0,
            spark_conf = cluster.TryGetProperty("spark_conf", out var sparkConf) ? sparkConf : (object?)null,
            spark_env_vars = cluster.TryGetProperty("spark_env_vars", out var sparkEnvVars) ? sparkEnvVars : (object?)null,
            custom_tags = cluster.TryGetProperty("custom_tags", out var customTags) ? customTags : (object?)null,
            ssh_public_keys = cluster.TryGetProperty("ssh_public_keys", out var sshPublicKeys) ? sshPublicKeys : (object?)null,
            init_scripts = cluster.TryGetProperty("init_scripts", out var initScripts) ? initScripts : (object?)null,
            data_security_mode = cluster.TryGetProperty("data_security_mode", out var dataSecurityMode) ? dataSecurityMode.GetString() : null,
            single_user_name = cluster.TryGetProperty("single_user_name", out var singleUserName) ? singleUserName.GetString() : null,
            runtime_engine = cluster.TryGetProperty("runtime_engine", out var runtimeEngine) ? runtimeEngine.GetString() : null
        };
    }

    private static void PopulateClusterProperties(DatabricksCluster props, dynamic cluster)
    {
        props.State = cluster.state;
        props.StateMessage = cluster.state_message;
        props.JdbcPort = cluster.jdbc_port ?? 0;
        props.ClusterCores = (int)(cluster.cluster_cores ?? 0.0);
        props.ClusterMemoryMb = (int)(cluster.cluster_memory_mb ?? 0L);
        props.StartTime = (int)(cluster.start_time ?? 0L);
        props.CreatorUserName = cluster.creator_user_name;

        // Update configuration properties that might have been defaulted
        if (cluster.autotermination_minutes != null)
            props.AutoterminationMinutes = cluster.autotermination_minutes;
        
        if (cluster.driver_node_type_id != null)
            props.DriverNodeTypeId = cluster.driver_node_type_id;

        if (cluster.spark_conf != null)
            props.SparkConf = cluster.spark_conf;

        if (cluster.spark_env_vars != null)
            props.SparkEnvVars = cluster.spark_env_vars;

        if (cluster.custom_tags != null)
            props.CustomTags = cluster.custom_tags;

        if (cluster.ssh_public_keys != null)
            props.SshPublicKeys = cluster.ssh_public_keys;

        if (cluster.init_scripts != null)
            props.InitScripts = cluster.init_scripts;

        if (cluster.data_security_mode != null)
            props.DataSecurityMode = cluster.data_security_mode;

        if (cluster.runtime_engine != null)
            props.RuntimeEngine = cluster.runtime_engine;
    }

    private static void ValidateClusterProperties(DatabricksCluster props)
    {
        if (string.IsNullOrWhiteSpace(props.ClusterName))
            throw new ArgumentException("Cluster name cannot be null or empty.", nameof(props.ClusterName));

        if (string.IsNullOrWhiteSpace(props.SparkVersion))
            throw new ArgumentException("Spark version cannot be null or empty.", nameof(props.SparkVersion));

        if (string.IsNullOrWhiteSpace(props.NodeTypeId))
            throw new ArgumentException("Node type ID cannot be null or empty.", nameof(props.NodeTypeId));

        // Validate that either NumWorkers or AutoScale is specified, but not both
        if (props.NumWorkers > 0 && props.AutoScale != null)
            throw new ArgumentException("Cannot specify both NumWorkers and AutoScale. Choose one.", nameof(props.NumWorkers));

        if (props.NumWorkers <= 0 && props.AutoScale == null)
            throw new ArgumentException("Must specify either NumWorkers or AutoScale configuration.", nameof(props.NumWorkers));

        if (props.AutoScale != null)
        {
            if (props.AutoScale.MinWorkers < 0)
                throw new ArgumentException("AutoScale MinWorkers must be non-negative.", nameof(props.AutoScale.MinWorkers));

            if (props.AutoScale.MaxWorkers < props.AutoScale.MinWorkers)
                throw new ArgumentException("AutoScale MaxWorkers must be greater than or equal to MinWorkers.", nameof(props.AutoScale.MaxWorkers));
        }

        // Validate Azure availability if specified
        if (props.AzureAttributes?.Availability != null)
        {
            var validAvailabilities = new[] { AzureAvailabilitySpot, AzureAvailabilityOnDemand, AzureAvailabilitySpotWithFallback };
            if (!validAvailabilities.Contains(props.AzureAttributes.Availability))
            {
                throw new ArgumentException($"Invalid Azure availability: {props.AzureAttributes.Availability}. " +
                    $"Valid values are: {string.Join(", ", validAvailabilities)}", nameof(props.AzureAttributes.Availability));
            }
        }
    }
}
