// requires Git credentials to be configured in Settings -> Linked Accounts
// Or use the 'gitCredential' resource

targetScope = 'local'

param workspaceUrl string


extension databricksExtension with { 
  workspaceUrl: workspaceUrl
}

resource gitRepo 'Repo' = {
  provider: 'GitHub'
  url: 'https://github.com/Azure/bicep'
  branch: 'main'
}

output repoId string = gitRepo.repoId
