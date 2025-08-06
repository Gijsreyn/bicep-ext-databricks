[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string]
    $WorkspaceUrl
)

BeforeDiscovery {
    $script:skip = $false 
    $bicepExe = Get-Command -Name 'bicep' -CommandType Application -ErrorAction Ignore
    if (-not $bicepExe) {
        $skip = $true
    }

    $version = & $bicepExe --version 
    if ($version -match '(\d+\.\d+\.\d+)') {
        $bicepVersion = [Version]$Matches[1]
        $minVersion = [Version]'0.36.1'
        if ($bicepVersion -le $minVersion) {
            $skip = $true
        }
    }
}

BeforeAll {
    # Prepare files
    $examplePath = Join-Path (Split-Path $PSScriptRoot -Parent) 'examples' 'workspace'
    $exampleFiles = Get-ChildItem -Path $examplePath -Include 'create.directory.bicep', 'create.directory.bicepparam' -Recurse

    $bicepParamFile = Get-Content -Path ($exampleFiles | Where-Object -Property Name -eq 'create.directory.bicepparam').FullName -Raw 
    $bicepParamFile = $bicepParamFile -replace '<workspaceUrl>', $WorkspaceUrl

    Set-Content -Path "$testDrive\create.directory.bicepparam" -Value $bicepParamFile -Encoding utf8
    Copy-Item -Path ($exampleFiles | Where-Object -Property Name -eq 'create.directory.bicep').FullName -Destination "$testDrive\create.directory.bicep"

    # Copy the config to test drive 
    Copy-Item -Path (Join-Path (Split-Path $PSScriptRoot -Parent) 'bicepconfig.json') -Destination "$testDrive\bicepconfig.json" -Force
}

Describe 'Workspace handler functionality' {
    Context "Workspace creation" {
        It "Should create a workspace directory successfully" -Skip:$skip {
            # TODO: Add br in the list
            $res = & bicep local-deploy "$testDrive\create.directory.bicepparam"
            $res | Should -Not -BeNullOrEmpty
            $res | Should -Not -Contain "error"
            $LASTEXITCODE | Should -Be 0
            $res | Should -BeLike "Output directoryId: *"
        }
    }
}