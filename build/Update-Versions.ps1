param(
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

function Update-CsprojThreePartVersion {
    param (
        [Parameter(Mandatory)]
        [string]$ProjectFile
    )

    if (-not (Test-Path $ProjectFile)) {
        throw "Project file not found: $ProjectFile"
    }

    $fourPartVersion = (Get-Content $script:versionFile -Raw).Trim()
    $threePartVersion = ($fourPartVersion -split '\.')[0..2] -join '.'

    $content = Get-Content $ProjectFile -Raw

    $tags = @('AssemblyVersion', 'Version', 'FileVersion')

    foreach ($tag in $tags) {
        if ($content -notmatch "<$tag>[^<]*</$tag>") {
            throw "Required <$tag> element not found in $ProjectFile"
        }

        $content = [Regex]::Replace(
            $content,
            "<$tag>[^<]*</$tag>",
            "<$tag>$threePartVersion</$tag>"
        )
    }

    [System.IO.File]::WriteAllText($ProjectFile, $content)
    Write-Info "Updated $(Split-Path $ProjectFile -Leaf) to version $threePartVersion"
}

function Update-AppxManifestVersion {
    param (
        [Parameter(Mandatory)]
        [string]$ManifestFile
    )

    if (-not (Test-Path $ManifestFile)) {
        throw "Manifest file not found: $ManifestFile"
    }

    $fourPartVersion = (Get-Content $script:versionFile -Raw).Trim()
    $content = Get-Content $ManifestFile -Raw

    $pattern = '(<Identity\b[^>]*\bVersion=")[^"]*(")'

    if ($content -notmatch $pattern) {
        throw "Version attribute in <Identity> tag not found or format changed"
    }

    $updated = [Regex]::Replace(
        $content,
        $pattern,
        "`$1$fourPartVersion`$2"
    )

    if ($content -ne $updated) {
        [System.IO.File]::WriteAllText($ManifestFile, $updated)
        Write-Info "Updated $(Split-Path $ManifestFile -Leaf) to version $fourPartVersion"
    }
}

# Main execution
Write-Header "Rebound Version Update Script"

try {
    $root = Resolve-Path "$PSScriptRoot\.."

    $script:versionFile     = Join-Path $root "var/VERSION.txt"
    $script:versionNameFile = Join-Path $root "var/VERSION_NAME.txt"
    $targetFile             = Join-Path $root "src/core/Rebound.Core/Version.cs"

    # Validate files exist
    Write-Step "Validating version files"
    if (-not (Test-Path $script:versionFile))     { throw "VERSION.txt not found" }
    if (-not (Test-Path $script:versionNameFile)) { throw "VERSION_NAME.txt not found" }
    if (-not (Test-Path $targetFile))             { throw "Version.cs not found" }
    Write-Success "All required files found"

    # Update Rebound.Core version
    Write-Step "Updating Rebound.Core version"
    $number = (Get-Content $script:versionFile -Raw).Trim()
    $title  = (Get-Content $script:versionNameFile -Raw).Trim()
    $newVersion = "v$number $title"

    $content = Get-Content $targetFile -Raw
    $pattern = '(public\s+static\s+string\s+ReboundVersion\s*=\s*")([^"]*)(";)'

    if ($content -notmatch $pattern) {
        throw "ReboundVersion field not found or format changed"
    }

    $updated = [Regex]::Replace($content, $pattern, "`$1$newVersion`$3")
    [System.IO.File]::WriteAllText($targetFile, $updated)
    Write-Success "Updated to: $newVersion"

    # Update .csproj files
    Write-Step "Updating .csproj versions"
    
    $csprojFiles = @(
        "src/apps/Rebound.About/Rebound.About.csproj",
        "src/apps/Rebound.UserAccountControlSettings/Rebound.UserAccountControlSettings.csproj",
        "src/platforms/shell/Rebound.Shell/Rebound.Shell.csproj",
        "src/system/Rebound.Hub/Rebound.Hub.csproj",
        "src/system/Rebound.ServiceHost/Rebound.ServiceHost.csproj"
    )

    foreach ($file in $csprojFiles) {
        Update-CsprojThreePartVersion -ProjectFile (Resolve-Path "$root\$file")
    }
    Write-Success "Updated $($csprojFiles.Count) project files"

    # Update manifest files
    Write-Step "Updating package manifests"
    
    $manifestFiles = @(
        "src/apps/Rebound.About/Package.appxmanifest",
        "src/apps/Rebound.UserAccountControlSettings/Package.appxmanifest",
        "src/platforms/shell/Rebound.Shell/Package.appxmanifest",
        "src/system/Rebound.Hub/Package.appxmanifest"
    )

    foreach ($file in $manifestFiles) {
        Update-AppxManifestVersion -ManifestFile (Resolve-Path "$root\$file")
    }
    Write-Success "Updated $($manifestFiles.Count) manifest files"

    Write-Host "`n  [SUCCESS] Version update completed!`n" -ForegroundColor Green
}
catch {
    Write-Host "`n  [FAILED] Version update failed!" -ForegroundColor Red
    Write-Host "  Error: $_`n" -ForegroundColor Red
    exit 1
}