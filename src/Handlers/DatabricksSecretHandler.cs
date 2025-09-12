using System.Text.Json;
using Microsoft.Extensions.Logging;
using DatabricksSecret = Databricks.Models.Secret;
using DatabricksSecretIdentifiers = Databricks.Models.SecretIdentifiers;
using Configuration = Databricks.Models.Configuration;

namespace Databricks.Handlers;

public class DatabricksSecretHandler : DatabricksResourceHandlerBase<DatabricksSecret, DatabricksSecretIdentifiers>
{
    private const string SecretGetApiEndpoint = "2.0/secrets/get";
    private const string SecretPutApiEndpoint = "2.0/secrets/put";

    public DatabricksSecretHandler(ILogger<DatabricksSecretHandler> logger) : base(logger) { }

    protected override async Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
    {
        var existing = await GetSecretByKeyAsync(request.Config, request.Properties.Scope, request.Properties.Key, cancellationToken);
        if (existing is not null)
        {
            PopulateSecretProperties(request.Properties, existing);
        }
        return GetResponse(request);
    }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var props = request.Properties;
        
        ValidateSecretProperties(props);
        
        _logger.LogInformation("Ensuring secret {Key} in scope {Scope}", props.Key, props.Scope);

        var existing = await GetSecretByKeyAsync(request.Config, props.Scope, props.Key, cancellationToken);

        if (existing is null)
        {
            _logger.LogInformation("Creating new secret {Key} in scope {Scope}", props.Key, props.Scope);
        }
        else
        {
            _logger.LogInformation("Updating existing secret {Key} in scope {Scope}", props.Key, props.Scope);
        }

        await PutSecretAsync(request.Config, props, cancellationToken);
        
        // Get the updated secret to populate properties
        existing = await GetSecretByKeyAsync(request.Config, props.Scope, props.Key, cancellationToken);
        if (existing is not null)
        {
            PopulateSecretProperties(props, existing);
        }

        // Set the config reference
        props.ConfigReference = $"{{{{secrets/{props.Scope}/{props.Key}}}}}";

        return GetResponse(request);
    }

    protected override DatabricksSecretIdentifiers GetIdentifiers(DatabricksSecret properties) => new()
    {
        Scope = properties.Scope,
        Key = properties.Key
    };

    private async Task<dynamic?> GetSecretByKeyAsync(Configuration configuration, string scope, string key, CancellationToken ct)
    {
        try
        {
            var endpoint = $"{SecretGetApiEndpoint}?scope={Uri.EscapeDataString(scope)}&key={Uri.EscapeDataString(key)}";
            var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Get, endpoint, ct);
            
            return CreateSecretObject(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting secret {Key} from scope {Scope}", key, scope);
            return null;
        }
    }

    private async Task PutSecretAsync(Configuration configuration, DatabricksSecret props, CancellationToken ct)
    {
        var putPayload = BuildSecretPayload(props);

        await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Post, SecretPutApiEndpoint, ct, putPayload);
        
        _logger.LogInformation("Put secret {Key} in scope {Scope}", props.Key, props.Scope);
    }

    private static dynamic BuildSecretPayload(DatabricksSecret props)
    {
        var payload = new
        {
            scope = props.Scope,
            key = props.Key,
            string_value = props.StringValue,
            bytes_value = props.BytesValue
        };

        return payload;
    }

    private static dynamic CreateSecretObject(JsonElement secret)
    {
        return new
        {
            key = secret.TryGetProperty("key", out var key) ? key.GetString() : null,
            last_updated_timestamp = secret.TryGetProperty("last_updated_timestamp", out var lastUpdated) ? lastUpdated.GetInt64() : 0L
        };
    }

    private static void PopulateSecretProperties(DatabricksSecret props, dynamic secret)
    {
        props.LastUpdatedTimestamp = (int)(secret.last_updated_timestamp ?? 0L);
    }

    private static void ValidateSecretProperties(DatabricksSecret props)
    {
        if (string.IsNullOrWhiteSpace(props.Scope))
            throw new ArgumentException("Secret scope cannot be null or empty.", nameof(props.Scope));

        if (string.IsNullOrWhiteSpace(props.Key))
            throw new ArgumentException("Secret key cannot be null or empty.", nameof(props.Key));

        // Validate that exactly one of StringValue or BytesValue is provided
        var hasStringValue = !string.IsNullOrWhiteSpace(props.StringValue);
        var hasBytesValue = !string.IsNullOrWhiteSpace(props.BytesValue);

        if (!hasStringValue && !hasBytesValue)
            throw new ArgumentException("Either StringValue or BytesValue must be provided.", nameof(props.StringValue));

        if (hasStringValue && hasBytesValue)
            throw new ArgumentException("Cannot specify both StringValue and BytesValue. Choose one.", nameof(props.StringValue));

        // Validate scope name format (same as secret scope validation)
        if (props.Scope.Length > 128)
            throw new ArgumentException("Secret scope name may not exceed 128 characters.", nameof(props.Scope));

        if (!System.Text.RegularExpressions.Regex.IsMatch(props.Scope, @"^[\w\.@_/-]{1,128}$"))
            throw new ArgumentException("Secret scope name must consist of alphanumeric characters, dashes, underscores, and periods.", nameof(props.Scope));

        // Validate key name format (same as scope validation)
        if (props.Key.Length > 128)
            throw new ArgumentException("Secret key name may not exceed 128 characters.", nameof(props.Key));

        if (!System.Text.RegularExpressions.Regex.IsMatch(props.Key, @"^[\w\.@_/-]{1,128}$"))
            throw new ArgumentException("Secret key name must consist of alphanumeric characters, dashes, underscores, and periods.", nameof(props.Key));
    }
}
