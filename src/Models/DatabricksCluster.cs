using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Databricks.Models;

[ResourceType("Cluster")]
public class Cluster : ClusterIdentifiers
{
    // Required properties
    [TypeProperty("The Spark version of the cluster.", ObjectTypePropertyFlags.Required)]
    public required string SparkVersion { get; set; }

    [TypeProperty("The node type ID for worker nodes.", ObjectTypePropertyFlags.Required)]
    public required string NodeTypeId { get; set; }

    // Size configuration (either NumWorkers or AutoScale)
    [TypeProperty("The number of worker nodes.")]
    public int NumWorkers { get; set; }

    [TypeProperty("Auto-scaling configuration for the cluster.")]
    public AutoScale? AutoScale { get; set; }

    // Optional configuration
    [TypeProperty("The node type ID for the driver node.")]
    public string? DriverNodeTypeId { get; set; }

    [TypeProperty("Azure-specific attributes for the cluster.")]
    public AzureAttributes? AzureAttributes { get; set; }

    [TypeProperty("Auto-termination time in minutes.")]
    public int AutoterminationMinutes { get; set; }

    [TypeProperty("Spark configuration properties.")]
    public object? SparkConf { get; set; }

    [TypeProperty("Spark environment variables.")]
    public object? SparkEnvVars { get; set; }

    [TypeProperty("Custom tags for the cluster.")]
    public object? CustomTags { get; set; }

    [TypeProperty("SSH public keys for cluster access.")]
    public object? SshPublicKeys { get; set; }

    [TypeProperty("Initialization scripts for the cluster.")]
    public object? InitScripts { get; set; }

    [TypeProperty("Data security mode for the cluster.")]
    public string? DataSecurityMode { get; set; }

    [TypeProperty("Single user name for single-user clusters.")]
    public string? SingleUserName { get; set; }

    [TypeProperty("Runtime engine for the cluster.")]
    public string? RuntimeEngine { get; set; }

    // Read-only properties (outputs)
    [TypeProperty("The current state of the cluster.", ObjectTypePropertyFlags.ReadOnly)]
    public string? State { get; set; }

    [TypeProperty("The state message of the cluster.", ObjectTypePropertyFlags.ReadOnly)]
    public string? StateMessage { get; set; }

    [TypeProperty("The JDBC port of the cluster.", ObjectTypePropertyFlags.ReadOnly)]
    public int JdbcPort { get; set; }

    [TypeProperty("The number of cores in the cluster.", ObjectTypePropertyFlags.ReadOnly)]
    public int ClusterCores { get; set; }

    [TypeProperty("The memory in MB of the cluster.", ObjectTypePropertyFlags.ReadOnly)]
    public int ClusterMemoryMb { get; set; }

    [TypeProperty("The start time of the cluster.", ObjectTypePropertyFlags.ReadOnly)]
    public int StartTime { get; set; }

    [TypeProperty("The creator username of the cluster.", ObjectTypePropertyFlags.ReadOnly)]
    public string? CreatorUserName { get; set; }
}

public class ClusterIdentifiers
{
    [TypeProperty("The name of the cluster.", ObjectTypePropertyFlags.Required | ObjectTypePropertyFlags.Identifier)]
    public required string ClusterName { get; set; }
}

public class AutoScale
{
    [TypeProperty("Minimum number of worker nodes.", ObjectTypePropertyFlags.Required)]
    public required int MinWorkers { get; set; }

    [TypeProperty("Maximum number of worker nodes.", ObjectTypePropertyFlags.Required)]
    public required int MaxWorkers { get; set; }
}

public class AzureAttributes
{
    [TypeProperty("Number of on-demand instances to use before using spot instances.")]
    public int FirstOnDemand { get; set; }

    [TypeProperty("Availability type for Azure instances.")]
    public string? Availability { get; set; }

    [TypeProperty("Maximum price for spot instances.")]
    public int SpotBidMaxPrice { get; set; }
}
