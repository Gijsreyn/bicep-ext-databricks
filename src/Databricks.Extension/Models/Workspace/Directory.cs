using Azure.Bicep.Types.Concrete;
using System.Text.Json.Serialization;

namespace Databricks.Models.Workspace;

public enum ObjectType
{
    NOTEBOOK,
    DIRECTORY,
    LIBRARY,
    FILE,
    REPO,
    DASHBOARD
}

[BicepFrontMatter("category", "Workspace")]
[BicepDocHeading("Directory", "Represents a directory in the Databricks workspace.")]
[BicepDocExample(
    "Creating a directory",
    "This example shows how to create a directory in the Databricks workspace.",
    @"resource directory 'Directory' = {
  path: '/Users/fake@example.com/directory'
}
"
)]
[BicepDocCustom("Notes", @"When working with the `Directory` resource, ensure you have the extension imported in your Bicep file:

```bicep
// main.bicep
targetScope = 'local'
param workspaceUrl string
extension databricksExtension with {
  workspaceUrl: workspaceUrl
}

// main.bicepparam
using 'main.bicep'
param workspaceUrl = '<workspaceUrl>'
```")]
[BicepDocCustom("Additional reference", @"For more information, see the following links:

- [Databricks Workspace API documentation][00]

<!-- Link reference definitions -->
[00]: https://docs.databricks.com/api/azure/workspace/workspace/mkdirs")]
[ResourceType("Directory")]
public class Directory : DirectoryIdentifiers
{
	// Outputs
    [TypeProperty("The object type of the directory.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ObjectType? ObjectType { get; set; }

    [TypeProperty("The object id of the directory.", ObjectTypePropertyFlags.ReadOnly)]
    public string? ObjectId { get; set; }

    [TypeProperty("The size of the directory (if provided by the API).", ObjectTypePropertyFlags.ReadOnly)]
    public string? Size { get; set; }
}

public class DirectoryIdentifiers
{
    [TypeProperty("The path of the directory.", ObjectTypePropertyFlags.Required | ObjectTypePropertyFlags.Identifier)]
    public required string Path { get; set; }
}
