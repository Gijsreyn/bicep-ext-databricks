#Requires -Version 7.0
[CmdletBinding()]
param (
    [Parameter()]
    [string]$ProjectName = 'Databricks.extension',

    [ValidateSet("Release", "Debug")]
    [string]
    $Configuration = "Debug",

    [string]
    $ResourceGroupName = 'rg-pub-dbt-extension',

    [string]
    $Location = 'westeurope',

    [string]
    $ContainerRegistryName = "acrdbtextension",

    [string]
    $DatabricksWorkspaceName = 'dbt-pub-dbt-extension',

    [switch]
    $Bootstrap,

    [switch]
    $Clean,

    [switch]
    $Publish,

    [switch]
    $Test
)

function getNetPath
{
    # Read version from global.json
    $globalJsonPath = Join-Path $PSScriptRoot 'global.json'
    $dotnetVersion = $null
    
    if (Test-Path $globalJsonPath)
    {
        if (Test-Json -Path $globalJsonPath)
        {
            $jsonContent = Get-Content $globalJsonPath -Raw | ConvertFrom-Json

            $dotnetVersion = $jsonContent.sdk.version
        }
        else
        {
            throw "Invalid JSON format in global.json"
        }
    }

    # TODO: Support multiple OS platforms
    $dotnetPath = Join-Path $env:ProgramFiles 'dotnet' 'sdk' $dotnetVersion
    if (-not (Test-Path $dotnetPath))
    {
        throw "Required .NET SDK version $dotnetVersion not found. Please install it using 'winget install Microsoft.DotNet.SDK.9'"
    }

    $dotnetExe = Join-Path (Split-Path (Split-Path $dotnetPath -Parent) -Parent) 'dotnet.exe'

    return $dotnetExe
}

function getProjectPath ($ProjectName)
{
    $projectPath = Get-ChildItem -Path $PSScriptRoot -Recurse -Filter "$ProjectName.csproj" -File -ErrorAction Ignore | Select-Object -First 1
    if ($null -eq $projectPath)
    {
        Write-Error "Project file '$ProjectName.csproj' not found in the script directory or its subdirectories."
        return
    }

    return $projectPath.FullName
}

function testBicepExe
{
    $bicepExe = Get-Command bicep -CommandType Application -ErrorAction Ignore 
    if (-not $bicepExe)
    {
        return $false
    }

    return $bicepExe.Source
}

function generateConfig ($configPath, $bicepRegistryUrl)
{
    if (-not (Test-Path $configPath))
    {
        $bicepConfig = [ordered]@{
            experimentalFeaturesEnabled = @{
                localDeploy   = $true
                extensibility = $true
            }
            cloud                       = @{
                credentialPrecedence = @('AzurePowerShell')
                currentProfile       = 'AzureCloud'
            }
            extensions                  = @{
                databricksExtension = $bicepRegistryUrl
            }
            implicitExtensions          = @()
        } | ConvertTo-Json -Depth 10

        $bicepConfig = $bicepConfig.Replace("\\", "/")
        
        Set-Content -Path $configPath -Value $bicepConfig -Encoding UTF8 -Force
    }
}

# Variables
$errorActionPreference = 'Stop'
$outputDirectory = Join-Path $PSScriptRoot 'output'
$dotNetExe = getNetPath
$projectPath = getProjectPath $ProjectName

if ($Bootstrap.IsPresent)
{
    # dot-source the Setup-Environment script
    $setupScriptPath = Join-Path $PSScriptRoot 'scripts' 'Setup-Environment.ps1'

    . $setupScriptPath

    Write-Verbose -Message "Bootstrapping environment by sourcing script '$setupScriptPath'. This can take a while..." -Verbose
    $environment = Setup-Environment -ResourceGroupName $ResourceGroupName `
        -Location $Location `
        -ContainerRegistryName $ContainerRegistryName `
        -DatabricksWorkspaceName $DatabricksWorkspaceName
}

if ($Clean.IsPresent)
{
    if (Test-Path $outputDirectory)
    {
        Write-Verbose "Removing output directory '$outputDirectory'." -Verbose
        Remove-Item -Path $outputDirectory -Recurse -Force
    }

    $cleanParams = @(
        'clean', $projectPath,
        '-c', $Configuration,
        '-nologo'
    )
    Write-Verbose "Cleaning project '$ProjectName' with" -Verbose
    Write-Verbose ($cleanParams | ConvertTo-Json | Out-String) -Verbose
    $null = & $dotNetExe @cleanParams
}

if ($Configuration -eq 'Release')
{
    $platforms = @('win-x64', 'linux-x64', 'osx-x64')
    $extensionParams = @(
        'publish-extension'
    )

    $exeName = (Split-Path $projectPath -LeafBase).Replace('.', '-').ToLower()
    $targetName = Join-Path $outputDirectory $exeName

    foreach ($platform in $platforms)
    {
        $out = (Join-Path $outputDirectory $platform)

        try
        {
            Push-Location (Join-Path $PSScriptRoot 'src')
            $publishParams = @(
                'publish',
                '-c', $Configuration,
                '-r', $platform,
                '-o', $out
            )
    
            Write-Verbose "Publishing project '$ProjectName' for platform '$platform' with" -Verbose
            Write-Verbose ($publishParams | ConvertTo-Json | Out-String) -Verbose
            $res = & $dotNetExe @publishParams 

            if ($LASTEXITCODE -ne 0)
            {
                throw $res
            }

            if ($platform -eq 'win-x64')
            {
                $extensionParams += @("--bin-$platform", (Join-Path $out "$exeName.exe"))
            }
            else
            {
                $extensionParams += @("--bin-$platform", (Join-Path $out $exeName))
            }
        }
        finally
        {
            Pop-Location
        }
    }

    $changeLog = Get-ChangelogData -Path (Join-Path $PSScriptRoot 'CHANGELOG.md') -ErrorAction Stop
    $version = $changeLog.LastVersion

    if ([string]::IsNullOrEmpty($environment))
    {
        $dbtInstance = Get-AzDatabricksWorkspace -ResourceGroupName $ResourceGroupName -Name $DatabricksWorkspaceName -ErrorAction Ignore

        if (-not $dbtInstance)
        {
            throw "Databricks workspace '$DatabricksWorkspaceName' not found in resource group '$ResourceGroupName'. Please ensure the environment is bootstrapped."
        }

        $environment = @{ 
            ContainerRegistryUrl = ([System.String]::Concat($ContainerRegistryName, '.azurecr.io'))
            WorkspaceUrl         = ('https://' + $dbtInstance.Url)
        }
    }

    $bicepRegistryUrl = [System.String]::Concat('br:', $environment.ContainerRegistryUrl, '/extension/', 'databricks', ':v', $version)

    if ($Publish.IsPresent)
    {
        $containerParams = $extensionParams
        $containerParams += @('--target', $bicepRegistryUrl)
    }

    $extensionParams += @(
        '--target', $targetName,
        '--force'
    )

    $bicepExe = testBicepExe

    if ($bicepExe)
    {
        Write-Verbose -Message "Bicep CLI found at $bicepExe" -Verbose
        Write-Verbose -Message "Compiling Bicep files for extension '$targetName'." -Verbose
        Write-Verbose -Message ($extensionParams | ConvertTo-Json | Out-String) -Verbose
        & $bicepExe @extensionParams

        if ($Publish.IsPresent)
        {
            Write-Verbose -Message "Publishing extension to Bicep registry at '$bicepRegistryUrl'." -Verbose
            & $bicepExe @containerParams
        }
    }
    else
    {
        Write-Warning "Bicep CLI is not installed. Skipping Bicep compilation."
        return
    }

    $configPath = Join-Path $PSScriptRoot 'bicepconfig.json'
    generateConfig -configPath $configPath -bicepRegistryUrl $bicepRegistryUrl

    if ($Test.IsPresent)
    {
        $testContainerData = @{
            workspaceUrl = $environment.workspaceUrl
        }

        $testPath = Join-Path $PSScriptRoot 'tests'

        Invoke-Pester -Configuration @{
            Run    = @{
                Container = New-PesterContainer -Path $testPath -Data $testContainerData
            }
            Output = @{
                Verbosity = 'Detailed'
            }
        } -ErrorAction Stop
    }
}