using System.Text.Json.Serialization;
using Azure.Bicep.Types.Concrete;

namespace Databricks.Models.Workspace;

public enum GitProvider
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
[BicepDocHeading("GitCredential", "Represents a Git credential in the Databricks workspace.")]
[BicepDocExample(
    "Creating a Git credential",
    "This example shows how to create a Git credential in the Databricks workspace.",
    @"resource gitCredential 'GitCredential' = {
  gitProvider: 'gitHub'
  gitUsername: 'myusername'
  personalAccessToken: 'ghp_xxxxxxxxxxxxxxxxxxxx'
  name: 'My GitHub Credential'
  isDefaultForProvider: true
}
"
)]
[BicepDocCustom("Notes", @"When working with the `GitCredential` resource, ensure you have the extension imported in your Bicep file:

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
```")]
[BicepDocCustom("Additional reference", @"For more information, see the following links:

- [Databricks Git credentials API documentation][00]

<!-- Link reference definitions -->
[00]: https://docs.databricks.com/api/azure/workspace/gitcredentials/create")]
[ResourceType("GitCredential")]
public class GitCredential : GitCredentialIdentifiers
{
    [TypeProperty("The name of the Git credential.")]
    public string? Name { get; set; }

    [TypeProperty("Whether this credential is the default for the provider.")]
    public bool IsDefaultForProvider { get; set; } = false;

    // Outputs
    [TypeProperty("The unique identifier of the Git credential.",ObjectTypePropertyFlags.ReadOnly)]
    public string? CredentialId { get; set; }
}

public class GitCredentialIdentifiers
{
    [TypeProperty("The Git provider.", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required GitProvider? GitProvider { get; set; }

    [TypeProperty("The username for Git authentication.", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string GitUsername { get; set; }

    [TypeProperty("The personal access token for Git authentication.", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string PersonalAccessToken { get; set; }
}
