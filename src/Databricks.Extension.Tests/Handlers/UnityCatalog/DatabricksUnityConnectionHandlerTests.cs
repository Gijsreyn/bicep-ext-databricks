using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Databricks.Handlers.UnityCatalog;
using Databricks.Models.UnityCatalog;

namespace Databricks.Extension.Tests.Handlers.UnityCatalog;

public class DatabricksUnityConnectionHandlerTests : HandlerTestBase<DatabricksUnityConnectionHandler, UnityConnection, UnityConnectionIdentifiers>
{
    [Fact]
    public void Constructor_ShouldCreateInstanceWithLogger()
    {
        // Arrange & Act
        var handler = new DatabricksUnityConnectionHandler(MockLogger.Object);

        // Assert
        handler.Should().NotBeNull();
    }

    [Fact]
    public void AzureDatabricksUnityConnection_ShouldBeSetCorrectly()
    {
        // Arrange - Azure Databricks Unity Connection configuration
        var connection = new UnityConnection
        {
            Name = "azure-databricks-connection",
            Comment = "Azure Databricks connection for Unity Catalog",
            ConnectionType = ConnectionType.SQLSERVER, // Using SQLSERVER for Azure SQL
            Owner = "azure-admin@company.com",
            ReadOnly = false
        };

        // Act & Assert
        connection.Name.Should().Be("azure-databricks-connection");
        connection.Comment.Should().Be("Azure Databricks connection for Unity Catalog");
        connection.ConnectionType.Should().Be(ConnectionType.SQLSERVER);
        connection.Owner.Should().Be("azure-admin@company.com");
        connection.ReadOnly.Should().BeFalse();
    }

    [Theory]
    [InlineData(ConnectionType.SQLSERVER)] // Azure SQL Server
    [InlineData(ConnectionType.SQLDW)] // Azure SQL Data Warehouse
    [InlineData(ConnectionType.DATABRICKS)] // Azure Databricks
    public void AzureDatabricksConnectionType_ShouldSupportAzureTypes(ConnectionType connectionType)
    {
        // Arrange
        var connection = new UnityConnection
        {
            Name = "azure-connection",
            ConnectionType = connectionType
        };

        // Act & Assert
        connection.ConnectionType.Should().Be(connectionType);
    }

    [Theory]
    [InlineData(CredentialType.UNKNOWN_CREDENTIAL_TYPE)]
    public void AzureDatabricksCredentialType_ShouldSupportAzureCredentials(CredentialType credentialType)
    {
        // Arrange
        var connection = new UnityConnection
        {
            Name = "azure-connection",
            CredentialType = credentialType
        };

        // Act & Assert
        connection.CredentialType.Should().Be(credentialType);
    }

    [Fact]
    public void AzureDatabricksConnectionOptions_ShouldSupportAzureSpecificSettings()
    {
        // Arrange - Azure-specific connection options
        var options = new
        {
            server = "myserver.database.windows.net",
            port = "1433",
            database = "mydatabase",
            authentication = "ActiveDirectoryPassword",
            encrypt = "true",
            trust_server_certificate = "false",
            azure_tenant_id = "12345678-1234-1234-1234-123456789012"
        };

        var connection = new UnityConnection
        {
            Name = "azure-sql-connection",
            ConnectionType = ConnectionType.SQLSERVER,
            Options = options
        };

        // Act & Assert
        connection.Options.Should().NotBeNull();
        connection.Options.Should().BeEquivalentTo(options);
    }

    [Theory]
    [InlineData(SecurableType.CATALOG)]
    [InlineData(SecurableType.SCHEMA)]
    [InlineData(SecurableType.TABLE)]
    public void AzureDatabricksSecurableType_ShouldSupportAllTypes(SecurableType securableType)
    {
        // Arrange
        var connection = new UnityConnection
        {
            Name = "azure-connection",
            SecurableType = securableType
        };

        // Act & Assert
        connection.SecurableType.Should().Be(securableType);
    }

    [Fact]
    public void AzureDatabricksReadOnlyProperties_ShouldBeSettable()
    {
        // Arrange
        var connection = new UnityConnection
        {
            Name = "azure-connection",
            ConnectionType = ConnectionType.SQLSERVER,
            // Setting read-only properties (would typically come from API responses)
            CreatedAt = 1640995200,
            CreatedBy = "azure-admin@company.com",
            FullName = "azure-connection",
            MetastoreId = "azure-metastore-456",
            UpdatedAt = 1641081600,
            UpdatedBy = "azure-updater@company.com"
        };

        // Act & Assert
        connection.CreatedAt.Should().Be(1640995200);
        connection.CreatedBy.Should().Be("azure-admin@company.com");
        connection.FullName.Should().Be("azure-connection");
        connection.MetastoreId.Should().Be("azure-metastore-456");
        connection.UpdatedAt.Should().Be(1641081600);
        connection.UpdatedBy.Should().Be("azure-updater@company.com");
    }

    [Fact]
    public void AzureDatabricksProperties_ShouldSupportCustomProperties()
    {
        // Arrange
        var properties = new
        {
            azure_subscription_id = "12345678-1234-1234-1234-123456789012",
            azure_resource_group = "databricks-rg",
            environment = "production",
            region = "East US 2"
        };

        var connection = new UnityConnection
        {
            Name = "azure-production-connection",
            ConnectionType = ConnectionType.SQLSERVER,
            Properties = properties
        };

        // Act & Assert
        connection.Properties.Should().NotBeNull();
        connection.Properties.Should().BeEquivalentTo(properties);
    }
}
