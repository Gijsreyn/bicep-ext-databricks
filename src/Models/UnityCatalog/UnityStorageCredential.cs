using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;
using System.Text.Json.Serialization;

namespace Databricks.Models.UnityCatalog;

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
