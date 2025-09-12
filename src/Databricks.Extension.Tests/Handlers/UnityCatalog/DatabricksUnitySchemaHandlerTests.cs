using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Databricks.Handlers.UnityCatalog;
using Databricks.Models.UnityCatalog;

namespace Databricks.Extension.Tests.Handlers.UnityCatalog;

public class DatabricksUnitySchemaHandlerTests : HandlerTestBase<DatabricksUnitySchemaHandler, UnitySchema, UnitySchemaIdentifiers>
{
    [Fact]
    public void Constructor_ShouldCreateInstanceWithLogger()
    {
        // Arrange & Act
        var handler = new DatabricksUnitySchemaHandler(MockLogger.Object);

        // Assert
        handler.Should().NotBeNull();
    }

    [Fact]
    public void UnitySchemaProperties_ShouldBeSetCorrectly()
    {
        // Arrange
        var schema = new UnitySchema
        {
            CatalogName = "test-catalog",
            Name = "test-schema",
            Comment = "Test schema for unit tests",
            Owner = "test@databricks.com",
            StorageRoot = "s3://bucket/schema-root"
        };

        // Act & Assert
        schema.CatalogName.Should().Be("test-catalog");
        schema.Name.Should().Be("test-schema");
        schema.Comment.Should().Be("Test schema for unit tests");
        schema.Owner.Should().Be("test@databricks.com");
        schema.StorageRoot.Should().Be("s3://bucket/schema-root");
    }

    [Theory]
    [InlineData("schema1")]
    [InlineData("raw_data")]
    [InlineData("processed_data")]
    public void UnitySchemaIdentifiers_ShouldBeSetCorrectly(string schemaName)
    {
        // Arrange
        var identifiers = new UnitySchemaIdentifiers
        {
            Name = schemaName
        };

        // Act & Assert
        identifiers.Name.Should().Be(schemaName);
    }

    [Fact]
    public void ReadOnlyProperties_ShouldBeSettable()
    {
        // Arrange
        var schema = new UnitySchema
        {
            CatalogName = "test-catalog",
            Name = "test-schema",
            Comment = "Test schema",
            // Setting read-only properties (would typically come from API responses)
            CreatedAt = 1640995200,
            CreatedBy = "test@databricks.com",
            FullName = "test-catalog.test-schema",
            MetastoreId = "test-metastore-id",
            SchemaId = "schema-id-123",
            UpdatedAt = 1641081600,
            UpdatedBy = "updater@databricks.com"
        };

        // Act & Assert
        schema.CatalogName.Should().Be("test-catalog");
        schema.Name.Should().Be("test-schema");
        schema.CreatedAt.Should().Be(1640995200);
        schema.CreatedBy.Should().Be("test@databricks.com");
        schema.FullName.Should().Be("test-catalog.test-schema");
        schema.MetastoreId.Should().Be("test-metastore-id");
        schema.SchemaId.Should().Be("schema-id-123");
        schema.UpdatedAt.Should().Be(1641081600);
        schema.UpdatedBy.Should().Be("updater@databricks.com");
    }

    [Fact]
    public void EffectivePredictiveOptimizationFlag_ShouldBeSetCorrectly()
    {
        // Arrange
        var effectiveFlag = new SchemaEffectivePredictiveOptimizationFlag
        {
            Value = PredictiveOptimizationFlag.ENABLE,
            InheritedFromName = "parent-catalog",
            InheritedFromType = InheritedFromType.CATALOG
        };

        var schema = new UnitySchema
        {
            CatalogName = "test-catalog",
            Name = "test-schema",
            EffectivePredictiveOptimizationFlag = effectiveFlag
        };

        // Act & Assert
        schema.EffectivePredictiveOptimizationFlag.Should().NotBeNull();
        schema.EffectivePredictiveOptimizationFlag!.Value.Should().Be(PredictiveOptimizationFlag.ENABLE);
        schema.EffectivePredictiveOptimizationFlag.InheritedFromName.Should().Be("parent-catalog");
        schema.EffectivePredictiveOptimizationFlag.InheritedFromType.Should().Be(InheritedFromType.CATALOG);
    }

    [Fact]
    public void EnablePredictiveOptimization_ShouldBeSetCorrectly()
    {
        // Arrange
        var schema = new UnitySchema
        {
            CatalogName = "test-catalog",
            Name = "test-schema",
            EnablePredictiveOptimization = PredictiveOptimizationFlag.ENABLE
        };

        // Act & Assert
        schema.EnablePredictiveOptimization.Should().Be(PredictiveOptimizationFlag.ENABLE);
    }

    [Theory]
    [InlineData(PredictiveOptimizationFlag.ENABLE)]
    [InlineData(PredictiveOptimizationFlag.DISABLE)]
    [InlineData(PredictiveOptimizationFlag.INHERIT)]
    public void PredictiveOptimizationFlag_ShouldAcceptAllValues(PredictiveOptimizationFlag flag)
    {
        // Arrange
        var schema = new UnitySchema
        {
            CatalogName = "test-catalog",
            Name = "test-schema",
            EnablePredictiveOptimization = flag
        };

        // Act & Assert
        schema.EnablePredictiveOptimization.Should().Be(flag);
    }

    [Theory]
    [InlineData("s3://bucket/schema-root")]
    [InlineData("abfss://container@storage.dfs.core.windows.net/schema")]
    [InlineData("gs://bucket/schema-path")]
    public void StorageRoot_ShouldAcceptVariousUrls(string storageRoot)
    {
        // Arrange
        var schema = new UnitySchema
        {
            CatalogName = "test-catalog",
            Name = "test-schema",
            StorageRoot = storageRoot
        };

        // Act & Assert
        schema.StorageRoot.Should().Be(storageRoot);
    }

    [Fact]
    public void FullName_ShouldFollowCatalogDotSchemaPattern()
    {
        // Arrange
        var schema = new UnitySchema
        {
            CatalogName = "analytics",
            Name = "raw_data",
            FullName = "analytics.raw_data"
        };

        // Act & Assert
        schema.FullName.Should().Be("analytics.raw_data");
        schema.FullName.Should().Contain(schema.CatalogName);
        schema.FullName.Should().Contain(schema.Name);
    }

    [Fact]
    public void Properties_ShouldAcceptDynamicObject()
    {
        // Arrange
        var properties = new
        {
            custom_property = "custom_value",
            retention_days = 30,
            encrypted = true
        };

        var schema = new UnitySchema
        {
            CatalogName = "test-catalog",
            Name = "test-schema",
            Properties = properties
        };

        // Act & Assert
        schema.Properties.Should().NotBeNull();
        schema.Properties.Should().BeEquivalentTo(properties);
    }
}
