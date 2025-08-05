using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Databricks;

public class SecretIdentifiers
{
    [TypeProperty("The name of the secret scope in Databricks.", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Scope { get; set; }

    [TypeProperty("The key name of the secret in the scope.", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Key { get; set; }
}

[ResourceType("Secret")]
public class Secret : SecretIdentifiers
{
    [TypeProperty("The string value of the secret. Mutually exclusive with bytes_value.", ObjectTypePropertyFlags.WriteOnly)]
    public string? StringValue { get; set; }

    [TypeProperty("The bytes value of the secret. Mutually exclusive with string_value.", ObjectTypePropertyFlags.WriteOnly)]
    public string? BytesValue { get; set; }
}