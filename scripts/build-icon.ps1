[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$work = Join-Path $root 'obj\icon'
New-Item -ItemType Directory -Path $work -Force | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$generator = Join-Path $work 'MakeIcon.exe'
& $compiler /nologo /reference:System.Drawing.dll "/out:$generator" (Join-Path $PSScriptRoot 'MakeIcon.cs')
if ($LASTEXITCODE -ne 0) { throw 'Icon generator compilation failed.' }
& $generator $work
if ($LASTEXITCODE -ne 0) { throw 'Icon generation failed.' }
Copy-Item -LiteralPath (Join-Path $work 'NightScreenGuard.ico') -Destination (Join-Path $root 'assets\NightScreenGuard.ico') -Force
Write-Host 'Regenerated assets\NightScreenGuard.ico (16 to 256 pixels).'
