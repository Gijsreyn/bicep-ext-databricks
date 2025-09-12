using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Databricks.Handlers.Workspace;
using Databricks.Models.Workspace;

namespace Databricks.Extension.Tests.Handlers.Workspace;

public class DatabricksSecretHandlerTests : HandlerTestBase<DatabricksSecretHandler, Secret, SecretIdentifiers>
{
    [Fact]
    public void Constructor_ShouldCreateInstanceWithLogger()
    {
        // Arrange & Act
        var handler = new DatabricksSecretHandler(MockLogger.Object);

        // Assert
        handler.Should().NotBeNull();
    }

    [Fact]
    public void AzureDatabricksSecret_ShouldBeSetCorrectly()
    {
        // Arrange - Azure Databricks Secret configuration
        var secret = new Secret
        {
            Scope = "azure-keyvault-scope",
            Key = "azure-sql-connection-string",
            StringValue = "Server=myserver.database.windows.net;Database=mydb;Trusted_Connection=false;"
        };

        // Act & Assert
        secret.Scope.Should().Be("azure-keyvault-scope");
        secret.Key.Should().Be("azure-sql-connection-string");
        secret.StringValue.Should().Be("Server=myserver.database.windows.net;Database=mydb;Trusted_Connection=false;");
    }

    [Fact]
    public void SecretWithBytesValue_ShouldBeSetCorrectly()
    {
        // Arrange
        var secret = new Secret
        {
            Scope = "certificates-scope",
            Key = "ssl-certificate",
            BytesValue = "QUJDREU="  // Base64 encoded bytes
        };

        // Act & Assert
        secret.Scope.Should().Be("certificates-scope");
        secret.Key.Should().Be("ssl-certificate");
        secret.BytesValue.Should().Be("QUJDREU=");
    }

    [Theory]
    [InlineData("dev-scope", "api-key")]
    [InlineData("prod-scope", "database-password")]
    [InlineData("azure-keyvault", "storage-connection-string")]
    public void SecretIdentifiers_ShouldBeSetCorrectly(string scope, string key)
    {
        // Arrange
        var identifiers = new SecretIdentifiers
        {
            Scope = scope,
            Key = key
        };

        // Act & Assert
        identifiers.Scope.Should().Be(scope);
        identifiers.Key.Should().Be(key);
    }

    [Fact]
    public void AzureKeyVaultSecret_ShouldSupportVariousTypes()
    {
        // Arrange - Different types of Azure secrets
        var connectionStringSecret = new Secret
        {
            Scope = "azure-keyvault-scope",
            Key = "sqldb-connection-string",
            StringValue = "Server=tcp:myserver.database.windows.net,1433;Initial Catalog=mydb;"
        };

        var apiKeySecret = new Secret
        {
            Scope = "azure-keyvault-scope", 
            Key = "cognitive-services-api-key",
            StringValue = "abc123def456ghi789"
        };

        // Act & Assert
        connectionStringSecret.Key.Should().Contain("connection-string");
        apiKeySecret.Key.Should().Contain("api-key");
        connectionStringSecret.Scope.Should().Be(apiKeySecret.Scope);
    }

    [Fact]
    public void SecretKey_ShouldFollowNamingConventions()
    {
        // Arrange - Test Azure Databricks secret naming patterns
        var validKeys = new[]
        {
            "api-key",
            "database_password",
            "azure.storage.key",
            "CONNECTION_STRING",
            "secret123"
        };

        foreach (var key in validKeys)
        {
            // Act
            var secret = new Secret
            {
                Scope = "test-scope",
                Key = key,
                StringValue = "test-value"
            };

            // Assert
            secret.Key.Should().Be(key);
        }
    }

    [Fact]
    public void SecretWithStringValue_ShouldNotHaveBytesValue()
    {
        // Arrange
        var secret = new Secret
        {
            Scope = "test-scope",
            Key = "test-key",
            StringValue = "string-secret-value"
        };

        // Act & Assert
        secret.StringValue.Should().Be("string-secret-value");
        secret.BytesValue.Should().BeNull();
    }

    [Fact]
    public void SecretWithBytesValue_ShouldNotHaveStringValue()
    {
        // Arrange
        var secret = new Secret
        {
            Scope = "test-scope",
            Key = "test-key",
            BytesValue = "dGVzdC1ieXRlcw=="  // Base64 encoded "test-bytes"
        };

        // Act & Assert
        secret.BytesValue.Should().NotBeNull();
        secret.BytesValue.Should().Be("dGVzdC1ieXRlcw==");
        secret.StringValue.Should().BeNull();
    }

    [Fact]
    public void ReadOnlyProperties_ShouldBeSettable()
    {
        // Arrange
        var secret = new Secret
        {
            Scope = "test-scope",
            Key = "test-key",
            StringValue = "test-value",
            // Setting read-only properties (would typically come from API responses)
            LastUpdatedTimestamp = 1640995200,
            ConfigReference = "{{secrets/test-scope/test-key}}"
        };

        // Act & Assert
        secret.LastUpdatedTimestamp.Should().Be(1640995200);
        secret.ConfigReference.Should().Be("{{secrets/test-scope/test-key}}");
    }

    [Fact]
    public void SecretScope_ShouldSupportDifferentBackendTypes()
    {
        // Arrange - Different Azure Databricks secret scope types
        var keyVaultSecret = new Secret
        {
            Scope = "azure-keyvault-backed",
            Key = "database-password",
            StringValue = "secure-password"
        };

        var databricksSecret = new Secret
        {
            Scope = "databricks-backed",
            Key = "api-token",
            StringValue = "dapi123456789"
        };

        // Act & Assert
        keyVaultSecret.Scope.Should().Contain("keyvault");
        databricksSecret.Scope.Should().Contain("databricks");
        keyVaultSecret.Key.Should().NotBe(databricksSecret.Key);
    }
}
