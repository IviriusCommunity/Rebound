param(
    [switch]$Verbose
)

$ErrorActionPreference = 'Stop'

function Write-Header {
    param([string]$Message)
    Write-Host "`n$Message" -ForegroundColor Cyan
    Write-Host ("-" * $Message.Length) -ForegroundColor DarkGray
}

function Write-Success {
    param([string]$Message)
    Write-Host "  [OK] " -NoNewline -ForegroundColor Green
    Write-Host $Message
}

function Write-Step {
    param([string]$Message)
    Write-Host "`n  $Message" -ForegroundColor White
}

function Write-ErrorMessage {
    param([string]$Message)
    Write-Host "  [ERROR] " -NoNewline -ForegroundColor Red
    Write-Host $Message
}

# Main execution
Write-Header "Rebound Full Build"

try {
    $scriptFolder = Split-Path -Parent $MyInvocation.MyCommand.Path
    
    $scriptsToRun = @(
        "Build-Native.ps1",
        "Build-ServiceHost.ps1",
        "Build-Packaged-Apps.ps1"
    )

    $completedCount = 0
    
    foreach ($script in $scriptsToRun) {
        $scriptPath = Join-Path $scriptFolder $script
        
        Write-Step "Running $script"
        
        if (-not (Test-Path $scriptPath)) {
            throw "Script not found: $scriptPath"
        }

        $params = @{}
        if ($Verbose) {
            $params['Verbose'] = $true
        }

        # Reset LASTEXITCODE before running
        $global:LASTEXITCODE = 0
        
        & $scriptPath @params
        
        if ($LASTEXITCODE -ne 0) {
            throw "Script failed with exit code $LASTEXITCODE"
        }
        
        Write-Success "$script completed"
        $completedCount++
    }

    Write-Host "`n  [SUCCESS] All $completedCount build script(s) completed!`n" -ForegroundColor Green
}
catch {
    Write-Host "`n  [FAILED] Build process failed!" -ForegroundColor Red
    Write-Host "  Error: $_`n" -ForegroundColor Red
    exit 1
}