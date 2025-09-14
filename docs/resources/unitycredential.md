---
category: "Unity Catalog"
---

# UnityCredential

Represents a Unity Catalog credential for authenticating to external data sources.

## Example usage

### Creating a credential with Azure Managed Identity

This example shows how to create a credential using Azure Managed Identity.

```bicep
resource managedIdentityCredential 'UnityCredential' = {
  name: 'storage_managed_identity'
  purpose: 'STORAGE'
  comment: 'Managed identity credential for Azure Data Lake Storage'
  owner: 'data-engineering@company.com'
  readOnly: false
  azureManagedIdentity: {
    accessConnectorId: '/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/databricks-rg/providers/Microsoft.Databricks/accessConnectors/my-access-connector'
    managedIdentityId: '/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/databricks-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/databricks-identity'
  }
}
```

### Creating a credential with Azure Service Principal

This example shows how to create a credential using Azure Service Principal.

```bicep
resource servicePrincipalCredential 'UnityCredential' = {
  name: 'service_principal_cred'
  purpose: 'SERVICE'
  comment: 'Service principal credential for external services'
  owner: 'security-team@company.com'
  readOnly: true
  azureServicePrincipal: {
    applicationId: '12345678-1234-1234-1234-123456789012'
    clientSecret: 'your-client-secret-here'
    directoryId: '87654321-4321-4321-4321-210987654321'
  }
  skipValidation: false
}
```

### Creating a storage credential with validation skip

This example shows how to create a storage credential with validation skipped.

```bicep
resource storageCredential 'UnityCredential' = {
  name: 'external_storage_cred'
  purpose: 'STORAGE'
  comment: 'Credential for external storage access'
  owner: 'data-platform@company.com'
  readOnly: false
  skipValidation: true
  forceUpdate: false
  forceDestroy: false
  azureManagedIdentity: {
    managedIdentityId: '/subscriptions/11111111-1111-1111-1111-111111111111/resourceGroups/storage-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/storage-identity'
  }
}
```

## Argument reference

The following arguments are available:

- `name` - (Required) The name of the credential.
- `purpose` - (Required) The purpose of the credential. (Can be `STORAGE`, or `SERVICE`)
- `azureManagedIdentity` - (Optional) Azure Managed Identity configuration for the credential:
  - `accessConnectorId` - (Optional) The ID of the Azure Access Connector.
  - `managedIdentityId` - (Required) The ID of the Azure Managed Identity.
- `azureServicePrincipal` - (Optional) Azure Service Principal configuration for the credential:
  - `applicationId` - (Required) The application ID of the Azure Service Principal.
  - `clientSecret` - (Required) The client secret of the Azure Service Principal.
  - `directoryId` - (Required) The directory ID (tenant ID) of the Azure Service Principal.
- `comment` - (Optional) User-provided free-form text description.
- `forceDestroy` - (Optional) Whether to force destroy the credential.
- `forceUpdate` - (Optional) Whether to force update the credential.
- `owner` - (Optional) Username of current owner of credential.
- `readOnly` - (Optional) Whether the credential is read-only.
- `skipValidation` - (Optional) Whether to skip validation of the credential.

## Attribute reference

In addition to all arguments above, the following attributes are outputted:

- `createdAt` - Time at which this credential was created, in epoch milliseconds.
- `createdBy` - Username of credential creator.
- `fullName` - The full name of the credential.
- `id` - Unique identifier of the credential.
- `isolationMode` - Whether the credential is accessible from all workspaces or a specific set of workspaces. (Can be `ISOLATION_MODE_OPEN`, or `ISOLATION_MODE_ISOLATED`)
- `metastoreId` - Unique identifier of the metastore for the credential.
- `updatedAt` - Time at which this credential was last modified, in epoch milliseconds.
- `updatedBy` - Username of user who last modified credential.
- `usedForManagedStorage` - Whether this credential is used for managed storage.

## Notes

When working with the 'UnityCredential' resource, ensure you have the extension imported in your Bicep file:

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

Please note the following important considerations when using the `UnityCredential` resource:

- Either `azureManagedIdentity` or `azureServicePrincipal` must be specified, but not both
- For `STORAGE` purpose: Use for accessing external storage locations like Azure Data Lake Storage
- For `SERVICE` purpose: Use for accessing external services and APIs
- Managed Identity is the recommended approach for better security (no secrets to manage)
- Service Principal requires managing client secrets securely
- Use `skipValidation: true` only when you're certain the credential configuration is correct
- Set `readOnly: true` for credentials that should only allow read operations
- Use `forceDestroy: true` with caution as it will delete the credential even if it's in use

## Additional reference

For more information, see the following links:

- [Unity Catalog credentials API documentation][00]
- [Azure authentication in Unity Catalog][01]
- [Managing credentials for external data access][02]

<!-- Link reference definitions -->
[00]: https://docs.databricks.com/api/azure/workspace/credentials/create
[01]: https://docs.databricks.com/connect/unity-catalog/azure-credentials.html
[02]: https://docs.databricks.com/data-governance/unity-catalog/manage-external-locations-and-credentials.html

