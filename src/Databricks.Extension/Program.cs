using Microsoft.AspNetCore.Builder;
using Bicep.Local.Extension.Host.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Databricks.Models;
using Databricks.Models.UnityCatalog;
using Databricks.Handlers;
using Databricks.Handlers.Compute;
using Databricks.Handlers.UnityCatalog;
using Databricks.Handlers.Workspace;

var builder = WebApplication.CreateBuilder();

builder.AddBicepExtensionHost(args);
builder.Services
    .AddBicepExtension(
        name: "DatabricksExtension",
        version: "0.0.1",
        isSingleton: true,
        typeAssembly: typeof(Program).Assembly,
        configurationType: typeof(Configuration))
        .WithResourceHandler<DatabricksGitCredentialHandler>()
        .WithResourceHandler<DatabricksRepositoryHandler>()
        .WithResourceHandler<DatabricksDirectoryHandler>()
        .WithResourceHandler<DatabricksClusterHandler>()
        .WithResourceHandler<DatabricksSecretScopeHandler>()
        .WithResourceHandler<DatabricksSecretHandler>()
        .WithResourceHandler<DatabricksUnityCatalogHandler>()
        .WithResourceHandler<DatabricksUnityCredentialHandler>()
        .WithResourceHandler<DatabricksUnityConnectionHandler>()
        .WithResourceHandler<DatabricksUnityExternalLocationHandler>()
        .WithResourceHandler<DatabricksUnityStorageCredentialHandler>()
        .WithResourceHandler<DatabricksUnitySchemaHandler>();

var app = builder.Build();
app.MapBicepExtension();
await app.RunAsync();