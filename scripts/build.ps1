param(
    [string]$InputFile = 'examples/structural.ts',
    [string]$OutputDirectory = 'artifacts/app',
    [switch]$Run,
    [switch]$SkipCompilerBuild
)
$ErrorActionPreference = 'Stop'
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
    & dotnet "$root/compiler/bin/Release/net10.0/tsnetc.dll" $inputPath "$applicationPath/App.dll"
    if ($LASTEXITCODE) { throw 'TypeScript compilation failed' }
    # Build the shared runtime incrementally; generated applications go directly
    # from an IL plan to an assembly without compiling generated C# source.
    '<configuration><packageSources><clear /></packageSources></configuration>' |
        Set-Content -LiteralPath "$outputPath/NuGet.Config" -Encoding utf8
    & dotnet build "$root/runtime/Tsnet.Runtime.csproj" -c Release --configfile "$outputPath/NuGet.Config" --nologo -v quiet
    if ($LASTEXITCODE) { throw '.NET runtime build failed' }
    Copy-Item "$root/runtime/bin/Release/net10.0/Tsnet.Runtime.dll" $applicationPath
    '{"runtimeOptions":{"tfm":"net10.0","framework":{"name":"Microsoft.NETCore.App","version":"10.0.0"}}}' |
        Set-Content "$applicationPath/App.runtimeconfig.json" -Encoding utf8
    if ($Run) {
        & dotnet "$outputPath/bin/Release/net10.0/App.dll"
        if ($LASTEXITCODE) { throw 'Generated application failed' }
    }
}
finally { Pop-Location }
