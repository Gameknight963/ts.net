param([switch]$SkipPublish, [string]$RuntimeVersion = '10.0.2')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$experiment = Join-Path $root 'experiments/metadata-size'
$output = Join-Path $root 'artifacts/metadata-size'
$packages = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $env:USERPROFILE '.nuget/packages' }
$native = Join-Path $packages "microsoft.netcore.app.runtime.nativeaot.win-x64/$RuntimeVersion/runtimes/win-x64/native"
function TomlPath([string]$path) { return $path.Replace('\', '/') }
Push-Location $root
try {
    [IO.Directory]::CreateDirectory($output) | Out-Null
    '<configuration><packageSources><clear /></packageSources></configuration>' |
        Set-Content "$output/NuGet.Config"
    $results = @()
    foreach ($variant in @('baseline', 'default', 'small')) {
        $directory = Join-Path $output $variant
        [IO.Directory]::CreateDirectory($directory) | Out-Null
        if ($variant -ne 'baseline' -and !$SkipPublish) {
            $publishArgs = @('publish', "$experiment/MetadataWriter.csproj", '-r', 'win-x64', '-c', 'Release',
                '-o', $directory, "-p:IlcVersion=$RuntimeVersion", "-p:RuntimeFrameworkVersion=$RuntimeVersion",
                "-p:Small=$($variant -eq 'small')", '--configfile', "$output/NuGet.Config", '--nologo', '-v', 'minimal')
            & dotnet @publishArgs
            if ($LASTEXITCODE) { throw "Publishing $variant failed" }
        }
        $entry = if ($variant -eq 'baseline') { 'baseline.cr' } else { 'probe.cr' }
        $manifest = @"
[project]
name = "metadata-size-$variant"
[[targets]]
name = "probe"
kind = "executable"
entry = "$(TomlPath "$experiment/$entry")"
[targets.native]
debug = false
"@
        if ($variant -ne 'baseline') {
            $runtimeInputs = @('bootstrapperdll.obj', 'Runtime.WorkstationGC.lib', 'eventpipe-disabled.lib',
                'Runtime.VxsortDisabled.lib', 'standalonegc-disabled.lib', 'aotminipal.lib', 'zlibstatic.lib',
                'brotlicommon.lib', 'brotlienc.lib', 'brotlidec.lib', 'System.Globalization.Native.Aot.lib',
                'System.IO.Compression.Native.Aot.lib')
            # Default enables EventSource; its runtime implementation must agree.
            if ($variant -eq 'default') { $runtimeInputs[2] = 'eventpipe-enabled.lib' }
            $inputs = @("$directory/MetadataWriter.lib") + @($runtimeInputs | ForEach-Object { "$native/$_" })
            foreach ($inputPath in $inputs) { if (!(Test-Path $inputPath)) { throw "Missing input: $inputPath" } }
            $encodedInputs = ($inputs | ForEach-Object { '"' + (TomlPath $_) + '"' }) -join ', '
            $manifest += "`nsources = [`"$(TomlPath "$experiment/link-options.c")`"]`nlink-inputs = [$encodedInputs]`n"
            $manifest += 'libraries = ["advapi32", "bcrypt", "crypt32", "iphlpapi", "kernel32", "mswsock", "ncrypt", "normaliz", "ntdll", "ole32", "oleaut32", "secur32", "user32", "version", "ws2_32"]'
        }
        $manifest | Set-Content "$directory/crust.toml"
        & crust build --project $directory --release
        if ($LASTEXITCODE) { throw "Linking $variant failed" }
        $executable = "$directory/target/crust/probe/probe.exe"
        Push-Location $directory
        try {
            & $executable
            if ($LASTEXITCODE) { throw "Running $variant failed" }
            if ($variant -ne 'baseline') {
                '{"runtimeOptions":{"tfm":"net10.0","framework":{"name":"Microsoft.NETCore.App","version":"10.0.0"}}}' |
                    Set-Content Generated.runtimeconfig.json
                $actual = & dotnet Generated.dll
                if ($LASTEXITCODE -or "$actual" -ne '42') { throw "Invalid generated assembly: $actual" }
            }
        }
        finally { Pop-Location }
        $results += [pscustomobject]@{
            Variant = $variant
            ArchiveBytes = if ($variant -eq 'baseline') { 0 } else { (Get-Item "$directory/MetadataWriter.lib").Length }
            ExecutableBytes = (Get-Item $executable).Length
            GeneratedAssemblyBytes = if ($variant -eq 'baseline') { 0 } else { (Get-Item "$directory/Generated.dll").Length }
        }
    }
    $results | ConvertTo-Json | Set-Content "$output/sizes.json"
    $results | Format-Table -AutoSize
}
finally { Pop-Location }
