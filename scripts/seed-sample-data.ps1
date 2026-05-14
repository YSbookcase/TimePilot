param(
    [switch]$Clear,
    [switch]$Status
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$outputPath = Join-Path $repoRoot "TimePilot.WinForms\bin\CodexVerify\"
$appPath = Join-Path $outputPath "TimePilot.WinForms.dll"

Push-Location $repoRoot
try {
    dotnet build TimePilot.sln --no-restore /p:OutputPath=$outputPath
    if ($LASTEXITCODE -ne 0) {
        throw "TimePilot build failed."
    }

    $argument = if ($Status) { "--check-sample-data" } elseif ($Clear) { "--clear-sample-data" } else { "--seed-sample-data" }
    dotnet $appPath $argument
    if ($LASTEXITCODE -ne 0) {
        throw "TimePilot sample data command failed."
    }

    if ($Status) {
        return
    }

    if ($Clear) {
        Write-Host "TimePilot sample data has been removed from %LocalAppData%\TimePilot\timepilot.db."
    }
    else {
        Write-Host "TimePilot sample data has been seeded into %LocalAppData%\TimePilot\timepilot.db."
    }
    Write-Host "Sample app process names start with timepilot_sample_."
}
finally {
    Pop-Location
}
