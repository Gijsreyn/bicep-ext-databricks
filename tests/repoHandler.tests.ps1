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

Describe 'Repo handler functionality' -Skip:(!$skip) {
    It "Should create a repository successfully" {
        $initTestCaseParams = @{
            Path            = (Join-Path $rootPath 'examples' 'workspace')
            OutputPath      = $testDrive
            ExampleName     = 'repo.basic'
            ValuesToReplace = @{
                workspaceUrl = $WorkspaceUrl
            }
        }
        $testCase = Initialize-TestCase @initTestCaseParams -Verbose

        $result = & bicep local-deploy $testCase.filesCopied[1] # Should be .bicepparam file
        $LASTEXITCODE | Should -Be 0
        $result | Should -Not -BeNullOrEmpty
        $result | Should -Not -Contain "Failed"
        $result[0] | Should -BeLike "Output repoId*" # First line is the output result of the deployment
    }
}
