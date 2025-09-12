using System.Text.Json;
using Microsoft.Extensions.Logging;
using DatabricksSecretScope = Databricks.Models.Workspace.SecretScope;
using DatabricksSecretScopeIdentifiers = Databricks.Models.Workspace.SecretScopeIdentifiers;
using Configuration = Databricks.Models.Configuration;
using Databricks.Models.Workspace;

namespace Databricks.Handlers.Workspace;

public class DatabricksSecretScopeHandler : DatabricksResourceHandlerBase<DatabricksSecretScope, DatabricksSecretScopeIdentifiers>
{
    private const string SecretScopesListApiEndpoint = "2.0/secrets/scopes/list";
    private const string SecretScopeCreateApiEndpoint = "2.0/secrets/scopes/create";

    public DatabricksSecretScopeHandler(ILogger<DatabricksSecretScopeHandler> logger) : base(logger) { }

    protected override async Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
    {
        var existing = await GetSecretScopeByNameAsync(request.Config, request.Properties.ScopeName, cancellationToken);
        if (existing is not null)
        {
            PopulateSecretScopeProperties(request.Properties, existing);
        }
        return GetResponse(request);
    }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var props = request.Properties;
        
        ValidateSecretScopeProperties(props);
        
        _logger.LogInformation("Ensuring secret scope {ScopeName}", props.ScopeName);

        var existing = await GetSecretScopeByNameAsync(request.Config, props.ScopeName, cancellationToken);

        if (existing is null)
        {
            _logger.LogInformation("Creating new secret scope {ScopeName}", props.ScopeName);
            await CreateSecretScopeAsync(request.Config, props, cancellationToken);
            
            // Get the created scope to populate properties
            existing = await GetSecretScopeByNameAsync(request.Config, props.ScopeName, cancellationToken);
            if (existing is not null)
            {
                PopulateSecretScopeProperties(props, existing);
            }
        }
        else
        {
            _logger.LogInformation("Secret scope {ScopeName} already exists - secret scopes cannot be updated", props.ScopeName);
            PopulateSecretScopeProperties(props, existing);
        }

        return GetResponse(request);
    }

    protected override DatabricksSecretScopeIdentifiers GetIdentifiers(DatabricksSecretScope properties) => new()
    {
        ScopeName = properties.ScopeName
    };

    private async Task<dynamic?> GetSecretScopeByNameAsync(Configuration configuration, string scopeName, CancellationToken ct)
    {
        try
        {
            var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Get, SecretScopesListApiEndpoint, ct);
            
            if (!response.TryGetProperty("scopes", out var scopesArray))
                return null;

            foreach (var scope in scopesArray.EnumerateArray())
            {
                if (scope.TryGetProperty("name", out var nameProperty) && 
                    nameProperty.GetString() == scopeName)
                {
                    return CreateSecretScopeObject(scope);
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting secret scope by name {ScopeName}", scopeName);
            return null;
        }
    }

    private async Task CreateSecretScopeAsync(Configuration configuration, DatabricksSecretScope props, CancellationToken ct)
    {
        var createPayload = BuildSecretScopePayload(props);

        await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Post, SecretScopeCreateApiEndpoint, ct, createPayload);
        
        _logger.LogInformation("Created secret scope {ScopeName}", props.ScopeName);
    }

    private static dynamic BuildSecretScopePayload(DatabricksSecretScope props)
    {
        var payload = new
        {
            scope = props.ScopeName,
            scope_backend_type = DetermineBackendType(props),
            backend_azure_keyvault = props.KeyVaultMetadata != null ? new
            {
                resource_id = props.KeyVaultMetadata.ResourceId,
                dns_name = props.KeyVaultMetadata.DnsName
            } : null,
            initial_manage_principal = props.InitialManagePrincipal
        };

        return payload;
    }

    private static SecretScopeBackendType DetermineBackendType(DatabricksSecretScope props)
    {
        // If Azure Key Vault metadata is provided, use Azure Key Vault backend
        if (props.KeyVaultMetadata != null)
        {
            return SecretScopeBackendType.AZURE_KEYVAULT;
        }
        
        // Default to Databricks backend
        return SecretScopeBackendType.DATABRICKS;
    }

    private static dynamic CreateSecretScopeObject(JsonElement scope)
    {
        return new
        {
            name = scope.TryGetProperty("name", out var name) ? name.GetString() : null,
            backend_type = scope.TryGetProperty("backend_type", out var backendType) ? backendType.GetString() : null,
            keyvault_metadata = scope.TryGetProperty("keyvault_metadata", out var keyvaultMetadata) ? new
            {
                resource_id = keyvaultMetadata.TryGetProperty("resource_id", out var resourceId) ? resourceId.GetString() : null,
                dns_name = keyvaultMetadata.TryGetProperty("dns_name", out var dnsName) ? dnsName.GetString() : null
            } : null
        };
    }

    private static void PopulateSecretScopeProperties(DatabricksSecretScope props, dynamic scope)
    {
        if (scope.backend_type != null)
        {
            if (Enum.TryParse<SecretScopeBackendType>((string)scope.backend_type, out var backendType))
            {
                props.BackendType = backendType;
            }
        }
        
        if (scope.keyvault_metadata != null)
        {
            if (props.KeyVaultMetadata == null)
            {
                props.KeyVaultMetadata = new AzureKeyVaultMetadata();
            }
            
            props.KeyVaultMetadata.ResourceId = scope.keyvault_metadata.resource_id ?? props.KeyVaultMetadata.ResourceId;
            props.KeyVaultMetadata.DnsName = scope.keyvault_metadata.dns_name ?? props.KeyVaultMetadata.DnsName;
        }
    }

    private static void ValidateSecretScopeProperties(DatabricksSecretScope props)
    {
        if (string.IsNullOrWhiteSpace(props.ScopeName))
            throw new ArgumentException("Secret scope name cannot be null or empty.", nameof(props.ScopeName));

        // Validate scope name format (alphanumeric, dashes, underscores, periods, max 128 chars)
        if (props.ScopeName.Length > 128)
            throw new ArgumentException("Secret scope name may not exceed 128 characters.", nameof(props.ScopeName));

        if (!System.Text.RegularExpressions.Regex.IsMatch(props.ScopeName, @"^[\w\.@_/-]{1,128}$"))
            throw new ArgumentException("Secret scope name must consist of alphanumeric characters, dashes, underscores, and periods.", nameof(props.ScopeName));

        // Validate Azure Key Vault metadata if provided
        if (props.KeyVaultMetadata != null)
        {
            if (string.IsNullOrWhiteSpace(props.KeyVaultMetadata.ResourceId))
                throw new ArgumentException("Azure Key Vault resource ID cannot be null or empty when Key Vault metadata is specified.", nameof(props.KeyVaultMetadata.ResourceId));

            if (string.IsNullOrWhiteSpace(props.KeyVaultMetadata.DnsName))
                throw new ArgumentException("Azure Key Vault DNS name cannot be null or empty when Key Vault metadata is specified.", nameof(props.KeyVaultMetadata.DnsName));
        }
    }
}
