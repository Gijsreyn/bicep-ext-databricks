using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Databricks;

public class RepoIdentifiers
{
    [TypeProperty("The unique identifier of the repository.", ObjectTypePropertyFlags.Identifier)]
    public string? RepoId { get; set; }
}

[ResourceType("Repo")]
public class Repo : RepoIdentifiers
{
    [TypeProperty("The Git repository URL.", ObjectTypePropertyFlags.Required)]
    public required string Url { get; set; }

    [TypeProperty("The Git provider (gitHub, azureDevOpsServices, gitLab, bitbucketCloud, awsCodeCommit).", ObjectTypePropertyFlags.Required)]
    public required string Provider { get; set; }

    [TypeProperty("The path in the Databricks workspace where the repository will be cloned.", ObjectTypePropertyFlags.None)]
    public string? Path { get; set; }

    [TypeProperty("The branch to check out.", ObjectTypePropertyFlags.None)]
    public string? Branch { get; set; }

    [TypeProperty("The tag to check out.", ObjectTypePropertyFlags.None)]
    public string? Tag { get; set; }

    [TypeProperty("Patterns for sparse checkout.", ObjectTypePropertyFlags.None)]
    public string[]? SparseCheckoutPatterns { get; set; }

    [TypeProperty("The commit hash of the current HEAD.", ObjectTypePropertyFlags.ReadOnly)]
    public string HeadCommitId { get; set; } = string.Empty;

    [TypeProperty("The workspace path of the repository.", ObjectTypePropertyFlags.ReadOnly)]
    public string WorkspacePath { get; set; } = string.Empty;
}
