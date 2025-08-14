using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;
using Microsoft.Azure.Databricks.Client.Models;
using Microsoft.Azure.Databricks.Client.Models.MachineLearning.Experiment;
using System.Text.Json.Serialization;

namespace Bicep.Extension.Databricks;

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
    public int AutoTerminationMinutes { get; set; }

    [TypeProperty("Cluster log configuration.", ObjectTypePropertyFlags.None)]
    public StorageInfo? ClusterLogConf { get; set; }

    [TypeProperty("Data security mode for the cluster.", ObjectTypePropertyFlags.None)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DataSecurityMode? DataSecurityMode { get; set; }

    [TypeProperty("Single user name for single-user clusters.", ObjectTypePropertyFlags.None)]
    public string? SingleUserName { get; set; }

    [TypeProperty("Runtime engine for the cluster.", ObjectTypePropertyFlags.None)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RuntimeEngine? RuntimeEngine { get; set; }

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
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AzureAvailabilityType? Availability { get; set; }

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

public enum AzureAvailabilityType
{
    ON_DEMAND_AZURE,
    SPOT_AZURE,
    SPOT_WITH_FALLBACK_AZURE
}
public enum DataSecurityMode
{
    NONE,
    SINGLE_USER,
    USER_ISOLATION,
    LEGACY_TABLE_ACL,
    LEGACY_PASSTHROUGH,
    LEGACY_SINGLE_USER
}