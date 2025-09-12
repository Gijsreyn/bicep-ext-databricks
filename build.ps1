#Requires -Version 7.0
[CmdletBinding()]
param (
    [Parameter()]
    [string]
    $ProjectName = 'Databricks.extension',

    [ValidateSet("Release", "Debug")]
    [string]
    $Configuration = "Debug",

    [string]
    $resourceGroupName = 'rg-pub-dbt-extension',

    [string]
    $location = 'westeurope',

    [string]
    $containerRegistryName = "acrdbtextension",

    [string]
    $databricksWorkspaceName = 'dbt-pub-dbt-extension',

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
    $bicepFile = Join-Path $PSScriptRoot 'infrastructure' 'main.bicep'
    $bicepParameterFile = Join-Path $PSScriptRoot 'infrastructure' 'main.bicepparam'

    # Go through existing to see if user provided different parameters
    $currentParameters = $PSBoundParameters
    if ($currentParameters.ContainsKey('ResourceGroupName') -or $currentParameters.ContainsKey('Location') -or $currentParameters.ContainsKey('ContainerRegistryName') -or $currentParameters.ContainsKey('DatabricksWorkspaceName'))
    {
        $bicepExe = testBicepExe
        if ($bicepExe)
        {
            $randomFileName = [System.IO.Path]::GetRandomFileName()
            $fileExtension = [System.IO.Path]::GetExtension($randomFileName)
            $randomFileName = $randomFileName.Replace($fileExtension, '.json')
            $tempJsonPath = Join-Path $env:TEMP $randomFileName

            # Using outfile instead of stdout which has large output
            & $bicepExe build-params (Join-Path $PSScriptRoot 'infrastructure' 'main.bicepparam') `
                --outfile $tempJsonPath 

            $existingParameters = Get-Content -Path $tempJsonPath -Raw | ConvertFrom-Json -AsHashtable

            # Set new content
            $setContent = $false
            foreach ($key in $currentParameters.Keys)
            {
                Write-Verbose "Processing parameter '$key' with value '$($currentParameters[$key])'"
                if ($existingParameters.parameters.ContainsKey($key))
                {
                    Write-Verbose -Message "Parameter '$key' already exists with value '$($existingParameters.parameters[$key].value)'"
                    if ($existingParameters.parameters[$key].value -ne $currentParameters[$key])
                    {
                        $setContent = $true
                        Write-Verbose "Updating parameter '$key' from '$($existingParameters.parameters[$key].value)' to '$($currentParameters[$key])'"
                        $existingParameters.parameters[$key].value = $currentParameters[$key]
                    }
                }
            }

            if ($setContent)
            {
                Write-Verbose "Updating parameters in '$tempJsonPath'"
                Set-Content -Path $tempJsonPath -Value ($existingParameters | ConvertTo-Json -Depth 10) -Encoding UTF8 -Force

                $randomFileName = [System.IO.Path]::GetRandomFileName()
                $fileExtension = [System.IO.Path]::GetExtension($randomFileName)
                $randomFileName = $randomFileName.Replace($fileExtension, '.bicepparam')
                $tempBicepParameterFile = Join-Path (Split-Path $bicepParameterFile -Parent) $randomFileName
                & $bicepExe decompile-params $tempJsonPath `
                    --outfile $tempBicepParameterFile `
                    --bicep-file $bicepFile
                Write-Verbose "Decompiled parameters to '$tempBicepParameterFile'"

                # Reset bicepParameterFile to the new file
                $bicepParameterFile = $tempBicepParameterFile
            }
        }

    }

    $params = @{
        TemplateFile          = $bicepFile
        TemplateParameterFile = $bicepParameterFile
        Location              = $location
    }
    
    Write-Verbose "Deploying Bicep template with parameters:" -Verbose
    Write-Verbose ($params | ConvertTo-Json | Out-String) -Verbose
    $result = New-AzDeployment @params
    
    $environment = @{
        ContainerRegistryUrl = ([System.String]::Concat($result.Outputs.containerLoginServer.value, '.azurecr.io'))
        WorkspaceUrl         = ('https://' + $result.Outputs.dbtWorkspaceUrl.value)
    }

    if ($setContent)
    {
        Remove-Item -Path $tempBicepParameterFile -Force -ErrorAction Ignore
    }
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
    # Build the solution
    try
    {
        Push-Location (Join-Path $PSScriptRoot 'src')
        $buildParams = @(
            'build',
            'Databricks.Extension.sln',
            '-c', $Configuration
        )

        Write-Verbose "Building solution 'Databricks.Extension.sln' with" -Verbose
        Write-Verbose ($buildParams | ConvertTo-Json | Out-String) -Verbose
        $res = & $dotNetExe @buildParams 

        if ($LASTEXITCODE -ne 0)
        {
            throw $res
        }
    }
    finally
    {
        Pop-Location
    }

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

    if ($env:GITHUB_ACTIONS -ne $true)
    {
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
    }

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
        try
        {
            Push-Location (Join-Path $PSScriptRoot 'src')
        
            
            $testParams = @(
                'test',
                "$ProjectName.sln",
                '-c', $Configuration,
                '--verbosity', 'normal'
            )

            if ($env:GITHUB_ACTIONS -eq 'true')
            {
                Write-Verbose "Running test in GitHub actions"
                $testParams += @(
                    "--logger", "GitHubActions;summary.includePassedTests=true;summary.includeSkippedTests=true"
                )
            }
            else 
            {
                $testParams += @(
                    "--logger:junit"
                )
            }

            Write-Verbose "Running tests for '$ProjectName' with" -Verbose
            Write-Verbose ($testParams | ConvertTo-Json | Out-String) -Verbose
            $res = & $dotNetExe @testParams

            if ($LASTEXITCODE -ne 0)
            {
                throw "Tests failed: $res"
            }
        }
        finally
        {
            Pop-Location
        }
    }
}