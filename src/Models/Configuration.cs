using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Databricks;

public class Configuration
{
    [TypeProperty("The Azure Databricks workspace URL.", ObjectTypePropertyFlags.Required)]
    public required string WorkspaceUrl { get; set; }
}
