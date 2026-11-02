param(
    [string]$Configuration = "Release",
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

function Build-Publish {
    param(
        [string]$ProjectPath,
        [string]$ExeName,
        [string]$CopyDestination,
        [string]$Platform,
        [string]$RuntimeIdentifier
    )

    Write-Step "Building $ExeName ($Configuration | $Platform)"

    $publishDirectory = "bin\$Configuration\Publish\$RuntimeIdentifier\"

    $msbuildArgs = @(
        $ProjectPath
        "-p:Configuration=$Configuration"
        "-p:Platform=$Platform"
        "-p:PublishDir=$publishDirectory"
        "-p:PublishProtocol=FileSystem"
        "-p:_TargetId=Folder"
        "-p:TargetFramework=net10.0-windows10.0.26100.0"
        "-p:RuntimeIdentifier=$RuntimeIdentifier"
        "-p:SelfContained=true"
        "-p:DisableMsixProjectCapabilityAddedByProject=true"
        "-p:EnableMsixTooling=true"
        "-p:PublishSingleFile=true"
        "-p:IncludeAllContentForSelfExtract=true"
        "-p:WindowsPackageType=None"
    )

    Write-Host "Building Rebound.Core.UI for $RuntimeIdentifier..."

    dotnet build "..\src\core\Rebound.Core.UI\Rebound.Core.UI.csproj" `
        --configuration $Configuration `
        -p:Platform=$Platform `
        --runtime $RuntimeIdentifier

    if ($LASTEXITCODE -ne 0) {
        throw "Rebound.Core.UI build failed for $RuntimeIdentifier."
    }

    Write-Info "dotnet publish $($msbuildArgs -join ' ')"

    & dotnet publish @msbuildArgs

    if ($LASTEXITCODE -ne 0) {
        throw "$ExeName ($Platform) build failed with exit code $LASTEXITCODE"
    }

    $buildOutput = Join-Path (Split-Path $ProjectPath) "$publishDirectory$ExeName.exe"

    if (-not (Test-Path $buildOutput)) {
        throw "Published executable not found: $buildOutput"
    }

    Copy-Item -Path $buildOutput -Destination $CopyDestination -Force

    Write-Success "$ExeName ($Platform) built and copied to Desktop"
}

Write-Header "Installer Build Script"

try {
    $root = (Resolve-Path "$PSScriptRoot\..").Path
    $desktop = [Environment]::GetFolderPath("Desktop")

    # Project paths
    $installerProj = Join-Path $root "eng\Rebound.Installer\Rebound.Installer.csproj"

    if (-not (Test-Path $installerProj)) {
        throw "Installer project not found: $installerProj"
    }

    # Architectures
    $architectures = @(
        @{
            Platform = "x64"
            RuntimeIdentifier = "win-x64"
            Suffix = "x64"
        },
        @{
            Platform = "ARM64"
            RuntimeIdentifier = "win-arm64"
            Suffix = "ARM64"
        }
    )

    foreach ($architecture in $architectures) {
        $platform = $architecture.Platform
        $rid = $architecture.RuntimeIdentifier
        $suffix = $architecture.Suffix

        $destination = Join-Path $desktop "Rebound Installer ($suffix).exe"

        Build-Publish `
            -ProjectPath $installerProj `
            -ExeName "Rebound.Installer" `
            -CopyDestination $destination `
            -Platform $platform `
            -RuntimeIdentifier $rid
    }

    Write-Host "`n  [SUCCESS] Both architectures built successfully!`n" -ForegroundColor Green
}
catch {
    Write-Host "`n  [FAILED] Build process failed!" -ForegroundColor Red
    Write-Host "  Error: $_`n" -ForegroundColor Red
    exit 1
}