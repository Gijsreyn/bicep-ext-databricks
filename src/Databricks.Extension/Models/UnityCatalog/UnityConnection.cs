using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;
using System.Text.Json.Serialization;

namespace Databricks.Models.UnityCatalog;

public enum ConnectionType
{
    UNKNOWN_CONNECTION_TYPE,
    MYSQL,
    POSTGRESQL,
    SNOWFLAKE,
    REDSHIFT,
    SQLDW,
    SQLSERVER,
    DATABRICKS,
    SALESFORCE,
    BIGQUERY,
    WORKDAY_RAAS,
    HIVE_METASTORE,
    GA4_RAW_DATA,
    SERVICENOW,
    SALESFORCE_DATA_CLOUD,
    GLUE,
    ORACLE,
    TERADATA,
    HTTP,
    POWER_BI
}

public enum CredentialType
{
    UNKNOWN_CREDENTIAL_TYPE
}

[ResourceType("UnityConnection")]
public class UnityConnection : UnityConnectionIdentifiers
{
    // Configuration properties
    [TypeProperty("User-provided free-form text description.", ObjectTypePropertyFlags.None)]
    public string? Comment { get; set; }

    [TypeProperty("The type of connection.", ObjectTypePropertyFlags.Required)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ConnectionType? ConnectionType { get; set; }

    [TypeProperty("A map of key-value properties for connection options.", ObjectTypePropertyFlags.None)]
    public object? Options { get; set; }

    [TypeProperty("A map of key-value properties attached to the securable.", ObjectTypePropertyFlags.None)]
    public object? Properties { get; set; }

    [TypeProperty("Whether the connection is read-only.", ObjectTypePropertyFlags.None)]
    public bool ReadOnly { get; set; }

    // Read-only outputs
    [TypeProperty("Unique identifier of the connection.", ObjectTypePropertyFlags.ReadOnly)]
    public string? ConnectionId { get; set; }

    [TypeProperty("Time at which this connection was created, in epoch milliseconds.", ObjectTypePropertyFlags.ReadOnly)]
    public int CreatedAt { get; set; }

    [TypeProperty("Username of connection creator.", ObjectTypePropertyFlags.ReadOnly)]
    public string? CreatedBy { get; set; }

    [TypeProperty("The type of credential used for the connection.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CredentialType? CredentialType { get; set; }

    [TypeProperty("The full name of the connection.", ObjectTypePropertyFlags.ReadOnly)]
    public string? FullName { get; set; }

    [TypeProperty("Unique identifier of the metastore for the connection.", ObjectTypePropertyFlags.ReadOnly)]
    public string? MetastoreId { get; set; }

    [TypeProperty("Username of current owner of connection.", ObjectTypePropertyFlags.None)]
    public string? Owner { get; set; }

    [TypeProperty("Provisioning info about the connection.", ObjectTypePropertyFlags.ReadOnly)]
    public ConnectionProvisioningInfo? ProvisioningInfo { get; set; }

    [TypeProperty("The type of the securable.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SecurableType? SecurableType { get; set; }

    [TypeProperty("Time at which this connection was last modified, in epoch milliseconds.", ObjectTypePropertyFlags.ReadOnly)]
    public int UpdatedAt { get; set; }

    [TypeProperty("Username of user who last modified connection.", ObjectTypePropertyFlags.ReadOnly)]
    public string? UpdatedBy { get; set; }

    [TypeProperty("The URL of the connection.", ObjectTypePropertyFlags.ReadOnly)]
    public string? Url { get; set; }
}

public class UnityConnectionIdentifiers
{
    [TypeProperty("The name of the connection.", ObjectTypePropertyFlags.Required)]
    public string Name { get; set; } = string.Empty;
}

public class ConnectionProvisioningInfo
{
    [TypeProperty("The current provisioning state of the connection.", ObjectTypePropertyFlags.ReadOnly)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProvisioningState? State { get; set; }
}
