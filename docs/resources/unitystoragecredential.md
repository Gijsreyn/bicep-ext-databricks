---
category: "Unity Catalog"
---

# UnityStorageCredential

Represents a Unity Catalog storage credential for authenticating to external storage systems.

## Example usage

### Creating a storage credential with Azure Managed Identity

This example shows how to create a storage credential using Azure Managed Identity with Access Connector.

```bicep
resource storageCredential 'UnityStorageCredential' = {
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
```

### Creating a storage credential with Azure Service Principal

This example shows how to create a storage credential using Azure Service Principal.

```bicep
resource servicePrincipalCredential 'UnityStorageCredential' = {
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
```

### Creating a read-only storage credential

This example shows how to create a read-only storage credential for shared data access.

```bicep
resource readOnlyCredential 'UnityStorageCredential' = {
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
```

### Creating a storage credential with validation skip

This example shows how to create a storage credential with validation skipped for testing scenarios.

```bicep
resource testStorageCredential 'UnityStorageCredential' = {
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
```

## Argument reference

The following arguments are available:

- `name` - (Required) The name of the storage credential.
- `azureManagedIdentity` - (Optional) Azure Managed Identity configuration:
  - `accessConnectorId` - (Required) The resource ID of the Azure Databricks Access Connector.
  - `managedIdentityId` - (Required) The resource ID of the Azure User Assigned Managed Identity.
- `azureServicePrincipal` - (Optional) Azure Service Principal configuration:
  - `applicationId` - (Required) The application ID of the Azure service principal.
  - `clientSecret` - (Optional) The client secret of the Azure service principal.
  - `directoryId` - (Required) The directory ID of the Azure service principal.
- `comment` - (Optional) User-provided free-form text description.
- `owner` - (Optional) Username of current owner of storage credential.
- `readOnly` - (Optional) Whether the storage credential is read-only.
- `skipValidation` - (Optional) Suppress validation errors.

## Attribute reference

In addition to all arguments above, the following attributes are outputted:

- `createdAt` - Time at which this storage credential was created, in epoch milliseconds.
- `createdBy` - Username of storage credential creator.
- `fullName` - The full name of the storage credential.
- `id` - Unique identifier of the storage credential.
- `isolationMode` - Whether isolation mode is enabled for this storage credential.
- `metastoreId` - Unique identifier of the metastore for the storage credential.
- `updatedAt` - Time at which this storage credential was last modified, in epoch milliseconds.
- `updatedBy` - Username of user who last modified storage credential.
- `usedForManagedStorage` - Whether this credential is used for managed storage.

## Notes

When working with the 'UnityStorageCredential' resource, ensure you have the extension imported in your Bicep file:

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
- Ensure proper Azure permissions are granted to the identity for the target storage accounts

## Additional reference

For more information, see the following links:

- [Unity Catalog storage credentials API documentation][00]
- [Azure authentication for Unity Catalog][01]
- [Access Connectors for Azure Databricks][02]
- [Managed Identity authentication][03]

<!-- Link reference definitions -->
[00]: https://docs.databricks.com/api/azure/workspace/storagecredentials/create
[01]: https://docs.databricks.com/connect/unity-catalog/azure-credentials.html
[02]: https://docs.databricks.com/administration-guide/cloud-configurations/azure/access-connector.html
[03]: https://docs.databricks.com/connect/unity-catalog/azure-managed-identity.html

