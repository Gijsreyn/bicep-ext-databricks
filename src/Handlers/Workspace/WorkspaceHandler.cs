using System.Text.Json;

namespace Bicep.Extension.Databricks.Handlers.Workspace;

public class DirectoryHandler : BaseHandler<Directory, DirectoryIdentifiers>
{
    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var directory = request.Properties;
        var client = await GetClientAsync(request, cancellationToken);

        Console.WriteLine($"[TRACE] Creating/updating directory at path '{directory.Path}'");

        // Create directory using DatabricksClient
        await client.Workspace.Mkdirs(directory.Path, cancellationToken);

        // Get the directory information after creation
        var directoryResult = await client.Workspace.GetStatus(directory.Path, cancellationToken);

        Console.WriteLine($"[TRACE] Directory '{directory.Path}' created/updated successfully");

        // Create and return the ResourceResponse with the directory result
        return CreateDirectoryResourceResponse(directory, directoryResult);
    }

    private ResourceResponse CreateDirectoryResourceResponse(Directory directory, object directoryResult)
    {
        var identifiers = new DirectoryIdentifiers
        {
            Path = directory.Path
        };

        var directoryResponse = new Directory
        {
            Path = directory.Path
        };

        var response = new ResourceResponse
        {
            Type = "Directory",
            Identifiers = identifiers,
            Properties = directoryResponse
        };

        // Create a response with Status for proper serialization
        var responseWithStatus = new
        {
            Type = "Directory",
            Identifiers = identifiers,
            Properties = directoryResponse,
            Status = "Succeeded"
        };

        // Log the actual Databricks API result as JSON
        Console.WriteLine($"[TRACE] Databricks API result: {JsonSerializer.Serialize(directoryResult, new JsonSerializerOptions { WriteIndented = true })}");
        Console.WriteLine($"[TRACE] Response with Status: {JsonSerializer.Serialize(responseWithStatus, new JsonSerializerOptions { WriteIndented = true })}");

        return response;
    }

    protected override DirectoryIdentifiers GetIdentifiers(Directory properties)
        => new()
        {
            Path = properties.Path,
        };
}
