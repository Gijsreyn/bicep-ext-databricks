using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;
using System.Text.Json.Serialization;

namespace Databricks.Models.UnityCatalog;


[BicepFrontMatter("category", "Unity Catalog")]
[BicepDocHeading("UnitySchema", "Represents a Unity Catalog schema for organizing tables, views, and other database objects.")]
[BicepDocExample(
    "Creating a basic schema",
    "This example shows how to create a basic schema in a Unity Catalog.",
    @"resource schema 'UnitySchema' = {
  name: 'analytics'
  catalogName: 'my_catalog'
  comment: 'Schema for analytics data and models'
  owner: 'analytics-team@company.com'
  enablePredictiveOptimization: 'ENABLE'
}
"
)]
[BicepDocExample(
    "Creating a schema with custom storage",
    "This example shows how to create a schema with a custom storage location.",
    @"resource schemaWithStorage 'UnitySchema' = {
  name: 'external_data'
  catalogName: 'analytics_catalog'
  comment: 'Schema for external data sources'
  storageRoot: 'abfss://schemas@mydatalake.dfs.core.windows.net/external_data'
  owner: 'data-engineering@company.com'
  enablePredictiveOptimization: 'INHERIT'
  properties: {
    department: 'data-engineering'
    environment: 'production'
    data_classification: 'internal'
  }
}
"
)]
[BicepDocExample(
    "Creating a schema for machine learning",
    "This example shows how to create a schema specifically for machine learning workflows.",
    @"resource mlSchema 'UnitySchema' = {
  name: 'ml_models'
  catalogName: 'ml_catalog'
  comment: 'Schema for machine learning models and experiments'
  owner: 'ml-platform@company.com'
  enablePredictiveOptimization: 'DISABLE'
  properties: {
    team: 'ml-platform'
    use_case: 'model_training'
    governance_tier: 'standard'
  }
}
"
)]
[BicepDocExample(
    "Creating a schema for raw data",
    "This example shows how to create a schema for raw data ingestion.",
    @"resource rawDataSchema 'UnitySchema' = {
  name: 'raw_data'
  catalogName: 'data_lake_catalog'
  comment: 'Schema for raw data ingestion from various sources'
  storageRoot: 'abfss://raw@datalake.dfs.core.windows.net/raw'
  owner: 'data-ingestion@company.com'
  properties: {
    data_tier: 'bronze'
    retention_days: '365'
    compression: 'gzip'
  }
}
"
)]
[BicepDocCustom("Notes", @"When working with the `UnitySchema` resource, ensure you have the extension imported in your Bicep file:

```bicep
// main.bicep
targetScope = 'local'
param workspaceUrl string
extension databricksExtension with {
  workspaceUrl: workspaceUrl
}

// main.bicepparam
using 'main.bicep'
param workspaceUrl = '<workspaceUrl>'
```

Please note the following important considerations when using the `UnitySchema` resource:

- The catalog specified in `catalogName` must exist before creating the schema
- Schema names must be unique within the catalog and follow naming conventions (alphanumeric and underscores)
- The `storageRoot` is optional; if not specified, the schema will use the catalog's default storage
- Predictive optimization settings can be inherited from the catalog or set explicitly
- Use meaningful properties to add metadata for governance and discovery
- Schema ownership determines who can manage the schema and grant permissions
- Consider data tiering strategies (bronze/silver/gold) when organizing schemas")]
[BicepDocCustom("Additional reference", @"For more information, see the following links:

- [Unity Catalog schemas API documentation][00]
- [Schema management in Unity Catalog][01]
- [Data organization with Unity Catalog][02]
- [Predictive optimization in Unity Catalog][03]

<!-- Link reference definitions -->
[00]: https://docs.databricks.com/api/azure/workspace/schemas/create
[01]: https://docs.databricks.com/data-governance/unity-catalog/create-schemas.html
[02]: https://docs.databricks.com/data-governance/unity-catalog/index.html
[03]: https://docs.databricks.com/optimizations/predictive-optimization.html")]
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
