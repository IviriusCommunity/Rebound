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
        [string]$Platform,

        [Parameter(Mandatory)]
        [string]$SolutionDir
    )

    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($ProjectPath)
    Write-Step "Building $projectName"

    # Build the project
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

    Write-Info "MSBuild: msbuild.exe $($msbuildArgs -join ' ')"
    
    & msbuild.exe @msbuildArgs
    
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
Write-Header "Distribution Build & Package Script"

try {
    $root = Resolve-Path "$PSScriptRoot\.."
    $projectPath = Join-Path $root "eng\distribution\standalone\Rebound.Uninstaller\Rebound.Uninstaller.csproj"

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
    $buildOutput = Join-Path $root "eng\distribution\standalone\Rebound.Uninstaller\bin\$Configuration\Publish\win-$Platform\Rebound Uninstaller.exe"
    $executableDestination = Join-Path $root "eng\distribution\standalone\Rebound.Distribution\Rebound Uninstaller.exe"

    # Build project
    Write-Step "Restoring NuGet packages for Rebound.Uninstaller"
    dotnet restore $projectPath

    Write-Step "Building Rebound.Uninstaller ($Configuration | $Platform)"
    
    $solutionDir = (Resolve-Path "$PSScriptRoot\..").Path + "\\"
    $msbuildArgs = @(
        $projectPath
        "-p:Configuration=Release"
        "-p:Platform=$Platform"
        "-p:PublishDir=bin\Release\Publish\win-$Platform\"
        "-p:PublishProtocol=FileSystem"
        "-p:_TargetId=Folder"
        "-p:TargetFramework=net10.0-windows10.0.26100.0"
        "-p:RuntimeIdentifier=win-$Platform"
        "-p:SelfContained=true"
        "-p:CopyMsixContentFromProjectReferences=false"
        "-p:DisableMsixProjectCapabilityAddedByProject=true"
        "-p:EnableMsixTooling=true"
    )

    Write-Info "MSBuild: msbuild.exe $($msbuildArgs -join ' ')"
    
    & dotnet publish @msbuildArgs
    
    if ($LASTEXITCODE -ne 0) {
        throw "MSBuild failed with exit code $LASTEXITCODE"
    }
    
    Write-Success "Build completed successfully"

    # Copy output over to Rebound Distribution
    Copy-Item -Path $buildOutput -Destination $executableDestination -Force
    Write-Success "Copied Rebound Uninstaller.exe"
    
    # Build and package Rebound Hub
    Write-Step "Building Rebound.Hub ($Configuration | $Platform)"
    
    $versionFile = Join-Path $root "var\VERSION.txt"

    # Read version
    Write-Step "Reading version information"
    if (-not (Test-Path $versionFile)) {
        throw "VERSION.txt not found at $versionFile"
    }

    $msixVersion = (Get-Content $versionFile -Raw).Trim()

    $hubPath = Join-Path $root "src\system\Rebound.Hub\Rebound.Hub.csproj"
    $hubDestinationPath = Join-Path $root "eng\distribution\standalone\Rebound.Distribution\Rebound.Hub.msixbundle"

    Build-MsixPackage `
        -ProjectPath $hubPath `
        -DestinationPath $hubDestinationPath `
        -MsixVersion $msixVersion `
        -Configuration $Configuration `
        -Platform $Platform `
        -SolutionDir $solutionDir

    Write-Host "`n  [SUCCESS] Build and packaging completed!`n" -ForegroundColor Green

    Write-Host "`n  [SUCCESS] Build distribution completed!`n" -ForegroundColor Green
}
catch {
    Write-Host "`n  [FAILED] Build process failed!" -ForegroundColor Red
    Write-Host "  Error: $_`n" -ForegroundColor Red
    exit 1
}