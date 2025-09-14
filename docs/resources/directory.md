---
category: "Workspace"
---

# Directory

Represents a directory in the Databricks workspace.

## Example usage

### Creating a directory

This example shows how to create a directory in the Databricks workspace.

```bicep
resource directory 'Directory' = {
  path: '/Users/fake@example.com/directory'
}
```

## Argument reference

The following arguments are available:

- `path` - (Required) The path of the directory.

## Attribute reference

In addition to all arguments above, the following attributes are outputted:

- `objectId` - The object id of the directory.
- `objectType` - The object type of the directory. (Can be `NOTEBOOK`, `DIRECTORY`, `LIBRARY`, `FILE`, `REPO`, or `DASHBOARD`)
- `size` - The size of the directory (if provided by the API).

## Notes

When working with the `Directory` resource, ensure you have the extension imported in your Bicep file:

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
```

## Additional reference

For more information, see the following links:

- [Databricks Workspace API documentation][00]

<!-- Link reference definitions -->
[00]: https://docs.databricks.com/api/azure/workspace/workspace/mkdirs
