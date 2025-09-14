---
category: "Workspace"
---

# Repository

Represents a Git repository in the Databricks workspace.

## Example usage

### Creating a basic repository

This example shows how to create a basic Git repository in the Databricks workspace.

```bicep
resource repository 'Repository' = {
  provider: 'gitHub'
  url: 'https://github.com/myorg/myrepo.git'
  path: '/Repos/myuser/myrepo'
  branch: 'main'
}
```

### Repository with sparse checkout

This example shows how to create a repository with sparse checkout configuration.

```bicep
resource repository 'Repository' = {
  provider: 'gitHub'
  url: 'https://github.com/myorg/myrepo.git'
  path: '/Repos/myuser/myrepo'
  branch: 'develop'
  sparseCheckout: {
    patterns: [
      'src/*'
      'config/*'
      '*.md'
    ]
  }
}
```

### Azure DevOps repository

This example shows how to create a repository from Azure DevOps Services.

```bicep
resource repository 'Repository' = {
  provider: 'azureDevOpsServices'
  url: 'https://dev.azure.com/myorg/myproject/_git/myrepo'
  path: '/Repos/myuser/azure-repo'
  branch: 'feature/new-feature'
}
```

## Argument reference

The following arguments are available:

- `provider` - (Required) The Git provider for the repository. (Can be `gitHub`, `bitbucketCloud`, `gitLab`, `azureDevOpsServices`, `gitHubEnterprise`, `bitbucketServer`, `gitLabEnterpriseEdition`, or `awsCodeCommit`)
- `url` - (Required) The URL of the Git repository.
- `branch` - (Optional) The branch to checkout.
- `path` - (Optional) The path where the repository will be cloned in the Databricks workspace.
- `sparseCheckout` - (Optional) Sparse checkout configuration:
  - `patterns` - (Optional) List of patterns for sparse checkout.

## Attribute reference

In addition to all arguments above, the following attributes are outputted:

- `headCommitId` - The head commit ID of the checked out branch.
- `id` - The unique identifier of the repository.

## Notes

When working with the 'Repository' resource, ensure you have the extension imported in your Bicep file:

```bicep
// main.bicep
targetScope = 'local'
param workspaceUrl string
extension databricksExtension with {
  workspaceUrl: workspaceUrl
}

// main.bicepparam
using 'main.bicep'
param workspaceUrl = '<workspaceUrl>'
```

Make sure you have configured the appropriate Git credentials before creating a repository. 
You can use the `GitCredential` resource to set up authentication for your Git provider.

## Additional reference

For more information, see the following links:

- [Databricks Repos API documentation][00]
- [Git integration with Databricks][01]

<!-- Link reference definitions -->
[00]: https://docs.databricks.com/api/azure/workspace/repos/create
[01]: https://docs.databricks.com/repos/index.html

