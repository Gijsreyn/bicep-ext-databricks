using System;
using System.Text.Json.Serialization;
using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Databricks;

[ResourceType("ExternalLocation")]
public class ExternalLocation : ExternalLocationIdentifiers
{
    [TypeProperty("Comment for the external location.")]
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [TypeProperty("Indicates whether the location is read-only.")]
    [JsonPropertyName("read_only")]
    public bool ReadOnly { get; set; }

    [TypeProperty("Indicates whether fallback mode is enabled for this external location.")]
    [JsonPropertyName("fallback_mode")]
    public bool FallbackMode { get; set; }

    [TypeProperty("Skips validation of the storage credential associated with this external location.")]
    [JsonPropertyName("skip_validation")]
    public bool SkipValidation { get; set; }

    [TypeProperty("Owner of the external location.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("owner")]
    public string? Owner { get; set; }

    [TypeProperty("Metastore ID.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("metastore_id")]
    public string? MetastoreId { get; set; }

    [TypeProperty("Storage credential ID.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("credential_id")]
    public string? CredentialId { get; set; }

    [TypeProperty("Creation timestamp.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [TypeProperty("Creator username.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("created_by")]
    public string? CreatedBy { get; set; }

    [TypeProperty("Last update timestamp.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }

    [TypeProperty("Last updater username.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonPropertyName("updated_by")]
    public string? UpdatedBy { get; set; }
}

public class ExternalLocationIdentifiers
{
    [TypeProperty("The name of the external location.", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [TypeProperty("The URL to the external location root.", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [TypeProperty("The name of the storage credential to reference.", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string? CredentialName { get; set; } = string.Empty;
}