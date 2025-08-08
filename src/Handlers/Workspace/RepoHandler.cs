using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Host.Handlers;
using System.Text.Json;
using Microsoft.Azure.Databricks.Client;
using Microsoft.Azure.Databricks.Client.Models;

namespace Bicep.Extension.Databricks.Handlers.Workspace;

public class RepoHandler : BaseHandler<Repo, RepoIdentifiers>
{
    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var repo = request.Properties;
        var client = await GetClientAsync(request, cancellationToken);

        Console.WriteLine($"[TRACE] Creating/updating repository '{repo.Url}' at path '{repo.Path}'");

        // Parse the provider string to the enum
        if (!Enum.TryParse<RepoProvider>(repo.Provider, true, out var providerEnum))
        {
            throw new ArgumentException($"Invalid provider: {repo.Provider}. Valid providers are: {string.Join(", ", Enum.GetNames<RepoProvider>())}");
        }

        // Create sparse checkout configuration if patterns are provided
        RepoSparseCheckout? sparseCheckout = null;
        if (repo.SparseCheckoutPatterns != null && repo.SparseCheckoutPatterns.Length > 0)
        {
            sparseCheckout = new RepoSparseCheckout
            {
                Patterns = repo.SparseCheckoutPatterns.ToList()
            };
        }

        Microsoft.Azure.Databricks.Client.Models.Repo repoInfo;
        try
        {
            Console.WriteLine($"[TRACE] Creating new repository...");
            Console.WriteLine($"[TRACE] Create repo payload: URL={repo.Url}, Provider={providerEnum}, Path={repo.Path}");

            repoInfo = await client.Repos.Create(repo.Url, providerEnum, repo.Path, sparseCheckout, cancellationToken);
            Console.WriteLine($"[TRACE] Repository created with ID: {repoInfo.Id}");

            // Update to specific branch or tag if specified
            if (!string.IsNullOrEmpty(repo.Branch) || !string.IsNullOrEmpty(repo.Tag))
            {
                Console.WriteLine($"[TRACE] Updating repository to branch '{repo.Branch}' or tag '{repo.Tag}'");
                await client.Repos.Update(repoInfo.Id, repo.Branch, repo.Tag, sparseCheckout, cancellationToken);

                // Get updated repo info
                repoInfo = await client.Repos.Get(repoInfo.Id, cancellationToken);
            }
        }
        catch (Exception ex) when (ex.Message.Contains("already exists") || ex.Message.Contains("RESOURCE_ALREADY_EXISTS"))
        {
            Console.WriteLine($"[TRACE] Repository already exists. Attempting to update...");

            // List repos to find the existing one
            // TODO: Somehow the List() returns 0
            var (repos, _) = await client.Repos.List();
            var existingRepo = repos.FirstOrDefault(r => r.Path == repo.Path || r.Url == repo.Url);

            if (existingRepo == null)
            {
                throw new InvalidOperationException($"Repository reported as existing but could not be found at path '{repo.Path}'");
            }

            Console.WriteLine($"[TRACE] Found existing repository with ID: {existingRepo.Id}. Updating...");
            await client.Repos.Update(existingRepo.Id, repo.Branch, repo.Tag, sparseCheckout, cancellationToken);

            // Get updated repo info
            repoInfo = await client.Repos.Get(existingRepo.Id, cancellationToken);
            Console.WriteLine($"[TRACE] Repository updated successfully");
        }


        return CreateResourceResponse(
            repoInfo,
            "Repo",
            info => new RepoIdentifiers { RepoId = info.Id.ToString() },
            info => new Repo
            {
                RepoId = info.Id.ToString(),
                Url = info.Url ?? string.Empty,
                Provider = info.Provider.ToString(),
                Path = info.Path ?? string.Empty,
                Branch = info.Branch ?? string.Empty,
                Tag = string.Empty, // Tags are not returned in the response
                SparseCheckoutPatterns = info.SparseCheckout?.Patterns?.ToArray(),
                HeadCommitId = info.HeadCommitId ?? string.Empty,
                WorkspacePath = info.Path ?? string.Empty
            }
        );
    }

    protected override RepoIdentifiers GetIdentifiers(Repo properties)
        => new()
        {
            RepoId = properties.RepoId,
        };
}
