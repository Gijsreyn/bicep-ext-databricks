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


[BicepFrontMatter("category", "Unity Catalog")]
[BicepDocHeading("UnityConnection", "Represents a Unity Catalog connection for accessing external data sources.")]
[BicepDocExample(
    "Creating a PostgreSQL connection",
    "This example shows how to create a connection to a PostgreSQL database.",
    @"resource postgresConnection 'UnityConnection' = {
  name: 'postgres_analytics'
  connectionType: 'POSTGRESQL'
  comment: 'Connection to analytics PostgreSQL database'
  owner: 'data-engineering@company.com'
  readOnly: false
  options: {
    host: 'postgres.company.com'
    port: '5432'
    database: 'analytics'
  }
}
"
)]
[BicepDocExample(
    "Creating a Snowflake connection",
    "This example shows how to create a connection to Snowflake.",
    @"resource snowflakeConnection 'UnityConnection' = {
  name: 'snowflake_warehouse'
  connectionType: 'SNOWFLAKE'
  comment: 'Connection to Snowflake data warehouse'
  owner: 'analytics-team@company.com'
  readOnly: true
  options: {
    account: 'company.snowflakecomputing.com'
    warehouse: 'ANALYTICS_WH'
    database: 'PROD_DB'
    schema: 'PUBLIC'
  }
  properties: {
    environment: 'production'
    team: 'analytics'
  }
}
"
)]
[BicepDocExample(
    "Creating a SQL Server connection",
    "This example shows how to create a connection to SQL Server.",
    @"resource sqlServerConnection 'UnityConnection' = {
  name: 'sql_server_erp'
  connectionType: 'SQLSERVER'
  comment: 'Connection to ERP SQL Server database'
  owner: 'business-intelligence@company.com'
  readOnly: true
  options: {
    host: 'sqlserver.internal.com'
    port: '1433'
    database: 'ERP_PROD'
    encrypt: 'true'
    trustServerCertificate: 'false'
  }
}
"
)]
[BicepDocCustom("Notes", @"When working with the `UnityConnection` resource, ensure you have the extension imported in your Bicep file:

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

Please note the following important considerations when using the `UnityConnection` resource:

- Unity Catalog connections require appropriate network connectivity and credentials
- Connection names must be unique within the metastore
- The `options` object contains connection-specific parameters (host, port, database, etc.)
- Use `readOnly: true` for connections that should only allow read operations
- Credentials are managed separately and associated with the connection
- Different connection types require different options - refer to the specific database documentation
- Test connectivity before deploying to production environments")]
[BicepDocCustom("Additional reference", @"For more information, see the following links:

- [Unity Catalog connections API documentation][00]
- [External data sources in Unity Catalog][01]
- [Connection types and configuration][02]

<!-- Link reference definitions -->
[00]: https://docs.databricks.com/api/azure/workspace/connections/create
[01]: https://docs.databricks.com/connect/unity-catalog/index.html
[02]: https://docs.databricks.com/connect/unity-catalog/external-locations.html")]
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
