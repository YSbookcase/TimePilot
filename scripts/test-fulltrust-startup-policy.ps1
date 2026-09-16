#Requires -RunAsAdministrator
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Enable', 'Restore')]
    [string]$Mode,
    [string]$ReportPath = (Join-Path $PSScriptRoot '..\artifacts\fulltrust-startup-policy-test.json')
)

$ErrorActionPreference = 'Stop'
$failurePath = Join-Path $PSScriptRoot '..\artifacts\fulltrust-startup-policy-test-error.txt'
trap {
    $_ | Out-String | Set-Content -LiteralPath $failurePath -Encoding UTF8
    throw
}

$policyPath = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System'
$testValues = [ordered]@{
    EnableFullTrustStartupTasks = 2
    EnableUwpStartupTasks = 2
    SupportFullTrustStartupTasks = 1
    SupportUwpStartupTasks = 1
}

if ($Mode -eq 'Enable') {
    $properties = Get-ItemProperty -LiteralPath $policyPath
    if (Test-Path -LiteralPath $ReportPath) {
        $report = Get-Content -LiteralPath $ReportPath -Raw | ConvertFrom-Json
        foreach ($name in $testValues.Keys) {
            if ($null -ne $report.Previous.PSObject.Properties[$name]) {
                continue
            }

            $property = $properties.PSObject.Properties[$name]
            $report.Previous | Add-Member -NotePropertyName $name -NotePropertyValue ([pscustomobject]@{
                Exists = $null -ne $property
                Value = if ($null -ne $property) { [int]$property.Value } else { $null }
            })
        }

        $report | Add-Member -NotePropertyName LastAppliedAt `
            -NotePropertyValue ([DateTimeOffset]::Now.ToString('o')) -Force
        $report | Add-Member -NotePropertyName TestValues `
            -NotePropertyValue ([pscustomobject]$testValues) -Force
    } else {
        $previous = [ordered]@{}
        foreach ($name in $testValues.Keys) {
            $property = $properties.PSObject.Properties[$name]
            $previous[$name] = [ordered]@{
                Exists = $null -ne $property
                Value = if ($null -ne $property) { [int]$property.Value } else { $null }
            }
        }

        $report = [ordered]@{
            Timestamp = [DateTimeOffset]::Now.ToString('o')
            PolicyPath = $policyPath
            Previous = $previous
            TestValues = $testValues
        }
    }
    $report | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $ReportPath -Encoding UTF8

    foreach ($name in $testValues.Keys) {
        New-ItemProperty -LiteralPath $policyPath -Name $name -PropertyType DWord `
            -Value $testValues[$name] -Force | Out-Null
    }
} else {
    if (-not (Test-Path -LiteralPath $ReportPath)) {
        throw "No policy test report was found: $ReportPath"
    }

    $report = Get-Content -LiteralPath $ReportPath -Raw | ConvertFrom-Json
    foreach ($name in $testValues.Keys) {
        $prior = $report.Previous.$name
        if ($null -ne $prior -and $prior.Exists) {
            New-ItemProperty -LiteralPath $policyPath -Name $name -PropertyType DWord `
                -Value ([int]$prior.Value) -Force | Out-Null
        } else {
            Remove-ItemProperty -LiteralPath $policyPath -Name $name -ErrorAction SilentlyContinue
        }
    }
}

Get-ItemProperty -LiteralPath $policyPath -Name @($testValues.Keys) -ErrorAction SilentlyContinue |
    Select-Object @($testValues.Keys)
