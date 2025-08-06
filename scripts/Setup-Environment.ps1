#requires -Version 7.0

function Install-RequiredModules {
    @('Az.Accounts', 'Az.Resources', 'Az.Databricks', 'Az.ContainerRegistry') | ForEach-Object {
        if (-not (Get-Module -ListAvailable -Name $_)) {
            Write-Verbose -Message "Installing module: $_"
            $params = @{
                Name            = $_ 
                Scope           = 'CurrentUser'
                ReInstall       = $true 
                Repository      = 'PSGallery'
                TrustRepository = $true
            }
            Install-PSResource @params -ErrorAction Stop
        }
    }
}

function Setup-Environment {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $false)]
        [string]
        $ResourceGroupName = 'rg-publishing-dbt-extension',

        [Parameter(Mandatory = $false)]
        [string]
        $Location = 'westeurope',

        [Parameter(Mandatory = $false)]
        [string]
        $ContainerRegistryName = "acrdbtextension",

        [Parameter(Mandatory = $false)]
        [string]
        $DatabricksWorkspaceName = 'dbt-publishing-dbt-extension'
    )   

    Install-RequiredModules

    if (-not (Get-AzResourceGroup -Name $ResourceGroupName -ErrorAction SilentlyContinue)) {
        Write-Verbose -Message "Creating resource group: $ResourceGroupName in $Location"
        New-AzResourceGroup -Name $ResourceGroupName -Location $Location
    }

    $containerRegistry = (Get-AzContainerRegistry -Name $ContainerRegistryName -ResourceGroupName $ResourceGroupName -ErrorAction SilentlyContinue)
    if (-not $containerRegistry) {
        Write-Verbose -Message "Creating container registry: $ContainerRegistryName in resource group: $ResourceGroupName"
        $containerRegistry = @{
            Name                 = $ContainerRegistryName 
            ResourceGroupName    = $ResourceGroupName
            Location             = $Location
            Sku                  = 'Standard'
            AnonymousPullEnabled = $true 
            PublicNetworkAccess  = 'Enabled'
            EnableAdminUser      = $false
        }
        New-AzContainerRegistry @containerRegistry
    }

    $databricks = Get-AzDatabricksWorkspace -Name $DatabricksWorkspaceName -ResourceGroupName $ResourceGroupName -ErrorAction SilentlyContinue
    if (-not $databricks) {
        Write-Verbose -Message "Creating Databricks workspace: $DatabricksWorkspaceName in resource group: $ResourceGroupName"
        $databricksParams = @{
            Name                = $DatabricksWorkspaceName
            ResourceGroupName   = $ResourceGroupName
            Location            = $Location
            Sku                 = 'Standard'
        }
        $databricks = New-AzDatabricksWorkspace @databricksParams
    }

    return @{
        WorkspaceUrl = $databricks.Url
        ContainerRegistryUrl = $containerRegistry.LoginServer
    }
}