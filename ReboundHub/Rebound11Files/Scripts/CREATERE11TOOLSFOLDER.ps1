# Get the current user's AppData\Roaming directory
$roamingPath = [System.Environment]::GetFolderPath("ApplicationData")

# Define the path to the Start Menu Programs directory
$startMenuPath = Join-Path -Path $roamingPath -ChildPath "Microsoft\Windows\Start Menu\Programs"

# Specify the new folder name
$newFolderName = "Rebound11Tools"

# Combine the path
$newFolderPath = Join-Path -Path $startMenuPath -ChildPath $newFolderName

# Check if the folder already exists
if (-not (Test-Path -Path $newFolderPath)) {
    # Create the new folder
    New-Item -ItemType Directory -Path $newFolderPath
    Write-Host "Folder created at: $newFolderPath"
} else {
    Write-Host "Folder already exists at: $newFolderPath"
}
