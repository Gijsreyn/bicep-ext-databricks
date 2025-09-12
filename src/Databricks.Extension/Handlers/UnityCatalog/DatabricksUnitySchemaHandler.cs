using System.Text.Json;
using Microsoft.Extensions.Logging;
using DatabricksUnitySchema = Databricks.Models.UnityCatalog.UnitySchema;
using DatabricksUnitySchemaIdentifiers = Databricks.Models.UnityCatalog.UnitySchemaIdentifiers;
using Configuration = Databricks.Models.Configuration;
using Databricks.Models.UnityCatalog;

namespace Databricks.Handlers.UnityCatalog;

public class DatabricksUnitySchemaHandler : DatabricksResourceHandlerBase<DatabricksUnitySchema, DatabricksUnitySchemaIdentifiers>
{
    private const string SchemaCreateApiEndpoint = "2.1/unity-catalog/schemas";
    private const string SchemaListApiEndpoint = "2.1/unity-catalog/schemas";
    private const string SchemaUpdateApiEndpoint = "2.1/unity-catalog/schemas";

    public DatabricksUnitySchemaHandler(ILogger<DatabricksUnitySchemaHandler> logger) : base(logger) { }

    protected override async Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
    {
        var existing = await GetSchemaByNameAsync(request.Config, request.Properties.CatalogName, request.Properties.Name, cancellationToken);
        if (existing is not null)
        {
            PopulateSchemaProperties(request.Properties, existing);
        }
        return GetResponse(request);
    }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var props = request.Properties;
        
        ValidateSchemaProperties(props);
        
        _logger.LogInformation("Ensuring Unity Schema {SchemaName} in catalog {CatalogName}", props.Name, props.CatalogName);

        var existing = await GetSchemaByNameAsync(request.Config, props.CatalogName, props.Name, cancellationToken);

        dynamic schema;
        if (existing is null)
        {
            _logger.LogInformation("Creating new Unity Schema {SchemaName} in catalog {CatalogName}", props.Name, props.CatalogName);
            schema = await CreateSchemaAsync(request.Config, props, cancellationToken);
            
            // Update owner or predictive optimization if provided
            if (!string.IsNullOrEmpty(props.Owner) || props.EnablePredictiveOptimization.HasValue)
            {
                schema = await UpdateSchemaAsync(request.Config, props, schema, cancellationToken, true);
            }
        }
        else
        {
            _logger.LogInformation("Updating existing Unity Schema {SchemaName} in catalog {CatalogName}", props.Name, props.CatalogName);
            schema = await UpdateSchemaAsync(request.Config, props, existing, cancellationToken, false);
        }

        PopulateSchemaProperties(props, schema);
        return GetResponse(request);
    }

    protected override DatabricksUnitySchemaIdentifiers GetIdentifiers(DatabricksUnitySchema properties) => new()
    {
        Name = properties.Name
    };

    private async Task<dynamic?> GetSchemaByNameAsync(Configuration configuration, string catalogName, string schemaName, CancellationToken ct)
    {
        try
        {
            // Use list operation to find the schema by name
            var endpoint = $"{SchemaListApiEndpoint}?catalog_name={Uri.EscapeDataString(catalogName)}";
            var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Get, endpoint, ct);
            
            if (response.TryGetProperty("schemas", out var schemas))
            {
                foreach (var schemaElement in schemas.EnumerateArray())
                {
                    if (schemaElement.TryGetProperty("name", out var nameProperty) && 
                        nameProperty.GetString()?.Equals(schemaName, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return CreateSchemaObject(schemaElement);
                    }
                }
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Unity Schema by name {SchemaName} in catalog {CatalogName}", schemaName, catalogName);
            return null;
        }
    }

    private async Task<dynamic> CreateSchemaAsync(Configuration configuration, DatabricksUnitySchema props, CancellationToken ct)
    {
        var createPayload = BuildSchemaPayload(props);

        var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Post, SchemaCreateApiEndpoint, ct, createPayload);
        
        _logger.LogInformation("Created Unity Schema {SchemaName} in catalog {CatalogName}", props.Name, props.CatalogName);

        return CreateSchemaObject(response);
    }

    private async Task<dynamic> UpdateSchemaAsync(Configuration configuration, DatabricksUnitySchema props, dynamic existing, CancellationToken ct, bool isPostCreate)
    {
        var fullName = existing.full_name as string ?? $"{props.CatalogName}.{props.Name}";
        
        // Handle owner update separately if needed
        if (!string.IsNullOrEmpty(props.Owner) && props.Owner != existing.owner)
        {
            await UpdateSchemaOwnerAsync(configuration, fullName, props.Owner!, ct);
        }

        // Update other properties
        var updatePayload = BuildSchemaUpdatePayload(props);
        if (HasUpdateableChanges(updatePayload) || isPostCreate)
        {
            var endpoint = $"{SchemaUpdateApiEndpoint}/{Uri.EscapeDataString(fullName)}";
            var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Patch, endpoint, ct, updatePayload);
            return CreateSchemaObject(response);
        }

        return existing;
    }

    private async Task<dynamic> UpdateSchemaOwnerAsync(Configuration configuration, string fullName, string owner, CancellationToken ct)
    {
        var ownerPayload = new { owner = owner };
        var endpoint = $"{SchemaUpdateApiEndpoint}/{Uri.EscapeDataString(fullName)}";
        
        var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Patch, endpoint, ct, ownerPayload);
        
        _logger.LogInformation("Updated Unity Schema {FullName} owner to {Owner}", fullName, owner);
        
        return CreateSchemaObject(response);
    }

    private static dynamic BuildSchemaPayload(DatabricksUnitySchema props)
    {
        var payload = new
        {
            name = props.Name,
            catalog_name = props.CatalogName,
            comment = props.Comment,
            properties = props.Properties,
            storage_root = props.StorageRoot
        };

        return payload;
    }

    private static dynamic BuildSchemaUpdatePayload(DatabricksUnitySchema props)
    {
        var payload = new
        {
            comment = props.Comment,
            properties = props.Properties,
            enable_predictive_optimization = props.EnablePredictiveOptimization?.ToString()
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

    private static dynamic CreateSchemaObject(JsonElement response)
    {
        var schemaDict = new Dictionary<string, object?>();

        if (response.TryGetProperty("name", out var name))
            schemaDict["name"] = name.GetString();
        if (response.TryGetProperty("catalog_name", out var catalogName))
            schemaDict["catalog_name"] = catalogName.GetString();
        if (response.TryGetProperty("comment", out var comment))
            schemaDict["comment"] = comment.GetString();
        if (response.TryGetProperty("browse_only", out var browseOnly))
            schemaDict["browse_only"] = browseOnly.GetBoolean();
        if (response.TryGetProperty("catalog_type", out var catalogType))
            schemaDict["catalog_type"] = catalogType.GetString();
        if (response.TryGetProperty("created_at", out var createdAt))
        {
            if (createdAt.TryGetInt32(out var createdAtInt))
                schemaDict["created_at"] = createdAtInt;
            else if (createdAt.TryGetInt64(out var createdAtLong))
                schemaDict["created_at"] = (int)createdAtLong;
        }
        if (response.TryGetProperty("created_by", out var createdBy))
            schemaDict["created_by"] = createdBy.GetString();
        if (response.TryGetProperty("enable_predictive_optimization", out var enablePredictiveOptimization))
            schemaDict["enable_predictive_optimization"] = enablePredictiveOptimization.GetString();
        if (response.TryGetProperty("full_name", out var fullName))
            schemaDict["full_name"] = fullName.GetString();
        if (response.TryGetProperty("metastore_id", out var metastoreId))
            schemaDict["metastore_id"] = metastoreId.GetString();
        if (response.TryGetProperty("owner", out var owner))
            schemaDict["owner"] = owner.GetString();
        if (response.TryGetProperty("schema_id", out var schemaId))
            schemaDict["schema_id"] = schemaId.GetString();
        if (response.TryGetProperty("storage_location", out var storageLocation))
            schemaDict["storage_location"] = storageLocation.GetString();
        if (response.TryGetProperty("storage_root", out var storageRoot))
            schemaDict["storage_root"] = storageRoot.GetString();
        if (response.TryGetProperty("updated_at", out var updatedAt))
        {
            if (updatedAt.TryGetInt32(out var updatedAtInt))
                schemaDict["updated_at"] = updatedAtInt;
            else if (updatedAt.TryGetInt64(out var updatedAtLong))
                schemaDict["updated_at"] = (int)updatedAtLong;
        }
        if (response.TryGetProperty("updated_by", out var updatedBy))
            schemaDict["updated_by"] = updatedBy.GetString();

        // Handle properties as a dictionary
        if (response.TryGetProperty("properties", out var properties))
        {
            var propertiesDict = new Dictionary<string, object?>();
            foreach (var prop in properties.EnumerateObject())
            {
                propertiesDict[prop.Name] = prop.Value.GetString();
            }
            schemaDict["properties"] = propertiesDict;
        }

        // Handle effective_predictive_optimization_flag
        if (response.TryGetProperty("effective_predictive_optimization_flag", out var effectiveFlag))
        {
            schemaDict["effective_predictive_optimization_flag"] = ParseEffectivePredictiveOptimizationFlag(effectiveFlag);
        }

        var expando = new System.Dynamic.ExpandoObject();
        var expandoDict = (IDictionary<string, object?>)expando;
        foreach (var kvp in schemaDict)
        {
            expandoDict[kvp.Key] = kvp.Value;
        }
        return expando;
    }

    private static SchemaEffectivePredictiveOptimizationFlag? ParseEffectivePredictiveOptimizationFlag(JsonElement effectiveFlag)
    {
        var flag = new SchemaEffectivePredictiveOptimizationFlag();

        if (effectiveFlag.TryGetProperty("inherited_from_name", out var inheritedFromName))
            flag.InheritedFromName = inheritedFromName.GetString();
        if (effectiveFlag.TryGetProperty("inherited_from_type", out var inheritedFromType))
        {
            if (Enum.TryParse<InheritedFromType>(inheritedFromType.GetString(), true, out InheritedFromType inheritedType))
                flag.InheritedFromType = inheritedType;
        }
        if (effectiveFlag.TryGetProperty("value", out var value))
        {
            if (Enum.TryParse<PredictiveOptimizationFlag>(value.GetString(), true, out PredictiveOptimizationFlag flagValue))
                flag.Value = flagValue;
        }

        return flag;
    }

    private static void ValidateSchemaProperties(DatabricksUnitySchema props)
    {
        if (string.IsNullOrWhiteSpace(props.Name))
            throw new ArgumentException("Schema name is required.", nameof(props.Name));

        if (string.IsNullOrWhiteSpace(props.CatalogName))
            throw new ArgumentException("Catalog name is required.", nameof(props.CatalogName));
    }

    private void PopulateSchemaProperties(DatabricksUnitySchema props, dynamic schema)
    {
        props.BrowseOnly = schema.browse_only ?? false;
        props.CreatedAt = schema.created_at ?? 0;
        props.CreatedBy = schema.created_by;
        props.FullName = schema.full_name;
        props.MetastoreId = schema.metastore_id;
        props.SchemaId = schema.schema_id;
        props.StorageLocation = schema.storage_location;
        props.UpdatedAt = schema.updated_at ?? 0;
        props.UpdatedBy = schema.updated_by;

        // Parse catalog_type enum
        if (schema.catalog_type is not null)
        {
            if (Enum.TryParse<CatalogType>(schema.catalog_type, true, out CatalogType catType))
                props.CatalogType = catType;
        }

        // Parse enable_predictive_optimization enum
        if (schema.enable_predictive_optimization is not null)
        {
            if (Enum.TryParse<PredictiveOptimizationFlag>(schema.enable_predictive_optimization, true, out PredictiveOptimizationFlag predOptFlag))
                props.EnablePredictiveOptimization = predOptFlag;
        }

        // Handle owner
        if (schema.owner is not null)
            props.Owner = schema.owner;

        // Handle properties
        if (schema.properties is not null)
            props.Properties = schema.properties;

        // Handle effective_predictive_optimization_flag
        if (schema.effective_predictive_optimization_flag is not null)
            props.EffectivePredictiveOptimizationFlag = schema.effective_predictive_optimization_flag as SchemaEffectivePredictiveOptimizationFlag;
    }
}
