using Bicep.Extension.Databricks.Handlers;
using Microsoft.Azure.Databricks.Client.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bicep.Extension.Databricks.Handlers.UnityCatalog;

public class CatalogHandler : BaseHandler<Catalog, CatalogIdentifiers>
{
    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var catalog = request.Properties;
        var client = await GetClientAsync(request, cancellationToken);
        
        Console.WriteLine($"[TRACE] Creating/updating catalog: {catalog.Name}");

        // Validate that storage_root, provider_name, share_name, and connection_name are mutually exclusive
        var exclusiveFields = new[] { catalog.StorageRoot, catalog.ProviderName, catalog.ShareName, catalog.ConnectionName };
        var nonNullCount = exclusiveFields.Count(field => !string.IsNullOrEmpty(field));
        if (nonNullCount > 1)
        {
            throw new InvalidOperationException("Only one of storage_root, provider_name, share_name, or connection_name can be specified");
        }

        // Check if catalog already exists
        CatalogApiResponse? catalogInfo = null;
        bool catalogExists = false;

        try
        {
            // Try to get existing catalog using the client API
            var existingCatalog = await client.UnityCatalog.Catalogs.Get(catalog.Name, cancellationToken);
            catalogInfo = ParseCatalogResponse(existingCatalog);
            catalogExists = true;
            Console.WriteLine($"[TRACE] Catalog '{catalog.Name}' already exists. Will update properties...");
        }
        catch (Exception)
        {
            // Catalog doesn't exist, will create new one
            Console.WriteLine($"[TRACE] Catalog '{catalog.Name}' does not exist. Creating new catalog...");
        }

        if (!catalogExists)
        {
            // Create new catalog using the client API
            var createRequest = new Microsoft.Azure.Databricks.Client.Models.UnityCatalog.CatalogAttributes
            {
                Name = catalog.Name,
                Comment = catalog.Comment,
                StorageRoot = catalog.StorageRoot,
                ConnectionName = catalog.ConnectionName,
                ProviderName = catalog.ProviderName,
                ShareName = catalog.ShareName
            };

            try
            {
                var createdCatalog = await client.UnityCatalog.Catalogs.Create(createRequest, cancellationToken);
                catalogInfo = ParseCatalogResponse(createdCatalog);
                Console.WriteLine($"[TRACE] Catalog created successfully: {catalog.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to create catalog: {ex.Message}");
                throw;
            }
        }
        else
        {
            // Update existing catalog using the client API
            try
            {
                var updatedCatalog = await client.UnityCatalog.Catalogs.Update(catalog.Name, catalog.Comment ?? "", catalog.EnablePredictiveOptimization ?? "");
                catalogInfo = ParseCatalogResponse(updatedCatalog);
                Console.WriteLine($"[TRACE] Catalog updated successfully: {catalog.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to update catalog: {ex.Message}");
                throw;
            }
        }

        return CreateResourceResponse(
            catalogInfo,
            "Catalog",
            info => new CatalogIdentifiers { Name = info?.Name ?? string.Empty },
            info => new Catalog
            {
                Name = info?.Name ?? string.Empty,
                Comment = info?.Comment,
                StorageRoot = info?.StorageRoot,
                ConnectionName = info?.ConnectionName,
                ProviderName = info?.ProviderName,
                ShareName = info?.ShareName,
                EnablePredictiveOptimization = info?.EnablePredictiveOptimization,
                ForceDestroy = catalog.ForceDestroy,
                
                // Read-only properties from response
                Owner = info?.Owner ?? string.Empty,
                MetastoreId = info?.MetastoreId ?? string.Empty,
                CreatedAt = info?.CreatedAt?.ToString(),
                CreatedBy = info?.CreatedBy ?? string.Empty,
                UpdatedAt = info?.UpdatedAt?.ToString(),
                UpdatedBy = info?.UpdatedBy ?? string.Empty,
                CatalogType = MapCatalogType(info?.CatalogType),
                StorageLocation = info?.StorageLocation ?? string.Empty,
                IsolationMode = MapIsolationMode(info?.IsolationMode),
                FullName = info?.FullName ?? string.Empty,
                CatalogSecurableKind = MapSecurableKind(info?.SecurableKind),
                SecurableType = info?.SecurableType ?? "CATALOG",
                ProvisioningInfo = info?.ProvisioningInfo != null ? new ProvisioningInfo
                {
                    State = info.ProvisioningInfo.State
                } : null,
                BrowseOnly = info?.BrowseOnly ?? false
            }
        );
    }

    protected override CatalogIdentifiers GetIdentifiers(Catalog properties)
        => new()
        {
            Name = properties.Name,
        };

    private static CatalogApiResponse ParseCatalogResponse(object response)
    {
        // Convert the dynamic response to our API response model
        var json = JsonSerializer.Serialize(response);
        return JsonSerializer.Deserialize<CatalogApiResponse>(json) ?? new CatalogApiResponse();
    }

    private static CatalogType? MapCatalogType(string? catalogType)
    {
        return catalogType?.ToUpperInvariant() switch
        {
            "MANAGED_CATALOG" => CatalogType.MANAGED_CATALOG,
            "EXTERNAL_CATALOG" => CatalogType.EXTERNAL_CATALOG,
            "FOREIGN_CATALOG" => CatalogType.FOREIGN_CATALOG,
            _ => null
        };
    }

    private static IsolationMode? MapIsolationMode(string? isolationMode)
    {
        return isolationMode?.ToUpperInvariant() switch
        {
            "OPEN" => IsolationMode.OPEN,
            "ISOLATED" => IsolationMode.ISOLATED,
            _ => null
        };
    }

    private static CatalogSecurableKind? MapSecurableKind(string? securableKind)
    {
        return securableKind?.ToUpperInvariant() switch
        {
            "CATALOG_STANDARD" => CatalogSecurableKind.CATALOG_STANDARD,
            "CATALOG_FOREIGN" => CatalogSecurableKind.CATALOG_FOREIGN,
            _ => null
        };
    }
}
public class CatalogApiResponse
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [JsonPropertyName("storage_root")]
    public string? StorageRoot { get; set; }

    [JsonPropertyName("connection_name")]
    public string? ConnectionName { get; set; }

    [JsonPropertyName("provider_name")]
    public string? ProviderName { get; set; }

    [JsonPropertyName("share_name")]
    public string? ShareName { get; set; }

    [JsonPropertyName("enable_predictive_optimization")]
    public string? EnablePredictiveOptimization { get; set; }

    [JsonPropertyName("owner")]
    public string? Owner { get; set; }

    [JsonPropertyName("metastore_id")]
    public string? MetastoreId { get; set; }

    [JsonPropertyName("created_at")]
    public long? CreatedAt { get; set; }

    [JsonPropertyName("created_by")]
    public string? CreatedBy { get; set; }

    [JsonPropertyName("updated_at")]
    public long? UpdatedAt { get; set; }

    [JsonPropertyName("updated_by")]
    public string? UpdatedBy { get; set; }

    [JsonPropertyName("catalog_type")]
    public string? CatalogType { get; set; }

    [JsonPropertyName("storage_location")]
    public string? StorageLocation { get; set; }

    [JsonPropertyName("isolation_mode")]
    public string? IsolationMode { get; set; }

    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    [JsonPropertyName("securable_kind")]
    public string? SecurableKind { get; set; }

    [JsonPropertyName("securable_type")]
    public string? SecurableType { get; set; }

    [JsonPropertyName("provisioning_info")]
    public CatalogProvisioningInfo? ProvisioningInfo { get; set; }

    [JsonPropertyName("browse_only")]
    public bool? BrowseOnly { get; set; }
}

public class CatalogProvisioningInfo
{
    [JsonPropertyName("state")]
    public string? State { get; set; }
}
