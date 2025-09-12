using System.Text.Json;
using Microsoft.Extensions.Logging;
using Databricks.Models;

namespace Databricks.Handlers;

public class DatabricksRepositoryHandler : DatabricksResourceHandlerBase<Repository, RepositoryIdentifiers>
{
    private const string ReposApiEndpoint = "2.0/repos";
    
    public DatabricksRepositoryHandler(ILogger<DatabricksRepositoryHandler> logger) : base(logger) { }

    protected override async Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
    {
        var existing = await GetRepositoryAsync(request.Config, request.Properties, cancellationToken);
        if (existing is not null)
        {
            request.Properties.Id = existing.id;
            request.Properties.HeadCommitId = existing.head_commit_id;
            request.Properties.Branch = existing.branch;
            if (existing.sparse_checkout?.patterns != null)
            {
                request.Properties.SparseCheckout = new SparseCheckout
                {
                    Patterns = existing.sparse_checkout.patterns
                };
            }
        }
        return GetResponse(request);
    }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var props = request.Properties;
        
        ValidateRepositoryProperties(props);
        
        _logger.LogInformation("Ensuring repository for provider {Provider} url {Url}", props.Provider, props.Url);

        var existing = await GetRepositoryAsync(request.Config, props, cancellationToken);

        if (existing is null)
        {
            _logger.LogInformation("Creating new repository (provider {Provider} url {Url})", props.Provider, props.Url);
            var createdRepoId = await CreateRepositoryAsync(request.Config, props, cancellationToken);
            existing = await GetRepositoryByIdAsync(request.Config, createdRepoId, cancellationToken)
                ?? throw new InvalidOperationException("Repository creation did not return repository.");
        }
        else
        {
            _logger.LogInformation("Updating existing repository {Id}", (string)existing.id);
            await UpdateRepositoryAsync(request.Config, props, existing, cancellationToken);
            existing = await GetRepositoryByIdAsync(request.Config, (string)existing.id, cancellationToken)
                ?? throw new InvalidOperationException("Repository update did not return repository.");
        }

        props.Id = existing.id;
        props.HeadCommitId = existing.head_commit_id;
        props.Branch = existing.branch;
        if (existing.sparse_checkout?.patterns != null)
        {
            props.SparseCheckout = new SparseCheckout
            {
                Patterns = existing.sparse_checkout.patterns
            };
        }

        return GetResponse(request);
    }

    protected override RepositoryIdentifiers GetIdentifiers(Repository properties) => new()
    {
        Provider = properties.Provider,
        Url = properties.Url,
        Path = properties.Path
    };

    private async Task<dynamic?> GetRepositoryAsync(Configuration configuration, Repository props, CancellationToken ct)
    {
        try
        {
            var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Get, ReposApiEndpoint, ct);
            
            if (!response.TryGetProperty("repos", out var reposArray))
                return null;

            foreach (var repo in reposArray.EnumerateArray())
            {
                var provider = repo.TryGetProperty("provider", out var providerProp) ? providerProp.GetString() : null;
                var url = repo.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
                var path = repo.TryGetProperty("path", out var pathProp) ? pathProp.GetString() : null;
                
                if (provider == props.Provider.ToString() && url == props.Url && 
                    (string.IsNullOrEmpty(props.Path) || path == props.Path))
                {
                    return CreateRepositoryObject(repo);
                }
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<dynamic?> GetRepositoryByIdAsync(Configuration configuration, string repoId, CancellationToken ct)
    {
        try
        {
            var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Get, $"{ReposApiEndpoint}/{repoId}", ct);
            
            if (response.ValueKind == JsonValueKind.Undefined || response.ValueKind == JsonValueKind.Null)
                return null;

            return CreateRepositoryObject(response);
        }
        catch
        {
            return null;
        }
    }

    private static dynamic CreateRepositoryObject(JsonElement repo)
    {
        // Extract sparse checkout patterns if they exist
        string[]? patterns = null;
        if (repo.TryGetProperty("sparse_checkout", out var sparseCheckout) &&
            sparseCheckout.TryGetProperty("patterns", out var patternsArray))
        {
            patterns = patternsArray.EnumerateArray()
                .Select(p => p.GetString())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray()!;
        }

        return new
        {
            id = repo.GetProperty("id").GetInt64().ToString(),
            provider = repo.TryGetProperty("provider", out var provider) ? provider.GetString() : null,
            url = repo.TryGetProperty("url", out var url) ? url.GetString() : null,
            path = repo.TryGetProperty("path", out var path) ? path.GetString() : null,
            branch = repo.TryGetProperty("branch", out var b) ? b.GetString() : null,
            head_commit_id = repo.TryGetProperty("head_commit_id", out var hci) ? hci.GetString() : null,
            sparse_checkout = patterns != null ? new { patterns } : null
        };
    }

    private async Task<string> CreateRepositoryAsync(Configuration configuration, Repository props, CancellationToken ct)
    {
        var createPayload = new Dictionary<string, object?>
        {
            ["provider"] = props.Provider.ToString(),
            ["url"] = props.Url
        };

        if (!string.IsNullOrWhiteSpace(props.Path))
            createPayload["path"] = props.Path;

        if (!string.IsNullOrWhiteSpace(props.Branch))
            createPayload["branch"] = props.Branch;

        if (props.SparseCheckout?.Patterns != null && props.SparseCheckout.Patterns.Length > 0)
        {
            createPayload["sparse_checkout"] = new
            {
                patterns = props.SparseCheckout.Patterns
            };
        }

        var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Post, ReposApiEndpoint, ct, createPayload);
        if (response.ValueKind == JsonValueKind.Undefined || response.ValueKind == JsonValueKind.Null)
        {
            throw new InvalidOperationException($"Failed to create repository for provider '{props.Provider}' and url '{props.Url}'.");
        }

        // Extract the repository ID from the response
        if (response.TryGetProperty("id", out var idProp))
        {
            return idProp.GetInt64().ToString();
        }

        throw new InvalidOperationException("Repository creation response did not contain an ID.");
    }

    private async Task UpdateRepositoryAsync(Configuration configuration, Repository props, dynamic existing, CancellationToken ct)
    {
        var updatePayload = new Dictionary<string, object?>
        {
            ["branch"] = props.Branch
        };

        if (props.SparseCheckout?.Patterns != null && props.SparseCheckout.Patterns.Length > 0)
        {
            updatePayload["sparse_checkout"] = new
            {
                patterns = props.SparseCheckout.Patterns
            };
        }

        var repoId = (string)existing.id;
        var response = await CallDatabricksApiForResponse<JsonElement>(configuration.WorkspaceUrl, HttpMethod.Patch, $"{ReposApiEndpoint}/{repoId}", ct, updatePayload);
        if (response.ValueKind == JsonValueKind.Undefined || response.ValueKind == JsonValueKind.Null)
        {
            throw new InvalidOperationException($"Failed to update repository {repoId} for provider '{props.Provider}' and url '{props.Url}'.");
        }
    }

    private static void ValidateRepositoryProperties(Repository props)
    {
        if (!Uri.TryCreate(props.Url, UriKind.Absolute, out var uri) || 
            (uri.Scheme != "https" && uri.Scheme != "http"))
        {
            throw new ArgumentException("URL must be a valid HTTP or HTTPS URL.", nameof(props.Url));
        }
    }
}
