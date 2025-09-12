using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Databricks.Handlers.UnityCatalog;
using Databricks.Models.UnityCatalog;

namespace Databricks.Extension.Tests.Handlers.UnityCatalog;

public class DatabricksUnityStorageCredentialHandlerTests : HandlerTestBase<DatabricksUnityStorageCredentialHandler, UnityStorageCredential, UnityStorageCredentialIdentifiers>
{
    [Fact]
    public void Constructor_ShouldCreateInstanceWithLogger()
    {
        // Arrange & Act
        var handler = new DatabricksUnityStorageCredentialHandler(MockLogger.Object);

        // Assert
        handler.Should().NotBeNull();
    }

    [Fact]
    public void UnityStorageCredentialProperties_ShouldBeSetCorrectly()
    {
        // Arrange
        var storageCredential = new UnityStorageCredential
        {
            Name = "test-credential",
            Comment = "Test storage credential",
            Owner = "test@databricks.com",
            ReadOnly = false,
            SkipValidation = false
        };

        // Act & Assert
        storageCredential.Name.Should().Be("test-credential");
        storageCredential.Comment.Should().Be("Test storage credential");
        storageCredential.Owner.Should().Be("test@databricks.com");
        storageCredential.ReadOnly.Should().BeFalse();
        storageCredential.SkipValidation.Should().BeFalse();
    }

    [Theory]
    [InlineData("azure-credential")]
    [InlineData("aws-credential")]
    [InlineData("gcp_credential")]
    public void UnityStorageCredentialIdentifiers_ShouldBeSetCorrectly(string credentialName)
    {
        // Arrange
        var identifiers = new UnityStorageCredentialIdentifiers
        {
            Name = credentialName
        };

        // Act & Assert
        identifiers.Name.Should().Be(credentialName);
    }

    [Fact]
    public void ReadOnlyProperties_ShouldBeSettable()
    {
        // Arrange
        var storageCredential = new UnityStorageCredential
        {
            Name = "test-credential",
            Comment = "Test credential",
            // Setting read-only properties (would typically come from API responses)
            CreatedAt = 1640995200,
            CreatedBy = "test@databricks.com",
            Id = "credential-id-123",
            MetastoreId = "test-metastore-id",
            UpdatedAt = 1641081600,
            UpdatedBy = "updater@databricks.com"
        };

        // Act & Assert
        storageCredential.Name.Should().Be("test-credential");
        storageCredential.CreatedAt.Should().Be(1640995200);
        storageCredential.CreatedBy.Should().Be("test@databricks.com");
        storageCredential.Id.Should().Be("credential-id-123");
        storageCredential.MetastoreId.Should().Be("test-metastore-id");
        storageCredential.UpdatedAt.Should().Be(1641081600);
        storageCredential.UpdatedBy.Should().Be("updater@databricks.com");
    }

    [Fact]
    public void AzureManagedIdentity_ShouldBeSetCorrectly()
    {
        // Arrange
        var azureManagedIdentity = new StorageCredentialAzureManagedIdentity
        {
            AccessConnectorId = "/subscriptions/sub/resourceGroups/rg/providers/Microsoft.Databricks/accessConnectors/connector",
            CredentialId = "managed-identity-credential-id",
            ManagedIdentityId = "/subscriptions/sub/resourceGroups/rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/identity"
        };

        var storageCredential = new UnityStorageCredential
        {
            Name = "azure-mi-credential",
            AzureManagedIdentity = azureManagedIdentity
        };

        // Act & Assert
        storageCredential.AzureManagedIdentity.Should().NotBeNull();
        storageCredential.AzureManagedIdentity!.AccessConnectorId.Should().Be("/subscriptions/sub/resourceGroups/rg/providers/Microsoft.Databricks/accessConnectors/connector");
        storageCredential.AzureManagedIdentity.CredentialId.Should().Be("managed-identity-credential-id");
        storageCredential.AzureManagedIdentity.ManagedIdentityId.Should().Be("/subscriptions/sub/resourceGroups/rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/identity");
    }

    [Fact]
    public void AzureServicePrincipal_ShouldBeSetCorrectly()
    {
        // Arrange
        var azureServicePrincipal = new StorageCredentialAzureServicePrincipal
        {
            DirectoryId = "tenant-id-123",
            ApplicationId = "app-id-456",
            ClientSecret = "client-secret-789"
        };

        var storageCredential = new UnityStorageCredential
        {
            Name = "azure-sp-credential",
            AzureServicePrincipal = azureServicePrincipal
        };

        // Act & Assert
        storageCredential.AzureServicePrincipal.Should().NotBeNull();
        storageCredential.AzureServicePrincipal!.DirectoryId.Should().Be("tenant-id-123");
        storageCredential.AzureServicePrincipal.ApplicationId.Should().Be("app-id-456");
        storageCredential.AzureServicePrincipal.ClientSecret.Should().Be("client-secret-789");
    }

    [Fact]
    public void IsolationMode_ShouldBeSetCorrectly()
    {
        // Arrange
        var storageCredential = new UnityStorageCredential
        {
            Name = "test-credential",
            IsolationMode = ExternalLocationIsolationMode.ISOLATION_MODE_ISOLATED
        };

        // Act & Assert
        storageCredential.IsolationMode.Should().Be(ExternalLocationIsolationMode.ISOLATION_MODE_ISOLATED);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadOnlyFlag_ShouldAcceptBooleanValues(bool readOnly)
    {
        // Arrange
        var storageCredential = new UnityStorageCredential
        {
            Name = "test-credential",
            ReadOnly = readOnly
        };

        // Act & Assert
        storageCredential.ReadOnly.Should().Be(readOnly);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SkipValidationFlag_ShouldAcceptBooleanValues(bool skipValidation)
    {
        // Arrange
        var storageCredential = new UnityStorageCredential
        {
            Name = "test-credential",
            SkipValidation = skipValidation
        };

        // Act & Assert
        storageCredential.SkipValidation.Should().Be(skipValidation);
    }
}
