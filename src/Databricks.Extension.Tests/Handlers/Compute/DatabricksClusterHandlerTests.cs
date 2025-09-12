using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Databricks.Handlers.Compute;
using Databricks.Models.Compute;

namespace Databricks.Extension.Tests.Handlers.Compute;

public class DatabricksClusterHandlerTests : HandlerTestBase<DatabricksClusterHandler, Cluster, ClusterIdentifiers>
{
    [Fact]
    public void Constructor_ShouldCreateInstanceWithLogger()
    {
        // Arrange & Act
        var handler = new DatabricksClusterHandler(MockLogger.Object);

        // Assert
        handler.Should().NotBeNull();
    }

    [Fact]
    public void ClusterProperties_ShouldBeSetCorrectly()
    {
        // Arrange
        var cluster = new Cluster
        {
            ClusterName = "test-cluster",
            SparkVersion = "13.3.x-scala2.12",
            NodeTypeId = "Standard_D3_v2",
            NumWorkers = 4,
            AutoterminationMinutes = 60,
            DataSecurityMode = "SINGLE_USER",
            SingleUserName = "test@databricks.com"
        };

        // Act & Assert
        cluster.ClusterName.Should().Be("test-cluster");
        cluster.SparkVersion.Should().Be("13.3.x-scala2.12");
        cluster.NodeTypeId.Should().Be("Standard_D3_v2");
        cluster.NumWorkers.Should().Be(4);
        cluster.AutoterminationMinutes.Should().Be(60);
        cluster.DataSecurityMode.Should().Be("SINGLE_USER");
        cluster.SingleUserName.Should().Be("test@databricks.com");
    }

    [Fact]
    public void AutoScaleConfiguration_ShouldBeSetCorrectly()
    {
        // Arrange
        var autoScale = new AutoScale
        {
            MinWorkers = 2,
            MaxWorkers = 8
        };

        var cluster = new Cluster
        {
            ClusterName = "auto-scale-cluster",
            SparkVersion = "13.3.x-scala2.12",
            NodeTypeId = "Standard_D3_v2",
            AutoScale = autoScale
        };

        // Act & Assert
        cluster.AutoScale.Should().NotBeNull();
        cluster.AutoScale!.MinWorkers.Should().Be(2);
        cluster.AutoScale.MaxWorkers.Should().Be(8);
    }

    [Fact]
    public void AzureAttributes_ShouldBeSetCorrectly()
    {
        // Arrange
        var azureAttributes = new AzureAttributes
        {
            FirstOnDemand = 1,
            Availability = "SPOT_AZURE",
            SpotBidMaxPrice = 100
        };

        var cluster = new Cluster
        {
            ClusterName = "azure-cluster",
            SparkVersion = "13.3.x-scala2.12",
            NodeTypeId = "Standard_D3_v2",
            NumWorkers = 2,
            AzureAttributes = azureAttributes
        };

        // Act & Assert
        cluster.AzureAttributes.Should().NotBeNull();
        cluster.AzureAttributes!.FirstOnDemand.Should().Be(1);
        cluster.AzureAttributes.Availability.Should().Be("SPOT_AZURE");
        cluster.AzureAttributes.SpotBidMaxPrice.Should().Be(100);
    }

    [Theory]
    [InlineData("dev-cluster")]
    [InlineData("prod-cluster")]
    [InlineData("analytics-cluster")]
    public void ClusterIdentifiers_ShouldBeSetCorrectly(string clusterName)
    {
        // Arrange
        var identifiers = new ClusterIdentifiers
        {
            ClusterName = clusterName
        };

        // Act & Assert
        identifiers.ClusterName.Should().Be(clusterName);
    }

    [Fact]
    public void ReadOnlyProperties_ShouldBeSettable()
    {
        // Arrange
        var cluster = new Cluster
        {
            ClusterName = "test-cluster",
            SparkVersion = "13.3.x-scala2.12",
            NodeTypeId = "Standard_D3_v2",
            NumWorkers = 2,
            // Setting read-only properties (would typically come from API responses)
            State = "RUNNING",
            StateMessage = "Cluster is running",
            JdbcPort = 10000,
            ClusterCores = 8,
            ClusterMemoryMb = 28672,
            StartTime = 1640995200,
            CreatorUserName = "test@databricks.com"
        };

        // Act & Assert
        cluster.State.Should().Be("RUNNING");
        cluster.StateMessage.Should().Be("Cluster is running");
        cluster.JdbcPort.Should().Be(10000);
        cluster.ClusterCores.Should().Be(8);
        cluster.ClusterMemoryMb.Should().Be(28672);
        cluster.StartTime.Should().Be(1640995200);
        cluster.CreatorUserName.Should().Be("test@databricks.com");
    }
}
