targetScope = 'local'

param workspaceUrl string

extension databricksExtension with { 
  workspaceUrl: workspaceUrl
}

resource catalog 'Catalog' = {
  name: 'catalog1'
  comment: 'My first catalog'
}
