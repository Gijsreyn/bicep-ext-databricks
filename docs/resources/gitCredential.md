# Git credential resource

This resource allows you to [create a Git credential entry][00] in a workspace.

```bicep
resource gitCredential 'GitCredential' = {
  gitProvider: 'gitHub' // See other options in the REST API documentation
  gitUsername: '<userName>'
  personalAccessToken: '<personalAccessToken>'
  name: 'My Git Credential'
  isDefaultForProvider: true // or false
}
```

[00]: https://docs.databricks.com/api/azure/workspace/gitcredentials/create
