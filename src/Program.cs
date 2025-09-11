using Microsoft.AspNetCore.Builder;
using Bicep.Local.Extension.Host.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Bicep.Extension.Databricks;
using Bicep.Extension.Databricks.Handlers;

var builder = WebApplication.CreateBuilder();

builder.AddBicepExtensionHost(args);
builder.Services
    .AddBicepExtension(
        name: "DatabricksExtension",
        version: "0.0.1",
        isSingleton: true,
        typeAssembly: typeof(Program).Assembly,
        configurationType: typeof(Configuration))
        .WithResourceHandler<DatabricksGitCredentialHandler>();

var app = builder.Build();
app.MapBicepExtension();
await app.RunAsync();