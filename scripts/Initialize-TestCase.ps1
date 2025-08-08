function Initialize-TestCase {
    <#
    .SYNOPSIS
        Initializes a test case by copying example Bicep files and configuration, and replacing parameter values for testing.

    .DESCRIPTION
        The function `Initialize-TestCase` locates the specified example .bicep and .bicepparam files,
        copies them along with the bicepconfig.json to the output directory, and replaces parameter values in the 
        .bicepparam file as needed. It returns a hashtable with paths to the copied files and other metadata for use
        in automated tests.

    .PARAMETER Path
        The path to the directory containing the example Bicep files.

    .PARAMETER OutputPath
        The directory where the example files and configuration will be copied for the test case.

    .PARAMETER ExampleName
        The base name of the example files to locate (e.g., 'cluster.basic').

    .PARAMETER ValuesToReplace
        Hashtable of parameter names and values to replace in the .bicepparam file.

    .EXAMPLE
        The following example initializes a test case by copying the Bicep files from the 'examples/compute' directory,
        replacing the `workspaceUrl` parameter in the .bicepparam file, and returning
        a hashtable with paths to the copied files and other metadata.

        ```powershell
        $initTestCaseParams = @{
            Path            = (Join-Path $rootPath 'examples' 'compute')
            OutputPath      = $testDrive
            ExampleName     = 'cluster.basic'
            ValuesToReplace = @{
                workspaceUrl = $WorkspaceUrl
            }
        }
        $testCase = Initialize-TestCase @initTestCaseParams
        ```

    .NOTES
        Tags: Bicep, TestCase, Pester
        Author: Gijs Reijn
    #>
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

    $filePathsToCopy = @($bicepFile.FullName, $bicepFileParam.FullName)
    $filePathsToCopy += (Resolve-Path (Join-Path (Split-Path $PSScriptRoot -Parent) -ChildPath 'bicepconfig.json')).Path

    $fileCopy = foreach ($fileToCopy in $filePathsToCopy) {
        if (-not (Test-Path $fileToCopy)) {
            throw "File not found: $fileToCopy"
        } else {
            if (-not (Test-Path $OutputPath)) {
                New-Item -Path $OutputPath -ItemType Directory -Force | Out-Null
                Write-Verbose "Created output directory: $OutputPath"
            }

            if ($fileToCopy -like "*bicepconfig.json*") {
                $bicepConfig = Get-Content -Path $fileToCopy -Raw | ConvertFrom-Json

                if ($bicepConfig.extensions.databricksExtension.StartsWith('./output/databricks-extension')) {
                    $outputFolder = Join-Path (Split-Path $PSScriptRoot -Parent) 'output'
                    Write-Verbose -Message "Copying item: $outputFolder"
                    Copy-Item -Path $outputFolder -Destination $OutputPath -Recurse -Force
                }   
            }

            Copy-Item -Path $fileToCopy -Destination $OutputPath -Force -PassThru
        }
    }

    foreach ($valueReplace in $ValuesToReplace.GetEnumerator()) {
        # read the .bicepparam file and replace the values 
        $fileParam = $fileCopy | Where-Object -Property Extension -EQ '.bicepparam'
        $bicepParam = Get-Content -Path $fileParam.FullName -Raw

        Write-Verbose "Replacing '<$($valueReplace.Key)>' with '$($valueReplace.Value)' in bicepparam file: $($fileParam.FullName)"
        $bicepParam = $bicepParam -replace "<$($valueReplace.Key)>", $valueReplace.Value
        Set-Content -Path $fileParam.FullName -Value $bicepParam -Encoding utf8 -Force
    }

    return @{
        bicepFile = $bicepFile.FullName
        bicepParamFile = $bicepFileParam.FullName
        outputPath = $OutputPath
        filesCopied = $fileCopy | Select-Object -ExpandProperty FullName
    }
}
