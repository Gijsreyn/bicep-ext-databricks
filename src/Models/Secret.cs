using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Databricks.Models;

[ResourceType("Secret")]
public class Secret : SecretIdentifiers
{
    // Configuration properties
    [TypeProperty("The string value of the secret.", ObjectTypePropertyFlags.None)]
    public string? StringValue { get; set; }

    [TypeProperty("If specified, value will be stored as bytes.", ObjectTypePropertyFlags.None)]
    public string? BytesValue { get; set; }

    // Read-only outputs
    [TypeProperty("The last updated timestamp of the secret.", ObjectTypePropertyFlags.ReadOnly)]
    public int LastUpdatedTimestamp { get; set; }

    [TypeProperty("The configuration reference for the secret in the format {{secrets/scope/key}}.", ObjectTypePropertyFlags.ReadOnly)]
    public string? ConfigReference { get; set; }
}

public class SecretIdentifiers
{
    [TypeProperty("The name of the secret scope.", ObjectTypePropertyFlags.Required)]
    public string Scope { get; set; } = string.Empty;

    [TypeProperty("The key name of the secret.", ObjectTypePropertyFlags.Required)]
    public string Key { get; set; } = string.Empty;
}
