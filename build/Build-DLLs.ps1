param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

function Write-Header {
    param([string]$Message)
    Write-Host "`n$Message" -ForegroundColor Cyan
    Write-Host ("-" * $Message.Length) -ForegroundColor DarkGray
}

function Write-Info {
    param([string]$Message)
    Write-Host "  [INFO] " -NoNewline -ForegroundColor Blue
    Write-Host $Message
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

function Build-Hook {
    param(
        [string]$HookName,
        [string]$Root
    )

    $hookDirectory = Join-Path $Root "src\core\forge\hooks\$HookName"
    $buildScript = Join-Path $hookDirectory "build.ps1"
    $destinationDirectory = Join-Path $Root "src\core\forge\Rebound.Forge.Assets\Modding\DLLs"

    Write-Step "Building: $HookName"

    if (-not (Test-Path $buildScript)) {
        throw "Build script not found: $buildScript"
    }

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    Push-Location $hookDirectory
    try {
        & $buildScript
    }
    finally {
        Pop-Location
    }

    if (-not $?) {
        throw "Build failed for $HookName"
    }

    $stopwatch.Stop()

    Write-Success "Build completed in $([math]::Round($stopwatch.Elapsed.TotalSeconds, 2))s"

    foreach ($platform in @("x64", "ARM64")) {
        $dllName = "$HookName.$platform.dll"
        $sourceFile = Join-Path $hookDirectory "bin\$dllName"
        $destinationFile = Join-Path $destinationDirectory $dllName

        Write-Step "Copying $dllName"

        if (-not (Test-Path $sourceFile)) {
            throw "Build output not found: $sourceFile"
        }

        Copy-Item -Path $sourceFile -Destination $destinationFile -Force

        $fileSize = [math]::Round((Get-Item $destinationFile).Length / 1KB, 2)

        Write-Info "Destination: $destinationFile"
        Write-Info "Size: $fileSize KB"

        Write-Success "Copy completed"
    }
}

# Main execution
Write-Header "Rebound Native Build Script"
Write-Info "Started: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"

try {
    $root = Resolve-Path "$PSScriptRoot\.."

    $destinationDirectory = Join-Path $root "src\core\forge\Rebound.Forge.Assets\Modding\DLLs"

    if (-not (Test-Path $destinationDirectory)) {
        New-Item -Path $destinationDirectory -ItemType Directory -Force | Out-Null
        Write-Success "Created DLLs directory"
    }

    foreach ($hook in @(
        "Rebound.Forge.Hooks.About",
        "Rebound.Forge.Hooks.Launcher",
        "Rebound.Forge.Hooks.Run"
    )) {
        Build-Hook -HookName $hook -Root $root
    }

    Write-Host "`n  [SUCCESS] Build process completed!`n" -ForegroundColor Green
    exit 0
}
catch {
    Write-Host "`n  [FAILED] Build process failed!" -ForegroundColor Red
    Write-Host "  Error: $_`n" -ForegroundColor Red
    exit 1
}