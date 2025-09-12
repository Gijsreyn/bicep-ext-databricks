using System.Text.Json;
using Microsoft.Extensions.Logging;
using Databricks.Models;
using Databricks.Models.Workspace;

namespace Databricks.Handlers.Workspace;

public class DatabricksGitCredentialHandler : DatabricksResourceHandlerBase<GitCredential, GitCredentialIdentifiers>
{
    private const string GitCredentialsApiEndpoint = "2.0/git-credentials";
    
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
        
        if (props.GitProvider.HasValue)
        {
            ValidateGitUsername(props.GitProvider.Value, props.GitUsername);
        }
        
        if (props.GitProvider.HasValue)
        {
            ValidatePersonalAccessToken(props.GitProvider.Value, props.PersonalAccessToken);
        }
        
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
        GitProvider = properties.GitProvider,
        GitUsername = properties.GitUsername,
        PersonalAccessToken = properties.PersonalAccessToken
    };

    private async Task<dynamic?> GetGitCredentialAsync(Configuration configuration, GitCredential props, CancellationToken ct)
    {
        try
        {
            var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Get, GitCredentialsApiEndpoint, ct);
            
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

        var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Post, GitCredentialsApiEndpoint, ct, createPayload);
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
        var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Patch, $"{GitCredentialsApiEndpoint}/{credentialId}", ct, updatePayload);
        if (response.ValueKind == JsonValueKind.Undefined || response.ValueKind == JsonValueKind.Null)
        {
            throw new InvalidOperationException($"Failed to update git credential {credentialId} for provider '{props.GitProvider}' and user '{props.GitUsername}'.");
        }
    }

    private static void ValidateGitUsername(GitProvider provider, string gitUsername)
    {
        if (string.IsNullOrWhiteSpace(gitUsername))
        {
            throw new ArgumentException("Git username cannot be null or empty.", nameof(gitUsername));
        }

        switch (provider)
        {
            case GitProvider.gitLab:
            case GitProvider.gitLabEnterpriseEdition:
                // GitLab requires email format
                if (!IsValidEmail(gitUsername))
                {
                    throw new ArgumentException($"For {provider}, git username must be a valid email address.", nameof(gitUsername));
                }
                break;
            
            case GitProvider.awsCodeCommit:
            case GitProvider.bitbucketCloud:
            case GitProvider.bitbucketServer:
                // These providers require username (not email)
                if (IsValidEmail(gitUsername))
                {
                    throw new ArgumentException($"For {provider}, git username must be a username, not an email address.", nameof(gitUsername));
                }
                break;
            
            case GitProvider.gitHub:
            case GitProvider.gitHubEnterprise:
            case GitProvider.azureDevOpsServices:
                // These providers accept either email or username - no validation needed
                break;
            default:
                break;
        }
    }

    private static void ValidatePersonalAccessToken(GitProvider provider, string personalAccessToken)
    {
        if (string.IsNullOrWhiteSpace(personalAccessToken))
        {
            throw new ArgumentException("Personal access token cannot be null or empty.", nameof(personalAccessToken));
        }

        switch (provider)
        {
            case GitProvider.gitHub:
            case GitProvider.gitHubEnterprise:
                // GitHub tokens should be at least 40 characters long
                if (personalAccessToken.Length < 40)
                {
                    throw new ArgumentException($"For {provider}, personal access token must be at least 40 characters long.", nameof(personalAccessToken));
                }
                break;
            
            default:
                // For other providers, no specific length validation
                break;
        }
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
