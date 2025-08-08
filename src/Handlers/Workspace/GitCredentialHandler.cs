using System.Text.Json;

namespace Bicep.Extension.Databricks.Handlers.Workspace;

public class GitCredentialHandler : BaseHandler<GitCredential, GitCredentialIdentifiers>
{
    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var gitCredential = request.Properties;

        Console.WriteLine($"[TRACE] Creating/updating Git credential for provider '{gitCredential.GitProvider}' and username '{gitCredential.GitUsername}'");

        // First, list existing credentials to check if one already exists
        Console.WriteLine($"[TRACE] Checking for existing Git credentials");
        var listResponse = await CallDatabricksApiForResponse(request, "/api/2.0/git-credentials", new { }, cancellationToken);
        
        var existingCredentials = JsonSerializer.Deserialize<GitCredentialListResponse>(listResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        GitCredentialResponse? existingCredential = null;
        if (existingCredentials?.Credentials != null)
        {
            // First try to find by name if provided
            if (!string.IsNullOrEmpty(gitCredential.Name))
            {
                existingCredential = existingCredentials.Credentials.FirstOrDefault(c => 
                    c.Name == gitCredential.Name);
                Console.WriteLine($"[TRACE] Searched by name '{gitCredential.Name}': {(existingCredential != null ? $"Found credential ID {existingCredential.CredentialId}" : "Not found")}");
            }
            
            // If not found by name, fallback to provider + username matching
            if (existingCredential == null)
            {
                existingCredential = existingCredentials.Credentials.FirstOrDefault(c => 
                    c.GitProvider == gitCredential.GitProvider && 
                    c.GitUsername == gitCredential.GitUsername);
                Console.WriteLine($"[TRACE] Searched by provider/username '{gitCredential.GitProvider}/{gitCredential.GitUsername}': {(existingCredential != null ? $"Found credential ID {existingCredential.CredentialId}" : "Not found")}");
            }
        }

        GitCredentialResponse credentialInfo;
        if (existingCredential != null)
        {
            Console.WriteLine($"[TRACE] Found existing Git credential with ID: {existingCredential.CredentialId}. Updating...");
            
            // Update the existing credential
            var updatePayload = new
            {
                credential_id = existingCredential.CredentialId,
                git_username = gitCredential.GitUsername,
                git_provider = gitCredential.GitProvider,
                personal_access_token = gitCredential.PersonalAccessToken,
                name = gitCredential.Name,
                is_default_for_provider = gitCredential.IsDefaultForProvider
            };

            var updateResponse = await CallDatabricksApiForResponse(request, "/api/2.0/git-credentials", updatePayload, cancellationToken);
            credentialInfo = JsonSerializer.Deserialize<GitCredentialResponse>(updateResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }) ?? throw new InvalidOperationException("Failed to deserialize Git credential update response");
            
            Console.WriteLine($"[TRACE] Git credential updated successfully");
        }
        else
        {
            Console.WriteLine($"[TRACE] No existing Git credential found. Creating new credential...");
            
            // Create new credential
            var createPayload = new
            {
                git_provider = gitCredential.GitProvider,
                git_username = gitCredential.GitUsername,
                personal_access_token = gitCredential.PersonalAccessToken,
                name = gitCredential.Name,
                is_default_for_provider = gitCredential.IsDefaultForProvider
            };

            Console.WriteLine($"[TRACE] Create Git credential payload: Provider={gitCredential.GitProvider}, Username={gitCredential.GitUsername}, Name={gitCredential.Name}, IsDefault={gitCredential.IsDefaultForProvider}");

            var createResponse = await CallDatabricksApiForResponse(request, "/api/2.0/git-credentials", createPayload, cancellationToken);
            credentialInfo = JsonSerializer.Deserialize<GitCredentialResponse>(createResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }) ?? throw new InvalidOperationException("Failed to deserialize Git credential create response");
            
            Console.WriteLine($"[TRACE] Git credential created with ID: {credentialInfo.CredentialId}");
        }

        return new ResourceResponse
        {
            Type = "GitCredential",
            ApiVersion = "2.0",
            Identifiers = new GitCredentialIdentifiers { CredentialId = credentialInfo.CredentialId.ToString() },
            Properties = new GitCredential
            {
                CredentialId = credentialInfo.CredentialId.ToString(),
                GitProvider = credentialInfo.GitProvider ?? string.Empty,
                GitUsername = credentialInfo.GitUsername ?? string.Empty,
                PersonalAccessToken = gitCredential.PersonalAccessToken, // Keep the input value as it's not returned
                Name = credentialInfo.Name ?? gitCredential.Name,
                IsDefaultForProvider = credentialInfo.IsDefaultForProvider
            }
        };
    }

    protected override GitCredentialIdentifiers GetIdentifiers(GitCredential properties)
        => new()
        {
            CredentialId = properties.CredentialId ?? string.Empty,
        };
}

public class GitCredentialListResponse
{
    public GitCredentialResponse[]? Credentials { get; set; }
}

public class GitCredentialResponse
{
    public long CredentialId { get; set; }
    public string? GitProvider { get; set; }
    public string? GitUsername { get; set; }
    public string? Name { get; set; }
    public bool IsDefaultForProvider { get; set; }
}
