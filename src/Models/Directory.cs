using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Databricks;

public class DirectoryIdentifiers
{
    [TypeProperty("The path of the directory in Databricks workspace.", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Path { get; set; }
}

[ResourceType("Directory")]
public class Directory : DirectoryIdentifiers
{
    [TypeProperty("The object type in Databricks.", ObjectTypePropertyFlags.ReadOnly)]
    public int ObjectType { get; set; }

    [TypeProperty("The object ID in Databricks.", ObjectTypePropertyFlags.ReadOnly)]
    public string ObjectId { get; set; } = string.Empty;

    [TypeProperty("The size of the object.", ObjectTypePropertyFlags.ReadOnly)]
    public string Size { get; set; } = string.Empty;
}
