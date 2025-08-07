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

Describe 'Workspace handler functionality' -Skip:(!$skip) {
    It "Should create a directory successfully" {
        $initTestCaseParams = @{
            Path            = (Join-Path $rootPath 'examples' 'workspace')
            OutputPath      = $testDrive
            ExampleName     = 'directory'
            ValuesToReplace = @{
                workspaceUrl = $WorkspaceUrl
            }
        }
        $testCase = Initialize-TestCase @initTestCaseParams
        
        $result = & bicep local-deploy $testCase.filesCopied[1] # Should be .bicepparam file
        $LASTEXITCODE | Should -Be 0
        $result | Should -Not -BeNullOrEmpty
        $result | Should -Not -Contain "Failed"
        $result[0] | Should -BeLike "Output directoryId*" # First line is the output result of the deployment
    }



    It 'Should fail to create a directory in user path' {
        $bicepFile = @'
targetScope = 'local'

param workspaceUrl string


extension databricksExtension with { 
  workspaceUrl: workspaceUrl
}

resource directory 'Directory' = {
  path: '/Users/fake@example.com/project'
}

output directoryId string = directory.path

'@
        $bicepFilePath = Join-Path $testDrive 'create.directory.user.bicep'
        Set-Content -Path $bicepFilePath -Value $bicepFile -Encoding UTF8
        $bicepParamFile = @"
using 'create.directory.user.bicep'

param workspaceUrl = '$workspaceUrl'
"@
        $script:bicepParamFilePath = Join-Path $testDrive 'create.directory.user.bicepparam'
        Set-Content -Path $bicepParamFilePath -Value $bicepParamFile -Encoding UTF8

        $result = & bicep local-deploy $bicepParamFilePath
        $LASTEXITCODE | Should -Be 1
        $result | Should -Not -BeNullOrEmpty
        $result | Should -Contain 'Error: RpcException - Rpc request failed: Failed to call Databricks API /api/2.0/workspace/mkdirs. Status: BadRequest, Error: {"error_code":"DIRECTORY_PROTECTED","message":"Folder Users is protected"}'
    }
}

