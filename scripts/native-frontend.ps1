param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (!$SkipBuild) {
    & dotnet build "$root/compiler/Tsnet.Compiler.csproj" -c Release --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Compiler build failed' }
}
$extension = if ([Environment]::OSVersion.Platform -eq 'Win32NT') { '.exe' } else { '' }
$frontend = "$root/compiler/bin/Release/net10.0/tsnet-frontend$extension"
$output = "$root/artifacts/native-frontend"
[IO.Directory]::CreateDirectory($output) | Out-Null
$reports = @(& $frontend --inspect "$root/experiments/native-frontend/sample.ts" point count values selected optional label)
if ($LASTEXITCODE) { throw 'Frontend probe failed' }
$reports | Set-Content "$output/probe.jsonl" -Encoding utf8
$reports | Select-Object -Skip 1 | ForEach-Object {
    $entry = $_ | ConvertFrom-Json
    [pscustomobject]@{ Binding = $entry.name; Type = $entry.type; Initializer = $entry.initializerKind }
} | Format-Table -AutoSize | Out-Host
Write-Host "Full results: $output/probe.jsonl"
