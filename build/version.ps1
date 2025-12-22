$root = Resolve-Path "$PSScriptRoot\.."

$versionFile     = Join-Path $root "var/VERSION.txt"
$versionNameFile = Join-Path $root "var/VERSION_NAME.txt"
$targetFile      = Join-Path $root "src/core/Rebound.Core/Version.cs"

if (-not (Test-Path $versionFile))     { throw "VERSION.txt not found" }
if (-not (Test-Path $versionNameFile)) { throw "VERSION_NAME.txt not found" }
if (-not (Test-Path $targetFile))      { throw "Version.cs not found" }

# Rebound.Core version (Variables.cs)

$number = (Get-Content $versionFile -Raw).Trim()
$title  = (Get-Content $versionNameFile -Raw).Trim()

$newVersion = "v$number $title"

$content = Get-Content $targetFile -Raw

$pattern = '(public\s+static\s+string\s+ReboundVersion\s*=\s*")([^"]*)(";)'

if ($content -notmatch $pattern) {
    throw "ReboundVersion field not found or format changed"
}

$updated = [Regex]::Replace(
    $content,
    $pattern,
    "`$1$newVersion`$3"
)

[System.IO.File]::WriteAllText($targetFile, $updated)

# Win32 version helper

function Update-CsprojThreePartVersion {
    param (
        [Parameter(Mandatory)]
        [string]$ProjectFile
    )

    $root = Resolve-Path "$PSScriptRoot\.."

    if (-not (Test-Path $ProjectFile)) {
        throw "Project file not found: $ProjectFile"
    }

    $fourPartVersion = (Get-Content $script:versionFile -Raw).Trim()

    $threePartVersion = ($fourPartVersion -split '\.')[0..2] -join '.'

    $content = Get-Content $ProjectFile -Raw

    $tags = @(
        'AssemblyVersion',
        'Version',
        'FileVersion'
    )

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
}

# Rebound.About

Update-CsprojThreePartVersion `
    -ProjectFile (Resolve-Path "$PSScriptRoot\..\src/apps/Rebound.About/Rebound.About.csproj")

# Rebound.UserAccountControlSettings

Update-CsprojThreePartVersion `
    -ProjectFile (Resolve-Path "$PSScriptRoot\..\src/apps/Rebound.UserAccountControlSettings/Rebound.UserAccountControlSettings.csproj")

# Rebound.Shell

Update-CsprojThreePartVersion `
    -ProjectFile (Resolve-Path "$PSScriptRoot\..\src/platforms/shell/Rebound.Shell/Rebound.Shell.csproj")

# Rebound.Hub

Update-CsprojThreePartVersion `
    -ProjectFile (Resolve-Path "$PSScriptRoot\..\src/system/Rebound.Hub/Rebound.Hub.csproj")

# Rebound.ServiceHost

Update-CsprojThreePartVersion `
    -ProjectFile (Resolve-Path "$PSScriptRoot\..\src/system/Rebound.ServiceHost/Rebound.ServiceHost.csproj")

# Package manifest version helper

function Update-AppxManifestVersion {
    param (
        [Parameter(Mandatory)]
        [string]$ManifestFile
    )

    $root = Resolve-Path "$PSScriptRoot\.."

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
        Write-Host "Updated <Identity> Version to $fourPartVersion in $ManifestFile"
    }
    else {
        Write-Host "No changes needed in $ManifestFile"
    }
}