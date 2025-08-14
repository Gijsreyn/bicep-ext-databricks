using Microsoft.Extensions.Logging;
using Bicep.Extension.Databricks.Services;

namespace Bicep.Extension.Databricks.Handlers.Workspace;

public class DirectoryHandler : BaseHandler<Directory, DirectoryIdentifiers>
{
    public DirectoryHandler(IDatabricksClientFactory factory, ILogger<DirectoryHandler> logger)
        : base(factory, logger) { }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var desired = request.Properties; 
        var client = await GetClientAsync(request.Config.WorkspaceUrl, cancellationToken);
        _logger.LogInformation("Ensuring directory exists at path '{Path}'", desired.Path);
        await client.Workspace.Mkdirs(desired.Path, cancellationToken);
        var info = await client.Workspace.GetStatus(desired.Path, cancellationToken);

        request.Properties.Path = info.Path;
        request.Properties.ObjectType = (int)info.ObjectType;
        request.Properties.ObjectId = info.ObjectId.ToString();
        request.Properties.Size = (info.Size ?? 0).ToString();

        return GetResponse(request);
    }

    protected override DirectoryIdentifiers GetIdentifiers(Directory properties) => new() { Path = properties.Path };
}
