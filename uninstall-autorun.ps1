# Run as administrator
if (-Not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] 'Administrator')) {
    Start-Process -WorkingDirectory $PWD -FilePath PowerShell.exe -Verb Runas -ArgumentList $(('-File "{0}"' -f $MyInvocation.MyCommand.Path); $MyInvocation.UnboundArguments)
    Exit
}

Unregister-ScheduledTask `
    -TaskName 'Autorun on sign in' `
    -TaskPath '\Raw Accel\' `
    -Confirm:$false