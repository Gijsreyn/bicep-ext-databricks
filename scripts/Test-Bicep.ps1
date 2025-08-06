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