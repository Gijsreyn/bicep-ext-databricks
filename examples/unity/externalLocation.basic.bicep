targetScope = 'local'

param workspaceUrl string

extension databricksExtension with { 
  workspaceUrl: workspaceUrl
}

resource externalLocation 'ExternalLocation' = {
  name: 'externalLocation1'
  url: 'abfss://mystorageaccount.dfs.core.windows.net/mycontainer'
  credentialName: 'storageCredential1'
  comment: 'My first external location'
  readOnly: false
  fallbackMode: false
  skipValidation: false
}

output externalLocationId string = externalLocation.credentialId
