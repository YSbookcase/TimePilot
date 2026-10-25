[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9a-fA-F]{40}$')]
    [string]$CommunityCommit,
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    throw 'The offline adapter tests require Windows.'
}
$outputRoot = [IO.Path]::GetPathRoot($OutputDirectory)
if ([string]::IsNullOrWhiteSpace($outputRoot) -or $outputRoot.Length -lt 3 `
    -or ($outputRoot.Length -eq 2 -and $outputRoot[1] -eq ':')) {
    throw 'OutputDirectory must be an absolute, new directory.'
}
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) {
    throw 'OutputDirectory already exists. Existing artifacts are never overwritten or deleted.'
}
$parent = [IO.Path]::GetDirectoryName($output)
if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
    throw 'The output parent directory must already exist.'
}
$repository = Split-Path $PSScriptRoot -Parent
$resolved = & git -C $repository rev-parse --verify "$CommunityCommit^{commit}"
if ($LASTEXITCODE -ne 0 -or $resolved -ne $CommunityCommit) {
    throw 'The exact commit must already exist locally. Fetch it explicitly first.'
}

[IO.Directory]::CreateDirectory($output) | Out-Null
$community = Join-Path $output 'Community'
$consumer = Join-Path $output 'ExternalConsumer'
$archive = Join-Path $output 'community-input.zip'
& git -C $repository archive --format=zip --output=$archive $CommunityCommit -- `
    TimePilot.Analytics.Contracts TimePilot.Analytics.Sqlite TimePilot.Analytics.ConsumerTests samples/Analytics.HandoffConsumer
if ($LASTEXITCODE -ne 0) { throw 'Unable to export the fixed Community inputs.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory($archive, $community)
[IO.Directory]::CreateDirectory($consumer) | Out-Null
foreach ($name in @('Analytics.HandoffConsumer.csproj', 'Analytics.HandoffConsumer.sln', 'Program.cs')) {
    [IO.File]::Copy((Join-Path $community "samples/Analytics.HandoffConsumer/$name"), (Join-Path $consumer $name))
}
$sdk = & dotnet --version
if ($LASTEXITCODE -ne 0) { throw 'A .NET SDK is required.' }
@{
    CommunityCommit = $resolved
    SdkVersion = $sdk
    Platform = 'Windows'
    Is64BitOperatingSystem = [Environment]::Is64BitOperatingSystem
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'input.json') -Encoding UTF8

# The external solution references only two fixed-source projects via an explicit MSBuild property.
$solution = Join-Path $consumer 'Analytics.HandoffConsumer.sln'
& dotnet build $solution -c Release "-p:CommunityRoot=$community" -p:ShouldUnsetParentConfigurationAndPlatform=false
if ($LASTEXITCODE -ne 0) { throw 'External solution build failed.' }
& dotnet run --project (Join-Path $consumer 'Analytics.HandoffConsumer.csproj') -c Release `
    --no-build --no-restore "-p:CommunityRoot=$community" -- (Join-Path $output 'synthetic-run')
if ($LASTEXITCODE -ne 0) { throw 'Synthetic preparation-to-public-read verification failed.' }
& dotnet test (Join-Path $community 'TimePilot.Analytics.ConsumerTests/TimePilot.Analytics.ConsumerTests.csproj') `
    -c Release --logger trx --results-directory (Join-Path $output 'TestResults')
if ($LASTEXITCODE -ne 0) { throw 'Independent consumer tests failed.' }
Write-Output "Handoff verified at fixed Community commit $resolved. Outputs retained at $output"
