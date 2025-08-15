# Storage credential resource

This resource allows you to create a [Storage Credential][00] for Unity Catalog.

```bicep
resource storageCredential 'StorageCredential' = {
  name: 'storageCredential1'
  comment: 'My first storage credential'
  azureManagedIdentity: {
    accessConnectorId: '/subscriptions/<subscriptionId>/resourceGroups/<resourceGroupName>/providers/Microsoft.Databricks/accessConnectors/my-access-connector'
  }
}
```

[00]: https://docs.databricks.com/api/azure/workspace/storagecredentials/create
