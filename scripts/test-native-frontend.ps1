param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$experiment = Join-Path $root 'experiments/native-frontend'
$probe = "$experiment/target/crust/frontend-probe/frontend-probe.exe"
$output = Join-Path $root 'artifacts/native-frontend'

& "$PSScriptRoot/native-frontend.ps1" -SkipBuild:$SkipBuild
$reports = @(Get-Content -LiteralPath "$output/probe.jsonl" | ForEach-Object { $_ | ConvertFrom-Json })
if ($reports[0].diagnostics.Count -ne 0 -or $reports[0].sourceFiles -lt 2) { throw 'Expected a checked program with the standard library' }
$types = @{}
foreach ($report in $reports | Select-Object -Skip 1) { $types[$report.name] = $report }
if ($types['point'].type -ne '{ x: number; y: number; }') { throw 'Generic/object inference failed' }
if ($types['point'].properties.Count -ne 2 -or $types['point'].properties[0].type -ne 'number') { throw 'Structural property export failed' }
if ($types['count'].type -ne 'number') { throw 'Arithmetic inference failed' }
if ($types['values'].type -ne 'number[]') { throw 'Array callback inference failed' }
if ($types['selected'].flagNames -notcontains 'Union') { throw 'Union inference failed' }
if ($types['optional'].properties.Count -ne 2 -or $types['optional'].properties[1].type -ne 'number | undefined') { throw 'Mapped type expansion failed' }
if ($types['label'].type -ne 'string') { throw 'Template string inference failed' }
Write-Host 'PASS inferred types and structural properties'

& $probe --self-test "$experiment/sample.ts"
if ($LASTEXITCODE) { throw 'C ABI lifetime checks failed' }

$utf8 = New-Object System.Text.UTF8Encoding($false)
$cases = @(
    @{ File = 'invalid-type.ts'; Source = 'const broken: number = "wrong";'; Code = 2322 },
    @{ File = 'invalid-syntax.ts'; Source = 'const broken = ;'; Code = 1109 },
    @{ File = 'invalid-dependency.ts'; Source = 'import { broken } from "./broken-dependency"; const value = broken;'; Code = 2322 }
)
[IO.File]::WriteAllText("$output/broken-dependency.ts", 'export const broken: number = "wrong";', $utf8)
foreach ($case in $cases) {
    $path = Join-Path $output $case.File
    [IO.File]::WriteAllText($path, $case.Source, $utf8)
    $text = @(& $probe $path broken)
    if ($LASTEXITCODE -eq 0 -or $text.Count -ne 1) { throw "Expected frontend failure: $($case.File)" }
    $report = $text[0] | ConvertFrom-Json
    if ($report.diagnostics.code -notcontains $case.Code) { throw "Expected TS$($case.Code): $($text[0])" }
    Write-Host "PASS Microsoft diagnostic TS$($case.Code): $($case.File)"
}

$text = @(& $probe "$output/does-not-exist.ts" missing)
if ($LASTEXITCODE -eq 0 -or !($text[0] | ConvertFrom-Json).error) { throw 'Expected missing-file error' }
Write-Host 'PASS missing-file error'

# Verify that safe callers cannot forge owners, bypass the FFI boundary, or
# invalidate a live report view. These are compile-fail ownership tests.
$safetyRoot = Join-Path $output 'safety-tests'
[IO.Directory]::CreateDirectory($safetyRoot) | Out-Null
$frontendImport = '../../../experiments/native-frontend/frontend.cr'
$includeDirectory = $output.Replace('\', '/')
@"
[project]
name = "frontend-safety-tests"
[[targets]]
name = "safety"
kind = "executable"
entry = "case.cr"
[targets.native]
include-dirs = ["$includeDirectory"]
"@ | Set-Content -LiteralPath "$safetyRoot/crust.toml" -Encoding utf8
$rejectedPrograms = @(
    @{ Name = 'direct native call'; Source = 'int main() { return tsnet_close_file(0); }'; Error = 'unsafe' },
    @{ Name = 'forged session'; Source = 'int main() { FrontendSession session = { .handle = 123 }; return 0; }'; Error = 'private' },
    @{ Name = 'forged report'; Source = 'int main() { FrontendReport report = { .status = 0, .data = 0, .length = 0 }; return 0; }'; Error = 'private' },
    @{ Name = 'unchecked buffer adoption'; Source = 'int main() { FrontendReport report = FrontendReport::take_native(0, 0); return 0; }'; Error = 'unsafe' },
    @{ Name = 'copy of report owner'; Source = 'int main() { FrontendReport first = FrontendReport::empty(1); FrontendReport second = first; return 0; }'; Error = 'move' },
    @{ Name = 'use after move'; Source = 'int main() { FrontendReport first = FrontendReport::empty(1); FrontendReport second = move(first); return first.code(); }'; Error = 'moved' },
    @{ Name = 'move while view is borrowed'; Source = 'int main() { FrontendReport report = FrontendReport::empty(1); const char[]& view = report.view(); FrontendReport second = move(report); print(view); return 0; }'; Error = 'borrow' },
    @{ Name = 'escaping report view'; Source = 'const char[]& dangling() { FrontendReport report = FrontendReport::empty(1); return report.view(); } int main() { return 0; }'; Error = 'borrow' }
)
foreach ($case in $rejectedPrograms) {
    [IO.File]::WriteAllText("$safetyRoot/case.cr", 'import "' + $frontendImport + '";' + "`n" + $case.Source, $utf8)
    $start = New-Object System.Diagnostics.ProcessStartInfo
    $start.FileName = (Get-Command crust).Source
    $start.Arguments = 'check --project "' + $safetyRoot + '"'
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardError = $true
    $start.RedirectStandardOutput = $true
    $process = [Diagnostics.Process]::Start($start)
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit()
    $message = $stdout.Result + $stderr.Result
    $exitCode = $process.ExitCode
    $process.Dispose()
    if ($exitCode -eq 0 -or $message -notmatch $case.Error) { throw "Expected safety rejection: $($case.Name)`n$message" }
    Write-Host "PASS Crust ownership check: $($case.Name)"
}

# Source preparation and bridge compilation must not modify upstream files.
& git -C "$root/artifacts/typescript-native" diff --exit-code --quiet
if ($LASTEXITCODE) { throw 'The upstream checkout has been modified' }
Write-Host 'Native frontend experiment tests passed.'
