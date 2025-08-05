targetScope = 'local'

param workspaceUrl string


extension databricksExtension with { 
  workspaceUrl: workspaceUrl
}

resource directory 'Directory' = {
  path: '/Users/fake@example.com/project'
}
