namespace Bicep.Extension.Databricks.Handlers.Workspace;

public class DirectoryHandler : BaseHandler<Directory, DirectoryIdentifiers>
{
    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var payload = new
        {
            path = request.Properties.Path
        };

        return await CallDatabricksApi(request, "/api/2.0/workspace/mkdirs", payload, cancellationToken);
    }

    protected override DirectoryIdentifiers GetIdentifiers(Directory properties)
        => new()
        {
            Path = properties.Path,
        };
}
