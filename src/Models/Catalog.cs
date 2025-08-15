using System.Text.Json.Serialization;
using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Databricks;

public class KeyValuePair
{
    [TypeProperty("The key name.", ObjectTypePropertyFlags.Required)]
    public string Key { get; set; } = string.Empty;

    [TypeProperty("The value.", ObjectTypePropertyFlags.Required)]
    public string Value { get; set; } = string.Empty;
}

public class CatalogIdentifiers
{
    [TypeProperty("The name of the catalog.", ObjectTypePropertyFlags.Identifier)]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public record ProvisioningInfo
{
    [JsonPropertyName("state")]
    public string? State { get; set; }
}

[ResourceType("Catalog")]
public class Catalog : CatalogIdentifiers
{
    [TypeProperty("The comment/description for the catalog.", ObjectTypePropertyFlags.None)]
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [TypeProperty("Storage root path for managed tables within catalog.", ObjectTypePropertyFlags.None)]
    [JsonPropertyName("storage_root")]
    public string? StorageRoot { get; set; }

    [TypeProperty("The name of the connection for foreign catalogs.", ObjectTypePropertyFlags.None)]
    [JsonPropertyName("connection_name")]
    public string? ConnectionName { get; set; }

    [TypeProperty("Delta sharing provider name for foreign catalogs.", ObjectTypePropertyFlags.None)]
    [JsonPropertyName("provider_name")]
    public string? ProviderName { get; set; }

    [TypeProperty("Delta sharing share name for foreign catalogs.", ObjectTypePropertyFlags.None)]
    [JsonPropertyName("share_name")]
    public string? ShareName { get; set; }

    [TypeProperty("Options for the catalog as key-value pairs.", ObjectTypePropertyFlags.None)]
    public KeyValuePair[]? Options { get; set; }

    [TypeProperty("Properties for the catalog as key-value pairs.", ObjectTypePropertyFlags.None)]
    [JsonPropertyName("properties")]
    public KeyValuePair[]? Properties { get; set; }

    [TypeProperty("Whether to enable predictive optimization.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("enable_predictive_optimization")]
    public string? EnablePredictiveOptimization { get; set; }

    [TypeProperty("Whether to force destroy the catalog.", ObjectTypePropertyFlags.ReadOnly)]
    public bool ForceDestroy { get; set; } = false;

    [TypeProperty("The owner of the catalog.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("owner")]
    public string Owner { get; set; } = string.Empty;

    [TypeProperty("The metastore ID of the catalog.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("metastore_id")]
    public string MetastoreId { get; set; } = string.Empty;

    [TypeProperty("The creation timestamp of the catalog.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [TypeProperty("The user who created the catalog.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("created_by")]
    public string CreatedBy { get; set; } = string.Empty;

    [TypeProperty("The last update timestamp of the catalog.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }

    [TypeProperty("The user who last updated the catalog.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("updated_by")]
    public string UpdatedBy { get; set; } = string.Empty;

    [TypeProperty("The type of the catalog.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("catalog_type")]
    public string? CatalogType { get; set; }

    [TypeProperty("The storage location of the catalog.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("storage_location")]
    public string StorageLocation { get; set; } = string.Empty;

    [TypeProperty("The isolation mode of the catalog.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("isolation_mode")]
    public string? IsolationMode { get; set; }

    [TypeProperty("The full name of the catalog.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [TypeProperty("The securable kind of the catalog.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("securable_kind")]
    public string? CatalogSecurableKind { get; set; }

    [TypeProperty("The securable type of the catalog.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("securable_type")]
    public string SecurableType { get; set; } = "CATALOG";

    [TypeProperty("Provisioning information of the catalog.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("provisioning_info")]
    public ProvisioningInfo? ProvisioningInfo { get; set; }

    [TypeProperty("Whether the catalog is browse only.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("browse_only")]
    public bool BrowseOnly { get; set; }
}
