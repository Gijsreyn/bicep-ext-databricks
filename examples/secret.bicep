targetScope = 'local'

param workspaceUrl string

extension databricksExtension with { 
  workspaceUrl: workspaceUrl
}

resource secret 'Secret' = {
  key: 'example-secret-key'
  scope: 'example-secretscope'
  stringValue: 'example-secret-value'
}

output secretKey string = secret.key
