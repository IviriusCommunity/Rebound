param (
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [string]$RuntimeIdentifier = "win-x64"
)

$root = Resolve-Path "$PSScriptRoot\.."

$projectPath = Join-Path $root "src\system\Rebound.ServiceHost\Rebound.ServiceHost.csproj"

# Load the .csproj XML and get TargetFramework (first occurrence)
[xml]$projXml = Get-Content $projectPath
$targetFrameworkNode = $projXml.Project.PropertyGroup.TargetFramework | Select-Object -First 1

if (-not $targetFrameworkNode) {
    throw "TargetFramework element not found in $projectPath"
}

$TargetFramework = $targetFrameworkNode.Trim()

$buildOutput = Join-Path $root "src\system\Rebound.ServiceHost\bin\$Platform\$Configuration\$TargetFramework\$RuntimeIdentifier\"
$zipDestination = Join-Path $root "src\core\forge\Rebound.Forge.Assets\Modding\ServiceHost\ServiceHost.zip"
$copyDestinationFolder = Join-Path $root "src\core\forge\Rebound.Forge.Assets\Modding\ServiceHost"

Write-Host "[INFO] Building Rebound.ServiceHost project in Release | x64..."

$msbuildExe = "msbuild.exe"
$projectPath = "..\src\system\Rebound.ServiceHost\Rebound.ServiceHost.csproj"
$configuration = "Release"
$platform = "x64"
$targetFramework = "net10.0-windows10.0.26100.0"
$runtimeId = "win-x64"

$solutionDir = (Resolve-Path "..\" | Select-Object -ExpandProperty Path) + "\"

$arguments = @(
    $projectPath,
    "/p:Configuration=$configuration",
    "/p:Platform=$platform",
    "/p:TargetFramework=$targetFramework",
    "/p:RuntimeIdentifier=$runtimeId",
    "/p:SolutionDir=`"$solutionDir`"",
    "/verbosity:minimal"
) -join " "

Write-Host "Running: $msbuildExe $arguments"

Invoke-Expression "$msbuildExe $arguments"

Write-Host "MSBuild exited with code $LASTEXITCODE"

if ($LASTEXITCODE -ne 0) {
    throw "MSBuild failed with exit code $LASTEXITCODE"
}

Write-Host "[INFO] Zipping ServiceHost output folder '$buildOutput' to '$zipDestination'"

if (Test-Path $zipDestination) {
    Remove-Item $zipDestination -Force
}

if (-not (Test-Path $copyDestinationFolder)) {
    New-Item -ItemType Directory -Path $copyDestinationFolder | Out-Null
}

Compress-Archive -Path "$buildOutput*" -DestinationPath $zipDestination -Force

Write-Host "[INFO] Rebound.ServiceHost build and zip completed."
