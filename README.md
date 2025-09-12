# Bicep Databricks extension

Bicep Databricks extension works with Bicep v0.36.1 or higher. The extension is build
for Azure Databricks.

## Usage

To use the extension, make sure you add the `bicepconfig.json`
file and `databricksExtension`:

```json
// bicepconfig.json
{
  "experimentalFeaturesEnabled": {
    "localDeploy": true,
    "extensibility": true
  },
  "cloud": {
    "credentialPrecedence": [
      "AzurePowerShell",
      "AzureCLI"
    ],
    "currentProfile": "AzureCloud"
  },
  "extensions": {
    "databricksExtension": "br:acrdbtextension.azurecr.io/extension/databricks:v<version>"
  },
  "implicitExtensions": []
}
```

> [!NOTE]
> See the [CHANGELOG.md][00] file for available version and updates.

```bicep
// main.bicep
targetScope = 'local'

param workspaceUrl string

extension databricksExtension with { 
  workspaceUrl: workspaceUrl
}
```

## Build and test locally

This project contains a `build.ps1` automation file supporting multiple operations.
Each operation can run sequently of each other.
However, in some cases, it makes sense to first publish the extension
and execute tests afterwards.

To execute the all operations, make sure you have:

* .NET SDK v9.0 installed
* Bicep CLI v0.36.1 or higher
* An Azure subscription

Other prerequisites are saved locally in the project structure when you
bootstrap the environment.

> [!NOTE]
> Refer to the `global.json` file for the exact version number for the .NET SDK.

The following sections describe each operation. You can also refer to the help
inside the script.

### Bootstrap and clean

To bootstrap the environment, you need an active connection to your Azure
subscription. If you already connected using `az login` or `Connect-AzAccount`,
execute the following in a PowerShell session:

```powershell
./build.ps1 -Bootstrap -Clean
```

This will create the required Azure resources to test the extension. If you
want to customize the default values, add the following parameters:

```powershell
./build.ps1 -Bootstrap -ResourceGroupName '<resourceGroupName>' `
    -Location '<location>' `
    -ContainerRegistryName '<containerRegistryName>' `
    -DatabricksWorkspaceName '<databricksWorkspaceName>'
```

### Build + publish

To build and publish the extension locally, use the following parameter switches:

```powershell
./build.ps1 -Clean -Configuration Release
```

The extension is published to the `output` directory. If you want to leverage the
local extension rather than fetching it from the existing container registry,
make sure you update the `bicepconfig.json` to:

```json
{
  "experimentalFeaturesEnabled": {
    "localDeploy": true,
    "extensibility": true
  },
  "cloud": {
    "credentialPrecedence": [
      "AzurePowerShell",
      "AzureCLI"
    ],
    "currentProfile": "AzureCloud"
  },
  "extensions": {
    "databricksExtension": "br:acrdbtextension.azurecr.io/extension/databricks:v<version>" // Change this to ./output/databricks-extension after publishing
  },
  "implicitExtensions": []
}
```

## Publish to container registry

The Bicep Databricks extension can be published to a Azure Container Registry. When
you bootstrap the environment, a container registry is created. You can publish the
extension to this container registry by adding the `-Publish` switch parameter:

```powershell
./build.ps1 -Configuration Release -Publish
```

> [!IMPORTANT]
> An Azure Container Registry is a global resource. If the registry is not created,
> revert back to the bootstrapping process and customize the default values.
> Don't forget when you attempt to publish, to specify the `-ContainerRegistryName`
> parameter with the name you've created.

## Testing

To verify and test the working of the Bicep Databricks extension, .NET unit tests
are executed using xUnit. The test project `Databricks.Extension.Tests` contains
test coverage for all handlers and models:

1. **Handler Tests**: Validate constructor initialization and model property  
   assignments
2. **Model Tests**: Verify property validation, enum values, and data integrity
3. **Azure Databricks Integration**: Tests cover Unity Catalog, Workspace, and  
   Compute handlers

You can execute the unit tests by running the following in a PowerShell terminal:

```powershell
./build.ps1 -Configuration Release -Test
```

The test results are automatically generated in TRX and JUnit formats when  
running in CI environments, and all test artifacts are preserved for analysis.

## Documentation

Check out the [docs][01] section for more information around Databricks resources.

## Troubleshooting

Encounter any issues while running `bicep local-deploy`? Want to see what is
happening behind the scenes? In your terminal session, you can add the following
environment variable to enable tracing:

```powershell
$env:BICEP_TRACING_ENABLED = $true
```

## Contributing

Want to contribute? Check out the [CONTRIBUTING.md][02] for more information.

<!-- Link reference definitions -->
[00]: CHANGELOG.md
[01]: ./docs/index.md
[02]: CONTRIBUTING.md
