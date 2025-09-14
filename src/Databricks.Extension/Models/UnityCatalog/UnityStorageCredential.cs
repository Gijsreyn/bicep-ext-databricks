using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;
using System.Text.Json.Serialization;

namespace Databricks.Models.UnityCatalog;


[BicepFrontMatter("category", "Unity Catalog")]
[BicepDocHeading("UnityStorageCredential", "Represents a Unity Catalog storage credential for authenticating to external storage systems.")]
[BicepDocExample(
    "Creating a storage credential with Azure Managed Identity",
    "This example shows how to create a storage credential using Azure Managed Identity with Access Connector.",
    @"resource storageCredential 'UnityStorageCredential' = {
  name: 'adls_managed_identity'
  comment: 'Managed identity credential for Azure Data Lake Storage'
  owner: 'data-platform@company.com'
  readOnly: false
  skipValidation: false
  azureManagedIdentity: {
    accessConnectorId: '/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/databricks-rg/providers/Microsoft.Databricks/accessConnectors/databricks-access-connector'
    managedIdentityId: '/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/databricks-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/databricks-storage-identity'
  }
}
"
)]
[BicepDocExample(
    "Creating a storage credential with Azure Service Principal",
    "This example shows how to create a storage credential using Azure Service Principal.",
    @"resource servicePrincipalCredential 'UnityStorageCredential' = {
  name: 'adls_service_principal'
  comment: 'Service principal credential for Azure Data Lake Storage'
  owner: 'security-team@company.com'
  readOnly: false
  skipValidation: false
  azureServicePrincipal: {
    applicationId: '87654321-4321-4321-4321-210987654321'
    clientSecret: 'your-client-secret-here'
    directoryId: '11111111-1111-1111-1111-111111111111'
  }
}
"
)]
[BicepDocExample(
    "Creating a read-only storage credential",
    "This example shows how to create a read-only storage credential for shared data access.",
    @"resource readOnlyCredential 'UnityStorageCredential' = {
  name: 'readonly_shared_storage'
  comment: 'Read-only credential for shared storage access'
  owner: 'data-governance@company.com'
  readOnly: true
  skipValidation: false
  azureManagedIdentity: {
    accessConnectorId: '/subscriptions/99999999-9999-9999-9999-999999999999/resourceGroups/shared-rg/providers/Microsoft.Databricks/accessConnectors/shared-access-connector'
    managedIdentityId: '/subscriptions/99999999-9999-9999-9999-999999999999/resourceGroups/shared-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/readonly-identity'
  }
}
"
)]
[BicepDocExample(
    "Creating a storage credential with validation skip",
    "This example shows how to create a storage credential with validation skipped for testing scenarios.",
    @"resource testStorageCredential 'UnityStorageCredential' = {
  name: 'test_storage_credential'
  comment: 'Test storage credential with validation skipped'
  owner: 'test-team@company.com'
  readOnly: false
  skipValidation: true
  azureManagedIdentity: {
    accessConnectorId: '/subscriptions/testsubscription/resourceGroups/test-rg/providers/Microsoft.Databricks/accessConnectors/test-connector'
    managedIdentityId: '/subscriptions/testsubscription/resourceGroups/test-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/test-identity'
  }
}
"
)]
[BicepDocCustom("Notes", @"When working with the `UnityStorageCredential` resource, ensure you have the extension imported in your Bicep file:

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

Please note the following important considerations when using the `UnityStorageCredential` resource:

- Either `azureManagedIdentity` or `azureServicePrincipal` must be specified, but not both
- Azure Managed Identity is the recommended approach for better security (no secrets to manage)
- For Managed Identity: Both `accessConnectorId` and `managedIdentityId` are required
- For Service Principal: `applicationId` and `directoryId` are required; `clientSecret` is optional but typically needed
- Storage credential names must be unique within the metastore
- Use `readOnly: true` for credentials that should only allow read operations
- Set `skipValidation: true` only during testing or when you're certain the configuration is correct
- The Access Connector must be properly configured with the Managed Identity
- Ensure proper Azure permissions are granted to the identity for the target storage accounts")]
[BicepDocCustom("Additional reference", @"For more information, see the following links:

- [Unity Catalog storage credentials API documentation][00]
- [Azure authentication for Unity Catalog][01]
- [Access Connectors for Azure Databricks][02]
- [Managed Identity authentication][03]

<!-- Link reference definitions -->
[00]: https://docs.databricks.com/api/azure/workspace/storagecredentials/create
[01]: https://docs.databricks.com/connect/unity-catalog/azure-credentials.html
[02]: https://docs.databricks.com/administration-guide/cloud-configurations/azure/access-connector.html
[03]: https://docs.databricks.com/connect/unity-catalog/azure-managed-identity.html")]
[ResourceType("UnityStorageCredential")]
public class UnityStorageCredential : UnityStorageCredentialIdentifiers
{
    // Configuration properties
    [TypeProperty("Azure Managed Identity configuration.", ObjectTypePropertyFlags.None)]
    public StorageCredentialAzureManagedIdentity? AzureManagedIdentity { get; set; }

    [TypeProperty("Azure Service Principal configuration.", ObjectTypePropertyFlags.None)]
    public StorageCredentialAzureServicePrincipal? AzureServicePrincipal { get; set; }

    [TypeProperty("User-provided free-form text description.", ObjectTypePropertyFlags.None)]
    public string? Comment { get; set; }

    [TypeProperty("Whether the storage credential is read-only.", ObjectTypePropertyFlags.None)]
    public bool ReadOnly { get; set; }

    [TypeProperty("Suppress validation errors.", ObjectTypePropertyFlags.None)]
    public bool SkipValidation { get; set; }

    // Read-only outputs
    [TypeProperty("Time at which this storage credential was created, in epoch milliseconds.", ObjectTypePropertyFlags.ReadOnly)]
    public int CreatedAt { get; set; }

    [TypeProperty("Username of storage credential creator.", ObjectTypePropertyFlags.ReadOnly)]
    public string? CreatedBy { get; set; }

    [TypeProperty("The full name of the storage credential.", ObjectTypePropertyFlags.ReadOnly)]
    public string? FullName { get; set; }

    [TypeProperty("Unique identifier of the storage credential.", ObjectTypePropertyFlags.ReadOnly)]
    public string? Id { get; set; }

    [TypeProperty("Whether isolation mode is enabled for this storage credential.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ExternalLocationIsolationMode? IsolationMode { get; set; }

    [TypeProperty("Unique identifier of the metastore for the storage credential.", ObjectTypePropertyFlags.ReadOnly)]
    public string? MetastoreId { get; set; }

    [TypeProperty("Username of current owner of storage credential.", ObjectTypePropertyFlags.None)]
    public string? Owner { get; set; }

    [TypeProperty("Time at which this storage credential was last modified, in epoch milliseconds.", ObjectTypePropertyFlags.ReadOnly)]
    public int UpdatedAt { get; set; }

    [TypeProperty("Username of user who last modified storage credential.", ObjectTypePropertyFlags.ReadOnly)]
    public string? UpdatedBy { get; set; }

    [TypeProperty("Whether this credential is used for managed storage.", ObjectTypePropertyFlags.ReadOnly)]
    public bool UsedForManagedStorage { get; set; }
}

public class UnityStorageCredentialIdentifiers
{
    [TypeProperty("The name of the storage credential.", ObjectTypePropertyFlags.Required)]
    public string Name { get; set; } = string.Empty;
}

public class StorageCredentialAzureManagedIdentity
{
    [TypeProperty("The resource ID of the Azure Databricks Access Connector.", ObjectTypePropertyFlags.Required)]
    public string AccessConnectorId { get; set; } = string.Empty;

    [TypeProperty("The credential ID.", ObjectTypePropertyFlags.ReadOnly)]
    public string? CredentialId { get; set; }

    [TypeProperty("The resource ID of the Azure User Assigned Managed Identity.", ObjectTypePropertyFlags.Required)]
    public string ManagedIdentityId { get; set; } = string.Empty;
}

public class StorageCredentialAzureServicePrincipal
{
    [TypeProperty("The application ID of the Azure service principal.", ObjectTypePropertyFlags.Required)]
    public string ApplicationId { get; set; } = string.Empty;

    [TypeProperty("The client secret of the Azure service principal.", ObjectTypePropertyFlags.None)]
    public string? ClientSecret { get; set; }

    [TypeProperty("The directory ID of the Azure service principal.", ObjectTypePropertyFlags.Required)]
    public string DirectoryId { get; set; } = string.Empty;
}
