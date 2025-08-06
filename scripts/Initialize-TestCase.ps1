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
