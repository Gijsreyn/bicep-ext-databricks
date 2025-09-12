using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Databricks.Handlers.UnityCatalog;
using Databricks.Models.UnityCatalog;

namespace Databricks.Extension.Tests.Handlers.UnityCatalog;

public class DatabricksUnityExternalLocationHandlerTests : HandlerTestBase<DatabricksUnityExternalLocationHandler, UnityExternalLocation, UnityExternalLocationIdentifiers>
{
    [Fact]
    public void Constructor_ShouldCreateInstanceWithLogger()
    {
        // Arrange & Act
        var handler = new DatabricksUnityExternalLocationHandler(MockLogger.Object);

        // Assert
        handler.Should().NotBeNull();
    }

    [Fact]
    public void UnityExternalLocationProperties_ShouldBeSetCorrectly()
    {
        // Arrange
        var externalLocation = new UnityExternalLocation
        {
            Name = "test-location",
            Url = "s3://test-bucket/data",
            CredentialName = "test-credential",
            Comment = "Test external location",
            Owner = "test@databricks.com"
        };

        // Act & Assert
        externalLocation.Name.Should().Be("test-location");
        externalLocation.Url.Should().Be("s3://test-bucket/data");
        externalLocation.CredentialName.Should().Be("test-credential");
        externalLocation.Comment.Should().Be("Test external location");
        externalLocation.Owner.Should().Be("test@databricks.com");
    }

    [Theory]
    [InlineData("s3-location")]
    [InlineData("adls-gen2-location")]
    [InlineData("gcs_location")]
    public void UnityExternalLocationIdentifiers_ShouldBeSetCorrectly(string locationName)
    {
        // Arrange
        var identifiers = new UnityExternalLocationIdentifiers
        {
            Name = locationName
        };

        // Act & Assert
        identifiers.Name.Should().Be(locationName);
    }

    [Fact]
    public void ExternalLocationIsolationModeEnum_ShouldHaveCorrectValues()
    {
        // Arrange & Act & Assert
        ExternalLocationIsolationMode.ISOLATION_MODE_ISOLATED.Should().Be(ExternalLocationIsolationMode.ISOLATION_MODE_ISOLATED);
        ExternalLocationIsolationMode.ISOLATION_MODE_OPEN.Should().Be(ExternalLocationIsolationMode.ISOLATION_MODE_OPEN);
    }

    [Fact]
    public void ReadOnlyProperties_ShouldBeSettable()
    {
        // Arrange
        var externalLocation = new UnityExternalLocation
        {
            Name = "test-location",
            Url = "s3://test-bucket/data",
            CredentialName = "test-credential",
            // Setting read-only properties (would typically come from API responses)
            CreatedAt = 1640995200,
            CreatedBy = "test@databricks.com",
            MetastoreId = "test-metastore-id",
            UpdatedAt = 1641081600,
            UpdatedBy = "updater@databricks.com",
            IsolationMode = ExternalLocationIsolationMode.ISOLATION_MODE_OPEN
        };

        // Act & Assert
        externalLocation.Name.Should().Be("test-location");
        externalLocation.Url.Should().Be("s3://test-bucket/data");
        externalLocation.CredentialName.Should().Be("test-credential");
        externalLocation.CreatedAt.Should().Be(1640995200);
        externalLocation.CreatedBy.Should().Be("test@databricks.com");
        externalLocation.MetastoreId.Should().Be("test-metastore-id");
        externalLocation.UpdatedAt.Should().Be(1641081600);
        externalLocation.UpdatedBy.Should().Be("updater@databricks.com");
        externalLocation.IsolationMode.Should().Be(ExternalLocationIsolationMode.ISOLATION_MODE_OPEN);
    }

    [Fact]
    public void ManagedAqs_ShouldBeSetCorrectly()
    {
        // Arrange
        var managedAqs = new ManagedAqs
        {
            QueueUrl = "https://test.queue.amazonaws.com/123456789012/test-queue",
            ResourceGroup = "test-resource-group",
            SubscriptionId = "test-subscription-id"
        };

        var fileEventQueue = new FileEventQueue
        {
            ManagedAqs = managedAqs
        };

        var externalLocation = new UnityExternalLocation
        {
            Name = "test-location",
            Url = "abfss://container@storage.dfs.core.windows.net/path",
            CredentialName = "test-credential",
            FileEventQueue = fileEventQueue
        };

        // Act & Assert
        externalLocation.FileEventQueue.Should().NotBeNull();
        externalLocation.FileEventQueue!.ManagedAqs.Should().NotBeNull();
        externalLocation.FileEventQueue.ManagedAqs!.QueueUrl.Should().Be("https://test.queue.amazonaws.com/123456789012/test-queue");
        externalLocation.FileEventQueue.ManagedAqs.ResourceGroup.Should().Be("test-resource-group");
        externalLocation.FileEventQueue.ManagedAqs.SubscriptionId.Should().Be("test-subscription-id");
    }

    [Theory]
    [InlineData("s3://bucket/path")]
    [InlineData("abfss://container@storage.dfs.core.windows.net/path")]
    [InlineData("gs://bucket/path")]
    public void ExternalLocation_ShouldAcceptVariousUrls(string url)
    {
        // Arrange
        var externalLocation = new UnityExternalLocation
        {
            Name = "test-location",
            Url = url,
            CredentialName = "test-credential"
        };

        // Act & Assert
        externalLocation.Url.Should().Be(url);
    }
}
