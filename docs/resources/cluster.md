---
category: "Compute"
---

# Cluster

Represents a Databricks compute cluster for running workloads.

## Example usage

### Creating a basic cluster with fixed size

This example shows how to create a basic cluster with a fixed number of worker nodes.

```bicep
resource cluster 'Cluster' = {
  clusterName: 'analytics-cluster'
  sparkVersion: '13.3.x-scala2.12'
  nodeTypeId: 'Standard_DS3_v2'
  numWorkers: 2
  autoterminationMinutes: 120
  dataSecurityMode: 'SINGLE_USER'
  singleUserName: 'analyst@company.com'
}
```

### Creating an auto-scaling cluster

This example shows how to create a cluster with auto-scaling configuration.

```bicep
resource autoScalingCluster 'Cluster' = {
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
```

### Creating a cluster with Azure spot instances

This example shows how to create a cost-optimized cluster using Azure spot instances.

```bicep
resource spotCluster 'Cluster' = {
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
```

### Creating a high-concurrency cluster

This example shows how to create a high-concurrency cluster for shared analytics workloads.

```bicep
resource sharedCluster 'Cluster' = {
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
```

## Argument reference

The following arguments are available:

- `clusterName` - (Required) The name of the cluster.
- `nodeTypeId` - (Required) The node type ID for worker nodes.
- `sparkVersion` - (Required) The Spark version of the cluster.
- `autoScale` - (Optional) Auto-scaling configuration for the cluster:
  - `maxWorkers` - (Required) Maximum number of worker nodes.
  - `minWorkers` - (Required) Minimum number of worker nodes.
- `autoterminationMinutes` - (Optional) Auto-termination time in minutes.
- `azureAttributes` - (Optional) Azure-specific attributes for the cluster:
  - `availability` - (Optional) Availability type for Azure instances.
  - `firstOnDemand` - (Optional) Number of on-demand instances to use before using spot instances.
  - `spotBidMaxPrice` - (Optional) Maximum price for spot instances.
- `customTags` - (Optional) Custom tags for the cluster.
- `dataSecurityMode` - (Optional) Data security mode for the cluster.
- `driverNodeTypeId` - (Optional) The node type ID for the driver node.
- `initScripts` - (Optional) Initialization scripts for the cluster.
- `numWorkers` - (Optional) The number of worker nodes.
- `runtimeEngine` - (Optional) Runtime engine for the cluster.
- `singleUserName` - (Optional) Single user name for single-user clusters.
- `sparkConf` - (Optional) Spark configuration properties.
- `sparkEnvVars` - (Optional) Spark environment variables.
- `sshPublicKeys` - (Optional) SSH public keys for cluster access.

## Attribute reference

In addition to all arguments above, the following attributes are outputted:

- `clusterCores` - The number of cores in the cluster.
- `clusterMemoryMb` - The memory in MB of the cluster.
- `creatorUserName` - The creator username of the cluster.
- `jdbcPort` - The JDBC port of the cluster.
- `startTime` - The start time of the cluster.
- `state` - The current state of the cluster.
- `stateMessage` - The state message of the cluster.

## Notes

When working with the 'Cluster' resource, ensure you have the extension imported in your Bicep file:

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
- Consider `runtimeEngine` options: `STANDARD` or `PHOTON` for better performance

## Additional reference

For more information, see the following links:

- [Databricks Clusters API documentation][00]
- [Cluster configuration best practices][01]
- [Azure Databricks node types][02]
- [Spark configuration reference][03]

<!-- Link reference definitions -->
[00]: https://docs.databricks.com/api/azure/workspace/clusters/create
[01]: https://docs.databricks.com/compute/cluster-config-best-practices.html
[02]: https://docs.databricks.com/compute/configure.html#node-types
[03]: https://spark.apache.org/docs/latest/configuration.html

