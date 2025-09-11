using System.Text.Json;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace Bicep.Extension.Databricks.Handlers;

public class DatabricksGitCredentialHandler : DatabricksResourceHandlerBase<GitCredential, GitCredentialIdentifiers>
{
    public DatabricksGitCredentialHandler(ILogger<DatabricksGitCredentialHandler> logger) : base(logger) { }

    protected override async Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
    {
        var existing = await GetGitCredentialAsync(request.Config, request.Properties, cancellationToken);
        if (existing is not null)
        {
            request.Properties.CredentialId = existing.id;
            request.Properties.Name = existing.name;
            request.Properties.IsDefaultForProvider = existing.is_default_for_provider;
        }
        return GetResponse(request);
    }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var props = request.Properties;
        _logger.LogInformation("Ensuring git credential for provider {Provider} user {User}", props.GitProvider, props.GitUsername);

        var existing = await GetGitCredentialAsync(request.Config, props, cancellationToken);
        
        if (existing is null)
        {
            _logger.LogInformation("Creating new git credential (provider {Provider} user {User})", props.GitProvider, props.GitUsername);
            await CreateGitCredentialAsync(request.Config, props, cancellationToken);
            existing = await GetGitCredentialAsync(request.Config, props, cancellationToken) 
                ?? throw new InvalidOperationException("Git credential creation did not return credential.");
        }
        else
        {
            _logger.LogInformation("Updating existing git credential {Id}", (string)existing.id);
            await UpdateGitCredentialAsync(request.Config, props, existing, cancellationToken);
            existing = await GetGitCredentialAsync(request.Config, props, cancellationToken) 
                ?? throw new InvalidOperationException("Git credential update did not return credential.");
        }

        props.CredentialId = existing.id;
        props.Name = existing.name;
        props.IsDefaultForProvider = existing.is_default_for_provider;

        return GetResponse(request);
    }

    protected override GitCredentialIdentifiers GetIdentifiers(GitCredential properties) => new() 
    { 
        CredentialId = properties.CredentialId ?? string.Empty 
    };

    private async Task<dynamic?> GetGitCredentialAsync(Configuration configuration, GitCredential props, CancellationToken ct)
    {
        try
        {
            var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Get, "2.0/git-credentials", ct);
            
            if (!response.TryGetProperty("credentials", out var credentialsArray))
                return null;

            foreach (var credential in credentialsArray.EnumerateArray())
            {
                var gitProvider = credential.TryGetProperty("git_provider", out var provider) ? provider.GetString() : null;
                var gitUsername = credential.TryGetProperty("git_username", out var username) ? username.GetString() : null;
                
                if (gitProvider == props.GitProvider.ToString() && gitUsername == props.GitUsername)
                {
                    return new
                    {
                        id = credential.GetProperty("credential_id").GetInt64().ToString(),
                        git_provider = gitProvider,
                        git_username = gitUsername,
                        name = credential.TryGetProperty("name", out var n) ? n.GetString() : null,
                        is_default_for_provider = credential.TryGetProperty("is_default_for_provider", out var isDefault) && isDefault.GetBoolean()
                    };
                }
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task CreateGitCredentialAsync(Configuration configuration, GitCredential props, CancellationToken ct)
    {
        var createPayload = new
        {
            git_provider = props.GitProvider.ToString(),
            git_username = props.GitUsername,
            personal_access_token = props.PersonalAccessToken,
            name = props.Name,
            is_default_for_provider = props.IsDefaultForProvider
        };

        var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Post, "2.0/git-credentials", ct, createPayload);
        if (response.ValueKind == JsonValueKind.Undefined || response.ValueKind == JsonValueKind.Null)
        {
            throw new InvalidOperationException($"Failed to create git credential for provider '{props.GitProvider}' and user '{props.GitUsername}'.");
        }
    }

    private async Task UpdateGitCredentialAsync(Configuration configuration, GitCredential props, dynamic existing, CancellationToken ct)
    {
        var updatePayload = new
        {
            git_provider = props.GitProvider.ToString(),
            git_username = props.GitUsername,
            personal_access_token = props.PersonalAccessToken,
            name = props.Name,
            is_default_for_provider = props.IsDefaultForProvider
        };

        var credentialId = (string)existing.id;
        var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Patch, $"2.0/git-credentials/{credentialId}", ct, updatePayload);
        if (response.ValueKind == JsonValueKind.Undefined || response.ValueKind == JsonValueKind.Null)
        {
            throw new InvalidOperationException($"Failed to update git credential {credentialId} for provider '{props.GitProvider}' and user '{props.GitUsername}'.");
        }
    }
}