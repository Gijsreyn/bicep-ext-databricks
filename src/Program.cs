using Microsoft.AspNetCore.Builder;
using Bicep.Local.Extension.Host.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Bicep.Extension.Databricks;
using Bicep.Extension.Databricks.Handlers.Compute;
using Bicep.Extension.Databricks.Handlers.Workspace;
using Bicep.Extension.Databricks.Handlers.UnityCatalog;

var builder = WebApplication.CreateBuilder();

builder.AddBicepExtensionHost(args);
builder.Services
    .AddBicepExtension(
        name: "DatabricksExtension",
        version: "0.0.1",
        isSingleton: true,
        typeAssembly: typeof(Program).Assembly,
        configurationType: typeof(Configuration))
    .WithResourceHandler<SecretHandler>()
    .WithResourceHandler<DirectoryHandler>()
    .WithResourceHandler<RepoHandler>()
    .WithResourceHandler<GitCredentialHandler>()
    .WithResourceHandler<ClusterHandler>()
    .WithResourceHandler<CatalogHandler>();

var app = builder.Build();

app.MapBicepExtension();

await app.RunAsync();