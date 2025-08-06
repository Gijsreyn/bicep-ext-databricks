function Initialize-TestCase {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string]
        $Path,
        
        [Parameter(Mandatory = $true)]
        [string]
        $OutputPath,

        [Parameter(Mandatory = $true)]
        [string]
        $ExampleName,
        
        [Parameter()]
        [hashtable]
        $ValuesToReplace
    )

    $fileExamples = Get-ChildItem -Path $Path | Where-Object -Property Name -Like "$ExampleName*"
    if ($fileExamples.Count -eq 0) {
        throw "No example files found matching '$ExampleName' in path '$Path'."
    }

    foreach ($fileExample in $fileExamples) {
        if ($fileExample.Extension -eq '.bicep') {
            $bicepFile = $fileExample
        } elseif ($fileExample.Extension -eq '.bicepparam') {
            $bicepFileParam = $fileExample
        }
    }

    if (-not $bicepFile -or -not $bicepFileParam) {
        throw "Expected to find both .bicep and .bicepparam files, but found only one or none."
    }

    Write-Verbose "Found bicep file: $($bicepFile.FullName)"
    Write-Verbose "Found bicep parameter file: $($bicepFileParam.FullName)"
    $fileCopy = Copy-Item -Path @($bicepFile.FullName, $bicepFileParam.FullName) -Destination $OutputPath -Force -PassThru

    return $fileCopy

    # # Prepare files
    # $examplePath = Join-Path $ProjectRoot 'examples' 'workspace'
    
    # if (-not (Test-Path $examplePath)) {
    #     throw "Examples path not found: $examplePath"
    # }
    
    # $exampleFiles = Get-ChildItem -Path $examplePath -Include 'create.directory.bicep', 'create.directory.bicepparam' -Recurse
    
    # if ($exampleFiles.Count -ne 2) {
    #     throw "Expected to find 2 example files, but found $($exampleFiles.Count)"
    # }
    
    # # Process the bicep parameter file
    # $bicepParamSourceFile = $exampleFiles | Where-Object -Property Name -eq 'create.directory.bicepparam'
    # if (-not $bicepParamSourceFile) {
    #     throw "Could not find create.directory.bicepparam file"
    # }
    
    # $bicepParamFile = Get-Content -Path $bicepParamSourceFile.FullName -Raw 
    # $bicepParamFile = $bicepParamFile -replace '<workspaceUrl>', $WorkspaceUrl

    # $bicepParamDestPath = Join-Path $TestDrivePath 'create.directory.bicepparam'
    # Set-Content -Path $bicepParamDestPath -Value $bicepParamFile -Encoding utf8
    # Write-Verbose "Created bicep parameter file: $bicepParamDestPath"
    
    # # Copy the bicep template file
    # $bicepTemplateSourceFile = $exampleFiles | Where-Object -Property Name -eq 'create.directory.bicep'
    # if (-not $bicepTemplateSourceFile) {
    #     throw "Could not find create.directory.bicep file"
    # }
    
    # $bicepTemplateDestPath = Join-Path $TestDrivePath 'create.directory.bicep'
    # Copy-Item -Path $bicepTemplateSourceFile.FullName -Destination $bicepTemplateDestPath
    # Write-Verbose "Copied bicep template file: $bicepTemplateDestPath"

    # # Copy the bicep configuration file
    # $bicepConfigSourcePath = Join-Path $ProjectRoot 'bicepconfig.json'
    # if (-not (Test-Path $bicepConfigSourcePath)) {
    #     throw "Could not find bicepconfig.json at: $bicepConfigSourcePath"
    # }
    
    # $bicepConfigDestPath = Join-Path $TestDrivePath 'bicepconfig.json'
    # Copy-Item -Path $bicepConfigSourcePath -Destination $bicepConfigDestPath -Force
    # Write-Verbose "Copied bicep configuration file: $bicepConfigDestPath"
    
    # Write-Output "Test case preparation completed successfully."
    # Write-Output "Files prepared in: $TestDrivePath"
    
    # return @{
    #     BicepParamFile = $bicepParamDestPath
    #     BicepTemplateFile = $bicepTemplateDestPath
    #     BicepConfigFile = $bicepConfigDestPath
    #     TestDrivePath = $TestDrivePath
    # }
}
