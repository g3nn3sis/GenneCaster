param([string]$PrivateTermsFile)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'GenneCaster.csproj'
$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Path $dist -Force | Out-Null
$pluginInfo = [IO.File]::ReadAllText((Join-Path $root 'Base/PluginInfo.cs'))
$version = [regex]::Match($pluginInfo, 'Version\s*=\s*"([^"]+)"').Groups[1].Value
if (-not $version) { throw 'Could not read the plugin version.' }
$archive = Join-Path $dist "GenneCaster-$version.zip"
if (Test-Path -LiteralPath $archive) { Remove-Item -LiteralPath $archive -Force }
$buildLog = Join-Path $dist 'build.log'
& dotnet build $project -c Release --nologo -p:CI=TRUE -p:WarningLevel=4 -v:q *> $buildLog
if ($LASTEXITCODE -ne 0) {
    Get-Content -LiteralPath $buildLog -Tail 30
    throw 'Release build failed.'
}

$dll = Join-Path $root 'bin/Release/net48/GenneCaster.dll'
if (-not (Test-Path -LiteralPath $dll)) { throw 'Release DLL was not produced.' }

$patterns = @(
    @{ Name = 'user profile path'; Value = '(?i)(?:[A-Z]:[\\/](?:Users|Documents and Settings)[\\/]|/(?:Users|home)/)[^\\/\s"''<>]+' },
    @{ Name = 'email address'; Value = '(?i)\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b' },
    @{ Name = 'private key'; Value = '-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----' },
    @{ Name = 'service token'; Value = '(?i)\b(?:gh[pousr]_[A-Za-z0-9_]{20,}|github_pat_[A-Za-z0-9_]{20,}|AKIA[0-9A-Z]{16})\b' },
    @{ Name = 'credential assignment'; Value = '(?i)\b(?:api[_-]?key|client[_-]?secret|password|passwd|auth[_-]?token)\b\s*[:=]\s*["''][^"'']{8,}' }
)
if ($PrivateTermsFile) {
    if (-not (Test-Path -LiteralPath $PrivateTermsFile)) { throw 'Private terms file was not found.' }
    foreach ($term in [IO.File]::ReadAllLines((Resolve-Path -LiteralPath $PrivateTermsFile))) {
        if (-not [string]::IsNullOrWhiteSpace($term)) {
            $patterns += @{ Name = 'private term'; Value = [regex]::Escape($term.Trim()) }
        }
    }
}

$textExtensions = @('.cs', '.csproj', '.props', '.targets', '.md', '.ps1', '.slnx', '.xml', '.json', '.yml', '.yaml', '.txt', '.config')
$sourceFiles = @(Get-ChildItem -LiteralPath $root -Recurse -File | Where-Object {
    $relativePath = $_.FullName.Substring($root.Length + 1)
    $relativePath -notmatch '^(?:bin|obj|dist|Libs|\.vs|\.git)[\\/]' -and
    ($_.Extension -in $textExtensions -or $_.Name -eq 'LICENSE')
})
$sourceFiles += Get-Item -LiteralPath (Join-Path $root '.gitignore')
$findings = [Collections.Generic.List[string]]::new()
foreach ($file in $sourceFiles) {
    $lineNumber = 0
    foreach ($line in [IO.File]::ReadLines($file.FullName)) {
        $lineNumber++
        foreach ($pattern in $patterns) {
            if ([regex]::IsMatch($line, $pattern.Value)) {
                $relativePath = $file.FullName.Substring($root.Length + 1)
                $findings.Add("$($pattern.Name): ${relativePath}:$lineNumber")
            }
        }
    }
}

$bytes = [IO.File]::ReadAllBytes($dll)
$views = @(
    [Text.Encoding]::GetEncoding(28591).GetString($bytes),
    [Text.Encoding]::Unicode.GetString($bytes),
    [Text.Encoding]::Unicode.GetString($bytes, 1, $bytes.Length - 1)
)
foreach ($pattern in $patterns) {
    if ($views.Where({ [regex]::IsMatch($_, $pattern.Value) }).Count -gt 0) {
        $findings.Add("$($pattern.Name): release DLL")
    }
}
if ($findings.Count -gt 0) {
    $findings | Sort-Object -Unique | ForEach-Object { Write-Warning $_ }
    throw 'Review these possible private values before publishing.'
}

$gitDir = Join-Path $root '.git'
if (Test-Path -LiteralPath $gitDir) {
    $tracked = & git -C $root ls-files
    if ($LASTEXITCODE -ne 0) { throw 'Could not inspect tracked files.' }
    $unwanted = @($tracked | Where-Object { $_ -match '(?i)(^|/)(?:\.vs|bin|obj|dist|Libs)/|\.(?:dll|exe|pdb|user|suo|pfx|p12|pem|key|snk)$|(^|/)\.env(?:\.|$)' })
    if ($unwanted.Count -gt 0) { throw "Generated or private files are tracked: $($unwanted -join ', ')" }
} else {
    Write-Warning 'No Git repository found. Inspect the initial commit and any prior history before publishing source.'
}

$hash = (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash
$hashFile = Join-Path $dist 'SHA256SUMS.txt'
[IO.File]::WriteAllText($hashFile, "$hash  GenneCaster.dll`n", [Text.UTF8Encoding]::new($false))
$discordLicense = Join-Path $dist 'DISCORD_RPC_LICENSE.txt'
Copy-Item -LiteralPath (Join-Path $root 'External/DiscordRPC/LICENSE') -Destination $discordLicense -Force
$releaseFiles = @(
    $dll,
    (Join-Path $root 'README.md'),
    (Join-Path $root 'LICENSE'),
    (Join-Path $root 'THIRD_PARTY_NOTICES.md'),
    $discordLicense,
    $hashFile
)
Compress-Archive -LiteralPath $releaseFiles -DestinationPath $archive -Force
if ($PSVersionTable.PSVersion.Major -lt 6) { Add-Type -AssemblyName System.IO.Compression.FileSystem }
$zip = [IO.Compression.ZipFile]::OpenRead($archive)
try {
    $entries = @($zip.Entries | ForEach-Object { $_.FullName } | Sort-Object)
    $expected = @($releaseFiles | ForEach-Object { [IO.Path]::GetFileName($_) } | Sort-Object)
    if (@(Compare-Object -ReferenceObject $expected -DifferenceObject $entries).Count -gt 0) {
        throw 'Release archive contents differ from the expected file list.'
    }
    $dllEntry = $zip.GetEntry('GenneCaster.dll')
    if ($null -eq $dllEntry) { throw 'Release archive is missing the DLL.' }
    $dllStream = $dllEntry.Open()
    try {
        $sha256 = [Security.Cryptography.SHA256]::Create()
        try {
            $archivedHash = [BitConverter]::ToString($sha256.ComputeHash($dllStream)).Replace('-', '')
            if ($archivedHash -ne $hash) { throw 'Archived DLL does not match the built DLL.' }
        } finally {
            $sha256.Dispose()
        }
    } finally {
        $dllStream.Dispose()
    }
} finally {
    $zip.Dispose()
}
Write-Output "Release archive: $archive"
Write-Output "DLL SHA-256: $hash"
Write-Output 'Privacy pattern scan: passed. Review branding and the exact archive before publishing.'
