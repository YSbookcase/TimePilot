param(
    [string]$Version = "0.2.2",
    [string]$Runtime = "win-x64",
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "TimePilot.WinForms\TimePilot.WinForms.csproj"
$artifactsPath = Join-Path $repoRoot "artifacts\release"
$publishPath = Join-Path $artifactsPath "publish\$Runtime"
$zipPath = Join-Path $artifactsPath "TimePilot-$Version-$Runtime-portable.zip"
$installerScriptPath = Join-Path $repoRoot "installer\windows\TimePilot.iss"

New-Item -ItemType Directory -Force -Path $artifactsPath | Out-Null

if (Test-Path $publishPath) {
    Remove-Item -LiteralPath $publishPath -Recurse -Force
}

& dotnet publish $projectPath `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    /p:PublishSingleFile=false `
    /p:Version=$Version `
    /p:FileVersion=$Version.0 `
    /p:AssemblyVersion=$Version.0 `
    /p:PublishDir="$publishPath\"

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $publishPath "*") -DestinationPath $zipPath
Write-Host "Portable zip created: $zipPath"

if ($SkipInstaller) {
    return
}

$iscc = Get-Command iscc -ErrorAction SilentlyContinue
$isccPath = $iscc?.Source
if ($isccPath -eq $null) {
    $defaultIsccPaths = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
    )
    $isccPath = $defaultIsccPaths | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if ($isccPath -eq $null) {
    Write-Warning "Inno Setup Compiler (iscc) was not found. Install Inno Setup to build the installer."
    Write-Warning "Installer script: $installerScriptPath"
    return
}

& $isccPath "/DAppVersion=$Version" "/DSourceDir=$publishPath" "/DOutputDir=$artifactsPath" $installerScriptPath
