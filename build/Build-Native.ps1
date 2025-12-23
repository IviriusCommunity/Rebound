param(
    [string]$Configuration = 'Release',
    [string]$Platform = 'x64'
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

function Build-NativeProject {
    param(
        [string]$ProjectPath,
        [string]$OutputFileRelativePath,
        [string]$CopyDestinationRelativePath,
        [string]$Configuration = "Release",
        [string]$Platform = "x64"
    )
    
    $root = Resolve-Path "$PSScriptRoot\.."
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($ProjectPath)
    
    Write-Step "Building: $projectName"
    Write-Info "Configuration: $Configuration | Platform: $Platform"
    
    $msbuildArgs = @(
        $ProjectPath,
        "/p:Configuration=$Configuration",
        "/p:Platform=$Platform",
        "/verbosity:minimal",
        "/nologo",
        "/consoleloggerparameters:NoSummary"
    )
    
    $tempOutput = [System.IO.Path]::GetTempFileName()
    $tempError = [System.IO.Path]::GetTempFileName()
    
    try {
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        
        $process = Start-Process msbuild.exe `
            -ArgumentList $msbuildArgs `
            -WorkingDirectory $root `
            -NoNewWindow `
            -Wait `
            -PassThru `
            -RedirectStandardOutput $tempOutput `
            -RedirectStandardError $tempError
        
        $stopwatch.Stop()
        
        $output = Get-Content $tempOutput -Raw
        $errors = Get-Content $tempError -Raw
        
        if ($process.ExitCode -ne 0) {
            Write-ErrorMessage "Build failed (Exit code: $($process.ExitCode))"
            
            if ($errors -and $errors.Trim()) {
                Write-Host "`n  Error Details:" -ForegroundColor Red
                $errors -split "`n" | ForEach-Object {
                    if ($_.Trim()) { Write-Host "    $_" -ForegroundColor Red }
                }
            }
            
            if ($output -match "(error|warning)" -and $output.Trim()) {
                Write-Host "`n  Build Output:" -ForegroundColor Yellow
                $output -split "`n" | Where-Object { $_ -match "(error|warning)" } | ForEach-Object {
                    if ($_.Trim()) { Write-Host "    $_" -ForegroundColor Yellow }
                }
            }
            
            throw "MSBuild failed"
        }
        
        # Show warnings even on success
        $warningLines = $output -split "`n" | Where-Object { $_ -match "warning" }
        if ($warningLines) {
            Write-Host "`n  Warnings:" -ForegroundColor Yellow
            $warningLines | ForEach-Object {
                Write-Host "    $_" -ForegroundColor Yellow
            }
        }
        
        Write-Success "Build completed in $([math]::Round($stopwatch.Elapsed.TotalSeconds, 2))s"
        
    }
    finally {
        Remove-Item $tempOutput -ErrorAction SilentlyContinue
        Remove-Item $tempError -ErrorAction SilentlyContinue
    }
    
    # Copy output
    Write-Step "Copying Build Output"
    $sourceFile = Join-Path $root $OutputFileRelativePath
    $destFile = Join-Path $root $CopyDestinationRelativePath
    
    if (-not (Test-Path $sourceFile)) {
        Write-ErrorMessage "Source file not found: $sourceFile"
        throw "Build output not found"
    }
    
    $destDir = Split-Path $destFile -Parent
    if (-not (Test-Path $destDir)) {
        New-Item -Path $destDir -ItemType Directory -Force | Out-Null
    }
    
    Copy-Item -Path $sourceFile -Destination $destFile -Force
    
    $fileSize = [math]::Round((Get-Item $destFile).Length / 1KB, 2)
    Write-Info "Source: $(Split-Path $sourceFile -Leaf)"
    Write-Info "Destination: $CopyDestinationRelativePath"
    Write-Info "Size: $fileSize KB"
    Write-Success "Copy completed"
}

# Main execution
Write-Header "Rebound Native Build Script"
Write-Info "Started: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"

try {
    Build-NativeProject `
        -ProjectPath "src\system\Rebound.Launcher\Rebound.Launcher.vcxproj" `
        -OutputFileRelativePath "src\system\Rebound.Launcher\$Platform\$Configuration\Rebound.Launcher.exe" `
        -CopyDestinationRelativePath "src\core\forge\Rebound.Forge.Assets\Modding\Launchers\Rebound.Launcher.exe" `
        -Configuration $Configuration `
        -Platform $Platform
    
    Write-Host "`n  [SUCCESS] Build process completed!`n" -ForegroundColor Green
    exit 0
}
catch {
    Write-Host "`n  [FAILED] Build process failed!" -ForegroundColor Red
    Write-Host "  Error: $_`n" -ForegroundColor Red
    exit 1
}