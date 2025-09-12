using System.Text.Json;
using Microsoft.Extensions.Logging;
using DatabricksUnityExternalLocation = Databricks.Models.UnityCatalog.UnityExternalLocation;
using DatabricksUnityExternalLocationIdentifiers = Databricks.Models.UnityCatalog.UnityExternalLocationIdentifiers;
using Configuration = Databricks.Models.Configuration;
using Databricks.Models.UnityCatalog;

namespace Databricks.Handlers.UnityCatalog;

public class DatabricksUnityExternalLocationHandler : DatabricksResourceHandlerBase<DatabricksUnityExternalLocation, DatabricksUnityExternalLocationIdentifiers>
{
    private const string ExternalLocationCreateApiEndpoint = "2.1/unity-catalog/external-locations";
    private const string ExternalLocationGetApiEndpoint = "2.1/unity-catalog/external-locations";
    private const string ExternalLocationUpdateApiEndpoint = "2.1/unity-catalog/external-locations";

    public DatabricksUnityExternalLocationHandler(ILogger<DatabricksUnityExternalLocationHandler> logger) : base(logger) { }

    protected override async Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
    {
        var existing = await GetExternalLocationByNameAsync(request.Config, request.Properties.Name, cancellationToken);
        if (existing is not null)
        {
            PopulateExternalLocationProperties(request.Properties, existing);
        }
        return GetResponse(request);
    }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var props = request.Properties;
        
        ValidateExternalLocationProperties(props);
        
        _logger.LogInformation("Ensuring Unity External Location {ExternalLocationName}", props.Name);

        var existing = await GetExternalLocationByNameAsync(request.Config, props.Name, cancellationToken);

        dynamic externalLocation;
        if (existing is null)
        {
            _logger.LogInformation("Creating new Unity External Location {ExternalLocationName}", props.Name);
            externalLocation = await CreateExternalLocationAsync(request.Config, props, cancellationToken);
            
            // Update owner or isolation mode if provided
            if (!string.IsNullOrEmpty(props.Owner))
            {
                externalLocation = await UpdateExternalLocationOwnerAsync(request.Config, props.Name, props.Owner!, cancellationToken);
            }
        }
        else
        {
            _logger.LogInformation("Updating existing Unity External Location {ExternalLocationName}", props.Name);
            externalLocation = await UpdateExternalLocationAsync(request.Config, props, existing, cancellationToken);
        }

        PopulateExternalLocationProperties(props, externalLocation);
        return GetResponse(request);
    }

    protected override DatabricksUnityExternalLocationIdentifiers GetIdentifiers(DatabricksUnityExternalLocation properties) => new()
    {
        Name = properties.Name
    };

    private async Task<dynamic?> GetExternalLocationByNameAsync(Configuration configuration, string externalLocationName, CancellationToken ct)
    {
        try
        {
            var endpoint = $"{ExternalLocationGetApiEndpoint}/{Uri.EscapeDataString(externalLocationName)}";
            var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Get, endpoint, ct);
            
            return CreateExternalLocationObject(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Unity External Location by name {ExternalLocationName}", externalLocationName);
            return null;
        }
    }

    private async Task<dynamic> CreateExternalLocationAsync(Configuration configuration, DatabricksUnityExternalLocation props, CancellationToken ct)
    {
        var createPayload = BuildExternalLocationPayload(props);

        var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Post, ExternalLocationCreateApiEndpoint, ct, createPayload);
        
        _logger.LogInformation("Created Unity External Location {ExternalLocationName}", props.Name);

        return CreateExternalLocationObject(response);
    }

    private async Task<dynamic> UpdateExternalLocationAsync(Configuration configuration, DatabricksUnityExternalLocation props, dynamic existing, CancellationToken ct)
    {
        // Handle owner update separately if needed
        if (!string.IsNullOrEmpty(props.Owner) && props.Owner != existing.owner)
        {
            await UpdateExternalLocationOwnerAsync(configuration, props.Name, props.Owner!, ct);
        }

        // Update other properties
        var updatePayload = BuildExternalLocationUpdatePayload(props);
        if (HasUpdateableChanges(updatePayload))
        {
            var endpoint = $"{ExternalLocationUpdateApiEndpoint}/{Uri.EscapeDataString(props.Name)}";
            var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Patch, endpoint, ct, updatePayload);
            return CreateExternalLocationObject(response);
        }

        return existing;
    }

    private async Task<dynamic> UpdateExternalLocationOwnerAsync(Configuration configuration, string externalLocationName, string owner, CancellationToken ct)
    {
        var ownerPayload = new { owner = owner };
        var endpoint = $"{ExternalLocationUpdateApiEndpoint}/{Uri.EscapeDataString(externalLocationName)}";
        
        var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Patch, endpoint, ct, ownerPayload);
        
        _logger.LogInformation("Updated Unity External Location {ExternalLocationName} owner to {Owner}", externalLocationName, owner);
        
        return CreateExternalLocationObject(response);
    }

    private static dynamic BuildExternalLocationPayload(DatabricksUnityExternalLocation props)
    {
        var payload = new
        {
            name = props.Name,
            url = props.Url,
            credential_name = props.CredentialName,
            comment = props.Comment,
            enable_file_events = props.EnableFileEvents,
            encryption_details = props.EncryptionDetails,
            fallback = props.Fallback,
            file_event_queue = BuildFileEventQueue(props.FileEventQueue),
            read_only = props.ReadOnly,
            skip_validation = props.SkipValidation
        };

        return payload;
    }

    private static dynamic BuildExternalLocationUpdatePayload(DatabricksUnityExternalLocation props)
    {
        var payload = new
        {
            url = props.Url,
            credential_name = props.CredentialName,
            comment = props.Comment,
            enable_file_events = props.EnableFileEvents,
            encryption_details = props.EncryptionDetails,
            fallback = props.Fallback,
            file_event_queue = BuildFileEventQueue(props.FileEventQueue),
            read_only = props.ReadOnly,
            skip_validation = props.SkipValidation
        };

        return payload;
    }

    private static object? BuildFileEventQueue(FileEventQueue? fileEventQueue)
    {
        if (fileEventQueue is null) return null;

        var queue = new Dictionary<string, object?>();

        if (fileEventQueue.ManagedAqs is not null)
        {
            queue["managed_aqs"] = new
            {
                resource_group = fileEventQueue.ManagedAqs.ResourceGroup,
                subscription_id = fileEventQueue.ManagedAqs.SubscriptionId,
                queue_url = fileEventQueue.ManagedAqs.QueueUrl
            };
        }

        if (fileEventQueue.ManagedPubsub is not null)
        {
            queue["managed_pubsub"] = new
            {
                subscription_name = fileEventQueue.ManagedPubsub.SubscriptionName
            };
        }

        if (fileEventQueue.ManagedSqs is not null)
        {
            queue["managed_sqs"] = new
            {
                queue_url = fileEventQueue.ManagedSqs.QueueUrl
            };
        }

        if (fileEventQueue.ProvidedAqs is not null)
        {
            queue["provided_aqs"] = new
            {
                queue_url = fileEventQueue.ProvidedAqs.QueueUrl,
                resource_group = fileEventQueue.ProvidedAqs.ResourceGroup,
                subscription_id = fileEventQueue.ProvidedAqs.SubscriptionId
            };
        }

        if (fileEventQueue.ProvidedPubsub is not null)
        {
            queue["provided_pubsub"] = new
            {
                subscription_name = fileEventQueue.ProvidedPubsub.SubscriptionName
            };
        }

        if (fileEventQueue.ProvidedSqs is not null)
        {
            queue["provided_sqs"] = new
            {
                queue_url = fileEventQueue.ProvidedSqs.QueueUrl
            };
        }

        return queue.Count > 0 ? queue : null;
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

    private static dynamic CreateExternalLocationObject(JsonElement response)
    {
        var locationDict = new Dictionary<string, object?>();

        if (response.TryGetProperty("name", out var name))
            locationDict["name"] = name.GetString();
        if (response.TryGetProperty("url", out var url))
            locationDict["url"] = url.GetString();
        if (response.TryGetProperty("credential_name", out var credentialName))
            locationDict["credential_name"] = credentialName.GetString();
        if (response.TryGetProperty("comment", out var comment))
            locationDict["comment"] = comment.GetString();
        if (response.TryGetProperty("browse_only", out var browseOnly))
            locationDict["browse_only"] = browseOnly.GetBoolean();
        if (response.TryGetProperty("created_at", out var createdAt))
        {
            if (createdAt.TryGetInt32(out var createdAtInt))
                locationDict["created_at"] = createdAtInt;
            else if (createdAt.TryGetInt64(out var createdAtLong))
                locationDict["created_at"] = (int)createdAtLong;
        }
        if (response.TryGetProperty("created_by", out var createdBy))
            locationDict["created_by"] = createdBy.GetString();
        if (response.TryGetProperty("credential_id", out var credentialId))
            locationDict["credential_id"] = credentialId.GetString();
        if (response.TryGetProperty("enable_file_events", out var enableFileEvents))
            locationDict["enable_file_events"] = enableFileEvents.GetBoolean();
        if (response.TryGetProperty("fallback", out var fallback))
            locationDict["fallback"] = fallback.GetBoolean();
        if (response.TryGetProperty("isolation_mode", out var isolationMode))
            locationDict["isolation_mode"] = isolationMode.GetString();
        if (response.TryGetProperty("metastore_id", out var metastoreId))
            locationDict["metastore_id"] = metastoreId.GetString();
        if (response.TryGetProperty("owner", out var owner))
            locationDict["owner"] = owner.GetString();
        if (response.TryGetProperty("read_only", out var readOnly))
            locationDict["read_only"] = readOnly.GetBoolean();
        if (response.TryGetProperty("updated_at", out var updatedAt))
        {
            if (updatedAt.TryGetInt32(out var updatedAtInt))
                locationDict["updated_at"] = updatedAtInt;
            else if (updatedAt.TryGetInt64(out var updatedAtLong))
                locationDict["updated_at"] = (int)updatedAtLong;
        }
        if (response.TryGetProperty("updated_by", out var updatedBy))
            locationDict["updated_by"] = updatedBy.GetString();

        // Handle encryption_details
        if (response.TryGetProperty("encryption_details", out var encryptionDetails))
        {
            locationDict["encryption_details"] = JsonSerializer.Deserialize<object>(encryptionDetails.GetRawText());
        }

        // Handle file_event_queue
        if (response.TryGetProperty("file_event_queue", out var fileEventQueue))
        {
            locationDict["file_event_queue"] = ParseFileEventQueue(fileEventQueue);
        }

        var expando = new System.Dynamic.ExpandoObject();
        var expandoDict = (IDictionary<string, object?>)expando;
        foreach (var kvp in locationDict)
        {
            expandoDict[kvp.Key] = kvp.Value;
        }
        return expando;
    }

    private static object? ParseFileEventQueue(JsonElement fileEventQueue)
    {
        var queue = new FileEventQueue();

        if (fileEventQueue.TryGetProperty("managed_aqs", out var managedAqs))
        {
            queue.ManagedAqs = new ManagedAqs
            {
                ManagedResourceId = managedAqs.TryGetProperty("managed_resource_id", out var managedResourceId) ? managedResourceId.GetString() : null,
                QueueUrl = managedAqs.TryGetProperty("queue_url", out var queueUrl) ? queueUrl.GetString() : null,
                ResourceGroup = managedAqs.TryGetProperty("resource_group", out var resourceGroup) ? resourceGroup.GetString() ?? string.Empty : string.Empty,
                SubscriptionId = managedAqs.TryGetProperty("subscription_id", out var subscriptionId) ? subscriptionId.GetString() ?? string.Empty : string.Empty
            };
        }

        if (fileEventQueue.TryGetProperty("managed_pubsub", out var managedPubsub))
        {
            queue.ManagedPubsub = new ManagedPubSub
            {
                ManagedResourceId = managedPubsub.TryGetProperty("managed_resource_id", out var managedResourceId) ? managedResourceId.GetString() : null,
                SubscriptionName = managedPubsub.TryGetProperty("subscription_name", out var subscriptionName) ? subscriptionName.GetString() : null
            };
        }

        if (fileEventQueue.TryGetProperty("managed_sqs", out var managedSqs))
        {
            queue.ManagedSqs = new ManagedSqs
            {
                ManagedResourceId = managedSqs.TryGetProperty("managed_resource_id", out var managedResourceId) ? managedResourceId.GetString() : null,
                QueueUrl = managedSqs.TryGetProperty("queue_url", out var queueUrl) ? queueUrl.GetString() : null
            };
        }

        if (fileEventQueue.TryGetProperty("provided_aqs", out var providedAqs))
        {
            queue.ProvidedAqs = new ProvidedAqs
            {
                ManagedResourceId = providedAqs.TryGetProperty("managed_resource_id", out var managedResourceId) ? managedResourceId.GetString() : null,
                QueueUrl = providedAqs.TryGetProperty("queue_url", out var queueUrl) ? queueUrl.GetString() ?? string.Empty : string.Empty,
                ResourceGroup = providedAqs.TryGetProperty("resource_group", out var resourceGroup) ? resourceGroup.GetString() : null,
                SubscriptionId = providedAqs.TryGetProperty("subscription_id", out var subscriptionId) ? subscriptionId.GetString() : null
            };
        }

        if (fileEventQueue.TryGetProperty("provided_pubsub", out var providedPubsub))
        {
            queue.ProvidedPubsub = new ProvidedPubSub
            {
                ManagedResourceId = providedPubsub.TryGetProperty("managed_resource_id", out var managedResourceId) ? managedResourceId.GetString() : null,
                SubscriptionName = providedPubsub.TryGetProperty("subscription_name", out var subscriptionName) ? subscriptionName.GetString() ?? string.Empty : string.Empty
            };
        }

        if (fileEventQueue.TryGetProperty("provided_sqs", out var providedSqs))
        {
            queue.ProvidedSqs = new ProvidedSqs
            {
                ManagedResourceId = providedSqs.TryGetProperty("managed_resource_id", out var managedResourceId) ? managedResourceId.GetString() : null,
                QueueUrl = providedSqs.TryGetProperty("queue_url", out var queueUrl) ? queueUrl.GetString() ?? string.Empty : string.Empty
            };
        }

        return queue;
    }

    private static void ValidateExternalLocationProperties(DatabricksUnityExternalLocation props)
    {
        if (string.IsNullOrWhiteSpace(props.Name))
            throw new ArgumentException("External location name is required.", nameof(props.Name));

        if (string.IsNullOrWhiteSpace(props.Url))
            throw new ArgumentException("External location URL is required.", nameof(props.Url));

        if (string.IsNullOrWhiteSpace(props.CredentialName))
            throw new ArgumentException("Credential name is required.", nameof(props.CredentialName));
    }

    private void PopulateExternalLocationProperties(DatabricksUnityExternalLocation props, dynamic externalLocation)
    {
        props.BrowseOnly = externalLocation.browse_only ?? false;
        props.CreatedAt = externalLocation.created_at ?? 0;
        props.CreatedBy = externalLocation.created_by;
        props.CredentialId = externalLocation.credential_id;
        props.MetastoreId = externalLocation.metastore_id;
        props.UpdatedAt = externalLocation.updated_at ?? 0;
        props.UpdatedBy = externalLocation.updated_by;

        // Parse isolation_mode enum
        if (externalLocation.isolation_mode is not null)
        {
            if (Enum.TryParse<ExternalLocationIsolationMode>(externalLocation.isolation_mode, true, out ExternalLocationIsolationMode isoMode))
                props.IsolationMode = isoMode;
        }

        // Handle owner
        if (externalLocation.owner is not null)
            props.Owner = externalLocation.owner;

        // Handle encryption_details
        if (externalLocation.encryption_details is not null)
            props.EncryptionDetails = externalLocation.encryption_details;

        // Handle file_event_queue
        if (externalLocation.file_event_queue is not null)
            props.FileEventQueue = externalLocation.file_event_queue as FileEventQueue;
    }
}
