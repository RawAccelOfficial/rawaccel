# Run as administrator
if (-Not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] 'Administrator')) {
    Start-Process -WorkingDirectory $PWD -FilePath PowerShell.exe -Verb Runas -ArgumentList $(('-File "{0}"' -f $MyInvocation.MyCommand.Path); $MyInvocation.UnboundArguments)
    Exit
}

$autorunScript = Join-Path $PSScriptRoot autorun.ps1
$stateChangeTrigger = Get-CimClass `
    -Namespace ROOT\Microsoft\Windows\TaskScheduler `
    -ClassName MSFT_TaskSessionStateChangeTrigger

Register-ScheduledTask `
    -Force `
    -TaskName 'Autorun on sign in' `
    -TaskPath '\Raw Accel\' `
    -Description 'Apply user specific Raw Accel settings at sign in for each user.' `
    -Principal (New-ScheduledTaskPrincipal -GroupId Users) `
    -Trigger (New-ScheduledTaskTrigger -AtLogOn), `
        (
            New-CimInstance $stateChangeTrigger `
                -Property @{
                    StateChange = 8  # TASK_SESSION_STATE_CHANGE_TYPE.TASK_SESSION_UNLOCK (taskschd.h)
                } `
                -ClientOnly
        ) `
    -Action (New-ScheduledTaskAction -Execute 'PowerShell' -Argument ('-WindowStyle Hidden -File "{0}"' -f $autorunScript)) `
    -Settings (New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -ExecutionTimeLimit 0)
