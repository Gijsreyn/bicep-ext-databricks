using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Host.Handlers;
using Bicep.Local.Extension.Types.Attributes;
using System.Text.Json;

namespace Bicep.Extension.Databricks.Handlers.Compute;

public class ClusterHandler : BaseHandler<Cluster, ClusterIdentifiers>
{
    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var cluster = request.Properties;
        // TODO: Add handling for creating/existing cluster based on the state
        // First, try to get an existing cluster by name to see if it already exists
        ClusterInfo? existingCluster = null;
        try
        {
            existingCluster = await GetClusterByName(request, cluster.ClusterName, cancellationToken);
        }
        catch
        {

        }

        if (existingCluster != null)
        {
            // Update existing cluster
            var editPayload = new
            {
                cluster_id = existingCluster.ClusterId,
                cluster_name = cluster.ClusterName,
                spark_version = cluster.SparkVersion,
                num_workers = cluster.NumWorkers > 0 ? cluster.NumWorkers : (int?)null,
                autoscale = cluster.Autoscale != null ? new
                {
                    min_workers = cluster.Autoscale.MinWorkers,
                    max_workers = cluster.Autoscale.MaxWorkers
                } : null,
                node_type_id = cluster.NodeTypeId,
                driver_node_type_id = cluster.DriverNodeTypeId,
                enable_elastic_disk = cluster.EnableElasticDisk,
                enable_local_disk_encryption = cluster.EnableLocalDiskEncryption,
                azure_attributes = cluster.AzureAttributes != null ? new
                {
                    first_on_demand = cluster.AzureAttributes.FirstOnDemand > 0 ? cluster.AzureAttributes.FirstOnDemand : (int?)null,
                    availability = cluster.AzureAttributes.Availability,
                    spot_bid_max_price = !string.IsNullOrEmpty(cluster.AzureAttributes.SpotBidMaxPrice) ? cluster.AzureAttributes.SpotBidMaxPrice : null
                } : null,
                autotermination_minutes = cluster.AutoterminationMinutes > 0 ? cluster.AutoterminationMinutes : (int?)null,
                cluster_log_conf = cluster.ClusterLogConf != null ? new
                {
                    dbfs = cluster.ClusterLogConf.Dbfs != null ? new { destination = cluster.ClusterLogConf.Dbfs.Destination } : null,
                    abfss = cluster.ClusterLogConf.Abfss != null ? new { destination = cluster.ClusterLogConf.Abfss.Destination } : null
                } : null,
                data_security_mode = cluster.DataSecurityMode,
                single_user_name = cluster.SingleUserName,
                runtime_engine = cluster.RuntimeEngine,
                policy_id = cluster.PolicyId
            };

            Console.WriteLine($"[TRACE] Edit cluster payload: {JsonSerializer.Serialize(editPayload, new JsonSerializerOptions { WriteIndented = true })}");
            return await CallDatabricksApi(request, "/api/2.1/clusters/edit", editPayload, cancellationToken);
        }
        else
        {
            // Create new cluster
            var createPayload = new
            {
                cluster_name = cluster.ClusterName,
                spark_version = cluster.SparkVersion,
                num_workers = cluster.NumWorkers > 0 ? cluster.NumWorkers : (int?)null,
                autoscale = cluster.Autoscale != null ? new
                {
                    min_workers = cluster.Autoscale.MinWorkers,
                    max_workers = cluster.Autoscale.MaxWorkers
                } : null,
                node_type_id = cluster.NodeTypeId,
                driver_node_type_id = cluster.DriverNodeTypeId,
                enable_elastic_disk = cluster.EnableElasticDisk,
                enable_local_disk_encryption = cluster.EnableLocalDiskEncryption,
                azure_attributes = cluster.AzureAttributes != null ? new
                {
                    first_on_demand = cluster.AzureAttributes.FirstOnDemand > 0 ? cluster.AzureAttributes.FirstOnDemand : (int?)null,
                    availability = cluster.AzureAttributes.Availability,
                    spot_bid_max_price = !string.IsNullOrEmpty(cluster.AzureAttributes.SpotBidMaxPrice) ? cluster.AzureAttributes.SpotBidMaxPrice : null
                } : null,
                autotermination_minutes = cluster.AutoterminationMinutes > 0 ? cluster.AutoterminationMinutes : (int?)null,
                cluster_log_conf = cluster.ClusterLogConf != null ? new
                {
                    dbfs = cluster.ClusterLogConf.Dbfs != null ? new { destination = cluster.ClusterLogConf.Dbfs.Destination } : null,
                    abfss = cluster.ClusterLogConf.Abfss != null ? new { destination = cluster.ClusterLogConf.Abfss.Destination } : null
                } : null,
                data_security_mode = cluster.DataSecurityMode,
                single_user_name = cluster.SingleUserName,
                runtime_engine = cluster.RuntimeEngine,
                policy_id = cluster.PolicyId
            };

            Console.WriteLine($"[TRACE] Create cluster payload: {JsonSerializer.Serialize(createPayload, new JsonSerializerOptions { WriteIndented = true })}");
            return await CallDatabricksApi(request, "/api/2.1/clusters/create", createPayload, cancellationToken);
        }
    }

    private async Task<ClusterInfo?> GetClusterByName(ResourceRequest request, string clusterName, CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"[TRACE] Searching for cluster with name: {clusterName}");
            
            // Call the clusters/list API to get all clusters
            var responseJson = await CallDatabricksApiForResponse(request, "/api/2.1/clusters/list", new { }, cancellationToken);
            
            // Parse the JSON response
            var responseDoc = JsonDocument.Parse(responseJson);
            
            if (responseDoc.RootElement.TryGetProperty("clusters", out var clustersElement))
            {
                var matchingClusters = new List<ClusterInfo>();
                
                foreach (var clusterElement in clustersElement.EnumerateArray())
                {
                    if (clusterElement.TryGetProperty("cluster_name", out var nameElement) &&
                        nameElement.GetString() == clusterName)
                    {
                        var clusterId = clusterElement.TryGetProperty("cluster_id", out var idElement) 
                            ? idElement.GetString() 
                            : null;
                            
                        var state = clusterElement.TryGetProperty("state", out var stateElement) 
                            ? stateElement.GetString() 
                            : null;
                        
                        matchingClusters.Add(new ClusterInfo
                        {
                            ClusterId = clusterId,
                            ClusterName = clusterName,
                            State = state
                        });
                    }
                }
                
                if (matchingClusters.Count == 0)
                {
                    Console.WriteLine($"[TRACE] No cluster found with name: {clusterName}");
                    return null;
                }
                else if (matchingClusters.Count == 1)
                {
                    Console.WriteLine($"[TRACE] Found cluster {clusterName} with ID: {matchingClusters[0].ClusterId}");
                    return matchingClusters[0];
                }
                else
                {
                    Console.WriteLine($"[TRACE] Found {matchingClusters.Count} clusters with name '{clusterName}'. Creating new one.");
                    return null;
                }
            }
            
            Console.WriteLine("[TRACE] No clusters array found in API response");
            return null;
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
