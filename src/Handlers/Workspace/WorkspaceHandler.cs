using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Host.Handlers;
using System.Text.Json;
using Microsoft.Azure.Databricks.Client;

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
        var directoryInfo = await client.Workspace.GetStatus(directory.Path, cancellationToken);

        Console.WriteLine($"[TRACE] Directory info from Databricks: {JsonSerializer.Serialize(directoryInfo, new JsonSerializerOptions { WriteIndented = true })}");

        // Create and return ResourceResponse
        return new ResourceResponse
        {
            Type = "Directory",
            Identifiers = new DirectoryIdentifiers { Path = directory.Path },
            Properties = new Directory { Path = directory.Path }
        };
    }

    protected override DirectoryIdentifiers GetIdentifiers(Directory properties)
        => new()
        {
            Path = properties.Path,
        };
}
