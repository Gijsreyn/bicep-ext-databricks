using Microsoft.Extensions.Logging;
using Bicep.Extension.Databricks.Services;
using Microsoft.Azure.Databricks.Client.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bicep.Extension.Databricks.Handlers.UnityCatalog;

public class CatalogHandler : BaseHandler<Catalog, CatalogIdentifiers>
{
    public CatalogHandler(IDatabricksClientFactory factory, ILogger<CatalogHandler> logger) : base(factory, logger) { }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var desired = request.Properties;
        var client = await GetClientAsync(request.Config.WorkspaceUrl, cancellationToken);
        _logger.LogInformation("Ensuring catalog '{Name}'", desired.Name);

        var exclusive = new[] { desired.StorageRoot, desired.ProviderName, desired.ShareName, desired.ConnectionName };
        if (exclusive.Count(f => !string.IsNullOrEmpty(f)) > 1)
            throw new InvalidOperationException("Only one of storage_root, provider_name, share_name, or connection_name can be specified");

        CatalogApiResponse? info = null;
        bool exists = false;
        try
        {
            var existing = await client.UnityCatalog.Catalogs.Get(desired.Name, cancellationToken);
            info = ParseCatalogResponse(existing);
            exists = true;
            _logger.LogInformation("Catalog '{Name}' exists - updating", desired.Name);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Catalog '{Name}' not found - creating", desired.Name);
        }

        if (!exists)
        {
            var createReq = new Microsoft.Azure.Databricks.Client.Models.UnityCatalog.CatalogAttributes
            {
                Name = desired.Name,
                Comment = desired.Comment,
                StorageRoot = desired.StorageRoot,
                ConnectionName = desired.ConnectionName,
                ProviderName = desired.ProviderName,
                ShareName = desired.ShareName
            };
            info = ParseCatalogResponse(await client.UnityCatalog.Catalogs.Create(createReq, cancellationToken));
        }
        else
        {
            info = ParseCatalogResponse(await client.UnityCatalog.Catalogs.Update(desired.Name, desired.Comment ?? string.Empty, desired.EnablePredictiveOptimization ?? string.Empty));
        }

        if (info != null)
        {
            desired.Comment = info.Comment;
            desired.StorageRoot = info.StorageRoot;
            desired.ConnectionName = info.ConnectionName;
            desired.ProviderName = info.ProviderName;
            desired.ShareName = info.ShareName;
            desired.EnablePredictiveOptimization = info.EnablePredictiveOptimization;
            desired.Owner = info.Owner ?? string.Empty;
            desired.MetastoreId = info.MetastoreId ?? string.Empty;
            desired.CreatedAt = info.CreatedAt?.ToString();
            desired.CreatedBy = info.CreatedBy ?? string.Empty;
            desired.UpdatedAt = info.UpdatedAt?.ToString();
            desired.UpdatedBy = info.UpdatedBy ?? string.Empty;
            desired.CatalogType = info.CatalogType;
            desired.StorageLocation = info.StorageLocation ?? string.Empty;
            desired.IsolationMode = info.IsolationMode;
            desired.FullName = info.FullName ?? string.Empty;
            desired.CatalogSecurableKind = info.SecurableKind;
            desired.SecurableType = info.SecurableType ?? "CATALOG";
            desired.ProvisioningInfo = info.ProvisioningInfo != null ? new ProvisioningInfo { State = info.ProvisioningInfo.State } : null;
            desired.BrowseOnly = info.BrowseOnly ?? false;
        }

        return GetResponse(request);
    }

    protected override CatalogIdentifiers GetIdentifiers(Catalog properties) => new() { Name = properties.Name };

    private static CatalogApiResponse ParseCatalogResponse(object response)
    {
        var json = JsonSerializer.Serialize(response);
        return JsonSerializer.Deserialize<CatalogApiResponse>(json) ?? new CatalogApiResponse();
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
