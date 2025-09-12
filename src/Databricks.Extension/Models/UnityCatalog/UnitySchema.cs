using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;
using System.Text.Json.Serialization;

namespace Databricks.Models.UnityCatalog;

[ResourceType("UnitySchema")]
public class UnitySchema : UnitySchemaIdentifiers
{
    // Configuration properties
    [TypeProperty("The name of the catalog.", ObjectTypePropertyFlags.Required)]
    public string CatalogName { get; set; } = string.Empty;

    [TypeProperty("User-provided free-form text description.", ObjectTypePropertyFlags.None)]
    public string? Comment { get; set; }

    [TypeProperty("A map of key-value properties attached to the securable.", ObjectTypePropertyFlags.None)]
    public object? Properties { get; set; }

    [TypeProperty("Storage root URL for the schema.", ObjectTypePropertyFlags.None)]
    public string? StorageRoot { get; set; }

    // Read-only outputs
    [TypeProperty("Whether this schema can only be browsed.", ObjectTypePropertyFlags.ReadOnly)]
    public bool BrowseOnly { get; set; }

    [TypeProperty("The type of the catalog.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CatalogType? CatalogType { get; set; }

    [TypeProperty("Time at which this schema was created, in epoch milliseconds.", ObjectTypePropertyFlags.ReadOnly)]
    public int CreatedAt { get; set; }

    [TypeProperty("Username of schema creator.", ObjectTypePropertyFlags.ReadOnly)]
    public string? CreatedBy { get; set; }

    [TypeProperty("Effective predictive optimization flag for the schema.", ObjectTypePropertyFlags.ReadOnly)]
    public SchemaEffectivePredictiveOptimizationFlag? EffectivePredictiveOptimizationFlag { get; set; }

    [TypeProperty("Whether predictive optimization is enabled for the schema.", ObjectTypePropertyFlags.None)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PredictiveOptimizationFlag? EnablePredictiveOptimization { get; set; }

    [TypeProperty("The full name of the schema.", ObjectTypePropertyFlags.ReadOnly)]
    public string? FullName { get; set; }

    [TypeProperty("Unique identifier of the metastore for the schema.", ObjectTypePropertyFlags.ReadOnly)]
    public string? MetastoreId { get; set; }

    [TypeProperty("Username of current owner of schema.", ObjectTypePropertyFlags.None)]
    public string? Owner { get; set; }

    [TypeProperty("Unique identifier of the schema.", ObjectTypePropertyFlags.ReadOnly)]
    public string? SchemaId { get; set; }

    [TypeProperty("Storage location for the schema.", ObjectTypePropertyFlags.ReadOnly)]
    public string? StorageLocation { get; set; }

    [TypeProperty("Time at which this schema was last modified, in epoch milliseconds.", ObjectTypePropertyFlags.ReadOnly)]
    public int UpdatedAt { get; set; }

    [TypeProperty("Username of user who last modified schema.", ObjectTypePropertyFlags.ReadOnly)]
    public string? UpdatedBy { get; set; }
}

public class UnitySchemaIdentifiers
{
    [TypeProperty("The name of the schema.", ObjectTypePropertyFlags.Required)]
    public string Name { get; set; } = string.Empty;
}

public class SchemaEffectivePredictiveOptimizationFlag
{
    [TypeProperty("The name from which the flag is inherited.", ObjectTypePropertyFlags.ReadOnly)]
    public string? InheritedFromName { get; set; }

    [TypeProperty("The type from which the flag is inherited.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public InheritedFromType? InheritedFromType { get; set; }

    [TypeProperty("The effective predictive optimization flag value.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PredictiveOptimizationFlag? Value { get; set; }
}
