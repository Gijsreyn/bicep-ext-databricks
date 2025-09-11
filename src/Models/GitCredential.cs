using System.Text.Json.Serialization;
using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Databricks;

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
    [TypeProperty("The unique identifier of the Git credential.", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("credential_id")]
    public string? CredentialId { get; set; }
}

[ResourceType("GitCredential")]
public class GitCredential : GitCredentialIdentifiers
{
    [TypeProperty("The Git provider.", ObjectTypePropertyFlags.Required)]
    [JsonPropertyName("git_provider")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required GitProvider? GitProvider { get; set; }

    [TypeProperty("The username for Git authentication.", ObjectTypePropertyFlags.Required)]
    [JsonPropertyName("git_username")]
    public required string GitUsername { get; set; }

    [TypeProperty("The personal access token for Git authentication.", ObjectTypePropertyFlags.Required)]
    [JsonPropertyName("personal_access_token")]
    public required string PersonalAccessToken { get; set; }

    [TypeProperty("The name of the Git credential.")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [TypeProperty("Whether this credential is the default for the provider.")]
    [JsonPropertyName("is_default_for_provider")]
    public bool IsDefaultForProvider { get; set; } = false;
}
