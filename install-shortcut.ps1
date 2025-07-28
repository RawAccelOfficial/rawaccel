# Run as administrator
if (-Not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] 'Administrator')) {
    Start-Process -WorkingDirectory $PWD -FilePath PowerShell.exe -Verb Runas -ArgumentList $(('-File "{0}"' -f $MyInvocation.MyCommand.Path); $MyInvocation.UnboundArguments)
    Exit
}

$rawaccel_exe = Join-Path $PSScriptRoot rawaccel.exe

$shell = New-Object -comObject WScript.Shell
$shortcut = $shell.CreateShortcut("$Env:PROGRAMDATA\Microsoft\Windows\Start Menu\Programs\Raw Accel.lnk")
$shortcut.TargetPath = $rawaccel_exe
$Shortcut.WorkingDirectory = '%LOCALAPPDATA%\Raw Accel'
$Shortcut.Description = 'Edit mouse acceleration curves'
$shortcut.Save()
