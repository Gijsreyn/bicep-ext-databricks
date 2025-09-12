# Contributing to Bicep Databricks extension

<!-- markdownlint-disable MD007 -->
- [Contributing to Bicep Databricks extension](#contributing-to-bicep-databricks-extension)
  - [Contribution Workflow](#contribution-workflow)
  - [Changelog](#changelog)
  - [Contributing documentation](#contributing-documentation)
  - [Adding a new resource](#adding-a-new-resource)
  - [Unit tests](#unit-tests)
    - [Test Structure](#test-structure)
    - [Writing Tests](#writing-tests)
    - [Test Dependencies](#test-dependencies)
    - [Running Tests](#running-tests)
  - [Code conventions](#code-conventions)
    - [General Guidelines](#general-guidelines)
    - [Project Structure](#project-structure)
    - [Handler Conventions](#handler-conventions)
    - [Model Conventions](#model-conventions)

Every contribution is welcome to the Bicep Databricks extension.
We make use of GitHub issues to track reported issues by the community.
GitHub pull request are used to merge in code changes.

## Contribution Workflow

Code contributions follow a GitHub-centered workflow. To participate in
the development of the Bicep Databricks extension, you require a GitHub account first.

Then, you can follow the steps below:

1. Fork this repo by going to the project repo page and use the _Fork` button.
2. Clone down the repo to your local system

    ```bash
    git clone https://github.com/<username>/bicep-ext-databricks.git
    ```

3. Create a new branch to hold your code changes you want to make:

    ```bash
    git checkout -b branch-name
    ```

4. Work on your code and write tests if applicable.

When you are done with your work, make sure you commit the changes to
your branch. Then, you can open a pull request on this repository.

## Changelog

Every PR that adds functionality to the Bicep Databricks extension must
include a short summary of changes in the `CHANGELOG.md` file. This file
is used to determine the next version.

Changelog entries should look something like:

```plaintext
* Added new handler to perform x operation on Databricks [#123](https://github.com/Gijsreyn/issues/123)
```

Feel free to add extra content or links if needed.

## Contributing documentation

Documentation doesn't require the `CHANGELOG.md` to be updated. Every update
to the documentation is welcome! Is there an example missing? Are there spelling
mistakes? Isn't the information accurate? Feel free to go through the [docs][00].

## Adding a new resource

To add a new resource:

1. Create a new directory or add a new handler in the `Handlers` directory.
2. Implement the model in the `Models` directory.
3. Add unit tests in the `tests` folder.
4. Run the tests locally by executing `./build.ps1 -Configuration Release -Test`.
5. Open a PR for review.

> [!NOTE]
> The directory structure follows the REST API reference on the [Databricks site][01].

## Unit tests

The Bicep Databricks extension uses .NET unit tests with xUnit framework to ensure
code quality and reliability. All tests are located in the `Databricks.Extension.Tests`
project under the `src` directory.

### Test Structure

Tests are organized by functionality and follow a consistent pattern:

- **Handler Tests**: Located in `Handlers/` subdirectories matching the main  
  project structure.
- **Model Tests**: Validate model properties, enums, and data integrity.
- **Test Base Classes**: `HandlerTestBase<THandler, TModel, TIdentifiers>`  
  provides common functionality.

### Writing Tests

When adding new handlers or models, follow these guidelines:

1. **Test File Naming**: Use the pattern `Databricks{HandlerName}HandlerTests.cs`.
2. **Test Method Naming**: Use descriptive names like `Constructor_ShouldCreateInstanceWithLogger()`.
3. **Test Categories**: Group tests by functionality (constructor, properties, validation).
4. **Azure Focus**: Include Azure Databricks-specific scenarios in test data.

Example test structure:

```csharp
public class DatabricksExampleHandlerTests : HandlerTestBase<DatabricksExampleHandler, Example, ExampleIdentifiers>
{
    [Fact]
    public void Constructor_ShouldCreateInstanceWithLogger()
    {
        // Arrange & Act
        var handler = new DatabricksExampleHandler(MockLogger.Object);

        // Assert
        handler.Should().NotBeNull();
    }

    [Theory]
    [InlineData("test-value-1")]
    [InlineData("test-value-2")]
    public void PropertyName_ShouldBeSetCorrectly(string value)
    {
        // Arrange
        var model = new Example { PropertyName = value };

        // Act & Assert
        model.PropertyName.Should().Be(value);
    }
}
```

### Test Dependencies

The test project uses the following packages:

- **xUnit**: Primary testing framework
- **FluentAssertions**: Readable assertion library
- **Moq**: Mocking framework for dependencies
- **Microsoft.Extensions.Diagnostics.Testing**: Logging test utilities

### Running Tests

Execute tests using the build script:

```powershell
./build.ps1 -Configuration Release -Test
```

For development, you can also run tests directly:

```powershell
dotnet test src/Databricks.Extension.sln --verbosity normal
```

## Code conventions

The Bicep Databricks extension follows C# and .NET coding standards with
specific conventions for Azure Databricks integration.

### General Guidelines

- **Language Version**: Use C# 12 features and latest .NET 9.0 capabilities
- **Nullable Reference Types**: Enabled project-wide for better null safety
- **Async/Await**: Use async patterns consistently for I/O operations
- **Naming**: Follow PascalCase for public members, camelCase for private fields

### Project Structure

Organize code following the Azure Databricks REST API structure:

```plaintext
src/Databricks.Extension/
├── Handlers/
│   ├── Compute/          # Cluster management
│   ├── UnityCatalog/     # Unity Catalog resources
│   └── Workspace/        # Workspace operations
├── Models/
│   ├── Compute/          # Cluster models
│   ├── UnityCatalog/     # Unity Catalog models
│   └── Workspace/        # Workspace models
└── Program.cs            # Entry point
```

### Handler Conventions

All handlers must:

1. **Inherit**: Extend `DatabricksResourceHandlerBase<TResource, TIdentifiers>`
2. **Constructor**: Accept `ILogger<THandler>` parameter
3. **Endpoints**: Use const fields for API endpoint definitions
4. **Error Handling**: Implement proper exception handling and logging

Example handler structure:

```csharp
public class DatabricksExampleHandler : DatabricksResourceHandlerBase<Example, ExampleIdentifiers>
{
    private const string ApiEndpoint = "2.0/example/endpoint";
    
    public DatabricksExampleHandler(ILogger<DatabricksExampleHandler> logger) : base(logger) { }
    
    // Implementation methods...
}
```

### Model Conventions

Models should follow these patterns:

- **Properties**: Use nullable reference types appropriately.
- **Attributes**: Apply `TypeProperty` attributes for Bicep integration.
- **Enums**: Use string enums with `JsonStringEnumConverter` for API compatibility.
- **Identifiers**: Create separate identifier classes inheriting from base models.

Example model structure:

```csharp
[ResourceType("Example")]
public class Example : ExampleIdentifiers
{
    [TypeProperty("Description of the property.", ObjectTypePropertyFlags.ReadOnly)]
    public string? ReadOnlyProperty { get; set; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ExampleType? Type { get; set; }
}

public class ExampleIdentifiers
{
    [TypeProperty("Required identifier.", ObjectTypePropertyFlags.Required | ObjectTypePropertyFlags.Identifier)]
    public required string Id { get; set; }
}
```

<!-- Link reference definitions -->
[00]: ./docs/index.md
[01]: https://docs.databricks.com/api/azure/workspace/introduction
