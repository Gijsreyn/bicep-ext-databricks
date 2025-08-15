targetScope = 'local'

param workspaceUrl string

extension databricksExtension with { 
  workspaceUrl: workspaceUrl
}

resource storageCredential 'StorageCredential' = {
  name: 'storageCredential1'
  comment: 'My first storage credential'
  azureManagedIdentity: {}
}
