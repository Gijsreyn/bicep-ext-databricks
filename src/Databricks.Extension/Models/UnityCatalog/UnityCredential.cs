using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;
using System.Text.Json.Serialization;

namespace Databricks.Models.UnityCatalog;

public enum CredentialPurpose
{
    STORAGE,
    SERVICE
}

public enum CredentialIsolationMode
{
    ISOLATION_MODE_OPEN,
    ISOLATION_MODE_ISOLATED
}


[BicepFrontMatter("category", "Unity Catalog")]
[BicepDocHeading("UnityCredential", "Represents a Unity Catalog credential for authenticating to external data sources.")]
[BicepDocExample(
    "Creating a credential with Azure Managed Identity",
    "This example shows how to create a credential using Azure Managed Identity.",
    @"resource managedIdentityCredential 'UnityCredential' = {
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
"
)]
[BicepDocExample(
    "Creating a credential with Azure Service Principal",
    "This example shows how to create a credential using Azure Service Principal.",
    @"resource servicePrincipalCredential 'UnityCredential' = {
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
"
)]
[BicepDocExample(
    "Creating a storage credential with validation skip",
    "This example shows how to create a storage credential with validation skipped.",
    @"resource storageCredential 'UnityCredential' = {
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
"
)]
[BicepDocCustom("Notes", @"When working with the 'UnityCredential' resource, ensure you have the extension imported in your Bicep file:

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
- Use `forceDestroy: true` with caution as it will delete the credential even if it's in use")]
[BicepDocCustom("Additional reference", @"For more information, see the following links:

- [Unity Catalog credentials API documentation][00]
- [Azure authentication in Unity Catalog][01]
- [Managing credentials for external data access][02]

<!-- Link reference definitions -->
[00]: https://docs.databricks.com/api/azure/workspace/credentials/create
[01]: https://docs.databricks.com/connect/unity-catalog/azure-credentials.html
[02]: https://docs.databricks.com/data-governance/unity-catalog/manage-external-locations-and-credentials.html")]
[ResourceType("UnityCredential")]
public class UnityCredential : UnityCredentialIdentifiers
{
    // Configuration properties
    [TypeProperty("Azure Managed Identity configuration for the credential.", ObjectTypePropertyFlags.None)]
    public AzureManagedIdentity? AzureManagedIdentity { get; set; }

    [TypeProperty("Azure Service Principal configuration for the credential.", ObjectTypePropertyFlags.None)]
    public AzureServicePrincipal? AzureServicePrincipal { get; set; }

    [TypeProperty("User-provided free-form text description.", ObjectTypePropertyFlags.None)]
    public string? Comment { get; set; }

    [TypeProperty("The purpose of the credential.", ObjectTypePropertyFlags.Required)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CredentialPurpose? Purpose { get; set; }

    [TypeProperty("Whether the credential is read-only.", ObjectTypePropertyFlags.None)]
    public bool ReadOnly { get; set; }

    [TypeProperty("Whether to skip validation of the credential.", ObjectTypePropertyFlags.None)]
    public bool SkipValidation { get; set; }

    [TypeProperty("Whether to force update the credential.", ObjectTypePropertyFlags.None)]
    public bool ForceUpdate { get; set; }

    [TypeProperty("Whether to force destroy the credential.", ObjectTypePropertyFlags.None)]
    public bool ForceDestroy { get; set; }

    // Read-only outputs
    [TypeProperty("Time at which this credential was created, in epoch milliseconds.", ObjectTypePropertyFlags.ReadOnly)]
    public int CreatedAt { get; set; }

    [TypeProperty("Username of credential creator.", ObjectTypePropertyFlags.ReadOnly)]
    public string? CreatedBy { get; set; }

    [TypeProperty("The full name of the credential.", ObjectTypePropertyFlags.ReadOnly)]
    public string? FullName { get; set; }

    [TypeProperty("Unique identifier of the credential.", ObjectTypePropertyFlags.ReadOnly)]
    public string? Id { get; set; }

    [TypeProperty("Whether the credential is accessible from all workspaces or a specific set of workspaces.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CredentialIsolationMode? IsolationMode { get; set; }

    [TypeProperty("Unique identifier of the metastore for the credential.", ObjectTypePropertyFlags.ReadOnly)]
    public string? MetastoreId { get; set; }

    [TypeProperty("Username of current owner of credential.", ObjectTypePropertyFlags.None)]
    public string? Owner { get; set; }

    [TypeProperty("Time at which this credential was last modified, in epoch milliseconds.", ObjectTypePropertyFlags.ReadOnly)]
    public int UpdatedAt { get; set; }

    [TypeProperty("Username of user who last modified credential.", ObjectTypePropertyFlags.ReadOnly)]
    public string? UpdatedBy { get; set; }

    [TypeProperty("Whether this credential is used for managed storage.", ObjectTypePropertyFlags.ReadOnly)]
    public bool UsedForManagedStorage { get; set; }
}

public class UnityCredentialIdentifiers
{
    [TypeProperty("The name of the credential.", ObjectTypePropertyFlags.Required)]
    public string Name { get; set; } = string.Empty;
}

public class AzureManagedIdentity
{
    [TypeProperty("The ID of the Azure Access Connector.", ObjectTypePropertyFlags.None)]
    public string? AccessConnectorId { get; set; }

    [TypeProperty("The credential ID (computed).", ObjectTypePropertyFlags.ReadOnly)]
    public string? CredentialId { get; set; }

    [TypeProperty("The ID of the Azure Managed Identity.", ObjectTypePropertyFlags.Required)]
    public string ManagedIdentityId { get; set; } = string.Empty;
}

public class AzureServicePrincipal
{
    [TypeProperty("The application ID of the Azure Service Principal.", ObjectTypePropertyFlags.Required)]
    public string ApplicationId { get; set; } = string.Empty;

    [TypeProperty("The client secret of the Azure Service Principal.", ObjectTypePropertyFlags.Required)]
    public string ClientSecret { get; set; } = string.Empty;

    [TypeProperty("The directory ID (tenant ID) of the Azure Service Principal.", ObjectTypePropertyFlags.Required)]
    public string DirectoryId { get; set; } = string.Empty;
}
