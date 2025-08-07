# Databricks extension

You can use the Databricks extension to create or update almost any
[Databricks][00] resources. If you're new to Azure Databricks, please
follow the official guide on [Microsoft Learn][01].

Each resource defined in Bicep always requires the `workspaceUrl` and
the [databricksExtension][02] imported.

The following list represent the available resources in the extension.

**Compute resources:**

- Deploy a [Databricks cluster][03]

**Workspace resources:**

- Create a directory in a workspace

## `workspaceUrl` argument

When working with the Databricks extension, you need to specify the workspace
URL to a valid Azure Databricks instance. According to the Microsoft documentation,
the following table represents valid Azure Databricks instances:

| Environment                       | `workspaceUrl`                         |
| --------------------------------- | -------------------------------------- |
| Azure Databricks Account          | `https://accounts.azuredatabricks.net` |
| Azure Databricks Account (US Gov) | `https://accounts.azuredatabricks.us`  |
| Azure Databricks Account (China)  | `https://accounts.azuredatabricks.cn`  |

## Authentication

There are currently two supported methods to authenticate into an Azure Databricks
instance to create resources:

- An environment variable (DATABRICKS_ACCESS_TOKEN)
- Leverage the Azure's `DefaultAzureCredential` to obtain a token
    - Automatically tries multiple authentication methods like Azure CLI
    or Managed Identity to get a valid token. The requested resource scope
    is `2ff814a6-3304-4ab8-85cb-cd0e6f879c1d/.default`, which is the Azure
    Databricks resource ID

To set the environment variable, you can run the following in a PowerShell session:

```powershell
# Using Azure PowerShell (requires Az.Accounts)
Connect-AzAccount
$token = (Get-AzAccessToken -ResourceUrl "2ff814a6-3304-4ab8-85cb-cd0e6f879c1d").Token |
    ConvertFrom-SecureString -AsPlainText

# All sessions
$env:DATABRICKS_ACCESS_TOKEN = $token

# Store in environment variables
[Environment]::SetEnvironmentVariable("DATABRICKS_ACCESS_TOKEN", $token, "User") # Or machine

# Using Azure CLI (requires Az CLI)
$token = az account get-access-token \
--resource 2ff814a6-3304-4ab8-85cb-cd0e6f879c1d \
--query "accessToken" \
-o tsv

set DATABRICKS_ACCESS_TOKEN=$token
```

<!-- Link reference definitions -->
[00]: https://docs.databricks.com/api/azure/workspace/introduction
[01]: https://learn.microsoft.com/en-us/azure/databricks/introduction/
[02]: databricks-extension.md
[03]: ./resources/cluster.md
