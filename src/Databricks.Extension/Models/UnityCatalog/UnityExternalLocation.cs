using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;
using System.Text.Json.Serialization;

namespace Databricks.Models.UnityCatalog;

public enum ExternalLocationIsolationMode
{
    ISOLATION_MODE_OPEN,
    ISOLATION_MODE_ISOLATED
}


[BicepFrontMatter("category", "Unity Catalog")]
[BicepDocHeading("UnityExternalLocation", "Represents a Unity Catalog external location for accessing external data storage.")]
[BicepDocExample(
    "Creating a basic external location",
    "This example shows how to create a basic external location for Azure Data Lake Storage.",
    @"resource externalLocation 'UnityExternalLocation' = {
  name: 'analytics_data_lake'
  url: 'abfss://analytics@mydatalake.dfs.core.windows.net/data'
  credentialName: 'storage_managed_identity'
  comment: 'External location for analytics data in ADLS'
  owner: 'data-engineering@company.com'
  readOnly: false
  skipValidation: false
}
"
)]
[BicepDocExample(
    "Creating an external location with file events",
    "This example shows how to create an external location with file events enabled using managed Azure Queue Storage.",
    @"resource externalLocationWithEvents 'UnityExternalLocation' = {
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
"
)]
[BicepDocExample(
    "Creating a read-only external location",
    "This example shows how to create a read-only external location for shared data access.",
    @"resource readOnlyLocation 'UnityExternalLocation' = {
  name: 'shared_reference_data'
  url: 'abfss://reference@shareddata.dfs.core.windows.net/reference'
  credentialName: 'readonly_credential'
  comment: 'Read-only access to shared reference data'
  owner: 'data-governance@company.com'
  readOnly: true
  fallback: false
  skipValidation: false
}
"
)]
[BicepDocExample(
    "Creating an external location with provided queue",
    "This example shows how to create an external location with a provided Azure Queue Storage for file events.",
    @"resource externalLocationProvidedQueue 'UnityExternalLocation' = {
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
"
)]
[BicepDocCustom("Notes", @"When working with the `UnityExternalLocation` resource, ensure you have the extension imported in your Bicep file:

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
- File event queues can be managed (Databricks-managed) or provided (customer-managed)")]
[BicepDocCustom("Additional reference", @"For more information, see the following links:

- [Unity Catalog external locations API documentation][00]
- [External locations in Unity Catalog][01]
- [File events and notifications][02]
- [Azure Data Lake Storage with Unity Catalog][03]

<!-- Link reference definitions -->
[00]: https://docs.databricks.com/api/azure/workspace/externallocations/create
[01]: https://docs.databricks.com/data-governance/unity-catalog/manage-external-locations-and-credentials.html
[02]: https://docs.databricks.com/ingestion/file-detection/index.html
[03]: https://docs.databricks.com/connect/unity-catalog/azure-adls.html")]
[ResourceType("UnityExternalLocation")]
public class UnityExternalLocation : UnityExternalLocationIdentifiers
{
    // Configuration properties
    [TypeProperty("User-provided free-form text description.", ObjectTypePropertyFlags.None)]
    public string? Comment { get; set; }

    [TypeProperty("The name of the credential used to access the external location.", ObjectTypePropertyFlags.Required)]
    public string CredentialName { get; set; } = string.Empty;

    [TypeProperty("Indicates whether file events are enabled for this external location.", ObjectTypePropertyFlags.None)]
    public bool EnableFileEvents { get; set; }

    [TypeProperty("Encryption details for the external location.", ObjectTypePropertyFlags.None)]
    public object? EncryptionDetails { get; set; }

    [TypeProperty("Indicates whether this location will be used as a fallback location.", ObjectTypePropertyFlags.None)]
    public bool Fallback { get; set; }

    [TypeProperty("Configuration for file event queue.", ObjectTypePropertyFlags.None)]
    public FileEventQueue? FileEventQueue { get; set; }

    [TypeProperty("Whether the external location is read-only.", ObjectTypePropertyFlags.None)]
    public bool ReadOnly { get; set; }

    [TypeProperty("Suppress validation errors.", ObjectTypePropertyFlags.None)]
    public bool SkipValidation { get; set; }

    [TypeProperty("URL of the external location.", ObjectTypePropertyFlags.Required)]
    public string Url { get; set; } = string.Empty;

    // Read-only outputs
    [TypeProperty("Whether this external location can only be browsed.", ObjectTypePropertyFlags.ReadOnly)]
    public bool BrowseOnly { get; set; }

    [TypeProperty("Time at which this external location was created, in epoch milliseconds.", ObjectTypePropertyFlags.ReadOnly)]
    public int CreatedAt { get; set; }

    [TypeProperty("Username of external location creator.", ObjectTypePropertyFlags.ReadOnly)]
    public string? CreatedBy { get; set; }

    [TypeProperty("Unique identifier of the credential used to access the external location.", ObjectTypePropertyFlags.ReadOnly)]
    public string? CredentialId { get; set; }

    [TypeProperty("Whether isolation mode is enabled for this external location.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ExternalLocationIsolationMode? IsolationMode { get; set; }

    [TypeProperty("Unique identifier of the metastore for the external location.", ObjectTypePropertyFlags.ReadOnly)]
    public string? MetastoreId { get; set; }

    [TypeProperty("Username of current owner of external location.", ObjectTypePropertyFlags.None)]
    public string? Owner { get; set; }

    [TypeProperty("Time at which this external location was last modified, in epoch milliseconds.", ObjectTypePropertyFlags.ReadOnly)]
    public int UpdatedAt { get; set; }

    [TypeProperty("Username of user who last modified external location.", ObjectTypePropertyFlags.ReadOnly)]
    public string? UpdatedBy { get; set; }
}

public class UnityExternalLocationIdentifiers
{
    [TypeProperty("The name of the external location.", ObjectTypePropertyFlags.Required)]
    public string Name { get; set; } = string.Empty;
}

public class FileEventQueue
{
    [TypeProperty("Managed Azure Queue Storage configuration.", ObjectTypePropertyFlags.None)]
    public ManagedAqs? ManagedAqs { get; set; }

    [TypeProperty("Managed Google Pub/Sub configuration.", ObjectTypePropertyFlags.None)]
    public ManagedPubSub? ManagedPubsub { get; set; }

    [TypeProperty("Managed Amazon SQS configuration.", ObjectTypePropertyFlags.None)]
    public ManagedSqs? ManagedSqs { get; set; }

    [TypeProperty("Provided Azure Queue Storage configuration.", ObjectTypePropertyFlags.None)]
    public ProvidedAqs? ProvidedAqs { get; set; }

    [TypeProperty("Provided Google Pub/Sub configuration.", ObjectTypePropertyFlags.None)]
    public ProvidedPubSub? ProvidedPubsub { get; set; }

    [TypeProperty("Provided Amazon SQS configuration.", ObjectTypePropertyFlags.None)]
    public ProvidedSqs? ProvidedSqs { get; set; }
}

public class ManagedAqs
{
    [TypeProperty("The managed resource ID.", ObjectTypePropertyFlags.ReadOnly)]
    public string? ManagedResourceId { get; set; }

    [TypeProperty("The queue URL.", ObjectTypePropertyFlags.None)]
    public string? QueueUrl { get; set; }

    [TypeProperty("The resource group.", ObjectTypePropertyFlags.Required)]
    public string ResourceGroup { get; set; } = string.Empty;

    [TypeProperty("The subscription ID.", ObjectTypePropertyFlags.Required)]
    public string SubscriptionId { get; set; } = string.Empty;
}

public class ManagedPubSub
{
    [TypeProperty("The managed resource ID.", ObjectTypePropertyFlags.ReadOnly)]
    public string? ManagedResourceId { get; set; }

    [TypeProperty("The subscription name.", ObjectTypePropertyFlags.None)]
    public string? SubscriptionName { get; set; }
}

public class ManagedSqs
{
    [TypeProperty("The managed resource ID.", ObjectTypePropertyFlags.ReadOnly)]
    public string? ManagedResourceId { get; set; }

    [TypeProperty("The queue URL.", ObjectTypePropertyFlags.None)]
    public string? QueueUrl { get; set; }
}

public class ProvidedAqs
{
    [TypeProperty("The managed resource ID.", ObjectTypePropertyFlags.ReadOnly)]
    public string? ManagedResourceId { get; set; }

    [TypeProperty("The queue URL.", ObjectTypePropertyFlags.Required)]
    public string QueueUrl { get; set; } = string.Empty;

    [TypeProperty("The resource group.", ObjectTypePropertyFlags.None)]
    public string? ResourceGroup { get; set; }

    [TypeProperty("The subscription ID.", ObjectTypePropertyFlags.None)]
    public string? SubscriptionId { get; set; }
}

public class ProvidedPubSub
{
    [TypeProperty("The managed resource ID.", ObjectTypePropertyFlags.ReadOnly)]
    public string? ManagedResourceId { get; set; }

    [TypeProperty("The subscription name.", ObjectTypePropertyFlags.Required)]
    public string SubscriptionName { get; set; } = string.Empty;
}

public class ProvidedSqs
{
    [TypeProperty("The managed resource ID.", ObjectTypePropertyFlags.ReadOnly)]
    public string? ManagedResourceId { get; set; }

    [TypeProperty("The queue URL.", ObjectTypePropertyFlags.Required)]
    public string QueueUrl { get; set; } = string.Empty;
}
