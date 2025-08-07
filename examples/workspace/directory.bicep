targetScope = 'local'

param workspaceUrl string


extension databricksExtension with { 
  workspaceUrl: workspaceUrl
}

resource directory 'Directory' = {
  path: '/Shared/fake@example.com/project'
}

output directoryId string = directory.path
