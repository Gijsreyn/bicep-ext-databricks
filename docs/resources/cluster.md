# Cluster resource

This resource allows you to manage [Databricks clusters][00].

```bicep
param clusterName string = 'Shared cluster'
param sparkVersion string = '7.3.x-scala2.12'
param nodeTypeId string = 'Standard_DS3_v2'

resource cluster 'Cluster' = {
  clusterName: clusterName
  sparkVersion: sparkVersion
  autoscale: {
    minWorkers: 2
    maxWorkers: 8
  }
  nodeTypeId: nodeTypeId
}
```

[00]: https://docs.databricks.com/api/azure/workspace/clusters/create
