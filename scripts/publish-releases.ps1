[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true' -or $env:GITHUB_REF -ne 'refs/heads/main') {
    throw 'Run this publisher through the main-branch GitHub Actions workflow.'
}
$root = Split-Path -Parent $PSScriptRoot
$notes = @(Get-ChildItem -LiteralPath (Join-Path $root 'releases') -Filter 'v*.md' -File)
foreach ($note in $notes) {
    $tag = $note.BaseName
    if ($tag -notmatch '^v\d+\.\d+\.\d+$') { throw "Invalid release tag: $tag" }
    $existing = gh api "repos/$env:GH_REPO/releases" --paginate --jq '.[].tag_name'
    if ($LASTEXITCODE -ne 0) { throw 'Cannot read existing releases.' }
    if (@($existing) -contains $tag) { Write-Host "Already published: $tag"; continue }
    git -C $root rev-parse --verify "refs/tags/$tag^{commit}" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Push the version tag before publishing its notes: $tag" }
    $version = $tag.Substring(1)
    $work = Join-Path $env:RUNNER_TEMP ('night-screen-guard-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $work | Out-Null
    $sourceZip = Join-Path $work "NightScreenGuard-$version-source.zip"
    $prefix = "NightScreenGuard-$version-source/"
    git -C $root archive --format=zip "--prefix=$prefix" "--output=$sourceZip" $tag
    if ($LASTEXITCODE -ne 0) { throw 'Source archive failed.' }
    Expand-Archive -LiteralPath $sourceZip -DestinationPath $work
    $source = Join-Path $work "NightScreenGuard-$version-source"
    $portable = Join-Path $work "NightScreenGuard-$version-win-x64"
    & (Join-Path $source 'test.ps1') -OutputDirectory $portable
    $fileVersion = (Get-Item -LiteralPath (Join-Path $portable 'NightScreenGuard.exe')).VersionInfo.FileVersion
    if ($fileVersion -ne "$version.0") { throw "Binary version $fileVersion does not match tag $tag" }
    # Test output belongs outside the portable release.
    Get-ChildItem -LiteralPath $portable -Filter 'guard.log*' -File | Remove-Item -Force
    $portableZip = Join-Path $work "NightScreenGuard-$version-win-x64.zip"
    Compress-Archive -LiteralPath $portable -DestinationPath $portableZip
    $sumFile = Join-Path $work 'SHA256SUMS.txt'
    $lines = foreach ($asset in @($portableZip,$sourceZip)) {
        ((Get-FileHash -LiteralPath $asset -Algorithm SHA256).Hash.ToLowerInvariant()) + '  ' + [IO.Path]::GetFileName($asset)
    }
    [IO.File]::WriteAllLines($sumFile,$lines,[Text.UTF8Encoding]::new($false))
    gh release create $tag $portableZip $sourceZip $sumFile --verify-tag --title "Night Screen Guard $version" --notes-file $note.FullName
    if ($LASTEXITCODE -ne 0) { throw "Publishing failed: $tag" }
    Write-Host "Published $tag from its tagged source."
}
