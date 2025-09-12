using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;
using System.Text.Json.Serialization;

namespace Databricks.Models.UnityCatalog;

public enum CatalogType
{
    MANAGED_CATALOG,
    DELTASHARING_CATALOG,
    FOREIGN_CATALOG,
    SYSTEM_CATALOG
}

public enum IsolationMode
{
    OPEN,
    ISOLATED
}

public enum PredictiveOptimizationFlag
{
    DISABLE,
    ENABLE,
    INHERIT
}

public enum SecurableType
{
    CATALOG,
    SCHEMA,
    TABLE,
    VOLUME
}

public enum ProvisioningState
{
    PROVISIONING,
    PROVISIONED,
    FAILED
}

public enum InheritedFromType
{
    CATALOG,
    SCHEMA,
    TABLE
}

[ResourceType("UnityCatalog")]
public class UnityCatalog : UnityCatalogIdentifiers
{
    // Configuration properties
    [TypeProperty("User-provided free-form text description.", ObjectTypePropertyFlags.None)]
    public string? Comment { get; set; }

    [TypeProperty("The name of the connection to an external data source.", ObjectTypePropertyFlags.None)]
    public string? ConnectionName { get; set; }

    [TypeProperty("A map of key-value properties attached to the securable.", ObjectTypePropertyFlags.None)]
    public object? Options { get; set; }

    [TypeProperty("A map of key-value properties attached to the securable.", ObjectTypePropertyFlags.None)]
    public object? Properties { get; set; }

    [TypeProperty("For catalogs corresponding to a share: the name of the provider.", ObjectTypePropertyFlags.None)]
    public string? ProviderName { get; set; }

    [TypeProperty("For catalogs corresponding to a share: the name of the share.", ObjectTypePropertyFlags.None)]
    public string? ShareName { get; set; }

    [TypeProperty("Storage root URL for managed tables within catalog.", ObjectTypePropertyFlags.None)]
    public string? StorageRoot { get; set; }

    [TypeProperty("Whether to force destroy the catalog even if it contains schemas and tables.", ObjectTypePropertyFlags.None)]
    public bool ForceDestroy { get; set; }

    // Read-only outputs
    [TypeProperty("Whether the catalog is accessible from all workspaces or a specific set of workspaces.", ObjectTypePropertyFlags.ReadOnly)]
    public bool BrowseOnly { get; set; }

    [TypeProperty("The type of the catalog.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CatalogType? CatalogType { get; set; }

    [TypeProperty("Time at which this catalog was created, in epoch milliseconds.", ObjectTypePropertyFlags.ReadOnly)]
    public int CreatedAt { get; set; }

    [TypeProperty("Username of catalog creator.", ObjectTypePropertyFlags.ReadOnly)]
    public string? CreatedBy { get; set; }

    [TypeProperty("The effective predictive optimization flag.", ObjectTypePropertyFlags.ReadOnly)]
    public EffectivePredictiveOptimizationFlag? EffectivePredictiveOptimizationFlag { get; set; }

    [TypeProperty("Whether predictive optimization should be enabled for this object and objects under it.", ObjectTypePropertyFlags.None)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PredictiveOptimizationFlag? EnablePredictiveOptimization { get; set; }

    [TypeProperty("The full name of the catalog.", ObjectTypePropertyFlags.ReadOnly)]
    public string? FullName { get; set; }

    [TypeProperty("Whether the catalog is accessible from all workspaces or a specific set of workspaces.", ObjectTypePropertyFlags.None)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IsolationMode? IsolationMode { get; set; }

    [TypeProperty("Unique identifier of the metastore for the catalog.", ObjectTypePropertyFlags.ReadOnly)]
    public string? MetastoreId { get; set; }

    [TypeProperty("Username of current owner of catalog.", ObjectTypePropertyFlags.None)]
    public string? Owner { get; set; }

    [TypeProperty("Provisioning info about the catalog.", ObjectTypePropertyFlags.ReadOnly)]
    public ProvisioningInfo? ProvisioningInfo { get; set; }

    [TypeProperty("The type of the securable.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SecurableType? SecurableType { get; set; }

    [TypeProperty("Path to the storage location.", ObjectTypePropertyFlags.ReadOnly)]
    public string? StorageLocation { get; set; }

    [TypeProperty("Time at which this catalog was last modified, in epoch milliseconds.", ObjectTypePropertyFlags.ReadOnly)]
    public int UpdatedAt { get; set; }

    [TypeProperty("Username of user who last modified catalog.", ObjectTypePropertyFlags.ReadOnly)]
    public string? UpdatedBy { get; set; }
}

public class UnityCatalogIdentifiers
{
    [TypeProperty("Name of catalog.", ObjectTypePropertyFlags.Required)]
    public string Name { get; set; } = string.Empty;
}

public class EffectivePredictiveOptimizationFlag
{
    [TypeProperty("The name of the object from which the flag was inherited.", ObjectTypePropertyFlags.ReadOnly)]
    public string? InheritedFromName { get; set; }

    [TypeProperty("The type of the object from which the flag was inherited.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public InheritedFromType? InheritedFromType { get; set; }

    [TypeProperty("The value of the effective predictive optimization flag.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PredictiveOptimizationFlag? Value { get; set; }
}

public class ProvisioningInfo
{
    [TypeProperty("The current provisioning state of the catalog.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProvisioningState? State { get; set; }
}
