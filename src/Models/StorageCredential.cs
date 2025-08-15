using Azure.Bicep.Types.Concrete;
using System.Text.Json.Serialization;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Databricks;

[ResourceType("StorageCredential")]
public class StorageCredential : StorageCredentialIdentifiers
{
    [TypeProperty("Comment associated with the storage credential.")]
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [TypeProperty("Whether the storage credential is read-only.")]
    [JsonPropertyName("read_only")]
    public bool ReadOnly { get; set; } = false;

    [TypeProperty("Whether to skip validation of the storage credential.")]
    [JsonPropertyName("skip_validation")]
    public bool SkipValidation { get; set; } = false;

    [TypeProperty("Azure Managed Identity configuration.")]
    public AzureManagedIdentity? AzureManagedIdentity { get; set; }

    [TypeProperty("Azure Service Principal configuration.")]
    public AzureServicePrincipal? AzureServicePrincipal { get; set; }

    [TypeProperty("The identity of the storage credential.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [TypeProperty("The owner of the storage credential.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("owner")]
    public string? Owner { get; set; }

    [TypeProperty("The metastore ID.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("metastore_id")]
    public string? MetastoreId { get; set; }

    [TypeProperty("Creation timestamp.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [TypeProperty("Created by user.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("created_by")]
    public string? CreatedBy { get; set; }

    [TypeProperty("Last updated timestamp.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }

    [TypeProperty("Last updated by user.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("updated_by")]
    public string? UpdatedBy { get; set; }

    [TypeProperty("Whether the storage credential is isolated.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("isolation_mode")]
    public bool Isolated { get; set; }

    [TypeProperty("The full name of the storage credential.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    [TypeProperty("Whether the storage credential is used for managed storage.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("used_for_managed_storage")]
    public bool UsedForManagedStorage { get; set; }
}

public class StorageCredentialIdentifiers
{
    [TypeProperty("The name of the storage credential.", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class AzureManagedIdentity
{
    [TypeProperty("The access connector ID for the managed identity.", ObjectTypePropertyFlags.Required)]
    public required string AccessConnectorId { get; set; }

    [TypeProperty("The managed identity ID.")]
    public string? ManagedIdentityId { get; set; }
}

public class AzureServicePrincipal
{
    [TypeProperty("The application ID of the service principal.", ObjectTypePropertyFlags.Required)]
    public required string ApplicationId { get; set; }

    [TypeProperty("The client secret of the service principal.", ObjectTypePropertyFlags.Required)]
    public required string ClientSecret { get; set; }

    [TypeProperty("The directory (tenant) ID.", ObjectTypePropertyFlags.Required)]
    public required string DirectoryId { get; set; }
}
