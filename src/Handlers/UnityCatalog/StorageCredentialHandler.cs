using Microsoft.Extensions.Logging;
using Bicep.Extension.Databricks.Services;
using Microsoft.Azure.Databricks.Client.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bicep.Extension.Databricks.Handlers.UnityCatalog;

public class StorageCredentialHandler : BaseHandler<StorageCredential, StorageCredentialIdentifiers>
{
    public StorageCredentialHandler(IDatabricksClientFactory factory, ILogger<StorageCredentialHandler> logger) : base(factory, logger) { }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var desired = request.Properties;
        var client = await GetClientAsync(request.Config.WorkspaceUrl, cancellationToken);
        _logger.LogInformation("Ensuring storage credential '{Name}' exists", desired.Name);

        var desiredJson = JsonSerializer.Serialize(desired, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        _logger.LogInformation("Desired storage credential configuration: {DesiredConfig}", desiredJson);

        _logger.LogInformation("The managed identity id is {ManagedIdentityId}", desired.AzureManagedIdentity?.ManagedIdentityId);
        // Validate which authentication method is being used
        if (desired.AzureManagedIdentity != null)
        {
            if (string.IsNullOrEmpty(desired.AzureManagedIdentity.AccessConnectorId))
                throw new InvalidOperationException("AccessConnectorId is required when using AzureManagedIdentity authentication");
        }
        else if (desired.AzureServicePrincipal != null)
        {
            if (string.IsNullOrEmpty(desired.AzureServicePrincipal.ApplicationId))
                throw new InvalidOperationException("ApplicationId is required when using AzureServicePrincipal authentication");
            if (string.IsNullOrEmpty(desired.AzureServicePrincipal.ClientSecret))
                throw new InvalidOperationException("ClientSecret is required when using AzureServicePrincipal authentication");
            if (string.IsNullOrEmpty(desired.AzureServicePrincipal.DirectoryId))
                throw new InvalidOperationException("DirectoryId is required when using AzureServicePrincipal authentication");
        } 
        else 
        {
            throw new InvalidOperationException("Either AzureManagedIdentity or AzureServicePrincipal must be specified for authentication");
        }

        StorageCredential? info = null;
        bool exists = false;
        try
        {
            var existing = await client.UnityCatalog.StorageCredentials.Get(desired.Name, cancellationToken);
            info = ParseStorageCredentialResponse(existing);
            exists = true;
            _logger.LogInformation("Storage credential '{Name}' exists - updating", desired.Name);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Storage credential '{Name}' not found - creating", desired.Name);
        }

        if (!exists)
        {
            var createReq = new Microsoft.Azure.Databricks.Client.Models.UnityCatalog.StorageCredentialAttributes
            {
                Name = desired.Name,
                Comment = desired.Comment,
                ReadOnly = desired.ReadOnly
            };

            _logger.LogInformation("The managed identity connector is {AccessConnectorId}", desired.AzureManagedIdentity?.AccessConnectorId);

            // Set authentication method
            if (desired.AzureManagedIdentity?.AccessConnectorId != null)
            {
                _logger.LogInformation("Using Azure Managed Identity for storage credential '{Name}'", desired.Name);
                createReq.AzureManagedIdentity = new Microsoft.Azure.Databricks.Client.Models.UnityCatalog.AzureManagedIdentity
                {
                    AccessConnectorId = desired.AzureManagedIdentity.AccessConnectorId,
                    ManagedIdentityId = desired.AzureManagedIdentity.ManagedIdentityId
                };
            }
            else if (desired.AzureServicePrincipal != null)
            {
                createReq.AzureServicePrincipal = new Microsoft.Azure.Databricks.Client.Models.UnityCatalog.AzureServicePrincipal
                {
                    ApplicationId = desired.AzureServicePrincipal.ApplicationId,
                    ClientSecret = desired.AzureServicePrincipal.ClientSecret,
                    DirectoryId = desired.AzureServicePrincipal.DirectoryId
                };
            }
        
            var createReqJson = JsonSerializer.Serialize(createReq, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            _logger.LogInformation("Creating storage credential with request: {Request}", createReqJson);
            info = ParseStorageCredentialResponse(await client.UnityCatalog.StorageCredentials.Create(createReq));
        }
        else
        {
            var updateReq = new Microsoft.Azure.Databricks.Client.Models.UnityCatalog.StorageCredentialAttributes
            {
                Name = desired.Name,
                Comment = desired.Comment,
                ReadOnly = desired.ReadOnly
            };

            // Set authentication method for update
            if (desired.AzureManagedIdentity != null)
            {
                // updateReq.AzureManagedIdentity = new Microsoft.Azure.Databricks.Client.Models.UnityCatalog.AzureManagedIdentity
                // {
                //     AccessConnectorId = desired.AzureManagedIdentity.AccessConnectorId,
                //     ManagedIdentityId = desired.AzureManagedIdentity.ManagedIdentityId
                // };
            }
            else if (desired.AzureServicePrincipal != null)
            {
                updateReq.AzureServicePrincipal = new Microsoft.Azure.Databricks.Client.Models.UnityCatalog.AzureServicePrincipal
                {
                    ApplicationId = desired.AzureServicePrincipal.ApplicationId,
                    ClientSecret = desired.AzureServicePrincipal.ClientSecret,
                    DirectoryId = desired.AzureServicePrincipal.DirectoryId
                };
            }

            info = ParseStorageCredentialResponse(await client.UnityCatalog.StorageCredentials.Update(desired.Name, updateReq));
        }

        if (info != null)
        {
            desired.Comment = info.Comment;
            desired.ReadOnly = info.ReadOnly;
            desired.Owner = info.Owner ?? string.Empty;
            desired.MetastoreId = info.MetastoreId ?? string.Empty;
            desired.CreatedAt = info.CreatedAt;
            desired.CreatedBy = info.CreatedBy ?? string.Empty;
            desired.UpdatedAt = info.UpdatedAt;
            desired.UpdatedBy = info.UpdatedBy ?? string.Empty;
            desired.Isolated = info.Isolated;
            desired.FullName = info.FullName ?? string.Empty;
            desired.UsedForManagedStorage = info.UsedForManagedStorage;

            // Map authentication details
            if (info.AzureManagedIdentity != null)
            {
                // desired.AzureManagedIdentity = new AzureManagedIdentity
                // {
                //     AccessConnectorId = info.AzureManagedIdentity.AccessConnectorId,
                //     ManagedIdentityId = info.AzureManagedIdentity.ManagedIdentityId
                // };
                desired.AzureServicePrincipal = null;
            }
            else if (info.AzureServicePrincipal != null)
            {
                desired.AzureServicePrincipal = new AzureServicePrincipal
                {
                    ApplicationId = info.AzureServicePrincipal.ApplicationId,
                    ClientSecret = info.AzureServicePrincipal.ClientSecret,
                    DirectoryId = info.AzureServicePrincipal.DirectoryId
                };
                desired.AzureManagedIdentity = null;
            }
        }

        return GetResponse(request);
    }

    protected override StorageCredentialIdentifiers GetIdentifiers(StorageCredential properties) => new() { Name = properties.Name };

    private static StorageCredential ParseStorageCredentialResponse(object response)
    {
        var json = JsonSerializer.Serialize(response);
        return JsonSerializer.Deserialize<StorageCredential>(json) ?? new StorageCredential { Name = string.Empty };
    }
}
