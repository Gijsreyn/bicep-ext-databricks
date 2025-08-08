targetScope = 'local'

param workspaceUrl string

extension databricksExtension with { 
  workspaceUrl: workspaceUrl
}

resource cluster 'Cluster' = {
  clusterName: 'cluster2'
  sparkVersion: '7.3.x-scala2.12'
  autoscale: {
    minWorkers: 2
    maxWorkers: 8
  }
  nodeTypeId: 'Standard_DS3_v2'

}

output clusterName string = cluster.clusterName
