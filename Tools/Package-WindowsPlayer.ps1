param(
    [Parameter(Mandatory=$true)][string]$PlayerDirectory,
    [Parameter(Mandatory=$true)][string]$PackageName,
    [Parameter(Mandatory=$true)][string]$ReadmePath,
    [Parameter(Mandatory=$true)][int]$Protocol
)
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$playerRoot = (Resolve-Path -LiteralPath $PlayerDirectory).Path.TrimEnd('\')
if (-not $playerRoot.StartsWith($workspace + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Only package a build inside this workspace.' }
if ($PackageName -notmatch '^[A-Za-z0-9_-]+$') { throw 'Package name must contain only letters, digits, hyphens and underscores.' }
if (-not (Test-Path -LiteralPath (Join-Path $playerRoot 'LAST_PROTOCOL.exe'))) { throw 'Windows player executable missing.' }
$readmeText = [IO.File]::ReadAllText((Resolve-Path -LiteralPath $ReadmePath).Path)
$outputFolder = Join-Path $workspace 'Builds\Distributions'
New-Item -ItemType Directory -Path $outputFolder -Force | Out-Null
$zipPath = Join-Path $outputFolder ($PackageName + '.zip')
if (Test-Path -LiteralPath $zipPath) { throw ('Existing distribution preserved: ' + $zipPath) }
Add-Type -AssemblyName System.IO.Compression
$buildFiles = @(Get-ChildItem -LiteralPath $playerRoot -Recurse -File | Where-Object { $_.FullName -notmatch '_BurstDebugInformation_DoNotShip[\\/]' })
$fileEntries = @($buildFiles | ForEach-Object {
    [PSCustomObject]@{ Path=$_.FullName.Substring($playerRoot.Length + 1).Replace('\','/'); Length=$_.Length; SHA256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
})
$manifest = [PSCustomObject]@{ Protocol=$Protocol; CreatedUtc=[DateTime]::UtcNow.ToString('o'); InternetRetest='CHUA_KIEM_THU'; Files=$fileEntries }
$zipStream = [IO.File]::Open($zipPath, [IO.FileMode]::CreateNew)
$zip = [IO.Compression.ZipArchive]::new($zipStream, [IO.Compression.ZipArchiveMode]::Create, $false)
try {
    foreach ($fileEntry in $fileEntries) {
        $entry = $zip.CreateEntry(($PackageName + '/' + $fileEntry.Path), [IO.Compression.CompressionLevel]::Optimal)
        $destinationStream = $entry.Open()
        $inputStream = [IO.File]::OpenRead((Join-Path $playerRoot $fileEntry.Path))
        try { $inputStream.CopyTo($destinationStream) } finally { $inputStream.Dispose(); $destinationStream.Dispose() }
    }
    foreach ($textEntry in @(@{Name='HUONG_DAN.txt';Text=$readmeText}, @{Name='FILE_SHA256.json';Text=($manifest | ConvertTo-Json -Depth 5)})) {
        $entry = $zip.CreateEntry(($PackageName + '/' + $textEntry.Name))
        $writer = [IO.StreamWriter]::new($entry.Open(), [Text.UTF8Encoding]::new($false))
        try { $writer.Write($textEntry.Text) } finally { $writer.Dispose() }
    }
} finally { $zip.Dispose(); $zipStream.Dispose() }
# Read every compressed file back and compare to the tested on-disk build.
$archive = [IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    foreach ($fileEntry in $fileEntries) {
        $entry = $archive.GetEntry($PackageName + '/' + $fileEntry.Path)
        if ($null -eq $entry -or $entry.Length -ne $fileEntry.Length) { throw ('Package length mismatch: ' + $fileEntry.Path) }
        $stream = $entry.Open(); $sha = [Security.Cryptography.SHA256]::Create()
        try { $hash = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','') } finally { $sha.Dispose(); $stream.Dispose() }
        if ($hash -ne $fileEntry.SHA256) { throw ('Package hash mismatch: ' + $fileEntry.Path) }
    }
} finally { $archive.Dispose() }
$zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
[IO.File]::WriteAllText(($zipPath + '.sha256'), ($zipHash + '  ' + [IO.Path]::GetFileName($zipPath) + "`n"))
[PSCustomObject]@{ Path=$zipPath; BuildFiles=$fileEntries.Count; Bytes=(Get-Item -LiteralPath $zipPath).Length; SHA256=$zipHash; Verified=$true } | Format-List
