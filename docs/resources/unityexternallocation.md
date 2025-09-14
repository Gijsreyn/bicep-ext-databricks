---
category: "Unity Catalog"
---

# UnityExternalLocation

Represents a Unity Catalog external location for accessing external data storage.

## Example usage

### Creating a basic external location

This example shows how to create a basic external location for Azure Data Lake Storage.

```bicep
resource externalLocation 'UnityExternalLocation' = {
  name: 'analytics_data_lake'
  url: 'abfss://analytics@mydatalake.dfs.core.windows.net/data'
  credentialName: 'storage_managed_identity'
  comment: 'External location for analytics data in ADLS'
  owner: 'data-engineering@company.com'
  readOnly: false
  skipValidation: false
}
```

### Creating an external location with file events

This example shows how to create an external location with file events enabled using managed Azure Queue Storage.

```bicep
resource externalLocationWithEvents 'UnityExternalLocation' = {
  name: 'streaming_data_location'
  url: 'abfss://streaming@mydatalake.dfs.core.windows.net/events'
  credentialName: 'streaming_credential'
  comment: 'External location for streaming data with file events'
  owner: 'streaming-team@company.com'
  readOnly: false
  enableFileEvents: true
  fileEventQueue: {
    managedAqs: {
      resourceGroup: 'databricks-streaming-rg'
      subscriptionId: '12345678-1234-1234-1234-123456789012'
    }
  }
}
```

### Creating a read-only external location

This example shows how to create a read-only external location for shared data access.

```bicep
resource readOnlyLocation 'UnityExternalLocation' = {
  name: 'shared_reference_data'
  url: 'abfss://reference@shareddata.dfs.core.windows.net/reference'
  credentialName: 'readonly_credential'
  comment: 'Read-only access to shared reference data'
  owner: 'data-governance@company.com'
  readOnly: true
  fallback: false
  skipValidation: false
}
```

### Creating an external location with provided queue

This example shows how to create an external location with a provided Azure Queue Storage for file events.

```bicep
resource externalLocationProvidedQueue 'UnityExternalLocation' = {
  name: 'custom_events_location'
  url: 'abfss://events@mydatalake.dfs.core.windows.net/custom'
  credentialName: 'events_credential'
  comment: 'External location with custom queue configuration'
  owner: 'platform-team@company.com'
  enableFileEvents: true
  fileEventQueue: {
    providedAqs: {
      queueUrl: 'https://mystorageaccount.queue.core.windows.net/myqueue'
      resourceGroup: 'my-resource-group'
      subscriptionId: '87654321-4321-4321-4321-210987654321'
    }
  }
}
```

## Argument reference

The following arguments are available:

- `credentialName` - (Required) The name of the credential used to access the external location.
- `name` - (Required) The name of the external location.
- `url` - (Required) URL of the external location.
- `comment` - (Optional) User-provided free-form text description.
- `enableFileEvents` - (Optional) Indicates whether file events are enabled for this external location.
- `encryptionDetails` - (Optional) Encryption details for the external location.
- `fallback` - (Optional) Indicates whether this location will be used as a fallback location.
- `fileEventQueue` - (Optional) Configuration for file event queue:
  - `managedAqs` - (Optional) Managed Azure Queue Storage configuration.
  - `managedPubsub` - (Optional) Managed Google Pub/Sub configuration.
  - `managedSqs` - (Optional) Managed Amazon SQS configuration.
  - `providedAqs` - (Optional) Provided Azure Queue Storage configuration.
  - `providedPubsub` - (Optional) Provided Google Pub/Sub configuration.
  - `providedSqs` - (Optional) Provided Amazon SQS configuration.
- `owner` - (Optional) Username of current owner of external location.
- `readOnly` - (Optional) Whether the external location is read-only.
- `skipValidation` - (Optional) Suppress validation errors.

## Attribute reference

In addition to all arguments above, the following attributes are outputted:

- `browseOnly` - Whether this external location can only be browsed.
- `createdAt` - Time at which this external location was created, in epoch milliseconds.
- `createdBy` - Username of external location creator.
- `credentialId` - Unique identifier of the credential used to access the external location.
- `isolationMode` - Whether isolation mode is enabled for this external location. (Can be `ISOLATION_MODE_OPEN`, or `ISOLATION_MODE_ISOLATED`)
- `metastoreId` - Unique identifier of the metastore for the external location.
- `updatedAt` - Time at which this external location was last modified, in epoch milliseconds.
- `updatedBy` - Username of user who last modified external location.

## Notes

When working with the 'UnityExternalLocation' resource, ensure you have the extension imported in your Bicep file:

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

Please note the following important considerations when using the `UnityExternalLocation` resource:

- The credential specified in `credentialName` must exist before creating the external location
- External location names must be unique within the metastore
- The `url` must be accessible using the specified credential
- File events require proper queue configuration and permissions
- Use `readOnly: true` for locations that should only allow read operations
- Set `skipValidation: true` only when you're certain the configuration is correct
- For Azure: URLs typically use the `abfss://` scheme for Azure Data Lake Storage Gen2
- File event queues can be managed (Databricks-managed) or provided (customer-managed)

## Additional reference

For more information, see the following links:

- [Unity Catalog external locations API documentation][00]
- [External locations in Unity Catalog][01]
- [File events and notifications][02]
- [Azure Data Lake Storage with Unity Catalog][03]

<!-- Link reference definitions -->
[00]: https://docs.databricks.com/api/azure/workspace/externallocations/create
[01]: https://docs.databricks.com/data-governance/unity-catalog/manage-external-locations-and-credentials.html
[02]: https://docs.databricks.com/ingestion/file-detection/index.html
[03]: https://docs.databricks.com/connect/unity-catalog/azure-adls.html

