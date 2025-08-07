#requires -Version 7.0

function Install-RequiredModules {
    $modules = @{
        'Az.Accounts' = '5.2.0'
        'Az.Resources' = '8.1.0'
        'Az.Databricks' = '1.10.0'
        'Az.ContainerRegistry' = '4.3.0'
        'ChangelogManagement' = '3.1.0'
        'Pester' = '5.7.1'
    }
    
    $outputPath = Join-Path -Path (Split-Path $PSScriptRoot) -ChildPath 'output' 'PSModules'
    if (-not (Test-Path -Path $outputPath)) {
        New-Item -Path $outputPath -ItemType Directory -Force | Out-Null
    }
    
    $modules.GetEnumerator() | ForEach-Object {
        if (-not (Get-Module -ListAvailable -Name $_.Key)) {
            Write-Verbose -Message "Saving module: $($_.Key) v$($_.Value)"
            $params = @{
                Name            = $_.Key
                Version         = $_.Value
                Path            = $outputPath
                Repository      = 'PSGallery'
                TrustRepository = $true
            }
            Save-PSResource @params -ErrorAction Stop
        }
    }
}

function Setup-Environment {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]
        $ResourceGroupName,

        [Parameter()]
        [string]
        $Location = 'westeurope',

        [Parameter(Mandatory = $true)]
        [string]
        $ContainerRegistryName,

        [Parameter(Mandatory = $true)]
        [string]
        $DatabricksWorkspaceName
    )

    Install-RequiredModules

    # TODO: Add context validation for Azure PowerShell

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
        New-AzContainerRegistry @containerRegistry -ErrorAction Ignore
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
        workspaceUrl = ('https://' + $databricks.Url)
        containerRegistryUrl = $containerRegistry.LoginServer
    }
}
# TODO: To be replaced by main.bicep