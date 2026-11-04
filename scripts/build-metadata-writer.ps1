param([string]$RuntimeVersion = '10.0.2')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$output = Join-Path $root 'artifacts/metadata-writer'
$packages = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $env:USERPROFILE '.nuget/packages' }
[IO.Directory]::CreateDirectory($output) | Out-Null
$compilerPackage = Join-Path $packages "runtime.win-x64.microsoft.dotnet.ilcompiler/$RuntimeVersion"
if (Test-Path $compilerPackage) {
    '<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content "$output/NuGet.Config"
} else {
    '<configuration><packageSources><clear /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources></configuration>' |
        Set-Content "$output/NuGet.Config"
}
& dotnet publish "$root/backend/metadata/Tsnet.Metadata.csproj" -r win-x64 -c Release -o "$output/publish" `
    "-p:IlcVersion=$RuntimeVersion" "-p:RuntimeFrameworkVersion=$RuntimeVersion" --configfile "$output/NuGet.Config" --nologo -v minimal
if ($LASTEXITCODE) { throw 'Metadata writer publish failed' }
$native = Join-Path $packages "microsoft.netcore.app.runtime.nativeaot.win-x64/$RuntimeVersion/runtimes/win-x64/native"
$inputs = @("$output/publish/Tsnet.Metadata.lib") + @(
    'bootstrapperdll.obj', 'Runtime.WorkstationGC.lib', 'eventpipe-disabled.lib', 'Runtime.VxsortDisabled.lib',
    'standalonegc-disabled.lib', 'aotminipal.lib', 'zlibstatic.lib', 'brotlicommon.lib', 'brotlienc.lib',
    'brotlidec.lib', 'System.Globalization.Native.Aot.lib', 'System.IO.Compression.Native.Aot.lib'
) | ForEach-Object { if ([IO.Path]::IsPathRooted($_)) { $_ } else { Join-Path $native $_ } }
# Explicit bootstrapper input preserves its CRT initialization when linking an archive.
Copy-Item "$native/bootstrapperdll.obj" "$output/bootstrapperdll.obj"
$inputs = @($inputs | Where-Object { $_ -notlike '*bootstrapperdll.obj' })
& llvm-lib "/OUT:$output/Tsnet.Metadata.lib" @inputs
if ($LASTEXITCODE) { throw 'Metadata writer archive preparation failed' }
