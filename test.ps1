[CmdletBinding()]
param([switch]$UI, [string]$OutputDirectory = (Join-Path $PSScriptRoot 'dist'))
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build.ps1') -OutputDirectory $OutputDirectory
$exe = Join-Path ([IO.Path]::GetFullPath($OutputDirectory)) 'NightScreenGuard.exe'
$results = Join-Path $PSScriptRoot 'test-results'
New-Item -ItemType Directory -Path $results -Force | Out-Null
function Invoke-GuardCheck([string]$Mode, [string]$Destination) {
    $arguments = $Mode + ' "' + $Destination + '"'
    $process = Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit(20000)) {
        Stop-Process -Id $process.Id -ErrorAction SilentlyContinue
        throw "Test timed out: $Mode"
    }
    if ($process.ExitCode -ne 0) { throw "Test failed: $Mode (exit $($process.ExitCode))" }
}
Invoke-GuardCheck '--self-test' (Join-Path $results 'logic.txt')
Get-Content -LiteralPath (Join-Path $results 'logic.txt')
if ($UI) {
    Invoke-GuardCheck '--ui-test' $results
    Get-Content -LiteralPath (Join-Path $results 'ui-test.txt')
    Write-Host "Chinese and English previews: $results"
}
