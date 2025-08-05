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
}
