using System.Text.Json.Serialization;
using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Databricks.Models.Workspace;

public enum RepoProvider
{
    gitHub,
    bitbucketCloud,
    gitLab,
    azureDevOpsServices,
    gitHubEnterprise,
    bitbucketServer,
    gitLabEnterpriseEdition,
    awsCodeCommit
}


[BicepFrontMatter("category", "Workspace")]
[BicepDocHeading("Repository", "Represents a Git repository in the Databricks workspace.")]
[BicepDocExample(
    "Creating a basic repository",
    "This example shows how to create a basic Git repository in the Databricks workspace.",
    @"resource repository 'Repository' = {
  provider: 'gitHub'
  url: 'https://github.com/myorg/myrepo.git'
  path: '/Repos/myuser/myrepo'
  branch: 'main'
}
"
)]
[BicepDocExample(
    "Repository with sparse checkout",
    "This example shows how to create a repository with sparse checkout configuration.",
    @"resource repository 'Repository' = {
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
"
)]
[BicepDocExample(
    "Azure DevOps repository",
    "This example shows how to create a repository from Azure DevOps Services.",
    @"resource repository 'Repository' = {
  provider: 'azureDevOpsServices'
  url: 'https://dev.azure.com/myorg/myproject/_git/myrepo'
  path: '/Repos/myuser/azure-repo'
  branch: 'feature/new-feature'
}
"
)]
[BicepDocCustom("Notes", @"When working with the `Repository` resource, ensure you have the extension imported in your Bicep file:

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
You can use the `GitCredential` resource to set up authentication for your Git provider.")]
[BicepDocCustom("Additional reference", @"For more information, see the following links:

- [Databricks Repos API documentation][00]
- [Git integration with Databricks][01]

<!-- Link reference definitions -->
[00]: https://docs.databricks.com/api/azure/workspace/repos/create
[01]: https://docs.databricks.com/repos/index.html")]
[ResourceType("Repository")]
public class Repository : RepositoryIdentifiers
{
    [TypeProperty("The branch to checkout.")]
    public string? Branch { get; set; }

    [TypeProperty("Sparse checkout configuration.")]
    public SparseCheckout? SparseCheckout { get; set; }

    // Outputs - ReadOnly properties
    [TypeProperty("The unique identifier of the repository.", ObjectTypePropertyFlags.ReadOnly)]
    public string? Id { get; set; }

    [TypeProperty("The head commit ID of the checked out branch.", ObjectTypePropertyFlags.ReadOnly)]
    public string? HeadCommitId { get; set; }
}

public class SparseCheckout
{
    [TypeProperty("List of patterns for sparse checkout.")]
    public string[]? Patterns { get; set; }
}

public class RepositoryIdentifiers
{
    [TypeProperty("The Git provider for the repository.", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required RepoProvider? Provider { get; set; }

    [TypeProperty("The URL of the Git repository.", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Url { get; set; }

    [TypeProperty("The path where the repository will be cloned in the Databricks workspace.", ObjectTypePropertyFlags.Identifier)]
    public string? Path { get; set; }
}
