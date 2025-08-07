[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string]
    $WorkspaceUrl
)

$script:rootPath = Split-Path -Path $PSScriptRoot -Parent

BeforeDiscovery {
    . (Join-Path $script:rootPath 'scripts' 'Test-Bicep.ps1')
    $script:skip = Test-Bicep
}

BeforeAll {
    . (Join-Path $rootPath 'scripts' 'Initialize-TestCase.ps1')
}

Describe 'Cluster handler functionality' -Skip:(!$skip) {
    It "Should create a cluster successfully" {
        $initTestCaseParams = @{
            Path            = (Join-Path $rootPath 'examples' 'compute')
            OutputPath      = $testDrive
            ExampleName     = 'cluster.basic'
            ValuesToReplace = @{
                workspaceUrl = $WorkspaceUrl
            }
        }
        $testCase = Initialize-TestCase @initTestCaseParams

        $result = & bicep local-deploy $testCase.filesCopied[1] # Should be .bicepparam file
        $LASTEXITCODE | Should -Be 0
        # TODO: Validate the result more
    }
}
