using Microsoft.AspNetCore.Builder;
using Bicep.Local.Extension.Host.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Bicep.Extension.Databricks;
using Bicep.Extension.Databricks.Handlers.Workspace;
using Bicep.Extension.Databricks.Handlers.Compute;
using Bicep.Extension.Databricks.Handlers.UnityCatalog;
using Bicep.Extension.Databricks.Services;

var builder = WebApplication.CreateBuilder();

builder.AddBicepExtensionHost(args);
// builder.Logging.SetMinimumLevel(LogLevel.Trace);
builder.Services
    .AddBicepExtension(
        name: "DatabricksExtension",
        version: "0.0.1",
        isSingleton: true,
        typeAssembly: typeof(Program).Assembly,
        configurationType: typeof(Configuration))
    .WithResourceHandler<DirectoryHandler>()
    .WithResourceHandler<RepoHandler>()
    .WithResourceHandler<SecretHandler>()
    .WithResourceHandler<GitCredentialHandler>()
    .WithResourceHandler<ClusterHandler>()
    .WithResourceHandler<CatalogHandler>()
    .WithResourceHandler<StorageCredentialHandler>()
    .WithResourceHandler<ExternalLocationHandler>();

builder.Services.AddSingleton<IDatabricksClientFactory, DatabricksClientFactory>();

var app = builder.Build();
app.MapBicepExtension();
await app.RunAsync();