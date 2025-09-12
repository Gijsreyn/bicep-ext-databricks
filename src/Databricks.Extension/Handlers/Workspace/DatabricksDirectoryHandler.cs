using System.Text.Json;
using Microsoft.Extensions.Logging;
using DatabricksDirectory = Databricks.Models.Workspace.Directory;
using DatabricksDirectoryIdentifiers = Databricks.Models.Workspace.DirectoryIdentifiers;
using Configuration = Databricks.Models.Configuration;
using ObjectType = Databricks.Models.Workspace.ObjectType;

namespace Databricks.Handlers.Workspace;

public class DirectoryStatus
{
    public ObjectType ObjectType { get; set; }
    public string? ObjectId { get; set; }
    public string? Path { get; set; }
    public string? Size { get; set; }
}

public class DatabricksDirectoryHandler : DatabricksResourceHandlerBase<DatabricksDirectory, DatabricksDirectoryIdentifiers>
{
    private const string WorkspaceMkdirsApiEndpoint = "2.0/workspace/mkdirs";
    private const string WorkspaceGetStatusApiEndpoint = "2.0/workspace/get-status";
    
    public DatabricksDirectoryHandler(ILogger<DatabricksDirectoryHandler> logger) : base(logger) { }

    protected override async Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
    {
        var existing = await GetDirectoryStatusAsync(request.Config, request.Properties, cancellationToken);
        if (existing is not null)
        {
            request.Properties.ObjectType = existing.ObjectType;
            request.Properties.ObjectId = existing.ObjectId;
            request.Properties.Size = existing.Size;
        }
        return GetResponse(request);
    }

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
    {
        return base.Get(request, cancellationToken);
    }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var props = request.Properties;

        ValidateDirectoryProperties(props);

        _logger.LogInformation("Ensuring directory at path {Path}", props.Path);

        var existing = await GetDirectoryStatusAsync(request.Config, props, cancellationToken);

        if (existing is null)
        {
            _logger.LogInformation("Creating directory at path {Path}", props.Path);
            await CreateDirectoryAsync(request.Config, props, cancellationToken);
            existing = await GetDirectoryStatusAsync(request.Config, props, cancellationToken)
                ?? throw new InvalidOperationException("Directory creation did not return directory status.");
        }
        else
        {
            _logger.LogInformation("Directory already exists at path {Path}", props.Path);
        }

        props.ObjectType = existing.ObjectType;
        props.ObjectId = existing.ObjectId;
        props.Size = existing.Size;

        return GetResponse(request);
    }

    protected override DatabricksDirectoryIdentifiers GetIdentifiers(DatabricksDirectory properties) => new()
    {
        Path = properties.Path
    };

    private async Task<DirectoryStatus?> GetDirectoryStatusAsync(Configuration configuration, DatabricksDirectory props, CancellationToken ct)
    {
        try
        {
            // Use query parameter for GET request
            var endpoint = $"{WorkspaceGetStatusApiEndpoint}?path={Uri.EscapeDataString(props.Path)}";

            var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Get, endpoint, ct);
            
            if (response.ValueKind == JsonValueKind.Undefined || response.ValueKind == JsonValueKind.Null)
                return null;
            
            // Check if it's a directory (object_type = "DIRECTORY" or 2)
            if (response.TryGetProperty("object_type", out var objectType))
            {
                ObjectType? parsedObjectType = null;
                
                if (objectType.ValueKind == JsonValueKind.String)
                {
                    var objectTypeString = objectType.GetString();
                    if (Enum.TryParse<ObjectType>(objectTypeString, true, out var enumValue))
                    {
                        parsedObjectType = enumValue;
                    }
                }
                else if (objectType.ValueKind == JsonValueKind.Number)
                {
                    var objectTypeInt = objectType.GetInt32();
                    // Map integer values to enum (legacy API support)
                    parsedObjectType = objectTypeInt switch
                    {
                        1 => ObjectType.NOTEBOOK,
                        2 => ObjectType.DIRECTORY,
                        3 => ObjectType.LIBRARY,
                        4 => ObjectType.FILE,
                        5 => ObjectType.REPO,
                        6 => ObjectType.DASHBOARD,
                        _ => null
                    };
                }
                
                if (parsedObjectType == ObjectType.DIRECTORY)
                {
                    return new DirectoryStatus
                    {
                        ObjectType = parsedObjectType.Value,
                        ObjectId = response.TryGetProperty("object_id", out var objectId) ? objectId.GetInt64().ToString() : null,
                        Path = response.TryGetProperty("path", out var path) ? path.GetString() : null,
                        Size = response.TryGetProperty("size", out var size) ? size.GetInt64().ToString() : null
                    };
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting directory status for path {Path}", props.Path);
            return null;
        }
    }

    private async Task CreateDirectoryAsync(Configuration configuration, DatabricksDirectory props, CancellationToken ct)
    {
        var createPayload = new
        {
            path = props.Path
        };

        try
        {
            var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Post, WorkspaceMkdirsApiEndpoint, ct, createPayload);
            
            // The mkdirs API typically returns an empty response on success, so we don't need to check the response content
            _logger.LogInformation("Successfully created directory at path {Path}", props.Path);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists") || ex.Message.Contains("RESOURCE_ALREADY_EXISTS"))
        {
            // Directory already exists - this is fine for mkdirs
            _logger.LogInformation("Directory already exists at path {Path}", props.Path);
        }
    }

    private static void ValidateDirectoryProperties(DatabricksDirectory props)
    {
        if (string.IsNullOrWhiteSpace(props.Path))
        {
            throw new ArgumentException("Directory path cannot be null or empty.", nameof(props.Path));
        }

        if (!props.Path.StartsWith("/"))
        {
            throw new ArgumentException("Directory path must start with '/'.", nameof(props.Path));
        }
    }
}
