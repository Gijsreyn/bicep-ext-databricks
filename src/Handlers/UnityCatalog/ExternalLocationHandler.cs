using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http;
using System.Linq;
using Microsoft.Extensions.Logging;
using Bicep.Extension.Databricks.Services;

namespace Bicep.Extension.Databricks.Handlers.UnityCatalog;

public class ExternalLocationHandler : BaseHandler<ExternalLocation, ExternalLocationIdentifiers>
{
    public ExternalLocationHandler(IDatabricksClientFactory factory, ILogger<ExternalLocationHandler> logger) : base(factory, logger) { }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var desired = request.Properties;
        var client = await GetClientAsync(request.Config.WorkspaceUrl, cancellationToken);
        _logger.LogInformation("Ensuring external location '{Name}'", desired.Name);

        // Determine if external location exists
        bool exists = false;
        ExternalLocation? info = null;
        try
        {
            var existing = await client.UnityCatalog.ExternalLocations.Get(desired.Name, cancellationToken);
            info = ParseExternalLocationResponse(existing);
            exists = true;
            _logger.LogInformation("External location '{Name}' exists - updating", desired.Name);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "External location '{Name}' not found - creating", desired.Name);
        }

        if (!exists)
        {
            var createPayload = new Dictionary<string, object?>
            {
                ["name"] = desired.Name,
                ["url"] = desired.Url,
                ["credential_name"] = desired.CredentialName,
                ["comment"] = desired.Comment,
                ["read_only"] = desired.ReadOnly,
                ["fallback_mode"] = desired.FallbackMode,
                ["skip_validation"] = desired.SkipValidation
            };
            var createJson = JsonSerializer.Serialize(createPayload, new JsonSerializerOptions { WriteIndented = true });
            _logger.LogInformation("Creating external location with request: {Request}", createJson);

            try
            {
                info = await CallDatabricksApiForResponse<ExternalLocation>(request.Config.WorkspaceUrl, HttpMethod.Post, "2.1/unity-catalog/external-locations", cancellationToken, createPayload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create external location '{Name}'", desired.Name);
                throw;
            }
        }
        else
        {
            // Use direct PATCH call because SDK update signature is limited.
            var updatePayload = new Dictionary<string, object?>
            {
                ["url"] = desired.Url,
                ["credential_name"] = desired.CredentialName,
                ["comment"] = desired.Comment,
                ["read_only"] = desired.ReadOnly,
                ["fallback_mode"] = desired.FallbackMode,
                ["skip_validation"] = desired.SkipValidation
            };
            // Remove null entries
            var cleaned = updatePayload.Where(kv => kv.Value != null).ToDictionary(kv => kv.Key, kv => kv.Value);
            var updateJson = JsonSerializer.Serialize(cleaned, new JsonSerializerOptions { WriteIndented = true });
            _logger.LogInformation("Updating external location with PATCH payload: {Request}", updateJson);

            try
            {
                await CallDatabricksApiForResponse<ExternalLocation>(request.Config.WorkspaceUrl, new HttpMethod("PATCH"), $"2.1/unity-catalog/external-locations/{Uri.EscapeDataString(desired.Name)}", cancellationToken, cleaned);
                // Re-fetch to get latest state
                info = await CallDatabricksApiForResponse<ExternalLocation>(request.Config.WorkspaceUrl, HttpMethod.Get, $"2.1/unity-catalog/external-locations/{Uri.EscapeDataString(desired.Name)}", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update external location '{Name}'", desired.Name);
                throw;
            }
        }

        if (info != null)
        {
            desired.Comment = info.Comment;
            desired.CredentialName = info.CredentialName;
            desired.ReadOnly = info.ReadOnly;
            desired.Owner = info.Owner;
            desired.FallbackMode = info.FallbackMode;
            desired.MetastoreId = info.MetastoreId;
            desired.CredentialId = info.CredentialId;
            desired.CreatedAt = info.CreatedAt;
            desired.CreatedBy = info.CreatedBy;
            desired.UpdatedAt = info.UpdatedAt;
            desired.UpdatedBy = info.UpdatedBy;
        }

        return GetResponse(request);
    }

    protected override ExternalLocationIdentifiers GetIdentifiers(ExternalLocation properties) => new() { Name = properties.Name, Url = properties.Url, CredentialName = properties.CredentialName };

    private static ExternalLocation ParseExternalLocationResponse(object response)
    {
        var json = JsonSerializer.Serialize(response);
        return JsonSerializer.Deserialize<ExternalLocation>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new ExternalLocation { Name = string.Empty, Url = string.Empty };
    }
}