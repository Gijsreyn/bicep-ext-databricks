using System.Text.Json;
using Microsoft.Extensions.Logging;
using DatabricksUnityStorageCredential = Databricks.Models.UnityCatalog.UnityStorageCredential;
using DatabricksUnityStorageCredentialIdentifiers = Databricks.Models.UnityCatalog.UnityStorageCredentialIdentifiers;
using Configuration = Databricks.Models.Configuration;
using Databricks.Models.UnityCatalog;

namespace Databricks.Handlers.UnityCatalog;

public class DatabricksUnityStorageCredentialHandler : DatabricksResourceHandlerBase<DatabricksUnityStorageCredential, DatabricksUnityStorageCredentialIdentifiers>
{
    private const string StorageCredentialCreateApiEndpoint = "2.1/unity-catalog/storage-credentials";
    private const string StorageCredentialGetApiEndpoint = "2.1/unity-catalog/storage-credentials";
    private const string StorageCredentialUpdateApiEndpoint = "2.1/unity-catalog/storage-credentials";

    // Sensitive fields that should be preserved when reading from API
    private static readonly string[] SensitiveFields = { "client_secret" };

    public DatabricksUnityStorageCredentialHandler(ILogger<DatabricksUnityStorageCredentialHandler> logger) : base(logger) { }

    protected override async Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
    {
        var existing = await GetStorageCredentialByNameAsync(request.Config, request.Properties.Name, cancellationToken);
        if (existing is not null)
        {
            PopulateStorageCredentialProperties(request.Properties, existing);
        }
        return GetResponse(request);
    }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var props = request.Properties;
        
        ValidateStorageCredentialProperties(props);
        
        _logger.LogInformation("Ensuring Unity Storage Credential {StorageCredentialName}", props.Name);

        var existing = await GetStorageCredentialByNameAsync(request.Config, props.Name, cancellationToken);

        dynamic storageCredential;
        if (existing is null)
        {
            _logger.LogInformation("Creating new Unity Storage Credential {StorageCredentialName}", props.Name);
            storageCredential = await CreateStorageCredentialAsync(request.Config, props, cancellationToken);
            
            // Update owner or isolation mode if provided
            if (!string.IsNullOrEmpty(props.Owner))
            {
                storageCredential = await UpdateStorageCredentialOwnerAsync(request.Config, props.Name, props.Owner!, cancellationToken);
            }
        }
        else
        {
            _logger.LogInformation("Updating existing Unity Storage Credential {StorageCredentialName}", props.Name);
            storageCredential = await UpdateStorageCredentialAsync(request.Config, props, existing, cancellationToken);
        }

        PopulateStorageCredentialProperties(props, storageCredential);
        return GetResponse(request);
    }

    protected override DatabricksUnityStorageCredentialIdentifiers GetIdentifiers(DatabricksUnityStorageCredential properties) => new()
    {
        Name = properties.Name
    };

    private async Task<dynamic?> GetStorageCredentialByNameAsync(Configuration configuration, string storageCredentialName, CancellationToken ct)
    {
        try
        {
            var endpoint = $"{StorageCredentialGetApiEndpoint}/{Uri.EscapeDataString(storageCredentialName)}";
            var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Get, endpoint, ct);
            
            return CreateStorageCredentialObject(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Unity Storage Credential by name {StorageCredentialName}", storageCredentialName);
            return null;
        }
    }

    private async Task<dynamic> CreateStorageCredentialAsync(Configuration configuration, DatabricksUnityStorageCredential props, CancellationToken ct)
    {
        var createPayload = BuildStorageCredentialPayload(props);

        var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Post, StorageCredentialCreateApiEndpoint, ct, createPayload);
        
        _logger.LogInformation("Created Unity Storage Credential {StorageCredentialName}", props.Name);

        return CreateStorageCredentialObject(response);
    }

    private async Task<dynamic> UpdateStorageCredentialAsync(Configuration configuration, DatabricksUnityStorageCredential props, dynamic existing, CancellationToken ct)
    {
        // Handle owner update separately if needed
        if (!string.IsNullOrEmpty(props.Owner) && props.Owner != existing.owner)
        {
            await UpdateStorageCredentialOwnerAsync(configuration, props.Name, props.Owner!, ct);
        }

        // Update other properties
        var updatePayload = BuildStorageCredentialUpdatePayload(props);
        if (HasUpdateableChanges(updatePayload))
        {
            var endpoint = $"{StorageCredentialUpdateApiEndpoint}/{Uri.EscapeDataString(props.Name)}";
            var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Patch, endpoint, ct, updatePayload);
            return CreateStorageCredentialObject(response);
        }

        return existing;
    }

    private async Task<dynamic> UpdateStorageCredentialOwnerAsync(Configuration configuration, string storageCredentialName, string owner, CancellationToken ct)
    {
        var ownerPayload = new { owner = owner };
        var endpoint = $"{StorageCredentialUpdateApiEndpoint}/{Uri.EscapeDataString(storageCredentialName)}";
        
        var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Patch, endpoint, ct, ownerPayload);
        
        _logger.LogInformation("Updated Unity Storage Credential {StorageCredentialName} owner to {Owner}", storageCredentialName, owner);
        
        return CreateStorageCredentialObject(response);
    }

    private static dynamic BuildStorageCredentialPayload(DatabricksUnityStorageCredential props)
    {
        var payload = new
        {
            name = props.Name,
            comment = props.Comment,
            azure_managed_identity = BuildAzureManagedIdentity(props.AzureManagedIdentity),
            azure_service_principal = BuildAzureServicePrincipal(props.AzureServicePrincipal),
            read_only = props.ReadOnly,
            skip_validation = props.SkipValidation
        };

        return payload;
    }

    private static dynamic BuildStorageCredentialUpdatePayload(DatabricksUnityStorageCredential props)
    {
        var payload = new
        {
            comment = props.Comment,
            azure_managed_identity = BuildAzureManagedIdentity(props.AzureManagedIdentity),
            azure_service_principal = BuildAzureServicePrincipal(props.AzureServicePrincipal),
            read_only = props.ReadOnly,
            skip_validation = props.SkipValidation
        };

        return payload;
    }

    private static object? BuildAzureManagedIdentity(StorageCredentialAzureManagedIdentity? azureManagedIdentity)
    {
        if (azureManagedIdentity is null) return null;

        return new
        {
            access_connector_id = azureManagedIdentity.AccessConnectorId,
            managed_identity_id = azureManagedIdentity.ManagedIdentityId
        };
    }

    private static object? BuildAzureServicePrincipal(StorageCredentialAzureServicePrincipal? azureServicePrincipal)
    {
        if (azureServicePrincipal is null) return null;

        return new
        {
            application_id = azureServicePrincipal.ApplicationId,
            client_secret = azureServicePrincipal.ClientSecret,
            directory_id = azureServicePrincipal.DirectoryId
        };
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
                if (value is bool)
                    return true;
                if (value is not string && value is not bool)
                    return true;
            }
        }
        return false;
    }

    private static dynamic CreateStorageCredentialObject(JsonElement response)
    {
        var credentialDict = new Dictionary<string, object?>();

        if (response.TryGetProperty("name", out var name))
            credentialDict["name"] = name.GetString();
        if (response.TryGetProperty("comment", out var comment))
            credentialDict["comment"] = comment.GetString();
        if (response.TryGetProperty("created_at", out var createdAt))
        {
            if (createdAt.TryGetInt32(out var createdAtInt))
                credentialDict["created_at"] = createdAtInt;
            else if (createdAt.TryGetInt64(out var createdAtLong))
                credentialDict["created_at"] = (int)createdAtLong;
        }
        if (response.TryGetProperty("created_by", out var createdBy))
            credentialDict["created_by"] = createdBy.GetString();
        if (response.TryGetProperty("full_name", out var fullName))
            credentialDict["full_name"] = fullName.GetString();
        if (response.TryGetProperty("id", out var id))
            credentialDict["id"] = id.GetString();
        if (response.TryGetProperty("isolation_mode", out var isolationMode))
            credentialDict["isolation_mode"] = isolationMode.GetString();
        if (response.TryGetProperty("metastore_id", out var metastoreId))
            credentialDict["metastore_id"] = metastoreId.GetString();
        if (response.TryGetProperty("owner", out var owner))
            credentialDict["owner"] = owner.GetString();
        if (response.TryGetProperty("read_only", out var readOnly))
            credentialDict["read_only"] = readOnly.GetBoolean();
        if (response.TryGetProperty("updated_at", out var updatedAt))
        {
            if (updatedAt.TryGetInt32(out var updatedAtInt))
                credentialDict["updated_at"] = updatedAtInt;
            else if (updatedAt.TryGetInt64(out var updatedAtLong))
                credentialDict["updated_at"] = (int)updatedAtLong;
        }
        if (response.TryGetProperty("updated_by", out var updatedBy))
            credentialDict["updated_by"] = updatedBy.GetString();
        if (response.TryGetProperty("used_for_managed_storage", out var usedForManagedStorage))
            credentialDict["used_for_managed_storage"] = usedForManagedStorage.GetBoolean();

        // Handle azure_managed_identity
        if (response.TryGetProperty("azure_managed_identity", out var azureManagedIdentity))
        {
            credentialDict["azure_managed_identity"] = ParseAzureManagedIdentity(azureManagedIdentity);
        }

        // Handle azure_service_principal
        if (response.TryGetProperty("azure_service_principal", out var azureServicePrincipal))
        {
            credentialDict["azure_service_principal"] = ParseAzureServicePrincipal(azureServicePrincipal);
        }

        var expando = new System.Dynamic.ExpandoObject();
        var expandoDict = (IDictionary<string, object?>)expando;
        foreach (var kvp in credentialDict)
        {
            expandoDict[kvp.Key] = kvp.Value;
        }
        return expando;
    }

    private static StorageCredentialAzureManagedIdentity? ParseAzureManagedIdentity(JsonElement azureManagedIdentity)
    {
        var managedIdentity = new StorageCredentialAzureManagedIdentity();

        if (azureManagedIdentity.TryGetProperty("access_connector_id", out var accessConnectorId))
            managedIdentity.AccessConnectorId = accessConnectorId.GetString() ?? string.Empty;
        if (azureManagedIdentity.TryGetProperty("credential_id", out var credentialId))
            managedIdentity.CredentialId = credentialId.GetString();
        if (azureManagedIdentity.TryGetProperty("managed_identity_id", out var managedIdentityId))
            managedIdentity.ManagedIdentityId = managedIdentityId.GetString() ?? string.Empty;

        return managedIdentity;
    }

    private static StorageCredentialAzureServicePrincipal? ParseAzureServicePrincipal(JsonElement azureServicePrincipal)
    {
        var servicePrincipal = new StorageCredentialAzureServicePrincipal();

        if (azureServicePrincipal.TryGetProperty("application_id", out var applicationId))
            servicePrincipal.ApplicationId = applicationId.GetString() ?? string.Empty;
        if (azureServicePrincipal.TryGetProperty("client_secret", out var clientSecret))
            servicePrincipal.ClientSecret = clientSecret.GetString();
        if (azureServicePrincipal.TryGetProperty("directory_id", out var directoryId))
            servicePrincipal.DirectoryId = directoryId.GetString() ?? string.Empty;

        return servicePrincipal;
    }

    private static void ValidateStorageCredentialProperties(DatabricksUnityStorageCredential props)
    {
        if (string.IsNullOrWhiteSpace(props.Name))
            throw new ArgumentException("Storage credential name is required.", nameof(props.Name));

        // At least one credential type must be specified
        if (props.AzureManagedIdentity is null && props.AzureServicePrincipal is null)
            throw new ArgumentException("At least one credential type (Azure Managed Identity or Azure Service Principal) must be specified.");

        // Validate Azure Managed Identity
        if (props.AzureManagedIdentity is not null)
        {
            if (string.IsNullOrWhiteSpace(props.AzureManagedIdentity.AccessConnectorId))
                throw new ArgumentException("Access Connector ID is required for Azure Managed Identity.", nameof(props.AzureManagedIdentity.AccessConnectorId));
            if (string.IsNullOrWhiteSpace(props.AzureManagedIdentity.ManagedIdentityId))
                throw new ArgumentException("Managed Identity ID is required for Azure Managed Identity.", nameof(props.AzureManagedIdentity.ManagedIdentityId));
        }

        // Validate Azure Service Principal
        if (props.AzureServicePrincipal is not null)
        {
            if (string.IsNullOrWhiteSpace(props.AzureServicePrincipal.ApplicationId))
                throw new ArgumentException("Application ID is required for Azure Service Principal.", nameof(props.AzureServicePrincipal.ApplicationId));
            if (string.IsNullOrWhiteSpace(props.AzureServicePrincipal.DirectoryId))
                throw new ArgumentException("Directory ID is required for Azure Service Principal.", nameof(props.AzureServicePrincipal.DirectoryId));
        }
    }

    private void PopulateStorageCredentialProperties(DatabricksUnityStorageCredential props, dynamic storageCredential)
    {
        props.CreatedAt = storageCredential.created_at ?? 0;
        props.CreatedBy = storageCredential.created_by;
        props.FullName = storageCredential.full_name;
        props.Id = storageCredential.id;
        props.MetastoreId = storageCredential.metastore_id;
        props.UpdatedAt = storageCredential.updated_at ?? 0;
        props.UpdatedBy = storageCredential.updated_by;
        props.UsedForManagedStorage = storageCredential.used_for_managed_storage ?? false;

        // Parse isolation_mode enum
        if (storageCredential.isolation_mode is not null)
        {
            if (Enum.TryParse<ExternalLocationIsolationMode>(storageCredential.isolation_mode, true, out ExternalLocationIsolationMode isoMode))
                props.IsolationMode = isoMode;
        }

        // Handle owner
        if (storageCredential.owner is not null)
            props.Owner = storageCredential.owner;

        // Handle azure_managed_identity
        if (storageCredential.azure_managed_identity is not null)
            props.AzureManagedIdentity = storageCredential.azure_managed_identity as StorageCredentialAzureManagedIdentity;

        // Handle azure_service_principal and preserve sensitive data
        if (storageCredential.azure_service_principal is not null)
        {
            var servicePrincipal = storageCredential.azure_service_principal as StorageCredentialAzureServicePrincipal;
            
            // Preserve original client_secret if it was provided and not returned by API
            if (props.AzureServicePrincipal?.ClientSecret is not null && 
                (servicePrincipal?.ClientSecret is null || string.IsNullOrEmpty(servicePrincipal.ClientSecret)))
            {
                if (servicePrincipal is not null)
                    servicePrincipal.ClientSecret = props.AzureServicePrincipal.ClientSecret;
            }
            
            props.AzureServicePrincipal = servicePrincipal;
        }
    }
}
