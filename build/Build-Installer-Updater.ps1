param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [string]$RuntimeIdentifier = "win-x64",
    [switch]$Verbose
)

$ErrorActionPreference = 'Stop'

function Write-Header {
    param([string]$Message)
    Write-Host "`n$Message" -ForegroundColor Cyan
    Write-Host ("-" * $Message.Length) -ForegroundColor DarkGray
}

function Write-Info {
    param([string]$Message)
    if ($Verbose) {
        Write-Host "  [INFO] " -NoNewline -ForegroundColor Blue
        Write-Host $Message
    }
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

function Build-Publish {
    param(
        [string]$ProjectPath,
        [string]$ExeName,
        [string]$CopyDestination
    )

    Write-Step "Building $ExeName ($Configuration | $Platform)"

    $msbuildArgs = @(
        $ProjectPath
        "-p:Configuration=$Configuration"
        "-p:Platform=$Platform"
        "-p:PublishDir=bin\$Configuration\Publish\win-$Platform\"
        "-p:PublishProtocol=FileSystem"
        "-p:_TargetId=Folder"
        "-p:TargetFramework=net10.0-windows10.0.26100.0"
        "-p:RuntimeIdentifier=win-$Platform"
        "-p:SelfContained=true"
        "-p:CopyMsixContentFromProjectReferences=false"
        "-p:DisableMsixProjectCapabilityAddedByProject=true"
        "-p:EnableMsixTooling=true"
    )

    Write-Info "dotnet publish $($msbuildArgs -join ' ')"
    & dotnet publish @msbuildArgs

    if ($LASTEXITCODE -ne 0) {
        throw "$ExeName build failed with exit code $LASTEXITCODE"
    }

    $buildOutput = Join-Path (Split-Path $ProjectPath) "bin\$Configuration\Publish\win-$Platform\$ExeName.exe"
    Copy-Item -Path $buildOutput -Destination $CopyDestination -Force

    Write-Success "$ExeName built and copied"
}

# ===========================
# Main execution
# ===========================

Write-Header "Installer and Updater Build Script"

try {
    $root = Resolve-Path "$PSScriptRoot\.."
    $desktop = [Environment]::GetFolderPath("Desktop")

    # Project paths
    $installerProj   = Join-Path $root "eng\distribution\standalone\Rebound.Installer\Rebound.Installer.csproj"
    $updaterProj     = Join-Path $root "eng\distribution\standalone\Rebound.Updater\Rebound.Updater.csproj"

    # Destinations
    $installerDest = Join-Path $desktop "Rebound Installer.exe"
    $updaterDest   = Join-Path $desktop "Rebound Updater.exe"

    # Build all
    Build-Publish `
        -ProjectPath $installerProj `
        -ExeName "Rebound Installer" `
        -CopyDestination $installerDest

    Build-Publish `
        -ProjectPath $updaterProj `
        -ExeName "Rebound Updater" `
        -CopyDestination $updaterDest

    Write-Host "`n  [SUCCESS] Build Installer and Updater completed!`n" -ForegroundColor Green
}
catch {
    Write-Host "`n  [FAILED] Build process failed!" -ForegroundColor Red
    Write-Host "  Error: $_`n" -ForegroundColor Red
    exit 1
}
