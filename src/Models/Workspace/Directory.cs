using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Databricks.Models.Workspace;

[ResourceType("Directory")]
public class Directory : DirectoryIdentifiers
{
	// Outputs
    [TypeProperty("The object type of the directory.", ObjectTypePropertyFlags.ReadOnly)]
    public int ObjectType { get; set; }

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
