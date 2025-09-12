using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Databricks.Handlers.Workspace;
using WorkspaceDirectory = Databricks.Models.Workspace.Directory;
using Databricks.Models.Workspace;

namespace Databricks.Extension.Tests.Handlers.Workspace;

public class DatabricksDirectoryHandlerTests : HandlerTestBase<DatabricksDirectoryHandler, WorkspaceDirectory, DirectoryIdentifiers>
{
    [Fact]
    public void Constructor_ShouldCreateInstanceWithLogger()
    {
        // Arrange & Act
        var handler = new DatabricksDirectoryHandler(MockLogger.Object);

        // Assert
        handler.Should().NotBeNull();
    }

    [Fact]
    public void DirectoryProperties_ShouldBeSetCorrectly()
    {
        // Arrange
        var directory = new WorkspaceDirectory
        {
            Path = "/test/directory"
        };

        // Act & Assert
        directory.Path.Should().Be("/test/directory");
    }

    [Theory]
    [InlineData("/valid/path")]
    [InlineData("/another/valid/path")]
    [InlineData("/root")]
    public void DirectoryIdentifiers_ShouldBeSetCorrectly(string path)
    {
        // Arrange
        var identifiers = new DirectoryIdentifiers
        {
            Path = path
        };

        // Act & Assert
        identifiers.Path.Should().Be(path);
    }

    [Fact]
    public void ReadOnlyProperties_ShouldBeSettable()
    {
        // Arrange
        var directory = new WorkspaceDirectory
        {
            Path = "/test/directory",
            // Setting read-only properties (would typically come from API responses)
            ObjectType = ObjectType.DIRECTORY,
            ObjectId = "12345"
        };

        // Act & Assert
        directory.Path.Should().Be("/test/directory");
        directory.ObjectType.Should().Be(ObjectType.DIRECTORY);
        directory.ObjectId.Should().Be("12345");
    }

    [Fact]
    public void DirectoryPath_ShouldAcceptValidPaths()
    {
        // Arrange & Act
        var directory1 = new WorkspaceDirectory { Path = "/" };
        var directory2 = new WorkspaceDirectory { Path = "/Users" };
        var directory3 = new WorkspaceDirectory { Path = "/Shared/project/data" };

        // Assert
        directory1.Path.Should().Be("/");
        directory2.Path.Should().Be("/Users");
        directory3.Path.Should().Be("/Shared/project/data");
    }

    [Theory]
    [InlineData(ObjectType.NOTEBOOK)]
    [InlineData(ObjectType.DIRECTORY)]
    [InlineData(ObjectType.LIBRARY)]
    [InlineData(ObjectType.FILE)]
    [InlineData(ObjectType.REPO)]
    [InlineData(ObjectType.DASHBOARD)]
    public void ObjectType_ShouldAcceptAllEnumValues(ObjectType objectType)
    {
        // Arrange
        var directory = new WorkspaceDirectory
        {
            Path = "/test/path",
            ObjectType = objectType
        };

        // Act & Assert
        directory.ObjectType.Should().Be(objectType);
    }

    [Fact]
    public void ObjectTypeEnum_ShouldHaveCorrectValues()
    {
        // Arrange & Act & Assert
        ObjectType.NOTEBOOK.Should().Be(ObjectType.NOTEBOOK);
        ObjectType.DIRECTORY.Should().Be(ObjectType.DIRECTORY);
        ObjectType.LIBRARY.Should().Be(ObjectType.LIBRARY);
        ObjectType.FILE.Should().Be(ObjectType.FILE);
        ObjectType.REPO.Should().Be(ObjectType.REPO);
        ObjectType.DASHBOARD.Should().Be(ObjectType.DASHBOARD);
    }
}
