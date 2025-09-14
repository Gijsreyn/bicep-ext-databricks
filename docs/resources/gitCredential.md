---
category: "Workspace"
---

# GitCredential

Represents a Git credential in the Databricks workspace.

## Example usage

### Creating a Git credential

This example shows how to create a Git credential in the Databricks workspace.

```bicep
resource gitCredential 'GitCredential' = {
  gitProvider: 'gitHub'
  gitUsername: 'myusername'
  personalAccessToken: 'ghp_xxxxxxxxxxxxxxxxxxxx'
  name: 'My GitHub Credential'
  isDefaultForProvider: true
}
```

## Argument reference

The following arguments are available:

- `gitProvider` - (Required) The Git provider. (Can be `gitHub`, `bitbucketCloud`, `gitLab`, `azureDevOpsServices`, `gitHubEnterprise`, `bitbucketServer`, `gitLabEnterpriseEdition`, or `awsCodeCommit`)
- `gitUsername` - (Required) The username for Git authentication.
- `personalAccessToken` - (Required) The personal access token for Git authentication.
- `isDefaultForProvider` - (Optional) Whether this credential is the default for the provider.
- `name` - (Optional) The name of the Git credential.

## Attribute reference

In addition to all arguments above, the following attributes are outputted:

- `credentialId` - The unique identifier of the Git credential.

## Notes

When working with the `GitCredential` resource, ensure you have the extension imported in your Bicep file:

```bicep
// main.bicep
targetScope = 'local'
param workspaceUrl string
extension databricksExtension with {
  workspaceUrl: workspaceUrl

// Add resource 

// main.bicepparam
using 'main.bicep'
param workspaceUrl = '<workspaceUrl>'
```

## Additional reference

For more information, see the following links:

- [Databricks Git credentials API documentation][00]

<!-- Link reference definitions -->
[00]: https://docs.databricks.com/api/azure/workspace/gitcredentials/create

