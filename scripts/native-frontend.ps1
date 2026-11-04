param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$experiment = Join-Path $root 'experiments/native-frontend'
$checkout = Join-Path $root 'artifacts/typescript-native'
$output = Join-Path $root 'artifacts/native-frontend'
$revision = '1e4744d68260a7cb91b62b12edc3f6a2187faaf1'
$goExe = (Get-Command go -ErrorAction SilentlyContinue).Source
if (!$goExe -and (Test-Path 'C:/Program Files/Go/bin/go.exe')) { $goExe = 'C:/Program Files/Go/bin/go.exe' }
if (!$goExe) { throw 'Go is required' }

# Save the caller's environment. cgo uses a GNU ABI C toolchain on Windows,
# while Crust continues to use its existing Clang/MSVC toolchain.
$environmentNames = @('GOCACHE', 'GOMODCACHE', 'GOWORK', 'CGO_ENABLED', 'CC', 'PATH')
$savedEnvironment = @{}
foreach ($name in $environmentNames) { $savedEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
Push-Location $root
try {
    if (!$SkipBuild) {
        [IO.Directory]::CreateDirectory($output) | Out-Null
        if (!(Test-Path -LiteralPath "$checkout/.git")) {
            & git clone --depth 1 --branch v7.0.2 --filter=blob:none --sparse https://github.com/microsoft/TypeScript.git $checkout
            if ($LASTEXITCODE) { throw 'TypeScript source download failed' }
            & git -C $checkout sparse-checkout set tsc
            if ($LASTEXITCODE) { throw 'TypeScript source checkout failed' }
        }
        $actualRevision = (& git -C $checkout rev-parse HEAD).Trim()
        if ($LASTEXITCODE -or $actualRevision -ne $revision) { throw "Expected TypeScript source revision $revision, found $actualRevision" }
        if (!(Test-Path "$checkout/tsc/internal/compiler/program.go")) { throw 'The checkout is missing the tsc source tree' }
        $gcc = (Get-Command gcc -ErrorAction SilentlyContinue).Source
        if (!$gcc -and (Test-Path 'C:/msys64/ucrt64/bin/gcc.exe')) { $gcc = 'C:/msys64/ucrt64/bin/gcc.exe' }
        if (!$gcc) { throw 'A MinGW/UCRT GCC toolchain is required for cgo on Windows' }
        $env:PATH = (Split-Path -Parent $gcc) + ';' + $env:PATH
        $env:CC = $gcc
        $env:CGO_ENABLED = '1'
        $env:GOWORK = 'off'
        $env:GOCACHE = Join-Path $root 'artifacts/go-build-cache'
        $env:GOMODCACHE = Join-Path $root 'artifacts/go-mod-cache'
        Push-Location "$experiment/bridge"
        try {
            & $goExe build -mod=readonly -buildmode=c-shared -o "$output/tsbridge.dll" .
            if ($LASTEXITCODE) { throw 'Go frontend DLL build failed' }
        }
        finally { Pop-Location }
        @'
LIBRARY tsbridge.dll
EXPORTS
tsnet_open_file
tsnet_inspect_binding
tsnet_close_file
tsnet_free_buffer
'@ | Set-Content -LiteralPath "$output/tsbridge.def" -Encoding ascii
        & llvm-dlltool -m i386:x86-64 -d "$output/tsbridge.def" -l "$output/tsbridge.lib"
        if ($LASTEXITCODE) { throw 'Import library generation failed' }
        # Keep upstream's license and notices beside the experimental binary.
        Copy-Item -LiteralPath "$checkout/LICENSE.txt" -Destination "$output/TypeScript-LICENSE.txt"
        if (Test-Path "$checkout/NOTICE.txt") { Copy-Item "$checkout/NOTICE.txt" "$output/TypeScript-NOTICE.txt" }
        $env:CC = $savedEnvironment['CC']
        $env:PATH = $savedEnvironment['PATH']
        & crust build --project $experiment --release
        if ($LASTEXITCODE) { throw 'Crust frontend probe build failed' }
    }
    $probe = "$experiment/target/crust/frontend-probe/frontend-probe.exe"
    $reports = @(& $probe "$experiment/sample.ts" point count values selected optional label)
    if ($LASTEXITCODE) { throw 'Frontend probe failed' }
    $reports | Set-Content -LiteralPath "$output/probe.jsonl" -Encoding utf8
    $reports | Select-Object -Skip 1 | ForEach-Object {
        $entry = $_ | ConvertFrom-Json
        [pscustomobject]@{ Binding = $entry.name; Type = $entry.type; Initializer = $entry.initializerKind }
    } | Format-Table -AutoSize | Out-Host
    Write-Host "Full results: $output/probe.jsonl"
}
finally {
    Pop-Location
    foreach ($name in $environmentNames) { [Environment]::SetEnvironmentVariable($name, $savedEnvironment[$name], 'Process') }
}
