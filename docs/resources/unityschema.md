---
category: "Unity Catalog"
---

# UnitySchema

Represents a Unity Catalog schema for organizing tables, views, and other database objects.

## Example usage

### Creating a basic schema

This example shows how to create a basic schema in a Unity Catalog.

```bicep
resource schema 'UnitySchema' = {
  name: 'analytics'
  catalogName: 'my_catalog'
  comment: 'Schema for analytics data and models'
  owner: 'analytics-team@company.com'
  enablePredictiveOptimization: 'ENABLE'
}
```

### Creating a schema with custom storage

This example shows how to create a schema with a custom storage location.

```bicep
resource schemaWithStorage 'UnitySchema' = {
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
```

### Creating a schema for machine learning

This example shows how to create a schema specifically for machine learning workflows.

```bicep
resource mlSchema 'UnitySchema' = {
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
```

### Creating a schema for raw data

This example shows how to create a schema for raw data ingestion.

```bicep
resource rawDataSchema 'UnitySchema' = {
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
```

## Argument reference

The following arguments are available:

- `catalogName` - (Required) The name of the catalog.
- `name` - (Required) The name of the schema.
- `comment` - (Optional) User-provided free-form text description.
- `enablePredictiveOptimization` - (Optional) Whether predictive optimization is enabled for the schema.
- `owner` - (Optional) Username of current owner of schema.
- `properties` - (Optional) A map of key-value properties attached to the securable.
- `storageRoot` - (Optional) Storage root URL for the schema.

## Attribute reference

In addition to all arguments above, the following attributes are outputted:

- `browseOnly` - Whether this schema can only be browsed.
- `catalogType` - The type of the catalog.
- `createdAt` - Time at which this schema was created, in epoch milliseconds.
- `createdBy` - Username of schema creator.
- `effectivePredictiveOptimizationFlag` - Effective predictive optimization flag for the schema:
  - `inheritedFromName` - The name from which the flag is inherited.
  - `inheritedFromType` - The type from which the flag is inherited.
  - `value` - The effective predictive optimization flag value.
- `fullName` - The full name of the schema.
- `metastoreId` - Unique identifier of the metastore for the schema.
- `schemaId` - Unique identifier of the schema.
- `storageLocation` - Storage location for the schema.
- `updatedAt` - Time at which this schema was last modified, in epoch milliseconds.
- `updatedBy` - Username of user who last modified schema.

## Notes

When working with the 'UnitySchema' resource, ensure you have the extension imported in your Bicep file:

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
- Consider data tiering strategies (bronze/silver/gold) when organizing schemas

## Additional reference

For more information, see the following links:

- [Unity Catalog schemas API documentation][00]
- [Schema management in Unity Catalog][01]
- [Data organization with Unity Catalog][02]
- [Predictive optimization in Unity Catalog][03]

<!-- Link reference definitions -->
[00]: https://docs.databricks.com/api/azure/workspace/schemas/create
[01]: https://docs.databricks.com/data-governance/unity-catalog/create-schemas.html
[02]: https://docs.databricks.com/data-governance/unity-catalog/index.html
[03]: https://docs.databricks.com/optimizations/predictive-optimization.html

