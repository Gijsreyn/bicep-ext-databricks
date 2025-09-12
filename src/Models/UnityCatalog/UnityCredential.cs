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
