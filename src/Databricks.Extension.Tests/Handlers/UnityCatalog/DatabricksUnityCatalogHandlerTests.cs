using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Databricks.Handlers.UnityCatalog;
using DatabricksUnityCatalog = Databricks.Models.UnityCatalog.UnityCatalog;
using Databricks.Models.UnityCatalog;

namespace Databricks.Extension.Tests.Handlers.UnityCatalog;

public class DatabricksUnityCatalogHandlerTests : HandlerTestBase<DatabricksUnityCatalogHandler, DatabricksUnityCatalog, UnityCatalogIdentifiers>
{
    [Fact]
    public void Constructor_ShouldCreateInstanceWithLogger()
    {
        // Arrange & Act
        var handler = new DatabricksUnityCatalogHandler(MockLogger.Object);

        // Assert
        handler.Should().NotBeNull();
    }

    [Fact]
    public void AzureDatabricksUnityCatalog_ShouldBeSetCorrectly()
    {
        // Arrange - Azure Databricks Unity Catalog configuration
        var catalog = new DatabricksUnityCatalog
        {
            Name = "azure-databricks-catalog",
            Comment = "Azure Databricks Unity Catalog for data governance",
            StorageRoot = "abfss://catalog@azurestorage.dfs.core.windows.net/",
            Owner = "azure-admin@company.com",
            EnablePredictiveOptimization = PredictiveOptimizationFlag.ENABLE
        };

        // Act & Assert
        catalog.Name.Should().Be("azure-databricks-catalog");
        catalog.Comment.Should().Be("Azure Databricks Unity Catalog for data governance");
        catalog.StorageRoot.Should().Be("abfss://catalog@azurestorage.dfs.core.windows.net/");
        catalog.Owner.Should().Be("azure-admin@company.com");
        catalog.EnablePredictiveOptimization.Should().Be(PredictiveOptimizationFlag.ENABLE);
    }

    [Theory]
    [InlineData(CatalogType.MANAGED_CATALOG)]
    [InlineData(CatalogType.DELTASHARING_CATALOG)]
    [InlineData(CatalogType.FOREIGN_CATALOG)]
    public void AzureDatabricksCatalogType_ShouldSupportAllTypes(CatalogType catalogType)
    {
        // Arrange
        var catalog = new DatabricksUnityCatalog
        {
            Name = "azure-catalog",
            CatalogType = catalogType
        };

        // Act & Assert
        catalog.CatalogType.Should().Be(catalogType);
    }

    [Theory]
    [InlineData(IsolationMode.OPEN)]
    [InlineData(IsolationMode.ISOLATED)]
    public void AzureDatabricksIsolationMode_ShouldSupportBothModes(IsolationMode isolationMode)
    {
        // Arrange
        var catalog = new DatabricksUnityCatalog
        {
            Name = "azure-catalog",
            IsolationMode = isolationMode
        };

        // Act & Assert
        catalog.IsolationMode.Should().Be(isolationMode);
    }

    [Fact]
    public void AzureDatabricksProvisioningInfo_ShouldBeSetCorrectly()
    {
        // Arrange
        var provisioningInfo = new ProvisioningInfo
        {
            State = ProvisioningState.PROVISIONED
        };

        var catalog = new DatabricksUnityCatalog
        {
            Name = "azure-databricks-catalog",
            ProvisioningInfo = provisioningInfo
        };

        // Act & Assert
        catalog.ProvisioningInfo.Should().NotBeNull();
        catalog.ProvisioningInfo!.State.Should().Be(ProvisioningState.PROVISIONED);
    }

    [Fact]
    public void AzureDatabricksEffectivePredictiveOptimization_ShouldInheritFromCatalog()
    {
        // Arrange
        var effectiveFlag = new EffectivePredictiveOptimizationFlag
        {
            Value = PredictiveOptimizationFlag.ENABLE,
            InheritedFromName = "parent-catalog",
            InheritedFromType = InheritedFromType.CATALOG
        };

        var catalog = new DatabricksUnityCatalog
        {
            Name = "azure-child-catalog",
            EffectivePredictiveOptimizationFlag = effectiveFlag
        };

        // Act & Assert
        catalog.EffectivePredictiveOptimizationFlag.Should().NotBeNull();
        catalog.EffectivePredictiveOptimizationFlag!.Value.Should().Be(PredictiveOptimizationFlag.ENABLE);
        catalog.EffectivePredictiveOptimizationFlag.InheritedFromName.Should().Be("parent-catalog");
        catalog.EffectivePredictiveOptimizationFlag.InheritedFromType.Should().Be(InheritedFromType.CATALOG);
    }

    [Theory]
    [InlineData("abfss://catalog@storage.dfs.core.windows.net/")]
    [InlineData("abfss://unity@datalake.dfs.core.windows.net/catalogs/")]
    public void AzureStorageRoot_ShouldAcceptAzureDataLakeUrls(string storageRoot)
    {
        // Arrange
        var catalog = new DatabricksUnityCatalog
        {
            Name = "azure-catalog",
            StorageRoot = storageRoot
        };

        // Act & Assert
        catalog.StorageRoot.Should().Be(storageRoot);
        catalog.StorageRoot.Should().StartWith("abfss://");
        catalog.StorageRoot.Should().Contain("dfs.core.windows.net");
    }

    [Fact]
    public void AzureDatabricksSecurableProperties_ShouldSupportCustomProperties()
    {
        // Arrange
        var properties = new
        {
            azure_subscription_id = "12345678-1234-1234-1234-123456789012",
            azure_resource_group = "databricks-rg",
            compliance_level = "high",
            data_classification = "confidential"
        };

        var catalog = new DatabricksUnityCatalog
        {
            Name = "azure-compliance-catalog",
            Properties = properties
        };

        // Act & Assert
        catalog.Properties.Should().NotBeNull();
        catalog.Properties.Should().BeEquivalentTo(properties);
    }
}
