targetScope = 'local'

param workspaceUrl string

extension databricksExtension with { 
  workspaceUrl: workspaceUrl
}

resource secretScope 'SecretScope' = {
  scopeName: 'example-secretscope'
}

output secretBackendType string = secretScope.backendType
