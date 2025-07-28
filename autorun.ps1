param (
    [ValidateNotNullOrEmpty()][string]$SettingsFile = "$(Join-Path $Env:LOCALAPPDATA (Join-Path 'Raw Accel' 'settings.json'))"
)

$SettingsFile = Convert-Path($SettingsFile)
$settingsDirectory = [IO.Path]::GetDirectoryName($SettingsFile)
$settingsFileName = [IO.Path]::GetFileName($SettingsFile)

$rawaccel_exe = Join-Path $PSScriptRoot rawaccel.exe
$writer_exe = Join-Path $PSScriptRoot writer.exe

if (Test-Path -PathType 'Container' -Path $settingsDirectory) {
    Start-Process -WorkingDirectory $settingsDirectory -FilePath $writer_exe -ArgumentList $settingsFileName
} elseif (New-Item -ItemType 'directory' -Path $settingsDirectory -Force) {
    Start-Process -WorkingDirectory $settingsDirectory -FilePath $rawaccel_exe
}