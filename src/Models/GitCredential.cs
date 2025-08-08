using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Databricks;

public class GitCredentialIdentifiers
{
    [TypeProperty("The unique identifier of the Git credential.", ObjectTypePropertyFlags.Identifier)]
    public string? CredentialId { get; set; }
}

[ResourceType("GitCredential")]
public class GitCredential : GitCredentialIdentifiers
{
    [TypeProperty("The Git provider (gitHub, azureDevOpsServices, gitLab, bitbucketCloud, awsCodeCommit, etc.).", ObjectTypePropertyFlags.Required)]
    public required string GitProvider { get; set; }

    [TypeProperty("The username for Git authentication.", ObjectTypePropertyFlags.Required)]
    public required string GitUsername { get; set; }

    [TypeProperty("The personal access token for Git authentication.")]
    public string? PersonalAccessToken { get; set; }

    [TypeProperty("The name of the Git credential.")]
    public string? Name { get; set; }

    [TypeProperty("Whether this credential is the default for the provider.")]
    public bool IsDefaultForProvider { get; set; } = false;
}
