using System.Text.Json;
using Microsoft.Extensions.Logging;
using DatabricksUnityCatalog = Databricks.Models.UnityCatalog.UnityCatalog;
using DatabricksUnityCatalogIdentifiers = Databricks.Models.UnityCatalog.UnityCatalogIdentifiers;
using Configuration = Databricks.Models.Configuration;
using Databricks.Models.UnityCatalog;

namespace Databricks.Handlers.UnityCatalog;

public class DatabricksUnityCatalogHandler : DatabricksResourceHandlerBase<DatabricksUnityCatalog, DatabricksUnityCatalogIdentifiers>
{
    private const string CatalogCreateApiEndpoint = "2.1/unity-catalog/catalogs";
    private const string CatalogGetApiEndpoint = "2.1/unity-catalog/catalogs";
    private const string CatalogUpdateApiEndpoint = "2.1/unity-catalog/catalogs";
    private const string SchemaDeleteApiEndpoint = "2.1/unity-catalog/schemas";

    public DatabricksUnityCatalogHandler(ILogger<DatabricksUnityCatalogHandler> logger) : base(logger) { }

    protected override async Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
    {
        var existing = await GetCatalogByNameAsync(request.Config, request.Properties.Name, cancellationToken);
        if (existing is not null)
        {
            PopulateCatalogProperties(request.Properties, existing);
        }
        return GetResponse(request);
    }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var props = request.Properties;
        
        ValidateCatalogProperties(props);
        
        _logger.LogInformation("Ensuring Unity Catalog {CatalogName}", props.Name);

        var existing = await GetCatalogByNameAsync(request.Config, props.Name, cancellationToken);

        dynamic catalog;
        if (existing is null)
        {
            _logger.LogInformation("Creating new Unity Catalog {CatalogName}", props.Name);
            catalog = await CreateCatalogAsync(request.Config, props, cancellationToken);
            
            // Remove default schema for standard catalogs (non-Delta Sharing, non-foreign)
            if (string.IsNullOrEmpty(props.ShareName) && string.IsNullOrEmpty(props.ConnectionName))
            {
                await RemoveDefaultSchemaAsync(request.Config, props.Name, cancellationToken);
            }
        }
        else
        {
            _logger.LogInformation("Updating existing Unity Catalog {CatalogName}", props.Name);
            catalog = await UpdateCatalogAsync(request.Config, props, existing, cancellationToken);
        }

        PopulateCatalogProperties(props, catalog);
        return GetResponse(request);
    }

    protected override DatabricksUnityCatalogIdentifiers GetIdentifiers(DatabricksUnityCatalog properties) => new()
    {
        Name = properties.Name
    };

    private async Task<dynamic?> GetCatalogByNameAsync(Configuration configuration, string catalogName, CancellationToken ct)
    {
        try
        {
            var endpoint = $"{CatalogGetApiEndpoint}/{Uri.EscapeDataString(catalogName)}";
            var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Get, endpoint, ct);
            
            return CreateCatalogObject(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Unity Catalog by name {CatalogName}", catalogName);
            return null;
        }
    }

    private async Task<dynamic> CreateCatalogAsync(Configuration configuration, DatabricksUnityCatalog props, CancellationToken ct)
    {
        var createPayload = BuildCatalogPayload(props);

        var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Post, CatalogCreateApiEndpoint, ct, createPayload);
        
        _logger.LogInformation("Created Unity Catalog {CatalogName}", props.Name);

        return CreateCatalogObject(response);
    }

    private async Task<dynamic> UpdateCatalogAsync(Configuration configuration, DatabricksUnityCatalog props, dynamic existing, CancellationToken ct)
    {
        // Handle owner update separately if needed
        if (!string.IsNullOrEmpty(props.Owner) && props.Owner != existing.owner)
        {
            await UpdateCatalogOwnerAsync(configuration, props.Name, props.Owner!, ct);
        }

        // Update other properties
        var updatePayload = BuildCatalogUpdatePayload(props);
        if (HasUpdateableChanges(updatePayload))
        {
            var endpoint = $"{CatalogUpdateApiEndpoint}/{Uri.EscapeDataString(props.Name)}";
            var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Patch, endpoint, ct, updatePayload);
            return CreateCatalogObject(response);
        }

        return existing;
    }

    private async Task UpdateCatalogOwnerAsync(Configuration configuration, string catalogName, string owner, CancellationToken ct)
    {
        var ownerPayload = new { owner = owner };
        var endpoint = $"{CatalogUpdateApiEndpoint}/{Uri.EscapeDataString(catalogName)}";
        
        await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Patch, endpoint, ct, ownerPayload);
        
        _logger.LogInformation("Updated Unity Catalog {CatalogName} owner to {Owner}", catalogName, owner);
    }

    private async Task RemoveDefaultSchemaAsync(Configuration configuration, string catalogName, CancellationToken ct)
    {
        try
        {
            var endpoint = $"{SchemaDeleteApiEndpoint}/{Uri.EscapeDataString(catalogName)}.default";
            await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Delete, endpoint, ct);
            
            _logger.LogInformation("Removed default schema for Unity Catalog {CatalogName}", catalogName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not remove default schema for Unity Catalog {CatalogName}", catalogName);
        }
    }

    private static dynamic BuildCatalogPayload(DatabricksUnityCatalog props)
    {
        var payload = new
        {
            name = props.Name,
            comment = props.Comment,
            connection_name = props.ConnectionName,
            options = props.Options,
            properties = props.Properties,
            provider_name = props.ProviderName,
            share_name = props.ShareName,
            storage_root = props.StorageRoot
        };

        return payload;
    }

    private static dynamic BuildCatalogUpdatePayload(DatabricksUnityCatalog props)
    {
        var payload = new
        {
            comment = props.Comment,
            isolation_mode = props.IsolationMode?.ToString(),
            enable_predictive_optimization = props.EnablePredictiveOptimization?.ToString(),
            options = props.Options,
            properties = props.Properties
        };

        return payload;
    }

    private static bool HasUpdateableChanges(dynamic payload)
    {
        return payload.comment != null || 
               payload.isolation_mode != null || 
               payload.enable_predictive_optimization != null ||
               payload.options != null ||
               payload.properties != null;
    }

    private static dynamic CreateCatalogObject(JsonElement catalog)
    {
        return new
        {
            browse_only = catalog.TryGetProperty("browse_only", out var browseOnly) && browseOnly.GetBoolean(),
            catalog_type = catalog.TryGetProperty("catalog_type", out var catalogType) ? catalogType.GetString() : null,
            comment = catalog.TryGetProperty("comment", out var comment) ? comment.GetString() : null,
            connection_name = catalog.TryGetProperty("connection_name", out var connectionName) ? connectionName.GetString() : null,
            created_at = catalog.TryGetProperty("created_at", out var createdAt) ? createdAt.GetInt64() : 0L,
            created_by = catalog.TryGetProperty("created_by", out var createdBy) ? createdBy.GetString() : null,
            effective_predictive_optimization_flag = catalog.TryGetProperty("effective_predictive_optimization_flag", out var effectiveFlag) ? new
            {
                inherited_from_name = effectiveFlag.TryGetProperty("inherited_from_name", out var inheritedFromName) ? inheritedFromName.GetString() : null,
                inherited_from_type = effectiveFlag.TryGetProperty("inherited_from_type", out var inheritedFromType) ? inheritedFromType.GetString() : null,
                value = effectiveFlag.TryGetProperty("value", out var value) ? value.GetString() : null
            } : null,
            enable_predictive_optimization = catalog.TryGetProperty("enable_predictive_optimization", out var enablePredictive) ? enablePredictive.GetString() : null,
            full_name = catalog.TryGetProperty("full_name", out var fullName) ? fullName.GetString() : null,
            isolation_mode = catalog.TryGetProperty("isolation_mode", out var isolationMode) ? isolationMode.GetString() : null,
            metastore_id = catalog.TryGetProperty("metastore_id", out var metastoreId) ? metastoreId.GetString() : null,
            name = catalog.TryGetProperty("name", out var name) ? name.GetString() : null,
            options = catalog.TryGetProperty("options", out var options) ? options : (object?)null,
            owner = catalog.TryGetProperty("owner", out var owner) ? owner.GetString() : null,
            properties = catalog.TryGetProperty("properties", out var properties) ? properties : (object?)null,
            provider_name = catalog.TryGetProperty("provider_name", out var providerName) ? providerName.GetString() : null,
            provisioning_info = catalog.TryGetProperty("provisioning_info", out var provisioningInfo) ? new
            {
                state = provisioningInfo.TryGetProperty("state", out var state) ? state.GetString() : null
            } : null,
            securable_type = catalog.TryGetProperty("securable_type", out var securableType) ? securableType.GetString() : null,
            share_name = catalog.TryGetProperty("share_name", out var shareName) ? shareName.GetString() : null,
            storage_location = catalog.TryGetProperty("storage_location", out var storageLocation) ? storageLocation.GetString() : null,
            storage_root = catalog.TryGetProperty("storage_root", out var storageRoot) ? storageRoot.GetString() : null,
            updated_at = catalog.TryGetProperty("updated_at", out var updatedAt) ? updatedAt.GetInt64() : 0L,
            updated_by = catalog.TryGetProperty("updated_by", out var updatedBy) ? updatedBy.GetString() : null
        };
    }

    private static void PopulateCatalogProperties(DatabricksUnityCatalog props, dynamic catalog)
    {
        props.BrowseOnly = catalog.browse_only ?? false;
        
        if (catalog.catalog_type != null)
        {
            if (Enum.TryParse<CatalogType>((string)catalog.catalog_type, out var catalogTypeEnum))
                props.CatalogType = catalogTypeEnum;
        }
            
        props.CreatedAt = (int)(catalog.created_at ?? 0L);
        props.CreatedBy = catalog.created_by;
        
        if (catalog.effective_predictive_optimization_flag != null)
        {
            props.EffectivePredictiveOptimizationFlag = new EffectivePredictiveOptimizationFlag
            {
                InheritedFromName = catalog.effective_predictive_optimization_flag.inherited_from_name,
                InheritedFromType = catalog.effective_predictive_optimization_flag.inherited_from_type != null ? 
                    (Enum.TryParse<InheritedFromType>((string)catalog.effective_predictive_optimization_flag.inherited_from_type, out var inheritedType) ? inheritedType : null) : null,
                Value = catalog.effective_predictive_optimization_flag.value != null ? 
                    (Enum.TryParse<PredictiveOptimizationFlag>((string)catalog.effective_predictive_optimization_flag.value, out var flagValue) ? flagValue : null) : null
            };
        }
        
        if (catalog.enable_predictive_optimization != null)
        {
            if (Enum.TryParse<PredictiveOptimizationFlag>((string)catalog.enable_predictive_optimization, out var enablePredictiveEnum))
                props.EnablePredictiveOptimization = enablePredictiveEnum;
        }
            
        props.FullName = catalog.full_name;
        
        if (catalog.isolation_mode != null)
        {
            if (Enum.TryParse<IsolationMode>((string)catalog.isolation_mode, out var isolationModeEnum))
                props.IsolationMode = isolationModeEnum;
        }
            
        props.MetastoreId = catalog.metastore_id;
        props.Owner = catalog.owner;
        
        if (catalog.provisioning_info != null)
        {
            props.ProvisioningInfo = new ProvisioningInfo
            {
                State = catalog.provisioning_info.state != null ? 
                    (Enum.TryParse<ProvisioningState>((string)catalog.provisioning_info.state, out var provisioningState) ? provisioningState : null) : null
            };
        }
        
        if (catalog.securable_type != null)
        {
            if (Enum.TryParse<SecurableType>((string)catalog.securable_type, out var securableTypeEnum))
                props.SecurableType = securableTypeEnum;
        }
            
        props.StorageLocation = catalog.storage_location;
        props.UpdatedAt = (int)(catalog.updated_at ?? 0L);
        props.UpdatedBy = catalog.updated_by;

        // Update configuration properties that might have been defaulted or changed
        if (catalog.comment != null)
            props.Comment = catalog.comment;
            
        if (catalog.options != null)
            props.Options = catalog.options;
            
        if (catalog.properties != null)
            props.Properties = catalog.properties;
    }

    private static void ValidateCatalogProperties(DatabricksUnityCatalog props)
    {
        if (string.IsNullOrWhiteSpace(props.Name))
            throw new ArgumentException("Catalog name cannot be null or empty.", nameof(props.Name));

        // Validate that only one of the mutually exclusive properties is set
        var hasConnectionName = !string.IsNullOrEmpty(props.ConnectionName);
        var hasProviderName = !string.IsNullOrEmpty(props.ProviderName);
        var hasShareName = !string.IsNullOrEmpty(props.ShareName);

        if (hasConnectionName && (hasProviderName || hasShareName))
            throw new ArgumentException("ConnectionName cannot be used with ProviderName or ShareName.", nameof(props.ConnectionName));

        if ((hasProviderName || hasShareName) && !string.IsNullOrEmpty(props.StorageRoot))
            throw new ArgumentException("ProviderName and ShareName cannot be used with StorageRoot.", nameof(props.ProviderName));

        if (hasProviderName && !hasShareName)
            throw new ArgumentException("ProviderName requires ShareName to also be specified.", nameof(props.ProviderName));

        if (hasShareName && !hasProviderName)
            throw new ArgumentException("ShareName requires ProviderName to also be specified.", nameof(props.ShareName));
    }
}
