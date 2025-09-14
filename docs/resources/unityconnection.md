---
category: "Unity Catalog"
---

# UnityConnection

Represents a Unity Catalog connection for accessing external data sources.

## Example usage

### Creating a PostgreSQL connection

This example shows how to create a connection to a PostgreSQL database.

```bicep
resource postgresConnection 'UnityConnection' = {
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
```

### Creating a Snowflake connection

This example shows how to create a connection to Snowflake.

```bicep
resource snowflakeConnection 'UnityConnection' = {
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
```

### Creating a SQL Server connection

This example shows how to create a connection to SQL Server.

```bicep
resource sqlServerConnection 'UnityConnection' = {
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
```

## Argument reference

The following arguments are available:

- `connectionType` - (Required) The type of connection. (Can be `UNKNOWN_CONNECTION_TYPE`, `MYSQL`, `POSTGRESQL`, `SNOWFLAKE`, `REDSHIFT`, `SQLDW`, `SQLSERVER`, `DATABRICKS`, `SALESFORCE`, `BIGQUERY`, `WORKDAY_RAAS`, `HIVE_METASTORE`, `GA4_RAW_DATA`, `SERVICENOW`, `SALESFORCE_DATA_CLOUD`, `GLUE`, `ORACLE`, `TERADATA`, `HTTP`, or `POWER_BI`)
- `name` - (Required) The name of the connection.
- `comment` - (Optional) User-provided free-form text description.
- `options` - (Optional) A map of key-value properties for connection options.
- `owner` - (Optional) Username of current owner of connection.
- `properties` - (Optional) A map of key-value properties attached to the securable.
- `readOnly` - (Optional) Whether the connection is read-only.

## Attribute reference

In addition to all arguments above, the following attributes are outputted:

- `connectionId` - Unique identifier of the connection.
- `createdAt` - Time at which this connection was created, in epoch milliseconds.
- `createdBy` - Username of connection creator.
- `credentialType` - The type of credential used for the connection. (Can be `UNKNOWN_CREDENTIAL_TYPE`)
- `fullName` - The full name of the connection.
- `metastoreId` - Unique identifier of the metastore for the connection.
- `provisioningInfo` - Provisioning info about the connection:
  - `state` - The current provisioning state of the connection.
- `securableType` - The type of the securable.
- `updatedAt` - Time at which this connection was last modified, in epoch milliseconds.
- `updatedBy` - Username of user who last modified connection.
- `url` - The URL of the connection.

## Notes

When working with the `UnityConnection` resource, ensure you have the extension imported in your Bicep file:

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
- Test connectivity before deploying to production environments

## Additional reference

For more information, see the following links:

- [Unity Catalog connections API documentation][00]
- [External data sources in Unity Catalog][01]
- [Connection types and configuration][02]

<!-- Link reference definitions -->
[00]: https://docs.databricks.com/api/azure/workspace/connections/create
[01]: https://docs.databricks.com/connect/unity-catalog/index.html
[02]: https://docs.databricks.com/connect/unity-catalog/external-locations.html

