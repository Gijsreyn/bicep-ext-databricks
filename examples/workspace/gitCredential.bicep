targetScope = 'local'

param workspaceUrl string


extension databricksExtension with { 
  workspaceUrl: workspaceUrl
}

resource gitCredential 'GitCredential' = {
  gitProvider: 'gitHub'
  gitUsername: '<userName>'
  personalAccessToken: '<personalAccessToken>'
  name: 'My Git Credential'
  isDefaultForProvider: true // or false
}

output gitCredentialId string = gitCredential.credentialId
