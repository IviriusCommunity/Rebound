$scriptFolder = Split-Path -Parent $MyInvocation.MyCommand.Path

$scriptsToRun = @(
    "Build-Native.ps1",
    "Build-ServiceHost.ps1"
)

foreach ($script in $scriptsToRun) {
    $scriptPath = Join-Path $scriptFolder $script
    Write-Host "[INFO] Running $script"
    
    try {
        & $scriptPath
        if ($LASTEXITCODE -ne 0) {
            throw "Script $script failed with exit code $LASTEXITCODE"
        }
    }
    catch {
        Write-Error $_
        break
    }
}

Write-Host "[INFO] All scripts completed."
