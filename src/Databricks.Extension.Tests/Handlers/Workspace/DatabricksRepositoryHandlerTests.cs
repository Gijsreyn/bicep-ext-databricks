using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Databricks.Handlers.Workspace;
using Databricks.Models.Workspace;

namespace Databricks.Extension.Tests.Handlers.Workspace;

public class DatabricksRepositoryHandlerTests : HandlerTestBase<DatabricksRepositoryHandler, Repository, RepositoryIdentifiers>
{
    [Fact]
    public void Constructor_ShouldCreateInstanceWithLogger()
    {
        // Arrange & Act
        var handler = new DatabricksRepositoryHandler(MockLogger.Object);

        // Assert
        handler.Should().NotBeNull();
    }

    [Fact]
    public void AzureDatabricksRepository_ShouldBeSetCorrectly()
    {
        // Arrange - Azure DevOps repository for Azure Databricks
        var repository = new Repository
        {
            Provider = RepoProvider.azureDevOpsServices,
            Url = "https://dev.azure.com/myorg/myproject/_git/databricks-notebooks",
            Path = "/Repos/azure-databricks/notebooks",
            Branch = "main"
        };

        // Act & Assert
        repository.Provider.Should().Be(RepoProvider.azureDevOpsServices);
        repository.Url.Should().Be("https://dev.azure.com/myorg/myproject/_git/databricks-notebooks");
        repository.Path.Should().Be("/Repos/azure-databricks/notebooks");
        repository.Branch.Should().Be("main");
    }

    [Theory]
    [InlineData(RepoProvider.gitHub)]
    [InlineData(RepoProvider.azureDevOpsServices)]
    [InlineData(RepoProvider.gitLab)]
    [InlineData(RepoProvider.bitbucketCloud)]
    [InlineData(RepoProvider.awsCodeCommit)]
    public void RepoProvider_ShouldAcceptAllValues(RepoProvider provider)
    {
        // Arrange
        var repository = new Repository
        {
            Provider = provider,
            Url = "https://example.com/repo.git"
        };

        // Act & Assert
        repository.Provider.Should().Be(provider);
    }

    [Fact]
    public void GitHubRepository_ShouldBeSetCorrectly()
    {
        // Arrange
        var repository = new Repository
        {
            Provider = RepoProvider.gitHub,
            Url = "https://github.com/myorg/databricks-notebooks.git",
            Path = "/Repos/github/notebooks",
            Branch = "develop"
        };

        // Act & Assert
        repository.Provider.Should().Be(RepoProvider.gitHub);
        repository.Url.Should().Be("https://github.com/myorg/databricks-notebooks.git");
        repository.Path.Should().Be("/Repos/github/notebooks");
        repository.Branch.Should().Be("develop");
    }

    [Fact]
    public void SparseCheckout_ShouldBeSetCorrectly()
    {
        // Arrange
        var sparseCheckout = new SparseCheckout
        {
            Patterns = new[] { "notebooks/*", "src/*", "*.md" }
        };

        var repository = new Repository
        {
            Provider = RepoProvider.gitHub,
            Url = "https://github.com/myorg/large-repo.git",
            Path = "/Repos/sparse/repo",
            SparseCheckout = sparseCheckout
        };

        // Act & Assert
        repository.SparseCheckout.Should().NotBeNull();
        repository.SparseCheckout!.Patterns.Should().NotBeNull();
        repository.SparseCheckout.Patterns!.Should().HaveCount(3);
        repository.SparseCheckout.Patterns.Should().Contain("notebooks/*");
        repository.SparseCheckout.Patterns.Should().Contain("src/*");
        repository.SparseCheckout.Patterns.Should().Contain("*.md");
    }

    [Theory]
    [InlineData("https://github.com/user/repo.git", "/Repos/user/repo")]
    [InlineData("https://dev.azure.com/org/project/_git/repo", "/Repos/azure/project")]
    [InlineData("https://gitlab.com/group/project.git", "/Repos/gitlab/project")]
    public void RepositoryIdentifiers_ShouldBeSetCorrectly(string url, string path)
    {
        // Arrange
        var identifiers = new RepositoryIdentifiers
        {
            Provider = RepoProvider.gitHub,
            Url = url,
            Path = path
        };

        // Act & Assert
        identifiers.Provider.Should().Be(RepoProvider.gitHub);
        identifiers.Url.Should().Be(url);
        identifiers.Path.Should().Be(path);
    }

    [Fact]
    public void ReadOnlyProperties_ShouldBeSettable()
    {
        // Arrange
        var repository = new Repository
        {
            Provider = RepoProvider.azureDevOpsServices,
            Url = "https://dev.azure.com/myorg/myproject/_git/repo",
            Path = "/Repos/azure/repo",
            Branch = "main",
            // Setting read-only properties (would typically come from API responses)
            Id = "repo-12345",
            HeadCommitId = "abc123def456"
        };

        // Act & Assert
        repository.Id.Should().Be("repo-12345");
        repository.HeadCommitId.Should().Be("abc123def456");
    }

    [Fact]
    public void AzureDevOpsRepository_ShouldValidateUrl()
    {
        // Arrange
        var repository = new Repository
        {
            Provider = RepoProvider.azureDevOpsServices,
            Url = "https://dev.azure.com/myorganization/myproject/_git/databricks-workspace",
            Path = "/Repos/azure-devops/workspace"
        };

        // Act & Assert
        repository.Url.Should().StartWith("https://dev.azure.com/");
        repository.Url.Should().Contain("/_git/");
        repository.Provider.Should().Be(RepoProvider.azureDevOpsServices);
    }

    [Fact]
    public void RepositoryWithoutOptionalProperties_ShouldBeValid()
    {
        // Arrange
        var repository = new Repository
        {
            Provider = RepoProvider.gitHub,
            Url = "https://github.com/user/simple-repo.git"
            // Path and Branch are optional
        };

        // Act & Assert
        repository.Provider.Should().Be(RepoProvider.gitHub);
        repository.Url.Should().Be("https://github.com/user/simple-repo.git");
        repository.Path.Should().BeNull();
        repository.Branch.Should().BeNull();
        repository.SparseCheckout.Should().BeNull();
    }

    [Theory]
    [InlineData("main")]
    [InlineData("develop")]
    [InlineData("feature/azure-integration")]
    [InlineData("release/v1.0.0")]
    public void BranchName_ShouldAcceptValidNames(string branchName)
    {
        // Arrange
        var repository = new Repository
        {
            Provider = RepoProvider.gitHub,
            Url = "https://github.com/user/repo.git",
            Branch = branchName
        };

        // Act & Assert
        repository.Branch.Should().Be(branchName);
    }
}
