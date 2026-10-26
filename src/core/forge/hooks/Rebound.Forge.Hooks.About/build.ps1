
$ErrorActionPreference = "Stop"

$projectDir = $PSScriptRoot
$vsRoot = "C:\Program Files\Microsoft Visual Studio\18\Community"
$vcvars = Join-Path $vsRoot "VC\Auxiliary\Build\vcvarsall.bat"

if (!(Test-Path -LiteralPath $vcvars)) {
    throw "MSVC environment script not found: $vcvars"
}

function Build-ReboundDll {
    param(
        [Parameter(Mandatory)]
        [ValidateSet("x64", "ARM64")]
        [string]$Architecture
    )

    $buildDir = Join-Path $projectDir "build\$Architecture"
    $dllPath = Join-Path $projectDir "bin\Rebound.Forge.Hooks.About.$Architecture.dll"
    $workerPath = Join-Path $projectDir "build\worker-$Architecture.cmd"

    $vcvarsArch = if ($Architecture -eq "x64") {
        "x64"
    } else {
        "x64_arm64"
    }

    New-Item -ItemType Directory -Force -Path (Split-Path $workerPath) |
        Out-Null

    $worker = @"
@echo off
setlocal
call "$vcvars" $vcvarsArch
if errorlevel 1 exit /b 1

echo.
echo Target environment: %VSCMD_ARG_TGT_ARCH%
where cl
if errorlevel 1 exit /b 1

cl 2>&1 | findstr /C:"for x64" /C:"for ARM64"
if errorlevel 1 exit /b 1

cmake -S "$projectDir" -B "$buildDir" --fresh -G Ninja -DCMAKE_BUILD_TYPE=Release -DREBOUND_TARGET_ARCH=$Architecture
if errorlevel 1 exit /b 1

cmake --build "$buildDir"
if errorlevel 1 exit /b 1

endlocal
"@

    Set-Content -LiteralPath $workerPath -Value $worker -Encoding ascii

    Write-Host "`nBuilding $Architecture..."

    & $env:ComSpec /d /c "`"$workerPath`""

    if ($LASTEXITCODE -ne 0) {
        throw "$Architecture build failed with exit code $LASTEXITCODE"
    }

    if (!(Test-Path -LiteralPath $dllPath)) {
        throw "Expected DLL was not produced: $dllPath"
    }

    Write-Host "Built: $dllPath"
}

Build-ReboundDll -Architecture x64
Build-ReboundDll -Architecture ARM64

Write-Host "`nBoth builds completed."
