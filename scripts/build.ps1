param(
    [string]$InputFile = 'examples/structural.ts',
    [string]$OutputDirectory = 'artifacts/app',
    [switch]$Run,
    [switch]$ServerGC,
    [ValidateRange(0, 1024)][int]$GCHeapCount = 0,
    [switch]$ExperimentalDecorators,
    [switch]$SkipCompilerBuild,
    [switch]$SkipRuntimeBuild,
    [switch]$LinkRuntime
)
$ErrorActionPreference = 'Stop'
if ($GCHeapCount -and !$ServerGC) { throw 'GCHeapCount requires ServerGC' }
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    if (!$SkipCompilerBuild) {
        & dotnet build "$root/compiler/Tsnet.Compiler.csproj" -c Release --nologo -v quiet
        if ($LASTEXITCODE) { throw 'Compiler build failed' }
    }
    $inputPath = (Resolve-Path -LiteralPath $InputFile).Path
    $outputPath = [IO.Path]::GetFullPath((Join-Path $root $OutputDirectory))
    $applicationPath = Join-Path $outputPath 'bin/Release/net10.0'
    [IO.Directory]::CreateDirectory($applicationPath) | Out-Null
    # Old generated C# projects left host/dependency files with incompatible
    # assembly lists. Directly emitted assemblies use directory probing.
    foreach ($staleName in @('App.deps.json', 'App.exe', 'App.pdb')) {
        $stalePath = Join-Path $applicationPath $staleName
        if (Test-Path -LiteralPath $stalePath) { Remove-Item -LiteralPath $stalePath }
    }
    $compilerOptions = @()
    if ($ExperimentalDecorators) { $compilerOptions += '--experimentalDecorators' }
    & dotnet "$root/compiler/bin/Release/net10.0/tsnetc.dll" @compilerOptions $inputPath "$applicationPath/App.dll"
    if ($LASTEXITCODE) { throw 'TypeScript compilation failed' }
    # Build the shared runtime incrementally; generated applications go directly
    # from an IL plan to an assembly without compiling generated C# source.
    if (!$SkipRuntimeBuild) {
        '<configuration><packageSources><clear /></packageSources></configuration>' |
            Set-Content -LiteralPath "$outputPath/NuGet.Config" -Encoding utf8
        & dotnet build "$root/runtime/Tsnet.Runtime.csproj" -c Release --configfile "$outputPath/NuGet.Config" --nologo -v quiet
        if ($LASTEXITCODE) { throw '.NET runtime build failed' }
    }
    $runtimeSource = [IO.Path]::GetFullPath("$root/runtime/bin/Release/net10.0/Tsnet.Runtime.dll")
    $runtimeTarget = Join-Path $applicationPath 'Tsnet.Runtime.dll'
    if (![StringComparer]::OrdinalIgnoreCase.Equals($runtimeSource, $runtimeTarget)) {
        if (Test-Path -LiteralPath $runtimeTarget) { Remove-Item -LiteralPath $runtimeTarget }
        if ($LinkRuntime) {
            try { New-Item -ItemType HardLink -Path $runtimeTarget -Target $runtimeSource -ErrorAction Stop | Out-Null }
            catch { Copy-Item -LiteralPath $runtimeSource -Destination $runtimeTarget }
        } else { Copy-Item -LiteralPath $runtimeSource -Destination $runtimeTarget }
    }
    $runtimeOptions = @{
        tfm = 'net10.0'
        framework = @{name = 'Microsoft.NETCore.App'; version = '10.0.0'}
    }
    if ($ServerGC) { $runtimeOptions.configProperties = @{'System.GC.Server' = $true} }
    if ($GCHeapCount) { $runtimeOptions.configProperties['System.GC.HeapCount'] = $GCHeapCount }
    @{runtimeOptions = $runtimeOptions} | ConvertTo-Json -Depth 4 |
        Set-Content "$applicationPath/App.runtimeconfig.json" -Encoding utf8
    if ($Run) {
        & dotnet "$outputPath/bin/Release/net10.0/App.dll"
        if ($LASTEXITCODE) { throw 'Generated application failed' }
    }
}
finally { Pop-Location }
