using System.Text.Json.Serialization;
using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Databricks.Models;

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
