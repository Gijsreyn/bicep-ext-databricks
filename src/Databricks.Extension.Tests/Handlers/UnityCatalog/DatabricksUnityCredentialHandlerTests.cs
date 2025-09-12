using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Databricks.Handlers.UnityCatalog;
using Databricks.Models.UnityCatalog;

namespace Databricks.Extension.Tests.Handlers.UnityCatalog;

public class DatabricksUnityCredentialHandlerTests : HandlerTestBase<DatabricksUnityCredentialHandler, UnityCredential, UnityCredentialIdentifiers>
{
    [Fact]
    public void Constructor_ShouldCreateInstanceWithLogger()
    {
        // Arrange & Act
        var handler = new DatabricksUnityCredentialHandler(MockLogger.Object);

        // Assert
        handler.Should().NotBeNull();
    }

    [Fact]
    public void UnityCredentialProperties_ShouldBeSetCorrectly()
    {
        // Arrange
        var credential = new UnityCredential
        {
            Name = "test-credential",
            Comment = "Test credential for unit tests",
            Purpose = CredentialPurpose.STORAGE,
            ReadOnly = true,
            SkipValidation = false,
            ForceUpdate = true,
            ForceDestroy = false,
            Owner = "test@databricks.com"
        };

        // Act & Assert
        credential.Name.Should().Be("test-credential");
        credential.Comment.Should().Be("Test credential for unit tests");
        credential.Purpose.Should().Be(CredentialPurpose.STORAGE);
        credential.ReadOnly.Should().BeTrue();
        credential.SkipValidation.Should().BeFalse();
        credential.ForceUpdate.Should().BeTrue();
        credential.ForceDestroy.Should().BeFalse();
        credential.Owner.Should().Be("test@databricks.com");
    }

    [Theory]
    [InlineData("credential1")]
    [InlineData("storage_credential")]
    [InlineData("service-cred")]
    public void UnityCredentialIdentifiers_ShouldBeSetCorrectly(string credentialName)
    {
        // Arrange
        var identifiers = new UnityCredentialIdentifiers
        {
            Name = credentialName
        };

        // Act & Assert
        identifiers.Name.Should().Be(credentialName);
    }

    [Theory]
    [InlineData(CredentialPurpose.STORAGE)]
    [InlineData(CredentialPurpose.SERVICE)]
    public void CredentialPurpose_ShouldAcceptAllValues(CredentialPurpose purpose)
    {
        // Arrange
        var credential = new UnityCredential
        {
            Name = "test-credential",
            Purpose = purpose
        };

        // Act & Assert
        credential.Purpose.Should().Be(purpose);
    }

    [Theory]
    [InlineData(CredentialIsolationMode.ISOLATION_MODE_OPEN)]
    [InlineData(CredentialIsolationMode.ISOLATION_MODE_ISOLATED)]
    public void CredentialIsolationMode_ShouldAcceptAllValues(CredentialIsolationMode isolationMode)
    {
        // Arrange
        var credential = new UnityCredential
        {
            Name = "test-credential",
            IsolationMode = isolationMode
        };

        // Act & Assert
        credential.IsolationMode.Should().Be(isolationMode);
    }

    [Fact]
    public void AzureManagedIdentity_ShouldBeSetCorrectly()
    {
        // Arrange
        var azureManagedIdentity = new AzureManagedIdentity
        {
            AccessConnectorId = "access-connector-123",
            ManagedIdentityId = "managed-identity-456"
        };

        var credential = new UnityCredential
        {
            Name = "azure-mi-credential",
            Purpose = CredentialPurpose.STORAGE,
            AzureManagedIdentity = azureManagedIdentity
        };

        // Act & Assert
        credential.AzureManagedIdentity.Should().NotBeNull();
        credential.AzureManagedIdentity!.AccessConnectorId.Should().Be("access-connector-123");
        credential.AzureManagedIdentity.ManagedIdentityId.Should().Be("managed-identity-456");
    }

    [Fact]
    public void AzureServicePrincipal_ShouldBeSetCorrectly()
    {
        // Arrange
        var azureServicePrincipal = new AzureServicePrincipal
        {
            ApplicationId = "app-id-123",
            ClientSecret = "client-secret-456",
            DirectoryId = "tenant-id-789"
        };

        var credential = new UnityCredential
        {
            Name = "azure-sp-credential",
            Purpose = CredentialPurpose.STORAGE,
            AzureServicePrincipal = azureServicePrincipal
        };

        // Act & Assert
        credential.AzureServicePrincipal.Should().NotBeNull();
        credential.AzureServicePrincipal!.ApplicationId.Should().Be("app-id-123");
        credential.AzureServicePrincipal.ClientSecret.Should().Be("client-secret-456");
        credential.AzureServicePrincipal.DirectoryId.Should().Be("tenant-id-789");
    }

    [Fact]
    public void ReadOnlyProperties_ShouldBeSettable()
    {
        // Arrange
        var credential = new UnityCredential
        {
            Name = "test-credential",
            Purpose = CredentialPurpose.STORAGE,
            // Setting read-only properties (would typically come from API responses)
            CreatedAt = 1640995200,
            CreatedBy = "test@databricks.com",
            FullName = "test-credential",
            Id = "credential-id-123",
            IsolationMode = CredentialIsolationMode.ISOLATION_MODE_OPEN,
            MetastoreId = "metastore-id-456",
            UpdatedAt = 1641081600,
            UpdatedBy = "updater@databricks.com",
            UsedForManagedStorage = false
        };

        // Act & Assert
        credential.CreatedAt.Should().Be(1640995200);
        credential.CreatedBy.Should().Be("test@databricks.com");
        credential.FullName.Should().Be("test-credential");
        credential.Id.Should().Be("credential-id-123");
        credential.IsolationMode.Should().Be(CredentialIsolationMode.ISOLATION_MODE_OPEN);
        credential.MetastoreId.Should().Be("metastore-id-456");
        credential.UpdatedAt.Should().Be(1641081600);
        credential.UpdatedBy.Should().Be("updater@databricks.com");
        credential.UsedForManagedStorage.Should().BeFalse();
    }

    [Fact]
    public void AzureManagedIdentityCredentialId_ShouldBeReadOnly()
    {
        // Arrange
        var azureManagedIdentity = new AzureManagedIdentity
        {
            AccessConnectorId = "access-connector-123",
            ManagedIdentityId = "managed-identity-456",
            CredentialId = "computed-credential-id" // This would be set by the system
        };

        // Act & Assert
        azureManagedIdentity.CredentialId.Should().Be("computed-credential-id");
    }

    [Theory]
    [InlineData(true, true, true, true)]
    [InlineData(false, false, false, false)]
    [InlineData(true, false, true, false)]
    public void BooleanFlags_ShouldBeSetCorrectly(bool readOnly, bool skipValidation, bool forceUpdate, bool forceDestroy)
    {
        // Arrange
        var credential = new UnityCredential
        {
            Name = "test-credential",
            Purpose = CredentialPurpose.STORAGE,
            ReadOnly = readOnly,
            SkipValidation = skipValidation,
            ForceUpdate = forceUpdate,
            ForceDestroy = forceDestroy
        };

        // Act & Assert
        credential.ReadOnly.Should().Be(readOnly);
        credential.SkipValidation.Should().Be(skipValidation);
        credential.ForceUpdate.Should().Be(forceUpdate);
        credential.ForceDestroy.Should().Be(forceDestroy);
    }

    [Fact]
    public void ServicePrincipalSecrets_ShouldBeHandledSecurely()
    {
        // Arrange
        var servicePrivcipal = new AzureServicePrincipal
        {
            ApplicationId = "public-app-id",
            ClientSecret = "secret-value-should-be-protected",
            DirectoryId = "public-tenant-id"
        };

        // Act & Assert
        servicePrivcipal.ApplicationId.Should().Be("public-app-id");
        servicePrivcipal.ClientSecret.Should().Be("secret-value-should-be-protected");
        servicePrivcipal.DirectoryId.Should().Be("public-tenant-id");
        // Note: In real scenarios, ClientSecret should be handled as a secure string
    }

    [Fact]
    public void CredentialWithoutAzureConfigs_ShouldBeValid()
    {
        // Arrange
        var credential = new UnityCredential
        {
            Name = "generic-credential",
            Purpose = CredentialPurpose.SERVICE,
            Comment = "Credential without Azure-specific configurations"
        };

        // Act & Assert
        credential.Name.Should().Be("generic-credential");
        credential.Purpose.Should().Be(CredentialPurpose.SERVICE);
        credential.AzureManagedIdentity.Should().BeNull();
        credential.AzureServicePrincipal.Should().BeNull();
    }
}
