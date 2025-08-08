using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Host.Handlers;
using System.Text.Json;
using Microsoft.Azure.Databricks.Client;
using Microsoft.Azure.Databricks.Client.Models;

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

        // Get the directory information after creation and convert using generic helper
        var directoryInfo = await client.Workspace.GetStatus(directory.Path, cancellationToken);
        
        return CreateResourceResponse(
            directoryInfo,
            "Directory",
            info => new DirectoryIdentifiers { Path = info.Path },
            info => new Directory 
            { 
                Path = info.Path,
                ObjectType = (int)info.ObjectType,
                ObjectId = info.ObjectId.ToString(),
                Size = (info.Size ?? 0).ToString()
            }
        );
    }

    protected override DirectoryIdentifiers GetIdentifiers(Directory properties)
        => new()
        {
            Path = properties.Path,
        };
}
