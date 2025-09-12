using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;
using System.Text.Json.Serialization;

namespace Databricks.Models.UnityCatalog;

public enum ExternalLocationIsolationMode
{
    ISOLATION_MODE_OPEN,
    ISOLATION_MODE_ISOLATED
}

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
