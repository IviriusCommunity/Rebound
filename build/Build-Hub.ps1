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

function Write-ErrorMessage {
    param([string]$Message)
    Write-Host "  [ERROR] " -NoNewline -ForegroundColor Red
    Write-Host $Message
}

function Resolve-MSBuild {
    $command = Get-Command msbuild.exe -ErrorAction SilentlyContinue

    if ($command) {
        return $command.Source
    }

    $vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"

    if (-not (Test-Path $vswhere)) {
        throw "vswhere.exe not found. Is Visual Studio installed?"
    }

    $msbuild = & $vswhere `
        -latest `
        -products '*' `
        -requires Microsoft.Component.MSBuild `
        -find 'MSBuild\Current\Bin\MSBuild.exe' |
        Select-Object -First 1

    if (-not $msbuild -or -not (Test-Path $msbuild)) {
        throw "Could not locate MSBuild. Ensure the MSBuild component is installed in Visual Studio."
    }

    return $msbuild
}

function Build-MsixPackage {
    param(
        [Parameter(Mandatory)]
        [string]$ProjectPath,

        [Parameter(Mandatory)]
        [string]$DestinationDirectory,

        [Parameter(Mandatory)]
        [string]$MsixVersion,

        [Parameter(Mandatory)]
        [string]$Configuration,

        [Parameter(Mandatory)]
        [string]$Platform,

        [Parameter(Mandatory)]
        [string]$SolutionDir
    )

    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($ProjectPath)

    Write-Step "Building $projectName"

    if (-not (Test-Path $ProjectPath)) {
        throw "Project not found: $ProjectPath"
    }

    $msbuildArgs = @(
        $ProjectPath
        "/t:Build"
        "/p:Configuration=$Configuration"
        "/p:Platform=$Platform"
        "/p:AppxBundle=Always"
        "/p:UapAppxPackageBuildMode=SideloadOnly"
        "/p:GenerateAppxPackageOnBuild=true"
        "/p:SolutionDir=$SolutionDir"
        "/verbosity:minimal"
    )

    Write-Info "MSBuild: $script:MSBuildPath $($msbuildArgs -join ' ')"

    & $script:MSBuildPath @msbuildArgs

    if ($LASTEXITCODE -ne 0) {
        throw "MSBuild failed for $projectName with exit code $LASTEXITCODE"
    }

    Write-Success "Build completed"

    $projectDir = [System.IO.Path]::GetDirectoryName($ProjectPath)
    $appPackagesFolder = Join-Path $projectDir "AppPackages"

    Write-Info "Searching for packages in $appPackagesFolder"

    if (-not (Test-Path $appPackagesFolder)) {
        throw "AppPackages folder not found at $appPackagesFolder"
    }

    $allBundles = @(Get-ChildItem -Path $appPackagesFolder -Filter "*.msixbundle" -Recurse -File)

    $bundleFile = $allBundles |
        Where-Object { $_.Name -like "*$MsixVersion*" } |
        Select-Object -First 1

    if (-not $bundleFile) {
        $foundFiles = $allBundles | ForEach-Object { $_.Name }
        throw "No .msixbundle found for version '$MsixVersion'. Found: $($foundFiles -join ', ')"
    }

    Write-Info "Selected bundle: $($bundleFile.FullName)"

    # Find the corresponding signing certificate.
    $certificateFile = Get-ChildItem -Path $appPackagesFolder -Filter "*.cer" -Recurse -File |
        Where-Object {
            $_.DirectoryName -eq $bundleFile.DirectoryName
        } |
        Select-Object -First 1

    if (-not $certificateFile) {
        throw "No signing certificate (.cer) found alongside $($bundleFile.Name)"
    }

    if (-not (Test-Path $DestinationDirectory)) {
        New-Item -ItemType Directory -Path $DestinationDirectory -Force | Out-Null
    }

    $bundleDestination = Join-Path $DestinationDirectory "Rebound.Hub.msixbundle"
    $certificateDestination = Join-Path $DestinationDirectory "Rebound.Hub.cer"

    Copy-Item -Path $bundleFile.FullName -Destination $bundleDestination -Force
    Write-Success "Copied Rebound.Hub.msixbundle"

    Copy-Item -Path $certificateFile.FullName -Destination $certificateDestination -Force
    Write-Success "Copied Rebound.Hub.cer"
}

# Main execution
Write-Header "Rebound Distribution Builder"

try {
    $root = (Resolve-Path "$PSScriptRoot\..").Path

    # Resolve MSBuild.
    $script:MSBuildPath = Resolve-MSBuild
    Write-Info "Using MSBuild: $script:MSBuildPath"

    # Read version.
    $versionFile = Join-Path $root "var\VERSION.txt"

    Write-Step "Reading version information"

    if (-not (Test-Path $versionFile)) {
        throw "VERSION.txt not found at $versionFile"
    }

    $msixVersion = (Get-Content $versionFile -Raw).Trim()

    Write-Success "Building packages for version: $msixVersion"

    $hubPath = Join-Path $root "src\system\Rebound.Hub\Rebound.Hub.csproj"

    $hubDestinationDirectory = Join-Path $root "eng\Rebound.Installer\Hub"

    $solutionDir = $root + "\"

    # Build Rebound.Hub and copy its bundle and certificate.
    Build-MsixPackage `
        -ProjectPath $hubPath `
        -DestinationDirectory $hubDestinationDirectory `
        -MsixVersion $msixVersion `
        -Configuration $Configuration `
        -Platform "x64" `
        -SolutionDir $solutionDir

    Write-Host "`n  [SUCCESS] Distribution build completed!`n" -ForegroundColor Green
}
catch {
    Write-Host "`n  [FAILED] Distribution build failed!" -ForegroundColor Red
    Write-Host "  Error: $_`n" -ForegroundColor Red
    exit 1
}