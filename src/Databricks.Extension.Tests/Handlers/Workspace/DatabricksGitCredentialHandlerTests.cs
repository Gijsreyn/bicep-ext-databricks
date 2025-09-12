using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Databricks.Handlers.Workspace;
using Databricks.Models.Workspace;

namespace Databricks.Extension.Tests.Handlers.Workspace;

public class DatabricksGitCredentialHandlerTests : HandlerTestBase<DatabricksGitCredentialHandler, GitCredential, GitCredentialIdentifiers>
{
    [Fact]
    public void Constructor_ShouldCreateInstanceWithLogger()
    {
        // Arrange & Act
        var handler = new DatabricksGitCredentialHandler(MockLogger.Object);

        // Assert
        handler.Should().NotBeNull();
    }

    [Fact]
    public void AzureDatabricksGitCredential_ShouldBeSetCorrectly()
    {
        // Arrange - Azure DevOps Git credential for Azure Databricks
        var gitCredential = new GitCredential
        {
            GitProvider = GitProvider.azureDevOpsServices,
            GitUsername = "azure-user@company.com",
            PersonalAccessToken = "pat-token-12345",
            Name = "Azure DevOps PAT",
            IsDefaultForProvider = true
        };

        // Act & Assert
        gitCredential.GitProvider.Should().Be(GitProvider.azureDevOpsServices);
        gitCredential.GitUsername.Should().Be("azure-user@company.com");
        gitCredential.PersonalAccessToken.Should().Be("pat-token-12345");
        gitCredential.Name.Should().Be("Azure DevOps PAT");
        gitCredential.IsDefaultForProvider.Should().BeTrue();
    }

    [Theory]
    [InlineData(GitProvider.gitHub)]
    [InlineData(GitProvider.azureDevOpsServices)]
    [InlineData(GitProvider.gitLab)]
    [InlineData(GitProvider.bitbucketCloud)]
    [InlineData(GitProvider.awsCodeCommit)]
    [InlineData(GitProvider.gitHubEnterprise)]
    public void GitProvider_ShouldAcceptAllValues(GitProvider provider)
    {
        // Arrange
        var gitCredential = new GitCredential
        {
            GitProvider = provider,
            GitUsername = "testuser",
            PersonalAccessToken = "test-token"
        };

        // Act & Assert
        gitCredential.GitProvider.Should().Be(provider);
    }

    [Fact]
    public void GitHubCredential_ShouldBeSetCorrectly()
    {
        // Arrange
        var gitCredential = new GitCredential
        {
            GitProvider = GitProvider.gitHub,
            GitUsername = "github-user",
            PersonalAccessToken = "ghp_1234567890abcdef",
            Name = "GitHub Personal Access Token",
            IsDefaultForProvider = false
        };

        // Act & Assert
        gitCredential.GitProvider.Should().Be(GitProvider.gitHub);
        gitCredential.GitUsername.Should().Be("github-user");
        gitCredential.PersonalAccessToken.Should().Be("ghp_1234567890abcdef");
        gitCredential.Name.Should().Be("GitHub Personal Access Token");
        gitCredential.IsDefaultForProvider.Should().BeFalse();
    }

    [Theory]
    [InlineData("user@company.com", "azure-pat-token")]
    [InlineData("github-user", "ghp_token123")]
    [InlineData("gitlab-user", "glpat-token456")]
    public void GitCredentialIdentifiers_ShouldBeSetCorrectly(string username, string token)
    {
        // Arrange
        var identifiers = new GitCredentialIdentifiers
        {
            GitProvider = GitProvider.gitHub,
            GitUsername = username,
            PersonalAccessToken = token
        };

        // Act & Assert
        identifiers.GitProvider.Should().Be(GitProvider.gitHub);
        identifiers.GitUsername.Should().Be(username);
        identifiers.PersonalAccessToken.Should().Be(token);
    }

    [Fact]
    public void ReadOnlyProperties_ShouldBeSettable()
    {
        // Arrange
        var gitCredential = new GitCredential
        {
            GitProvider = GitProvider.azureDevOpsServices,
            GitUsername = "azure-user",
            PersonalAccessToken = "azure-pat-token",
            Name = "Azure DevOps Credential",
            IsDefaultForProvider = true,
            // Setting read-only properties (would typically come from API responses)
            CredentialId = "cred-67890"
        };

        // Act & Assert
        gitCredential.CredentialId.Should().Be("cred-67890");
    }

    [Fact]
    public void AzureDevOpsCredential_ShouldSupportEmailUsername()
    {
        // Arrange
        var gitCredential = new GitCredential
        {
            GitProvider = GitProvider.azureDevOpsServices,
            GitUsername = "user@contoso.com",
            PersonalAccessToken = "azure-devops-pat-token",
            Name = "Contoso Azure DevOps"
        };

        // Act & Assert
        gitCredential.GitUsername.Should().Contain("@");
        gitCredential.GitUsername.Should().EndWith(".com");
        gitCredential.GitProvider.Should().Be(GitProvider.azureDevOpsServices);
    }

    [Fact]
    public void GitLabCredential_ShouldBeSetCorrectly()
    {
        // Arrange
        var gitCredential = new GitCredential
        {
            GitProvider = GitProvider.gitLab,
            GitUsername = "gitlab-user",
            PersonalAccessToken = "glpat-xxxxxxxxxxxxxxxxxxxx",
            Name = "GitLab Personal Access Token"
        };

        // Act & Assert
        gitCredential.GitProvider.Should().Be(GitProvider.gitLab);
        gitCredential.PersonalAccessToken.Should().StartWith("glpat-");
    }

    [Fact]
    public void DefaultCredential_ShouldBeSetCorrectly()
    {
        // Arrange
        var gitCredential = new GitCredential
        {
            GitProvider = GitProvider.gitHub,
            GitUsername = "default-user",
            PersonalAccessToken = "default-token",
            IsDefaultForProvider = true
        };

        // Act & Assert
        gitCredential.IsDefaultForProvider.Should().BeTrue();
    }

    [Fact]
    public void CredentialWithoutName_ShouldBeValid()
    {
        // Arrange
        var gitCredential = new GitCredential
        {
            GitProvider = GitProvider.bitbucketCloud,
            GitUsername = "bitbucket-user",
            PersonalAccessToken = "bitbucket-token"
            // Name is optional
        };

        // Act & Assert
        gitCredential.GitProvider.Should().Be(GitProvider.bitbucketCloud);
        gitCredential.GitUsername.Should().Be("bitbucket-user");
        gitCredential.PersonalAccessToken.Should().Be("bitbucket-token");
        gitCredential.Name.Should().BeNull();
        gitCredential.IsDefaultForProvider.Should().BeFalse(); // Default value
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsDefaultForProvider_ShouldAcceptBooleanValues(bool isDefault)
    {
        // Arrange
        var gitCredential = new GitCredential
        {
            GitProvider = GitProvider.gitHub,
            GitUsername = "test-user",
            PersonalAccessToken = "test-token",
            IsDefaultForProvider = isDefault
        };

        // Act & Assert
        gitCredential.IsDefaultForProvider.Should().Be(isDefault);
    }

    [Fact]
    public void PersonalAccessToken_ShouldBeHandledSecurely()
    {
        // Arrange
        var sensitiveToken = "very-secret-token-12345";
        var gitCredential = new GitCredential
        {
            GitProvider = GitProvider.gitHub,
            GitUsername = "secure-user",
            PersonalAccessToken = sensitiveToken
        };

        // Act & Assert
        gitCredential.PersonalAccessToken.Should().Be(sensitiveToken);
        // Note: In real scenarios, tokens should be handled as secure strings
        gitCredential.PersonalAccessToken.Should().NotBeEmpty();
    }
}
