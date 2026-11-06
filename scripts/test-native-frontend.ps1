param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$experiment = Join-Path $root 'experiments/native-frontend'
$extension = if ([Environment]::OSVersion.Platform -eq 'Win32NT') { '.exe' } else { '' }
$probe = "$root/compiler/bin/Release/net10.0/tsnet-frontend$extension"
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

function Invoke-Frontend([string]$Path) {
    $start = New-Object Diagnostics.ProcessStartInfo
    $start.FileName = $probe
    $start.Arguments = '"' + $Path + '"'
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = [Diagnostics.Process]::Start($start)
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit()
    $script:FrontendStatus = $process.ExitCode
    $text = $stdout.Result + $stderr.Result
    $process.Dispose()
    return $text.Trim()
}
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
    $text = @(Invoke-Frontend $path)
    if ($script:FrontendStatus -eq 0 -or $text.Count -ne 1) { throw "Expected frontend failure: $($case.File)" }
    $report = $text[0] | ConvertFrom-Json
    if ($report.diagnostics.code -notcontains $case.Code) { throw "Expected TS$($case.Code): $($text[0])" }
    Write-Host "PASS Microsoft diagnostic TS$($case.Code): $($case.File)"
}

$text = @(Invoke-Frontend "$output/does-not-exist.ts")
if ($script:FrontendStatus -eq 0 -or !($text[0] | ConvertFrom-Json).error) { throw 'Expected missing-file error' }
Write-Host 'PASS missing-file error'
