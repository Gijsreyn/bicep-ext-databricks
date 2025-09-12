using System.Text.Json;
using Microsoft.Extensions.Logging;
using DatabricksUnityCredential = Databricks.Models.UnityCatalog.UnityCredential;
using DatabricksUnityCredentialIdentifiers = Databricks.Models.UnityCatalog.UnityCredentialIdentifiers;
using Configuration = Databricks.Models.Configuration;
using Databricks.Models.UnityCatalog;

namespace Databricks.Handlers.UnityCatalog;

public class DatabricksUnityCredentialHandler : DatabricksResourceHandlerBase<DatabricksUnityCredential, DatabricksUnityCredentialIdentifiers>
{
    private const string CredentialCreateApiEndpoint = "2.1/unity-catalog/credentials";
    private const string CredentialGetApiEndpoint = "2.1/unity-catalog/credentials";
    private const string CredentialUpdateApiEndpoint = "2.1/unity-catalog/credentials";

    public DatabricksUnityCredentialHandler(ILogger<DatabricksUnityCredentialHandler> logger) : base(logger) { }

    protected override async Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
    {
        var existing = await GetCredentialByNameAsync(request.Config, request.Properties.Name, cancellationToken);
        if (existing is not null)
        {
            PopulateCredentialProperties(request.Properties, existing);
        }
        return GetResponse(request);
    }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var props = request.Properties;
        
        ValidateCredentialProperties(props);
        
        _logger.LogInformation("Ensuring Unity Credential {CredentialName}", props.Name);

        var existing = await GetCredentialByNameAsync(request.Config, props.Name, cancellationToken);

        dynamic credential;
        if (existing is null)
        {
            _logger.LogInformation("Creating new Unity Credential {CredentialName}", props.Name);
            credential = await CreateCredentialAsync(request.Config, props, cancellationToken);
        }
        else
        {
            _logger.LogInformation("Updating existing Unity Credential {CredentialName}", props.Name);
            credential = await UpdateCredentialAsync(request.Config, props, existing, cancellationToken);
        }

        PopulateCredentialProperties(props, credential);
        return GetResponse(request);
    }

    protected override DatabricksUnityCredentialIdentifiers GetIdentifiers(DatabricksUnityCredential properties) => new()
    {
        Name = properties.Name
    };

    private async Task<dynamic?> GetCredentialByNameAsync(Configuration configuration, string credentialName, CancellationToken ct)
    {
        try
        {
            var endpoint = $"{CredentialGetApiEndpoint}/{Uri.EscapeDataString(credentialName)}";
            var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Get, endpoint, ct);
            
            return CreateCredentialObject(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Unity Credential by name {CredentialName}", credentialName);
            return null;
        }
    }

    private async Task<dynamic> CreateCredentialAsync(Configuration configuration, DatabricksUnityCredential props, CancellationToken ct)
    {
        var createPayload = BuildCredentialPayload(props);

        var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Post, CredentialCreateApiEndpoint, ct, createPayload);
        
        _logger.LogInformation("Created Unity Credential {CredentialName}", props.Name);

        var credential = CreateCredentialObject(response);

        // Handle owner or isolation mode update if needed after creation
        if (RequiresPostCreationUpdate(props))
        {
            credential = await UpdateCredentialAsync(configuration, props, credential, ct);
        }

        return credential;
    }

    private async Task<dynamic> UpdateCredentialAsync(Configuration configuration, DatabricksUnityCredential props, dynamic existing, CancellationToken ct)
    {
        // Handle owner update separately if needed
        if (!string.IsNullOrEmpty(props.Owner) && props.Owner != existing.owner)
        {
            await UpdateCredentialOwnerAsync(configuration, props.Name, props.Owner!, ct);
        }

        // Handle other updates
        if (HasOtherUpdates(props, existing))
        {
            var updatePayload = BuildCredentialUpdatePayload(props);
            var endpoint = $"{CredentialUpdateApiEndpoint}/{Uri.EscapeDataString(props.Name)}";
            var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Patch, endpoint, ct, updatePayload);
            return CreateCredentialObject(response);
        }

        return existing;
    }

    private async Task UpdateCredentialOwnerAsync(Configuration configuration, string credentialName, string owner, CancellationToken ct)
    {
        var ownerPayload = new { owner = owner };
        var endpoint = $"{CredentialUpdateApiEndpoint}/{Uri.EscapeDataString(credentialName)}";
        
        await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Patch, endpoint, ct, ownerPayload);
        
        _logger.LogInformation("Updated Unity Credential {CredentialName} owner to {Owner}", credentialName, owner);
    }

    private static bool RequiresPostCreationUpdate(DatabricksUnityCredential props)
    {
        return !string.IsNullOrEmpty(props.Owner) || props.IsolationMode != null;
    }

    private static bool HasOtherUpdates(DatabricksUnityCredential props, dynamic existing)
    {
        // Check if there are updates other than owner
        return props.ReadOnly != (existing.read_only ?? false) ||
               !string.IsNullOrEmpty(props.Comment) && props.Comment != existing.comment;
    }

    private static dynamic BuildCredentialPayload(DatabricksUnityCredential props)
    {
        var payload = new
        {
            name = props.Name,
            purpose = props.Purpose?.ToString(),
            azure_managed_identity = props.AzureManagedIdentity != null ? new
            {
                access_connector_id = props.AzureManagedIdentity.AccessConnectorId,
                managed_identity_id = props.AzureManagedIdentity.ManagedIdentityId
            } : null,
            azure_service_principal = props.AzureServicePrincipal != null ? new
            {
                application_id = props.AzureServicePrincipal.ApplicationId,
                client_secret = props.AzureServicePrincipal.ClientSecret,
                directory_id = props.AzureServicePrincipal.DirectoryId
            } : null,
            comment = props.Comment,
            read_only = props.ReadOnly,
            skip_validation = props.SkipValidation
        };

        return payload;
    }

    private static dynamic BuildCredentialUpdatePayload(DatabricksUnityCredential props)
    {
        var payload = new
        {
            comment = props.Comment,
            read_only = props.ReadOnly,
            force = props.ForceUpdate,
            // Note: Based on Go code, some credential types have limitations during update
            azure_managed_identity = props.AzureManagedIdentity != null ? new
            {
                access_connector_id = props.AzureManagedIdentity.AccessConnectorId,
                managed_identity_id = props.AzureManagedIdentity.ManagedIdentityId
                // credential_id is computed and should not be sent in updates
            } : null,
            azure_service_principal = props.AzureServicePrincipal != null ? new
            {
                application_id = props.AzureServicePrincipal.ApplicationId,
                client_secret = props.AzureServicePrincipal.ClientSecret,
                directory_id = props.AzureServicePrincipal.DirectoryId
            } : null
        };

        return payload;
    }

    private static dynamic CreateCredentialObject(JsonElement credential)
    {
        return new
        {
            azure_managed_identity = credential.TryGetProperty("azure_managed_identity", out var azureManagedIdentity) ? new
            {
                access_connector_id = azureManagedIdentity.TryGetProperty("access_connector_id", out var accessConnectorId) ? accessConnectorId.GetString() : null,
                credential_id = azureManagedIdentity.TryGetProperty("credential_id", out var credentialId) ? credentialId.GetString() : null,
                managed_identity_id = azureManagedIdentity.TryGetProperty("managed_identity_id", out var managedIdentityId) ? managedIdentityId.GetString() : null
            } : null,
            azure_service_principal = credential.TryGetProperty("azure_service_principal", out var azureServicePrincipal) ? new
            {
                application_id = azureServicePrincipal.TryGetProperty("application_id", out var applicationId) ? applicationId.GetString() : null,
                client_secret = azureServicePrincipal.TryGetProperty("client_secret", out var clientSecret) ? clientSecret.GetString() : null,
                directory_id = azureServicePrincipal.TryGetProperty("directory_id", out var directoryId) ? directoryId.GetString() : null
            } : null,
            comment = credential.TryGetProperty("comment", out var comment) ? comment.GetString() : null,
            created_at = credential.TryGetProperty("created_at", out var createdAt) ? createdAt.GetInt64() : 0L,
            created_by = credential.TryGetProperty("created_by", out var createdBy) ? createdBy.GetString() : null,
            full_name = credential.TryGetProperty("full_name", out var fullName) ? fullName.GetString() : null,
            id = credential.TryGetProperty("id", out var id) ? id.GetString() : null,
            isolation_mode = credential.TryGetProperty("isolation_mode", out var isolationMode) ? isolationMode.GetString() : null,
            metastore_id = credential.TryGetProperty("metastore_id", out var metastoreId) ? metastoreId.GetString() : null,
            name = credential.TryGetProperty("name", out var name) ? name.GetString() : null,
            owner = credential.TryGetProperty("owner", out var owner) ? owner.GetString() : null,
            purpose = credential.TryGetProperty("purpose", out var purpose) ? purpose.GetString() : null,
            read_only = credential.TryGetProperty("read_only", out var readOnly) && readOnly.GetBoolean(),
            updated_at = credential.TryGetProperty("updated_at", out var updatedAt) ? updatedAt.GetInt64() : 0L,
            updated_by = credential.TryGetProperty("updated_by", out var updatedBy) ? updatedBy.GetString() : null,
            used_for_managed_storage = credential.TryGetProperty("used_for_managed_storage", out var usedForManagedStorage) && usedForManagedStorage.GetBoolean()
        };
    }

    private static void PopulateCredentialProperties(DatabricksUnityCredential props, dynamic credential)
    {
        // Populate Azure Managed Identity
        if (credential.azure_managed_identity != null)
        {
            if (props.AzureManagedIdentity == null)
                props.AzureManagedIdentity = new AzureManagedIdentity();
                
            props.AzureManagedIdentity.AccessConnectorId = credential.azure_managed_identity.access_connector_id;
            props.AzureManagedIdentity.CredentialId = credential.azure_managed_identity.credential_id;
            props.AzureManagedIdentity.ManagedIdentityId = credential.azure_managed_identity.managed_identity_id ?? props.AzureManagedIdentity.ManagedIdentityId;
        }

        // Populate Azure Service Principal (preserve sensitive client_secret from original if available)
        if (credential.azure_service_principal != null)
        {
            if (props.AzureServicePrincipal == null)
                props.AzureServicePrincipal = new AzureServicePrincipal();
                
            props.AzureServicePrincipal.ApplicationId = credential.azure_service_principal.application_id ?? props.AzureServicePrincipal.ApplicationId;
            props.AzureServicePrincipal.DirectoryId = credential.azure_service_principal.directory_id ?? props.AzureServicePrincipal.DirectoryId;
            
            // Only update client_secret if it's provided in the response and not empty
            if (!string.IsNullOrEmpty(credential.azure_service_principal.client_secret))
                props.AzureServicePrincipal.ClientSecret = credential.azure_service_principal.client_secret;
        }

        // Read-only properties
        props.CreatedAt = (int)(credential.created_at ?? 0L);
        props.CreatedBy = credential.created_by;
        props.FullName = credential.full_name;
        props.Id = credential.id;
        
        if (credential.isolation_mode != null)
        {
            if (Enum.TryParse<CredentialIsolationMode>((string)credential.isolation_mode, out var isolationModeEnum))
                props.IsolationMode = isolationModeEnum;
        }
            
        props.MetastoreId = credential.metastore_id;
        
        // Parse purpose enum
        if (credential.purpose != null)
        {
            if (Enum.TryParse<CredentialPurpose>((string)credential.purpose, out var purposeEnum))
                props.Purpose = purposeEnum;
        }
        
        props.UpdatedAt = (int)(credential.updated_at ?? 0L);
        props.UpdatedBy = credential.updated_by;
        props.UsedForManagedStorage = credential.used_for_managed_storage ?? false;

        // Update configuration properties that might have been defaulted or changed
        if (credential.comment != null)
            props.Comment = credential.comment;
            
        if (credential.owner != null)
            props.Owner = credential.owner;
            
        props.ReadOnly = credential.read_only ?? false;
    }

    private static void ValidateCredentialProperties(DatabricksUnityCredential props)
    {
        if (string.IsNullOrWhiteSpace(props.Name))
            throw new ArgumentException("Credential name cannot be null or empty.", nameof(props.Name));

        if (props.Purpose == null)
            throw new ArgumentException("Purpose is required.", nameof(props.Purpose));

        // Validate that exactly one credential type is specified
        var hasAzureManagedIdentity = props.AzureManagedIdentity != null;
        var hasAzureServicePrincipal = props.AzureServicePrincipal != null;

        if (!hasAzureManagedIdentity && !hasAzureServicePrincipal)
            throw new ArgumentException("Either AzureManagedIdentity or AzureServicePrincipal must be specified.", nameof(props.AzureManagedIdentity));

        if (hasAzureManagedIdentity && hasAzureServicePrincipal)
            throw new ArgumentException("Cannot specify both AzureManagedIdentity and AzureServicePrincipal. Choose one.", nameof(props.AzureManagedIdentity));

        // Validate Azure Managed Identity properties
        if (hasAzureManagedIdentity)
        {
            if (string.IsNullOrWhiteSpace(props.AzureManagedIdentity!.ManagedIdentityId))
                throw new ArgumentException("ManagedIdentityId is required for Azure Managed Identity credential.", nameof(props.AzureManagedIdentity.ManagedIdentityId));
        }

        // Validate Azure Service Principal properties
        if (hasAzureServicePrincipal)
        {
            if (string.IsNullOrWhiteSpace(props.AzureServicePrincipal!.ApplicationId))
                throw new ArgumentException("ApplicationId is required for Azure Service Principal credential.", nameof(props.AzureServicePrincipal.ApplicationId));

            if (string.IsNullOrWhiteSpace(props.AzureServicePrincipal.ClientSecret))
                throw new ArgumentException("ClientSecret is required for Azure Service Principal credential.", nameof(props.AzureServicePrincipal.ClientSecret));

            if (string.IsNullOrWhiteSpace(props.AzureServicePrincipal.DirectoryId))
                throw new ArgumentException("DirectoryId is required for Azure Service Principal credential.", nameof(props.AzureServicePrincipal.DirectoryId));
        }
    }
}
