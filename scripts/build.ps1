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
        & crust build --release
        if ($LASTEXITCODE) { throw 'Crust compiler build failed' }
    }
    $inputPath = (Resolve-Path -LiteralPath $InputFile).Path
    $outputPath = [IO.Path]::GetFullPath((Join-Path $root $OutputDirectory))
    [IO.Directory]::CreateDirectory($outputPath) | Out-Null
    & "$root/target/crust/tsnetc/tsnetc.exe" $inputPath "$outputPath/Program.cs"
    if ($LASTEXITCODE) { throw 'TypeScript compilation failed' }
    Copy-Item -LiteralPath "$root/runtime/Runtime.cs" -Destination "$outputPath/Runtime.cs"
    # This project uses only the installed SDK and framework reference packs.
    '<configuration><packageSources><clear /></packageSources></configuration>' |
        Set-Content -LiteralPath "$outputPath/NuGet.Config" -Encoding utf8
    @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <DebugType>portable</DebugType>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="Program.cs" />
    <Compile Include="Runtime.cs" />
  </ItemGroup>
</Project>
'@ | Set-Content -LiteralPath "$outputPath/App.csproj" -Encoding utf8
    & dotnet build "$outputPath/App.csproj" -c Release --nologo -v quiet
    if ($LASTEXITCODE) { throw '.NET build failed' }
    if ($Run) {
        & dotnet "$outputPath/bin/Release/net10.0/App.dll"
        if ($LASTEXITCODE) { throw 'Generated application failed' }
    }
}
finally { Pop-Location }
