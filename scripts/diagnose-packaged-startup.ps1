param(
    [Parameter(Mandatory = $true)][string]$OutputPath,
    [string]$TaskId = 'ActiveLogbookStartupV2'
)

$ErrorActionPreference = 'Stop'
$report = [ordered]@{ Timestamp = [DateTimeOffset]::Now.ToString('o') }
try {
    Add-Type -AssemblyName System.Runtime.WindowsRuntime
    $package = [Windows.ApplicationModel.Package,Windows.ApplicationModel,ContentType=WindowsRuntime]::Current
    $report.Package = $package.Id.FullName
    $startupType = [Windows.ApplicationModel.StartupTask,Windows.ApplicationModel,ContentType=WindowsRuntime]
    $operation = $startupType::GetAsync($TaskId)
    $asTask = [System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {
        $_.Name -eq 'AsTask' -and $_.IsGenericMethod -and
        $_.GetGenericArguments().Count -eq 1 -and $_.GetParameters().Count -eq 1 -and
        $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1'
    } | Select-Object -First 1
    $task = $asTask.MakeGenericMethod($startupType).Invoke($null, @($operation))
    if (-not $task.Wait(10000)) { throw 'StartupTask query timed out.' }
    $report.TaskId = $task.Result.TaskId
    $report.State = $task.Result.State.ToString()
    $report.StateValue = [int]$task.Result.State
    if ($package.Id.Name -eq 'YSBookcase.ActiveLogbook') {
        $settingsPath = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'TimePilot\settings.json'
        $report.SettingsPath = $settingsPath
        if (Test-Path -LiteralPath $settingsPath) {
            $report.StartWithWindows = (Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json).StartWithWindows
        }
    }
} catch {
    $report.Error = $_.Exception.ToString()
}
$report | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $OutputPath -Encoding UTF8
