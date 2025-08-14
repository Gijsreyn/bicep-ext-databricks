using System.Text.Json;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Bicep.Extension.Databricks.Services;
using System.Text.Json.Serialization;

namespace Bicep.Extension.Databricks.Handlers.Workspace;

public class GitCredentialHandler : BaseHandler<GitCredential, GitCredentialIdentifiers>
{
    public GitCredentialHandler(IDatabricksClientFactory factory, ILogger<GitCredentialHandler> logger) : base(factory, logger) { }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        const int TimeoutSeconds = 30;
        var desired = request.Properties;
        _logger.LogInformation("Ensuring git credential for provider {Provider} user {User}", desired.GitProvider, desired.GitUsername);

        var list = await CallDatabricksApiForResponse<GitCredentialListResponse>(request.Config.WorkspaceUrl, HttpMethod.Get, "2.0/git-credentials", cancellationToken, timeoutSeconds: TimeoutSeconds);

        var existing = list?.Credentials?.FirstOrDefault(c =>
            (!string.IsNullOrEmpty(desired.Name) && c.Name == desired.Name) ||
            (c.GitProvider == desired.GitProvider && c.GitUsername == desired.GitUsername));

        GitCredentialResponse final; 
        if (existing != null)
        {
            _logger.LogInformation("Updating existing git credential {Id}", existing.CredentialId);
            var updatePayload = new
            {
                git_username = desired.GitUsername,
                personal_access_token = desired.PersonalAccessToken,
                git_provider = desired.GitProvider,
                name = desired.Name,
                is_default_for_provider = desired.IsDefaultForProvider
            };
            final = await CallDatabricksApiForResponse<GitCredentialResponse>(request.Config.WorkspaceUrl, HttpMethod.Patch, $"2.0/git-credentials/{existing.CredentialId}", cancellationToken, updatePayload, TimeoutSeconds)
                ?? throw new InvalidOperationException("Null response updating git credential");
        }
        else
        {
            _logger.LogInformation("Creating new git credential (provider {Provider} user {User})", desired.GitProvider, desired.GitUsername);
            var createPayload = new
            {
                git_provider = desired.GitProvider,
                git_username = desired.GitUsername,
                personal_access_token = desired.PersonalAccessToken,
                name = desired.Name,
                is_default_for_provider = desired.IsDefaultForProvider
            };
            final = await CallDatabricksApiForResponse<GitCredentialResponse>(request.Config.WorkspaceUrl, HttpMethod.Post, "2.0/git-credentials", cancellationToken, createPayload, TimeoutSeconds)
                ?? throw new InvalidOperationException("Null response creating git credential");
        }

        request.Properties.CredentialId = final.CredentialId.ToString();
        request.Properties.GitProvider = final.GitProvider ?? desired.GitProvider;
        request.Properties.GitUsername = final.GitUsername ?? desired.GitUsername;
        request.Properties.Name = final.Name ?? desired.Name;
        request.Properties.IsDefaultForProvider = final.IsDefaultForProvider;
        request.Properties.PersonalAccessToken = final.PersonalAccessToken ?? desired.PersonalAccessToken;

        return GetResponse(request);
    }

    protected override GitCredentialIdentifiers GetIdentifiers(GitCredential properties) => new() { CredentialId = properties.CredentialId ?? string.Empty };
}

public class GitCredentialListResponse { public GitCredentialResponse[]? Credentials { get; set; } }

// 
public class GitCredentialResponse
{
    [JsonPropertyName("credential_id")] public long CredentialId { get; set; }
    [JsonPropertyName("git_provider")] public string? GitProvider { get; set; }
    [JsonPropertyName("git_username")] public string? GitUsername { get; set; }
    public string? Name { get; set; }
    [JsonPropertyName("is_default_for_provider")] public bool IsDefaultForProvider { get; set; }
    [JsonPropertyName("personal_access_token")] public string? PersonalAccessToken { get; set; }
}
