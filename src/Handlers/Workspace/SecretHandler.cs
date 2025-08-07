namespace Bicep.Extension.Databricks.Handlers.Workspace;

public class SecretHandler : BaseHandler<Secret, SecretIdentifiers>
{
    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var payload = new
        {
            scope = request.Properties.Scope,
            key = request.Properties.Key,
            string_value = request.Properties.StringValue,
            bytes_value = request.Properties.BytesValue
        };

        return await CallDatabricksApi(request, "/api/2.0/secrets/put", payload, cancellationToken);
    }

    protected override SecretIdentifiers GetIdentifiers(Secret properties)
        => new()
        {
            Scope = properties.Scope,
            Key = properties.Key,
        };
}
