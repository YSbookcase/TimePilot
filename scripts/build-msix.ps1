param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $repoRoot "TimePilot.Packaging\TimePilot.Packaging.wapproj"

$msbuildCandidates = @(
    "${env:ProgramFiles}\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
)

$msbuildPath = $msbuildCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if ($msbuildPath -eq $null) {
    throw "MSBuild.exe was not found. Install Visual Studio with MSIX Packaging Tools."
}

& $msbuildPath $projectPath `
    /restore `
    /p:Configuration=$Configuration `
    /p:Platform=$Platform `
    /p:AppxPackageSigningEnabled=false `
    /p:UapAppxPackageBuildMode=StoreUpload

if ($LASTEXITCODE -ne 0) {
    throw "MSIX package build failed with exit code $LASTEXITCODE."
}
