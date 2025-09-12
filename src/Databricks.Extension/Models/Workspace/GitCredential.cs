using System.Text.Json.Serialization;
using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

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
