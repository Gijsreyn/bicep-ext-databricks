[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string]
    $WorkspaceUrl
)

$script:rootPath = Split-Path -Path $PSScriptRoot -Parent

. (Join-Path $rootPath 'scripts' 'Initialize-TestCase.ps1')

BeforeDiscovery {
    $script:skip = Test-Bicep
}

BeforeAll {
    $initTestCaseParams = @{
        Path            = (Join-Path $rootPath 'examples' 'workspace')
        OutputPath      = $testDrive
        ExampleName     = 'create.directory'
        ValuesToReplace = @{
            workspaceUrl = $WorkspaceUrl
        }
    }
    $script:testCase = Initialize-TestCase @initTestCaseParams -Verbose
}

Describe 'Workspace handler functionality' -Skip:(!$skip) {
    Context "Workspace creation" {
        It "Should create a workspace directory successfully" {
            # TODO: Add br in the list
            $result = & bicep local-deploy $script:testCase.filesCopied[1] # Should be .bicepparam file
            $LASTEXITCODE | Should -Be 0
            $result | Should -Not -BeNullOrEmpty
            $result | Should -Not -Contain "Failed"
            $result[0] | Should -BeLike "Output directoryId*" # First line is the output result of the deployment
        }

        It "Should fail when the path is in user" {
            $bicepFile = @'

'@
        }
    }
}