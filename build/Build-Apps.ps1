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
    # First, check whether MSBuild is already available.
    $command = Get-Command msbuild.exe -ErrorAction SilentlyContinue

    if ($command) {
        return $command.Source
    }

    # Locate Visual Studio using vswhere.
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
        [string]$DestinationPath,
        
        [Parameter(Mandatory)]
        [string]$MsixVersion,
        
        [Parameter(Mandatory)]
        [string]$Configuration,
        
        [Parameter(Mandatory)]
        [string]$SolutionDir
    )

    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($ProjectPath)
    Write-Step "Building $projectName"

    # Build the project
    $msbuildArgs = @(
        $ProjectPath
        "/t:Restore;Build"
        "/p:Configuration=$Configuration"
        "/p:Platform=x64"
        "/p:AppxBundle=Always"
        "/p:AppxBundlePlatforms=x64|ARM64"
        "/p:UapAppxPackageBuildMode=SideloadOnly"
        "/p:GenerateAppxPackageOnBuild=true"
        "/p:SolutionDir=$SolutionDir"
        "/verbosity:minimal"
    )

    Write-Info "MSBuild: msbuild.exe $($msbuildArgs -join ' ')"

    & $script:MSBuildPath @msbuildArgs

    if ($LASTEXITCODE -ne 0) {
        throw "MSBuild failed for $projectName with exit code $LASTEXITCODE"
    }

    Write-Success "Build completed"

    # Find the generated .msixbundle
    $projectDir = [System.IO.Path]::GetDirectoryName($ProjectPath)
    $appPackagesFolder = Join-Path $projectDir "AppPackages"

    Write-Info "Searching for .msixbundle in $appPackagesFolder"

    if (-not (Test-Path $appPackagesFolder)) {
        throw "AppPackages folder not found at $appPackagesFolder"
    }

    $allBundles = Get-ChildItem -Path $appPackagesFolder -Filter "*.msixbundle" -Recurse
    
    if ($allBundles.Count -eq 0) {
        throw "No .msixbundle files found in $appPackagesFolder"
    }

    Write-Info "Found $($allBundles.Count) msixbundle file(s)"

    # Filter by version
    $versionedBundle = $allBundles | Where-Object { $_.Name -like "*$MsixVersion*" }

    if (-not $versionedBundle) {
        $foundFiles = $allBundles | ForEach-Object { $_.Name }
        throw "No .msixbundle file found containing version '$MsixVersion'. Found: $($foundFiles -join ', ')"
    }

    # Take the first match if multiple
    $bundleFile = $versionedBundle | Select-Object -First 1

    Write-Info "Selected bundle: $($bundleFile.Name)"

    # Ensure destination folder exists
    $destFolder = [System.IO.Path]::GetDirectoryName($DestinationPath)
    if (-not (Test-Path $destFolder)) {
        New-Item -ItemType Directory -Path $destFolder -Force | Out-Null
    }

    # Copy to destination
    Copy-Item -Path $bundleFile.FullName -Destination $DestinationPath -Force
    Write-Success "Copied to $(Split-Path $DestinationPath -Leaf)"
}

# Main execution
Write-Header "Rebound Package Builder"

try {
    $root = Resolve-Path "$PSScriptRoot\.."
    $script:MSBuildPath = Resolve-MSBuild
    Write-Info "Using MSBuild: $script:MSBuildPath"
    $versionFile = Join-Path $root "var\VERSION.txt"

    # Read version
    Write-Step "Reading version information"
    if (-not (Test-Path $versionFile)) {
        throw "VERSION.txt not found at $versionFile"
    }

    $msixVersion = (Get-Content $versionFile -Raw).Trim()
    Write-Success "Building packages for version: $msixVersion"

    # Define packages to build
    $packages = @(
        @{
            Name = "Rebound.About"
            Project = Join-Path $root "src\apps\Rebound.About\Rebound.About.csproj"
            Destination = Join-Path $root "src\core\forge\Rebound.Forge.Assets\Modding\Packages\Rebound.About.msixbundle"
        },
        @{
            Name = "Rebound.ServiceHost"
            Project = Join-Path $root "src\system\Rebound.ServiceHost\Rebound.ServiceHost.csproj"
            Destination = Join-Path $root "src\core\forge\Rebound.Forge.Assets\Modding\Packages\Rebound.ServiceHost.msixbundle"
        }
    )

    # Set SolutionDir
    $solutionDir = (Resolve-Path "$PSScriptRoot\..").Path + "\\"

    # Build each package
    $successCount = 0
    foreach ($package in $packages) {
        try {
            Build-MsixPackage `
                -ProjectPath $package.Project `
                -DestinationPath $package.Destination `
                -MsixVersion $msixVersion `
                -Configuration $Configuration `
                -SolutionDir $solutionDir
            
            $successCount++
        }
        catch {
            Write-ErrorMessage "Failed to build $($package.Name): $_"
            throw
        }
    }

    Write-Host "`n  [SUCCESS] Built and packaged $successCount application(s)!`n" -ForegroundColor Green
}
catch {
    Write-Host "`n  [FAILED] Package build failed!" -ForegroundColor Red
    Write-Host "  Error: $_`n" -ForegroundColor Red
    exit 1
}