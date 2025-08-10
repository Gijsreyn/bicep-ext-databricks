targetScope = 'subscription'

param rgName string
param location string
param storageName string
param containerRegistryName string
param workspaceName string

module rg 'br/public:avm/res/resources/resource-group:0.4.1' = {
  name: '${rgName}-${uniqueString(deployment().name, location)}'
  params: {
    name: rgName
    location: location
  }
}

module st 'br/public:avm/res/storage/storage-account:0.26.0' = {
  scope: resourceGroup(rgName)
  name: '${uniqueString(deployment().name, location)}-${storageName}'
  params: {
    name: storageName
    location: location
    kind: 'StorageV2'
    enableHierarchicalNamespace: true
    allowSharedKeyAccess: false
    allowBlobPublicAccess: true
    minimumTlsVersion: 'TLS1_2'
    accessTier: 'Hot'
    publicNetworkAccess: 'Disabled'
    blobServices:{
      containers: [
        {
          name: 'metastore'
        }
      ]
    }
    managedIdentities: {
      systemAssigned: true
    }
  }
}

module registry 'br/public:avm/res/container-registry/registry:0.9.1' = {
  name: '${rgName}-${uniqueString(deployment().name, location)}-registry'
  scope: resourceGroup(rgName)
  params: {
    name: containerRegistryName
    location: location
    acrAdminUserEnabled: false 
    acrSku: 'Standard'
    anonymousPullEnabled: true 
    publicNetworkAccess: 'Enabled'
  }
}

module dbt 'br/public:avm/res/databricks/workspace:0.11.2' = {
  scope: resourceGroup(rgName)
  params: {
    name: workspaceName
    location: location
    skuName: 'premium'
  }
}

output dbtWorkspaceUrl string = dbt.outputs.workspaceUrl
output containerLoginServer string = registry.outputs.loginServer
