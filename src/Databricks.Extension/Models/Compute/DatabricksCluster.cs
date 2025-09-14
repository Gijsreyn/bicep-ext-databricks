using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Databricks.Models.Compute;


[BicepFrontMatter("category", "Compute")]
[BicepDocHeading("Cluster", "Represents a Databricks compute cluster for running workloads.")]
[BicepDocExample(
    "Creating a basic cluster with fixed size",
    "This example shows how to create a basic cluster with a fixed number of worker nodes.",
    @"resource cluster 'Cluster' = {
  clusterName: 'analytics-cluster'
  sparkVersion: '13.3.x-scala2.12'
  nodeTypeId: 'Standard_DS3_v2'
  numWorkers: 2
  autoterminationMinutes: 120
  dataSecurityMode: 'SINGLE_USER'
  singleUserName: 'analyst@company.com'
}
"
)]
[BicepDocExample(
    "Creating an auto-scaling cluster",
    "This example shows how to create a cluster with auto-scaling configuration.",
    @"resource autoScalingCluster 'Cluster' = {
  clusterName: 'ml-training-cluster'
  sparkVersion: '13.3.x-gpu-ml-scala2.12'
  nodeTypeId: 'Standard_NC6s_v3'
  driverNodeTypeId: 'Standard_DS4_v2'
  autoScale: {
    minWorkers: 1
    maxWorkers: 8
  }
  autoterminationMinutes: 60
  dataSecurityMode: 'USER_ISOLATION'
  runtimeEngine: 'PHOTON'
  sparkConf: {
    'spark.sql.adaptive.enabled': 'true'
    'spark.sql.adaptive.coalescePartitions.enabled': 'true'
  }
  customTags: {
    team: 'ml-engineering'
    environment: 'production'
    cost_center: 'CC123'
  }
}
"
)]
[BicepDocExample(
    "Creating a cluster with Azure spot instances",
    "This example shows how to create a cost-optimized cluster using Azure spot instances.",
    @"resource spotCluster 'Cluster' = {
  clusterName: 'batch-processing-cluster'
  sparkVersion: '13.3.x-scala2.12'
  nodeTypeId: 'Standard_E4s_v3'
  autoScale: {
    minWorkers: 2
    maxWorkers: 10
  }
  autoterminationMinutes: 30
  azureAttributes: {
    firstOnDemand: 1
    availability: 'SPOT_WITH_FALLBACK_AZURE'
    spotBidMaxPrice: 100
  }
  sparkConf: {
    'spark.databricks.cluster.profile': 'serverless'
    'spark.databricks.delta.preview.enabled': 'true'
  }
  customTags: {
    workload: 'batch-processing'
    schedule: 'nightly'
  }
}
"
)]
[BicepDocExample(
    "Creating a high-concurrency cluster",
    "This example shows how to create a high-concurrency cluster for shared analytics workloads.",
    @"resource sharedCluster 'Cluster' = {
  clusterName: 'shared-analytics-cluster'
  sparkVersion: '13.3.x-scala2.12'
  nodeTypeId: 'Standard_DS4_v2'
  driverNodeTypeId: 'Standard_DS5_v2'
  autoScale: {
    minWorkers: 2
    maxWorkers: 20
  }
  autoterminationMinutes: 180
  dataSecurityMode: 'USER_ISOLATION'
  runtimeEngine: 'STANDARD'
  sparkConf: {
    'spark.databricks.cluster.profile': 'serverless'
    'spark.databricks.sql.initial.catalog.name': 'main'
    'spark.sql.execution.arrow.pyspark.enabled': 'true'
  }
  sparkEnvVars: {
    PYSPARK_PYTHON: '/databricks/python3/bin/python3'
  }
  customTags: {
    purpose: 'shared-analytics'
    department: 'data-science'
    auto_shutdown: 'enabled'
  }
}
"
)]
[BicepDocCustom("Notes", @"When working with the 'Cluster' resource, ensure you have the extension imported in your Bicep file:

```bicep
// main.bicep
targetScope = 'local'
param workspaceUrl string
extension databricksExtension with {
  workspaceUrl: workspaceUrl
}

// main.bicepparam
using 'main.bicep'
param workspaceUrl = '<workspaceUrl>'
```

Please note the following important considerations when using the `Cluster` resource:

- Either `numWorkers` or `autoScale` must be specified, but not both
- Choose appropriate node types based on your workload requirements (CPU, memory, GPU)
- Use `dataSecurityMode` to control access patterns: `SINGLE_USER`, `USER_ISOLATION`, or `LEGACY_SINGLE_USER_STANDARD`
- Set `autoterminationMinutes` to control costs by automatically terminating idle clusters
- Use Azure spot instances with `azureAttributes` for cost optimization on fault-tolerant workloads
- Configure `sparkConf` for performance tuning and feature enablement
- Use `customTags` for cost tracking, governance, and resource organization
- Consider `runtimeEngine` options: `STANDARD` or `PHOTON` for better performance")]
[BicepDocCustom("Additional reference", @"For more information, see the following links:

- [Databricks Clusters API documentation][00]
- [Cluster configuration best practices][01]
- [Azure Databricks node types][02]
- [Spark configuration reference][03]

<!-- Link reference definitions -->
[00]: https://docs.databricks.com/api/azure/workspace/clusters/create
[01]: https://docs.databricks.com/compute/cluster-config-best-practices.html
[02]: https://docs.databricks.com/compute/configure.html#node-types
[03]: https://spark.apache.org/docs/latest/configuration.html")]
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
