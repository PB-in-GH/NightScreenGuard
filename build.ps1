[CmdletBinding()]
param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'dist'))
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    throw '.NET Framework 4.x x64 compiler not found. Use Windows 10/11 x64 with .NET Framework 4.8.'
}
$outDir = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
$icon = Join-Path $PSScriptRoot 'assets\NightScreenGuard.ico'
$source = Join-Path $PSScriptRoot 'src\NightScreenGuard.cs'
$manifest = Join-Path $PSScriptRoot 'src\app.manifest'
$metadata = Join-Path $PSScriptRoot 'src\AssemblyInfo.cs'
$exe = Join-Path $outDir 'NightScreenGuard.exe'
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /utf8output /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "/win32icon:$icon" "/win32manifest:$manifest" "/resource:$icon,NightScreenGuard.ico" "/out:$exe" $source $metadata
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
foreach ($name in @('README.md', 'README.en.md', 'LICENSE', 'CONTRIBUTING.md', 'CHANGELOG.md')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $outDir -Force
}
$docsOut = Join-Path $outDir 'docs'
New-Item -ItemType Directory -Path $docsOut -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'docs') -File | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $docsOut -Force }
Write-Host "Built: $exe"
