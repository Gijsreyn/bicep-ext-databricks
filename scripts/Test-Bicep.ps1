<#
.SYNOPSIS
    Checks if the Bicep CLI is installed and meets the minimum required version.

.DESCRIPTION
    The function `Test-Bicep` verifies if the Bicep CLI v0.36.1 or higher is installed on the system. 

.EXAMPLE
    Tests if the Bicep CLI is available and meets the version requirement.
    ```powershell
    Test-Bicep
    ```

.NOTES
    Tags: Bicep, CLI
    Author: Gijs Reijn
#>
function Test-Bicep {
    $bicepExe = Get-Command -Name 'bicep' -CommandType Application -ErrorAction Ignore
    if (-not $bicepExe) {
        return $false
    }
    else {
        $version = & $bicepExe --version 
        if ($version -match '(\d+\.\d+\.\d+)') {
            $bicepVersion = [Version]$Matches[1]
            $minVersion = [Version]'0.36.1'
            if ($bicepVersion -le $minVersion) {
                return $false
            }
        }
    }
    
    return $true
}