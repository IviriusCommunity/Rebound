$ErrorActionPreference = "Stop"

$VS = "C:\Program Files\Microsoft Visual Studio\18\Community"
$VCVARS = "$VS\VC\Auxiliary\Build\vcvarsall.bat"

$Builds = @(
    @{ Name = "x64";   VcArch = "x64" }
    @{ Name = "ARM64"; VcArch = "x64_arm64" }
)

foreach ($Build in $Builds) {
    $BuildDir = "build\$($Build.Name)"

    Write-Host "`n=== Building $($Build.Name) ===" -ForegroundColor Cyan

    $Command = @(
        "call `"$VCVARS`" $($Build.VcArch)",
        "cmake -S . -B `"$BuildDir`" -G Ninja",
        "cmake --build `"$BuildDir`" --config Release"
    ) -join " && "

    & cmd.exe /d /c $Command

    if ($LASTEXITCODE -ne 0) {
        throw "Build failed for $($Build.Name) (exit code $LASTEXITCODE)"
    }
}

Write-Host "`nAll builds completed." -ForegroundColor Green