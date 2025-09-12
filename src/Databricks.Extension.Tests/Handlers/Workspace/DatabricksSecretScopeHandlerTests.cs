using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Databricks.Handlers.Workspace;
using Databricks.Models.Workspace;

namespace Databricks.Extension.Tests.Handlers.Workspace;

public class DatabricksSecretScopeHandlerTests : HandlerTestBase<DatabricksSecretScopeHandler, SecretScope, SecretScopeIdentifiers>
{
    [Fact]
    public void Constructor_ShouldCreateInstanceWithLogger()
    {
        // Arrange & Act
        var handler = new DatabricksSecretScopeHandler(MockLogger.Object);

        // Assert
        handler.Should().NotBeNull();
    }

    [Fact]
    public void AzureDatabricksSecretScope_ShouldBeSetCorrectly()
    {
        // Arrange - Azure Databricks SecretScope with Databricks backend
        var secretScope = new SecretScope
        {
            ScopeName = "azure-databricks-scope",
            BackendType = SecretScopeBackendType.DATABRICKS,
            InitialManagePrincipal = "azure-admin@company.com"
        };

        // Act & Assert
        secretScope.ScopeName.Should().Be("azure-databricks-scope");
        secretScope.BackendType.Should().Be(SecretScopeBackendType.DATABRICKS);
        secretScope.InitialManagePrincipal.Should().Be("azure-admin@company.com");
    }

    [Fact]
    public void AzureKeyVaultSecretScope_ShouldBeSetCorrectly()
    {
        // Arrange - Azure Key Vault backed SecretScope
        var keyVaultMetadata = new AzureKeyVaultMetadata
        {
            ResourceId = "/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/rg-databricks/providers/Microsoft.KeyVault/vaults/kv-databricks",
            DnsName = "kv-databricks.vault.azure.net"
        };

        var secretScope = new SecretScope
        {
            ScopeName = "azure-keyvault-scope",
            BackendType = SecretScopeBackendType.AZURE_KEYVAULT,
            KeyVaultMetadata = keyVaultMetadata,
            InitialManagePrincipal = "azure-admin@company.com"
        };

        // Act & Assert
        secretScope.ScopeName.Should().Be("azure-keyvault-scope");
        secretScope.BackendType.Should().Be(SecretScopeBackendType.AZURE_KEYVAULT);
        secretScope.KeyVaultMetadata.Should().NotBeNull();
        secretScope.KeyVaultMetadata!.ResourceId.Should().Be("/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/rg-databricks/providers/Microsoft.KeyVault/vaults/kv-databricks");
        secretScope.KeyVaultMetadata.DnsName.Should().Be("kv-databricks.vault.azure.net");
        secretScope.InitialManagePrincipal.Should().Be("azure-admin@company.com");
    }

    [Theory]
    [InlineData(SecretScopeBackendType.DATABRICKS)]
    [InlineData(SecretScopeBackendType.AZURE_KEYVAULT)]
    public void SecretScopeBackendType_ShouldAcceptAllValues(SecretScopeBackendType backendType)
    {
        // Arrange
        var secretScope = new SecretScope
        {
            ScopeName = "test-scope",
            BackendType = backendType
        };

        // Act & Assert
        secretScope.BackendType.Should().Be(backendType);
    }

    [Theory]
    [InlineData("dev-secrets")]
    [InlineData("prod_secrets")]
    [InlineData("analytics-scope")]
    [InlineData("azure.keyvault.scope")]
    public void SecretScopeIdentifiers_ShouldBeSetCorrectly(string scopeName)
    {
        // Arrange
        var identifiers = new SecretScopeIdentifiers
        {
            ScopeName = scopeName
        };

        // Act & Assert
        identifiers.ScopeName.Should().Be(scopeName);
    }

    [Fact]
    public void AzureKeyVaultMetadata_ShouldValidateAzureResources()
    {
        // Arrange
        var keyVaultMetadata = new AzureKeyVaultMetadata
        {
            ResourceId = "/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/rg-databricks/providers/Microsoft.KeyVault/vaults/my-keyvault",
            DnsName = "my-keyvault.vault.azure.net"
        };

        // Act & Assert
        keyVaultMetadata.ResourceId.Should().StartWith("/subscriptions/");
        keyVaultMetadata.ResourceId.Should().Contain("Microsoft.KeyVault/vaults/");
        keyVaultMetadata.DnsName.Should().EndWith(".vault.azure.net");
    }

    [Fact]
    public void SecretScopeName_ShouldFollowNamingConventions()
    {
        // Arrange - Test valid naming patterns for Azure Databricks
        var validNames = new[]
        {
            "azure-secrets",
            "prod_keyvault_scope",
            "dev.secrets.scope",
            "analytics123",
            "UPPER_CASE_SCOPE"
        };

        foreach (var name in validNames)
        {
            // Act
            var secretScope = new SecretScope
            {
                ScopeName = name
            };

            // Assert
            secretScope.ScopeName.Should().Be(name);
            secretScope.ScopeName.Length.Should().BeLessThan(129);
        }
    }

    [Fact]
    public void DatabricksBackendScope_ShouldNotRequireKeyVaultMetadata()
    {
        // Arrange
        var secretScope = new SecretScope
        {
            ScopeName = "databricks-managed-scope",
            BackendType = SecretScopeBackendType.DATABRICKS,
            InitialManagePrincipal = "user@company.com"
        };

        // Act & Assert
        secretScope.BackendType.Should().Be(SecretScopeBackendType.DATABRICKS);
        secretScope.KeyVaultMetadata.Should().BeNull();
        secretScope.InitialManagePrincipal.Should().Be("user@company.com");
    }
}
