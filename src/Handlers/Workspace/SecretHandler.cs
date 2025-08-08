using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Host.Handlers;
using System.Text.Json;
using Microsoft.Azure.Databricks.Client;

namespace Bicep.Extension.Databricks.Handlers.Workspace;

public class SecretHandler : BaseHandler<Secret, SecretIdentifiers>
{
    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var secret = request.Properties;
        var client = await GetClientAsync(request, cancellationToken);

        Console.WriteLine($"[TRACE] Creating/updating secret in scope '{secret.Scope}' with key '{secret.Key}'");

        // Check if the scope exists
        var scopes = await client.Secrets.ListScopes(cancellationToken);
        var scopeExists = scopes.Any(s => s.Name == secret.Scope);
        
        if (scopeExists)
        {
            Console.WriteLine($"[TRACE] Scope '{secret.Scope}' exists, updating secret");
        }
        else
        {
            Console.WriteLine($"[TRACE] Scope '{secret.Scope}' does not exist, creating new secret");
        }

        if (!string.IsNullOrEmpty(secret.StringValue))
        {
            Console.WriteLine($"[TRACE] Putting string secret: {secret.Scope}/{secret.Key}");
            await client.Secrets.PutSecret(secret.Scope, secret.Key, secret.StringValue, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(secret.BytesValue))
        {
            Console.WriteLine($"[TRACE] Putting bytes secret: {secret.Scope}/{secret.Key}");
            // For bytes, we pass the base64 string directly as Databricks expects base64
            await client.Secrets.PutSecret(secret.Scope, secret.Key, secret.BytesValue, cancellationToken);
        }
        else
        {
            throw new ArgumentException("Either StringValue or BytesValue must be provided for the secret");
        }

        Console.WriteLine($"[TRACE] Secret {secret.Scope}/{secret.Key} created/updated successfully");

        // Create response with the secret identifiers
        return CreateSecretResourceResponse(secret);
    }

    private ResourceResponse CreateSecretResourceResponse(Secret secret)
    {
        // Since secrets are write-only in Databricks (we can't read them back),
        // we create a response with the identifiers that were provided
        var identifiers = new SecretIdentifiers
        {
            Scope = secret.Scope,
            Key = secret.Key
        };

        var secretResponse = new Secret
        {
            Scope = secret.Scope,
            Key = secret.Key,
            // Don't include StringValue or BytesValue in the response for security
        };

        var response = new ResourceResponse
        {
            Type = "Secret",
            Identifiers = identifiers,
            Properties = secretResponse
        };

        Console.WriteLine($"[TRACE] Created ResourceResponse for secret {secret.Scope}/{secret.Key}: {JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true })}");
        return response;
    }

    protected override SecretIdentifiers GetIdentifiers(Secret properties)
        => new()
        {
            Scope = properties.Scope,
            Key = properties.Key,
        };
}
