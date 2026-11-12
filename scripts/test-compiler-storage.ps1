$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$work = Join-Path $root 'artifacts/compiler-storage-tests'
[IO.Directory]::CreateDirectory($work) | Out-Null
$utf8 = New-Object System.Text.UTF8Encoding($false)
function Invoke-Captured([string]$Executable, [string]$Arguments) {
    $start = New-Object System.Diagnostics.ProcessStartInfo
    $start.FileName = $Executable
    $start.Arguments = $Arguments
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = [Diagnostics.Process]::Start($start)
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit()
    $result = [pscustomobject]@{ Code = $process.ExitCode; Text = $stdout.Result + $stderr.Result }
    $process.Dispose()
    return $result
}
$compiler = Join-Path $root 'compiler/bin/Release/net10.0/tsnetc.dll'
$inputFile = Join-Path $work 'boundary.ts'
$outputFile = Join-Path $work 'boundary.dll'
foreach ($case in @(
    @{ Source = '//' + ('x' * (4 * 1024 * 1024 - 2)); Succeeds = $true; Message = '' },
    @{ Source = '//' + ('x' * (4 * 1024 * 1024 - 1)); Succeeds = $false; Message = '4 MiB' },
    @{ Source = 'const x = 1;' + [char]0; Succeeds = $false; Message = 'NUL' }
)) {
    [IO.File]::WriteAllText($inputFile, $case.Source, $utf8)
    [IO.File]::WriteAllText($outputFile, '// sentinel', $utf8)
    $run = Invoke-Captured 'dotnet' ('"' + $compiler + '" "' + $inputFile + '" "' + $outputFile + '"')
    if ($case.Succeeds) {
        if ($run.Code) { throw "Exact-limit input failed: $($run.Text)" }
    }
    elseif ($run.Code -ne 1 -or !$run.Text.Contains($case.Message) -or [IO.File]::ReadAllText($outputFile) -ne '// sentinel') {
        throw "Expected bounded input failure with output preservation: $($run.Text)"
    }
}
Write-Host 'PASS exact input limit, oversized input, NUL rejection, and output preservation'

$namedWork = Join-Path $work 'named-outputs'
[IO.Directory]::CreateDirectory($namedWork) | Out-Null
$firstInput = Join-Path $namedWork 'first.ts'
$secondInput = Join-Path $namedWork 'second.ts'
[IO.File]::WriteAllText($firstInput, 'console.log("first");', $utf8)
[IO.File]::WriteAllText($secondInput, 'console.log("second");', $utf8)
Copy-Item -LiteralPath (Join-Path $root 'runtime/bin/Release/net10.0/Tsnet.Runtime.dll') -Destination $namedWork -Force
foreach ($name in @('App', 'Other', 'named.output', ([string][char]0x96ea + ' run'))) {
    $namedInput = if ($name -eq 'App') { $firstInput } else { $secondInput }
    $namedOutput = Join-Path $namedWork ($name + '.dll')
    $run = Invoke-Captured 'dotnet' ('"' + $compiler + '" "' + $namedInput + '" "' + $namedOutput + '"')
    if ($run.Code) { throw "Named output failed to compile: $($run.Text)" }
    if ([Reflection.AssemblyName]::GetAssemblyName($namedOutput).Name -cne $name) { throw "Assembly name does not match $name" }
    [IO.File]::WriteAllText((Join-Path $namedWork ($name + '.runtimeconfig.json')), '{"runtimeOptions":{"tfm":"net10.0","framework":{"name":"Microsoft.NETCore.App","version":"10.0.0"}}}', $utf8)
    $run = Invoke-Captured 'dotnet' ('"' + $namedOutput + '"')
    $expected = if ($name -eq 'App') { 'first' } else { 'second' }
    if ($run.Code -or $run.Text.Trim() -cne $expected) { throw "Named output ran the wrong program: $($run.Text)" }
}
$emptyName = Join-Path $namedWork '.dll'
[IO.File]::WriteAllText($emptyName, 'sentinel', $utf8)
$run = Invoke-Captured 'dotnet' ('"' + $compiler + '" "' + $secondInput + '" "' + $emptyName + '"')
if ($run.Code -ne 1 -or !$run.Text.Contains('assembly name') -or [IO.File]::ReadAllText($emptyName) -ne 'sentinel') {
    throw "Expected an empty assembly name to fail without replacing output: $($run.Text)"
}
Write-Host 'PASS independent named outputs, dotted names, Unicode filenames, and empty-name output preservation'
