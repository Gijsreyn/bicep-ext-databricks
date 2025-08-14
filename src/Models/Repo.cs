using Bicep.Local.Extension.Types.Attributes;
using Azure.Bicep.Types.Concrete;
using Microsoft.Azure.Databricks.Client.Models;
using System.Text.Json.Serialization;

namespace Bicep.Extension.Databricks;

[ResourceType("Repo")]
public class Repo : RepoIdentifiers
{
    [TypeProperty("Git provider (gitHub, bitbucketCloud, gitLab, azureDevOpsServices, gitHubEnterprise, bitbucketServer, gitLabEnterpriseEdition, awsCodeCommit).", ObjectTypePropertyFlags.Required)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required RepoProvider? Provider { get; set; }

    [TypeProperty("Repository URL.", ObjectTypePropertyFlags.Required)]
    public required string Url { get; set; }

    [TypeProperty("Target path in workspace.")]
    public string? Path { get; set; }

    [TypeProperty("Branch to check out.")]
    public string? Branch { get; set; }

    [TypeProperty("Tag to check out (if branch not specified).")]
    public string? Tag { get; set; }

    [TypeProperty("Sparse checkout patterns.")]
    public string[]? SparseCheckoutPatterns { get; set; }

    [TypeProperty("Head commit id.", ObjectTypePropertyFlags.ReadOnly)]
    public string? HeadCommitId { get; set; }
}

public class RepoIdentifiers
{
    [TypeProperty("Repo id.", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.ReadOnly)]
    public string? RepoId { get; set; }
}
