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
* Basic knowledge around Pester

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

To verify and test the working of the Bicep Databricks extension, Pester tests
can be executed locally. Each Pester tests looks at the `examples` directory
and does the following in sequence:

1. Check if Bicep CLI is available.
   1. If not, the test(s) will be skipped.
2. It leverages the `Initialize-TestCase.ps1` script by transforming the
   `<workspaceUrl>` to an existing Azure Databricks instance. It copies
   over the specific example to the `$TestDrive` variable.
3. The test is executed.

You can execute Pester tests by running the following in a PowerShell terminal:

```powershell
./build.ps1 -Configuration Release -Test
```

## Examples

Each handler should have at least one example available in the `examples` directory.
These examples are also used for testing purposes. The following table represents
the available examples from the particular released versions:

| **Example name**                  | **Description**                                 | **Available from** |
|-----------------------------------|-------------------------------------------------|--------------------|
| [Create a directory][01]          | Creates a directory in a Databricks workspace   | v0.1.0 and above   |
| [Create basic cluster][02]        | Deploys a basic cluster                         | v0.1.2 and above   |
| [Create a repository][03]         | Create a repository in a Databricks workspace   | v0.1.5 and above   |
| [Create Git credential][04]       | Creates a new Git credential in Linked Accounts | v0.1.5 and above   |
| [Create a Unity Catalog][05]      | Create a new Unity Catalog                      | v0.1.6 and above   |
| [Create a Storage Credential][06] | Create a Storage Credential                     | v0.1.8 and above   |
| [Create an External Location][07]   | Create an External Location                     | v0.1.9 and above   |

> [!NOTE]
> Always make sure to use the latest version of the extension.

## Documentation

Check out the [docs][08] section for more information around Databricks resources.

## Troubleshooting

Encounter any issues while running `bicep local-deploy`? Want to see what is
happening behind the scenes? In your terminal session, you can add the following
environment variable to enable tracing:

```powershell
$env:BICEP_TRACING_ENABLED = $true
```

## Contributing

Want to contribute? Check out the [CONTRIBUTING.md][09] for more information.

<!-- Link reference definitions -->
[00]: CHANGELOG.md
[01]: ./examples/workspace/directory.bicep
[02]: ./examples/compute/cluster.basic.bicep
[03]: ./examples/workspace/repo.basic.bicep
[04]: ./examples/workspace/gitCredential.bicep
[05]: ./examples/unity/catalog.basic.bicep
[06]: ./examples/unity/catalog.basic.bicep
[07]: ./examples/unity/externalLocation.basic.bicep
[08]: ./docs/index.md
[09]: CONTRIBUTING.md
