---
category: "Unity Catalog"
---

# UnityCatalog

Represents a Unity Catalog in Databricks for data governance and management.

## Example usage

### Creating a basic managed catalog

This example shows how to create a basic managed Unity Catalog.

```bicep
resource catalog 'UnityCatalog' = {
  name: 'my_catalog'
  comment: 'Main data catalog for analytics'
  owner: 'data-team@company.com'
  enablePredictiveOptimization: 'ENABLE'
}
```

### Creating a catalog with storage root

This example shows how to create a catalog with a custom storage location.

```bicep
resource catalogWithStorage 'UnityCatalog' = {
  name: 'analytics_catalog'
  comment: 'Analytics catalog with custom storage'
  storageRoot: 'abfss://container@storageaccount.dfs.core.windows.net/catalogs/analytics'
  owner: 'analytics-team@company.com'
  isolationMode: 'ISOLATED'
  enablePredictiveOptimization: 'INHERIT'
  properties: {
    department: 'analytics'
    cost_center: 'CC123'
  }
}
```

### Creating a catalog for external data sharing

This example shows how to create a catalog connected to external data sharing.

```bicep
resource sharingCatalog 'UnityCatalog' = {
  name: 'external_data_catalog'
  comment: 'Catalog for external data sharing'
  connectionName: 'external-connection'
  providerName: 'external-provider'
  shareName: 'shared-dataset'
  owner: 'data-governance@company.com'
  forceDestroy: false
}
```

## Argument reference

The following arguments are available:

<!-- markdownlint-disable MD013 -->
- `name` - (Required) Name of catalog.
- `comment` - (Optional) User-provided free-form text description.
- `connectionName` - (Optional) The name of the connection to an external data source.
- `enablePredictiveOptimization` - (Optional) Whether predictive optimization should be enabled for this object and objects under it. (Can be `DISABLE`, `ENABLE`, or `INHERIT`)
- `forceDestroy` - (Optional) Whether to force destroy the catalog even if it contains schemas and tables.
- `isolationMode` - (Optional) Whether the catalog is accessible from all workspaces or a specific set of workspaces. (Can be `OPEN`, or `ISOLATED`)
- `options` - (Optional) A map of key-value properties attached to the securable.
- `owner` - (Optional) Username of current owner of catalog.
- `properties` - (Optional) A map of key-value properties attached to the securable.
- `providerName` - (Optional) For catalogs corresponding to a share: the name of the provider.
- `shareName` - (Optional) For catalogs corresponding to a share: the name of the share.
- `storageRoot` - (Optional) Storage root URL for managed tables within catalog.

## Attribute reference

In addition to all arguments above, the following attributes are outputted:

- `browseOnly` - Whether the catalog is accessible from all workspaces or a specific set of workspaces.
- `catalogType` - The type of the catalog. (Can be `MANAGED_CATALOG`, `DELTASHARING_CATALOG`, `FOREIGN_CATALOG`, or `SYSTEM_CATALOG`)
- `createdAt` - Time at which this catalog was created, in epoch milliseconds.
- `createdBy` - Username of catalog creator.
- `effectivePredictiveOptimizationFlag` - The effective predictive optimization flag. (Can be `DISABLE`, `ENABLE`, or `INHERIT`):
    - `inheritedFromName` - The name of the object from which the flag was inherited.
    - `inheritedFromType` - The type of the object from which the flag was inherited. (Can be `CATALOG`, `SCHEMA`, or `TABLE`)
    - `value` - The value of the effective predictive optimization flag. (Can be `DISABLE`, `ENABLE`, or `INHERIT`)
- `fullName` - The full name of the catalog.
- `metastoreId` - Unique identifier of the metastore for the catalog.
- `provisioningInfo` - Provisioning info about the catalog:
    - `state` - The current provisioning state of the catalog. (Can be `PROVISIONING`, `PROVISIONED`, or `FAILED`)
- `securableType` - The type of the securable. (Can be `CATALOG`, `SCHEMA`, `TABLE`, or `VOLUME`)
- `storageLocation` - Path to the storage location.
- `updatedAt` - Time at which this catalog was last modified, in epoch milliseconds.
- `updatedBy` - Username of user who last modified catalog.

## Notes

When working with the `UnityCatalog` resource, ensure you have the extension imported in your Bicep file:

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

Please note the following important considerations when using the `UnityCatalog` resource:

- Unity Catalog requires a metastore to be enabled in your workspace
- Catalog names must be unique within the metastore and follow naming conventions (alphanumeric and underscores)
- The `storageRoot` must be accessible by the Databricks workspace
- Use `forceDestroy: true` only when you're certain you want to delete the catalog and all its contents
- Predictive optimization settings can be inherited from parent objects or set explicitly
- External catalogs require proper connection and sharing configurations

## Additional reference

For more information, see the following links:

- [Unity Catalog API documentation][00]
- [Unity Catalog concepts][01]
- [Managing catalogs in Unity Catalog][02]

<!-- Link reference definitions -->
[00]: https://docs.databricks.com/api/azure/workspace/catalogs/create
[01]: https://docs.databricks.com/data-governance/unity-catalog/index.html
[02]: https://docs.databricks.com/data-governance/unity-catalog/create-catalogs.html
