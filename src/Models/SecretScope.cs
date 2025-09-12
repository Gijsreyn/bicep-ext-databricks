using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;
using System.Text.Json.Serialization;

namespace Databricks.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SecretScopeBackendType
{
    DATABRICKS,
    AZURE_KEYVAULT
}

[ResourceType("SecretScope")]
public class SecretScope : SecretScopeIdentifiers
{
    // Configuration properties
    [TypeProperty("The backend type for the secret scope. Either 'DATABRICKS' or 'AZURE_KEYVAULT'.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SecretScopeBackendType? BackendType { get; set; }

    [TypeProperty("Azure Key Vault metadata if using Azure Key Vault backend.", ObjectTypePropertyFlags.None)]
    public AzureKeyVaultMetadata? KeyVaultMetadata { get; set; }

    [TypeProperty("The principal that is initially granted MANAGE permission to the created scope.", ObjectTypePropertyFlags.None)]
    public string? InitialManagePrincipal { get; set; }
}

public class SecretScopeIdentifiers
{
    [TypeProperty("The name of the secret scope. Must consist of alphanumeric characters, dashes, underscores, and periods, and may not exceed 128 characters.", ObjectTypePropertyFlags.Required)]
    public string ScopeName { get; set; } = string.Empty;
}

public class AzureKeyVaultMetadata
{
    [TypeProperty("The resource ID of the Azure Key Vault.", ObjectTypePropertyFlags.Required)]
    public string ResourceId { get; set; } = string.Empty;

    [TypeProperty("The DNS name of the Azure Key Vault.", ObjectTypePropertyFlags.Required)]
    public string DnsName { get; set; } = string.Empty;
}
