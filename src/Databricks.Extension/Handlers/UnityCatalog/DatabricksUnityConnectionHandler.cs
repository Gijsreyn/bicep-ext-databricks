using System.Text.Json;
using Microsoft.Extensions.Logging;
using DatabricksUnityConnection = Databricks.Models.UnityCatalog.UnityConnection;
using DatabricksUnityConnectionIdentifiers = Databricks.Models.UnityCatalog.UnityConnectionIdentifiers;
using Configuration = Databricks.Models.Configuration;
using Databricks.Models.UnityCatalog;

namespace Databricks.Handlers.UnityCatalog;

public class DatabricksUnityConnectionHandler : DatabricksResourceHandlerBase<DatabricksUnityConnection, DatabricksUnityConnectionIdentifiers>
{
    private const string ConnectionCreateApiEndpoint = "2.1/unity-catalog/connections";
    private const string ConnectionGetApiEndpoint = "2.1/unity-catalog/connections";
    private const string ConnectionUpdateApiEndpoint = "2.1/unity-catalog/connections";

    // Sensitive options that should be preserved when reading from API
    private static readonly string[] SensitiveOptions = 
    {
        "user", "password", "personalAccessToken", "access_token", "client_secret",
        "pem_private_key", "OAuthPvtKey", "GoogleServiceAccountKeyJson", "bearer_token"
    };

    // Computed options that should be removed before updates
    private static readonly string[] ComputedOptions = 
    {
        "pem_private_key_expiration_epoch_sec", "access_token_expiration"
    };

    public DatabricksUnityConnectionHandler(ILogger<DatabricksUnityConnectionHandler> logger) : base(logger) { }

    protected override async Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
    {
        var existing = await GetConnectionByNameAsync(request.Config, request.Properties.Name, cancellationToken);
        if (existing is not null)
        {
            PopulateConnectionProperties(request.Properties, existing);
        }
        return GetResponse(request);
    }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var props = request.Properties;
        
        ValidateConnectionProperties(props);
        
        _logger.LogInformation("Ensuring Unity Connection {ConnectionName}", props.Name);

        var existing = await GetConnectionByNameAsync(request.Config, props.Name, cancellationToken);

        dynamic connection;
        if (existing is null)
        {
            _logger.LogInformation("Creating new Unity Connection {ConnectionName}", props.Name);
            connection = await CreateConnectionAsync(request.Config, props, cancellationToken);
            
            // Update owner if provided
            if (!string.IsNullOrEmpty(props.Owner))
            {
                connection = await UpdateConnectionOwnerAsync(request.Config, props.Name, props.Owner!, cancellationToken);
            }
        }
        else
        {
            _logger.LogInformation("Updating existing Unity Connection {ConnectionName}", props.Name);
            connection = await UpdateConnectionAsync(request.Config, props, existing, cancellationToken);
        }

        PopulateConnectionProperties(props, connection);
        return GetResponse(request);
    }

    protected override DatabricksUnityConnectionIdentifiers GetIdentifiers(DatabricksUnityConnection properties) => new()
    {
        Name = properties.Name
    };

    private async Task<dynamic?> GetConnectionByNameAsync(Configuration configuration, string connectionName, CancellationToken ct)
    {
        try
        {
            var endpoint = $"{ConnectionGetApiEndpoint}/{Uri.EscapeDataString(connectionName)}";
            var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Get, endpoint, ct);
            
            return CreateConnectionObject(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Unity Connection by name {ConnectionName}", connectionName);
            return null;
        }
    }

    private async Task<dynamic> CreateConnectionAsync(Configuration configuration, DatabricksUnityConnection props, CancellationToken ct)
    {
        var createPayload = BuildConnectionPayload(props);

        var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Post, ConnectionCreateApiEndpoint, ct, createPayload);
        
        _logger.LogInformation("Created Unity Connection {ConnectionName}", props.Name);

        return CreateConnectionObject(response);
    }

    private async Task<dynamic> UpdateConnectionAsync(Configuration configuration, DatabricksUnityConnection props, dynamic existing, CancellationToken ct)
    {
        // Handle owner update separately if needed
        if (!string.IsNullOrEmpty(props.Owner) && props.Owner != existing.owner)
        {
            await UpdateConnectionOwnerAsync(configuration, props.Name, props.Owner!, ct);
        }

        // Update other properties
        var updatePayload = BuildConnectionUpdatePayload(props);
        if (HasUpdateableChanges(updatePayload))
        {
            var endpoint = $"{ConnectionUpdateApiEndpoint}/{Uri.EscapeDataString(props.Name)}";
            var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Patch, endpoint, ct, updatePayload);
            return CreateConnectionObject(response);
        }

        return existing;
    }

    private async Task<dynamic> UpdateConnectionOwnerAsync(Configuration configuration, string connectionName, string owner, CancellationToken ct)
    {
        var ownerPayload = new { owner = owner };
        var endpoint = $"{ConnectionUpdateApiEndpoint}/{Uri.EscapeDataString(connectionName)}";
        
        var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Patch, endpoint, ct, ownerPayload);
        
        _logger.LogInformation("Updated Unity Connection {ConnectionName} owner to {Owner}", connectionName, owner);
        
        return CreateConnectionObject(response);
    }

    private static dynamic BuildConnectionPayload(DatabricksUnityConnection props)
    {
        var payload = new
        {
            name = props.Name,
            comment = props.Comment,
            connection_type = props.ConnectionType?.ToString(),
            options = props.Options,
            properties = props.Properties,
            read_only = props.ReadOnly
        };

        return payload;
    }

    private static dynamic BuildConnectionUpdatePayload(DatabricksUnityConnection props)
    {
        var options = props.Options as Dictionary<string, object> ?? new Dictionary<string, object>();
        
        // Remove computed options from update payload
        foreach (var computedOption in ComputedOptions)
        {
            options.Remove(computedOption);
        }

        var payload = new
        {
            comment = props.Comment,
            options = options.Count > 0 ? options : null,
            properties = props.Properties
        };

        return payload;
    }

    private static bool HasUpdateableChanges(dynamic updatePayload)
    {
        var payload = updatePayload as object;
        var properties = payload?.GetType().GetProperties();
        if (properties is null) return false;

        foreach (var prop in properties)
        {
            var value = prop.GetValue(payload);
            if (value is not null)
            {
                if (value is string str && !string.IsNullOrEmpty(str))
                    return true;
                if (value is not string)
                    return true;
            }
        }
        return false;
    }

    private static dynamic CreateConnectionObject(JsonElement response)
    {
        var connectionDict = new Dictionary<string, object?>();

        if (response.TryGetProperty("name", out var name))
            connectionDict["name"] = name.GetString();
        if (response.TryGetProperty("comment", out var comment))
            connectionDict["comment"] = comment.GetString();
        if (response.TryGetProperty("connection_id", out var connectionId))
            connectionDict["connection_id"] = connectionId.GetString();
        if (response.TryGetProperty("connection_type", out var connectionType))
            connectionDict["connection_type"] = connectionType.GetString();
        if (response.TryGetProperty("created_at", out var createdAt))
            connectionDict["created_at"] = createdAt.GetInt64();
        if (response.TryGetProperty("created_by", out var createdBy))
            connectionDict["created_by"] = createdBy.GetString();
        if (response.TryGetProperty("credential_type", out var credentialType))
            connectionDict["credential_type"] = credentialType.GetString();
        if (response.TryGetProperty("full_name", out var fullName))
            connectionDict["full_name"] = fullName.GetString();
        if (response.TryGetProperty("metastore_id", out var metastoreId))
            connectionDict["metastore_id"] = metastoreId.GetString();
        if (response.TryGetProperty("owner", out var owner))
            connectionDict["owner"] = owner.GetString();
        if (response.TryGetProperty("read_only", out var readOnly))
            connectionDict["read_only"] = readOnly.GetBoolean();
        if (response.TryGetProperty("securable_type", out var securableType))
            connectionDict["securable_type"] = securableType.GetString();
        if (response.TryGetProperty("updated_at", out var updatedAt))
            connectionDict["updated_at"] = updatedAt.GetInt64();
        if (response.TryGetProperty("updated_by", out var updatedBy))
            connectionDict["updated_by"] = updatedBy.GetString();
        if (response.TryGetProperty("url", out var url))
            connectionDict["url"] = url.GetString();

        // Handle options as a dictionary
        if (response.TryGetProperty("options", out var options))
        {
            var optionsDict = new Dictionary<string, object?>();
            foreach (var prop in options.EnumerateObject())
            {
                optionsDict[prop.Name] = prop.Value.GetString();
            }
            connectionDict["options"] = optionsDict;
        }
        else
        {
            connectionDict["options"] = new Dictionary<string, object?>();
        }

        // Handle properties as a dictionary
        if (response.TryGetProperty("properties", out var properties))
        {
            var propertiesDict = new Dictionary<string, object?>();
            foreach (var prop in properties.EnumerateObject())
            {
                propertiesDict[prop.Name] = prop.Value.GetString();
            }
            connectionDict["properties"] = propertiesDict;
        }

        // Handle provisioning_info
        if (response.TryGetProperty("provisioning_info", out var provisioningInfo))
        {
            var provisioningDict = new Dictionary<string, object?>();
            if (provisioningInfo.TryGetProperty("state", out var state))
                provisioningDict["state"] = state.GetString();
            connectionDict["provisioning_info"] = provisioningDict;
        }

        var expando = new System.Dynamic.ExpandoObject();
        var expandoDict = (IDictionary<string, object?>)expando;
        foreach (var kvp in connectionDict)
        {
            expandoDict[kvp.Key] = kvp.Value;
        }
        return expando;
    }

    private static void ValidateConnectionProperties(DatabricksUnityConnection props)
    {
        if (string.IsNullOrWhiteSpace(props.Name))
            throw new ArgumentException("Connection name is required.", nameof(props.Name));

        if (props.ConnectionType is null || props.ConnectionType == ConnectionType.UNKNOWN_CONNECTION_TYPE)
            throw new ArgumentException("Connection type must be specified.", nameof(props.ConnectionType));
    }

    private void PopulateConnectionProperties(DatabricksUnityConnection props, dynamic connection)
    {
        props.ConnectionId = connection.connection_id;
        props.FullName = connection.full_name;
        props.MetastoreId = connection.metastore_id;
        props.Url = connection.url;
        props.CreatedAt = connection.created_at ?? 0;
        props.CreatedBy = connection.created_by;
        props.UpdatedAt = connection.updated_at ?? 0;
        props.UpdatedBy = connection.updated_by;

        // Parse connection_type enum
        if (connection.connection_type is not null)
        {
            if (Enum.TryParse<ConnectionType>(connection.connection_type, true, out ConnectionType connType))
                props.ConnectionType = connType;
        }

        // Parse credential_type enum
        if (connection.credential_type is not null)
        {
            if (Enum.TryParse<CredentialType>(connection.credential_type, true, out CredentialType credType))
                props.CredentialType = credType;
        }

        // Parse securable_type enum
        if (connection.securable_type is not null)
        {
            if (Enum.TryParse<SecurableType>(connection.securable_type, true, out SecurableType secType))
                props.SecurableType = secType;
        }

        // Handle owner
        if (connection.owner is not null)
            props.Owner = connection.owner;

        // Handle provisioning_info
        if (connection.provisioning_info is not null)
        {
            props.ProvisioningInfo = new ConnectionProvisioningInfo();
            if (connection.provisioning_info.state is not null)
            {
                if (Enum.TryParse<ProvisioningState>(connection.provisioning_info.state, true, out ProvisioningState provState))
                    props.ProvisioningInfo.State = provState;
            }
        }

        // Preserve sensitive options from original properties
        if (connection.options is Dictionary<string, object?> responseOptions && 
            props.Options is Dictionary<string, object?> originalOptions)
        {
            foreach (var sensitiveOption in SensitiveOptions)
            {
                if (originalOptions.ContainsKey(sensitiveOption))
                {
                    responseOptions[sensitiveOption] = originalOptions[sensitiveOption];
                }
            }
            props.Options = responseOptions;
        }
        else if (connection.options is not null)
        {
            props.Options = connection.options;
        }

        // Handle builtin HMS special case
        if (connection.options is Dictionary<string, object?> options)
        {
            if (options.TryGetValue("builtin", out var builtin) && builtin?.ToString() == "true")
            {
                // Remove not necessary parameters for builtin HMS to avoid configuration drift
                options.Remove("host");
                options.Remove("port");
                options.Remove("home_workspace_id");
                options.Remove("database");
            }
        }

        // Set properties if available
        if (connection.properties is not null)
            props.Properties = connection.properties;
    }
}
