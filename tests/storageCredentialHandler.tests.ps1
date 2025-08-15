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

Describe 'Storage credential handler functionality' -Skip:(!$skip) {
    It "Should create a storage credential successfully" {
        # TODO: Figure out how to pass in multiple arguments
    }
}
