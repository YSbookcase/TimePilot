[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $true)]
    [string]$DataDirectory
)

$ErrorActionPreference = 'Stop'
$expanded = [Environment]::ExpandEnvironmentVariables($DataDirectory)
$root = [IO.Path]::GetPathRoot($expanded)
if ([string]::IsNullOrWhiteSpace($root) -or ($root.Length -eq 2 -and $root[1] -eq ':')) {
    throw 'DataDirectory must be an absolute file-system path.'
}
$directory = [IO.Path]::GetFullPath($expanded)
foreach ($name in @('timepilot.db', 'settings.json')) {
    if (-not (Test-Path -LiteralPath (Join-Path $directory $name) -PathType Leaf)) {
        throw "The selected folder must already contain $name. No data will be created or copied."
    }
}
if ($directory.Contains('"')) {
    throw 'The data directory cannot contain a quote.'
}

$projectDirectory = Join-Path (Split-Path $PSScriptRoot -Parent) 'TimePilot.WinForms'
$propertiesDirectory = Join-Path $projectDirectory 'Properties'
$launchSettingsPath = Join-Path $propertiesDirectory 'launchSettings.json'
$settings = if (Test-Path -LiteralPath $launchSettingsPath) {
    Get-Content -LiteralPath $launchSettingsPath -Raw | ConvertFrom-Json
} else {
    [pscustomobject]@{ profiles = [pscustomobject]@{} }
}
if ($null -eq $settings.profiles) {
    $settings | Add-Member -NotePropertyName profiles -NotePropertyValue ([pscustomobject]@{}) -Force
}
$profile = [pscustomobject]@{
    commandName = 'Project'
    commandLineArgs = "--data-dir `"$directory`""
}
if ($PSCmdlet.ShouldProcess($launchSettingsPath, 'Configure the SharedStoreData development profile')) {
    $settings.profiles | Add-Member -NotePropertyName SharedStoreData -NotePropertyValue $profile -Force
    New-Item -ItemType Directory -Path $propertiesDirectory -Force | Out-Null
    $settings | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $launchSettingsPath -Encoding UTF8
    Write-Output "Configured SharedStoreData: $directory"
    Write-Output 'Select SharedStoreData in Visual Studio, or use dotnet run --project TimePilot.WinForms --launch-profile SharedStoreData.'
    Write-Warning 'This profile writes to the selected real database and settings. Each launch creates a safety ZIP before recording. Windows startup registration is not changed.'
}
