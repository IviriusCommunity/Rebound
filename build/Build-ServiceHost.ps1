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

# Main execution
Write-Header "ServiceHost Build & Package Script"

try {
    $root = Resolve-Path "$PSScriptRoot\.."
    $projectPath = Join-Path $root "src\system\Rebound.ServiceHost\Rebound.ServiceHost.csproj"

    # Load target framework from .csproj
    Write-Step "Reading project configuration"
    [xml]$projXml = Get-Content $projectPath
    $targetFrameworkNode = $projXml.Project.PropertyGroup.TargetFramework | Select-Object -First 1
    
    if (-not $targetFrameworkNode) {
        throw "TargetFramework element not found in $projectPath"
    }
    
    $TargetFramework = $targetFrameworkNode.Trim()
    Write-Success "Target Framework: $TargetFramework"

    # Define paths
    $buildOutput = Join-Path $root "src\system\Rebound.ServiceHost\bin\$Platform\$Configuration\$TargetFramework\$RuntimeIdentifier\"
    $zipDestination = Join-Path $root "src\core\forge\Rebound.Forge.Assets\Modding\ServiceHost\ServiceHost.zip"
    $copyDestinationFolder = Join-Path $root "src\core\forge\Rebound.Forge.Assets\Modding\ServiceHost"

    # Build project
    Write-Step "Building Rebound.ServiceHost ($Configuration | $Platform)"
    
    $solutionDir = (Resolve-Path "$PSScriptRoot\..").Path + "\\"
    $msbuildArgs = @(
        $projectPath
        "/p:Configuration=$Configuration"
        "/p:Platform=$Platform"
        "/p:TargetFramework=$TargetFramework"
        "/p:RuntimeIdentifier=$RuntimeIdentifier"
        "/p:SolutionDir=$solutionDir"
        "/verbosity:minimal"
    )

    Write-Info "MSBuild: msbuild.exe $($msbuildArgs -join ' ')"
    
    & msbuild.exe @msbuildArgs
    
    if ($LASTEXITCODE -ne 0) {
        throw "MSBuild failed with exit code $LASTEXITCODE"
    }
    
    Write-Success "Build completed successfully"

    # Create destination folder if needed
    Write-Step "Packaging build output"
    
    if (-not (Test-Path $copyDestinationFolder)) {
        New-Item -ItemType Directory -Path $copyDestinationFolder | Out-Null
        Write-Info "Created destination folder"
    }

    # Remove old zip if exists
    if (Test-Path $zipDestination) {
        Remove-Item $zipDestination -Force
        Write-Info "Removed existing zip file"
    }

    # Create new zip
    Compress-Archive -Path "$buildOutput*" -DestinationPath $zipDestination -Force
    Write-Success "Created ServiceHost.zip"

    Write-Host "`n  [SUCCESS] Build and packaging completed!`n" -ForegroundColor Green
}
catch {
    Write-Host "`n  [FAILED] Build process failed!" -ForegroundColor Red
    Write-Host "  Error: $_`n" -ForegroundColor Red
    exit 1
}