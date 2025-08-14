// requires Git credentials to be configured in Settings -> Linked Accounts
// Or use the 'gitCredential' resource

targetScope = 'local'

param workspaceUrl string


extension databricksExtension with { 
  workspaceUrl: workspaceUrl
}

resource gitRepo 'Repo' = {
  provider: 'gitHub'
  url: 'https://github.com/Gijsreyn/bicep-ext-databricks'
  branch: 'main'
}

output repoId string = gitRepo.repoId
