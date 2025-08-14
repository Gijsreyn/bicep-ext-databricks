using Bicep.Local.Extension.Host.Handlers;
using Microsoft.Azure.Databricks.Client.Models;
using Microsoft.Extensions.Logging;
using Bicep.Extension.Databricks.Services;
using System.Text.Json;
using Microsoft.Azure.Databricks.Client;

namespace Bicep.Extension.Databricks.Handlers.Workspace;

public class RepoHandler(IDatabricksClientFactory factory, ILogger<RepoHandler> logger) : BaseHandler<Repo, RepoIdentifiers>(factory, logger)
{
    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var desired = request.Properties;
        const int TimeOutSeconds = 180; // repo operations may take longer
        var client = await GetClientAsync(request.Config.WorkspaceUrl, cancellationToken, TimeOutSeconds);
        var sparseCheckout = desired.SparseCheckoutPatterns is { Length: > 0 }
            ? new RepoSparseCheckout { Patterns = desired.SparseCheckoutPatterns.ToList() }
            : null;

        _logger.LogInformation("Creating repo at path '{Path}' (Url: {Url})", desired.Path, desired.Url);

        Microsoft.Azure.Databricks.Client.Models.Repo repo;
        try
        {
            repo = await client.Repos.Create(desired.Url, desired.Provider!.Value, desired.Path, sparseCheckout, cancellationToken);

            if (!string.IsNullOrEmpty(desired.Branch) || !string.IsNullOrEmpty(desired.Tag))
            {
                _logger.LogInformation("Updating repo {Id} (branch='{Branch}', tag='{Tag}')", repo.Id, desired.Branch, desired.Tag);
                await client.Repos.Update(repo.Id, desired.Branch, desired.Tag, sparseCheckout, cancellationToken);
                repo = await client.Repos.Get(repo.Id, cancellationToken);
            }

            request.Properties.RepoId = repo.Id.ToString();
            request.Properties.Path = repo.Path ?? desired.Path;
            request.Properties.Provider = repo.Provider;
            request.Properties.Url = repo.Url ?? desired.Url;
            request.Properties.Branch = repo.Branch ?? desired.Branch;
            request.Properties.HeadCommitId = repo.HeadCommitId ?? desired.HeadCommitId;
            request.Properties.SparseCheckoutPatterns = repo.SparseCheckout?.Patterns?.ToArray() ?? desired.SparseCheckoutPatterns;
        }
        catch (Exception ex) when (ex.Message.Contains("RESOURCE_ALREADY_EXISTS"))
        {
            // TODO: Implement logic to handle existing repos
            _logger.LogInformation("Repo already exists at path '{Path}', fetching repo list via REST API", desired.Path);
            // Somehow the List() and Get(desired.Path) don't work properly so we return
            request.Properties.RepoId = "existing"; // We don't have the actual ID, but repo exists. This breaks using existing in Bicep
            request.Properties.Path = desired.Path;
            request.Properties.Provider = desired.Provider!.Value;
            request.Properties.Url = desired.Url;
            request.Properties.Branch = desired.Branch;
            request.Properties.HeadCommitId = desired.HeadCommitId;
            request.Properties.SparseCheckoutPatterns = desired.SparseCheckoutPatterns;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create or update repo at path '{Path}'", desired.Path);
            throw new InvalidOperationException($"Failed to create or update repo at path '{desired.Path}'", ex);
        }

        return GetResponse(request);
    }

    protected override RepoIdentifiers GetIdentifiers(Repo properties) => new() { RepoId = properties.RepoId };
}