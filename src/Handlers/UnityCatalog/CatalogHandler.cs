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

        Catalog? info = null;
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
            desired.CreatedAt = info.CreatedAt;
            desired.CreatedBy = info.CreatedBy ?? string.Empty;
            desired.UpdatedAt = info.UpdatedAt;
            desired.UpdatedBy = info.UpdatedBy ?? string.Empty;
            desired.CatalogType = info.CatalogType;
            desired.StorageLocation = info.StorageLocation ?? string.Empty;
            desired.IsolationMode = info.IsolationMode;
            desired.FullName = info.FullName ?? string.Empty;
            desired.CatalogSecurableKind = info.CatalogSecurableKind;
            desired.SecurableType = info.SecurableType ?? "CATALOG";
            desired.ProvisioningInfo = info.ProvisioningInfo;
            desired.BrowseOnly = info.BrowseOnly;
        }

        return GetResponse(request);
    }

    protected override CatalogIdentifiers GetIdentifiers(Catalog properties) => new() { Name = properties.Name };

    private static Catalog ParseCatalogResponse(object response)
    {
        var json = JsonSerializer.Serialize(response);
        return JsonSerializer.Deserialize<Catalog>(json) ?? new Catalog { Name = string.Empty };
    }
}
