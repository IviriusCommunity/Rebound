# Get the current script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Define the paths for the source files and output (relative to the script directory)
$resourceFile = Join-Path $scriptDir "Resources.rc"
$cxxFile = Join-Path $scriptDir "Program.cpp"
$resFile = Join-Path $scriptDir "Resources.res"
$outputExe = Join-Path $scriptDir "Launcher.exe"

# Ensure that paths with spaces are properly quoted
$resourceFile = "`"$resourceFile`""
$cxxFile = "`"$cxxFile`""
$resFile = "`"$resFile`""
$outputExe = "`"$outputExe`""

# Step 1: Compile the resource file using rc.exe (assuming the Developer Command Prompt is set up)
Write-Host "Compiling the resource file..."
& "rc.exe" $resourceFile

# Step 2: Compile the C++ source code and link with the resource file using cl.exe
Write-Host "Compiling C++ source and linking with resource..."
& "cl.exe" $cxxFile $resFile /link /out:$outputExe
Write-Host "Build complete!"