# External location resource

This resource allows you to create an [External Location][00] for storage.

```bicep
resource externalLocation 'ExternalLocation' = {
  name: 'externalLocation1'
  url: 'abfss://mystorageaccount.dfs.core.windows.net/mycontainer'
  credentialName: 'storageCredential1'
  comment: 'My first external location'
  readOnly: false
  fallbackMode: false
  skipValidation: false
}
```

> [!NOTE]
> The credential used for the External Location should exist.

[00]: https://docs.databricks.com/api/azure/workspace/externallocations/create
