$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$work = Join-Path $root 'artifacts/compiler-storage-tests'
[IO.Directory]::CreateDirectory($work) | Out-Null
$utf8 = New-Object System.Text.UTF8Encoding($false)
@'
[project]
name = "compiler-storage-tests"
[[targets]]
name = "storage-test"
kind = "executable"
entry = "case.cr"
'@ | Set-Content -LiteralPath "$work/crust.toml" -Encoding utf8

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

function Write-Case([string]$Body) {
    [IO.File]::WriteAllText("$work/case.cr", 'import "../../src/support.cr";' + "`n" + $Body, $utf8)
}

$crust = (Get-Command crust).Source
$exe = "$work/target/crust/storage-test/storage-test.exe"
Write-Case @'
struct Item { size_t previous; int value; };
int main() {
    Store<Item> records = Store<Item>::create();
    size_t previous = 0;
    for (int i = 0; i < 10000; i++) {
        Item item = { .previous = previous, .value = i };
        previous = records.add(item);
    }
    for (int i = 9999; i >= 0; i--) {
        Item item = records.get(previous);
        if (item.value != i) { return 1; }
        previous = item.previous;
    }
    if (previous != 0) { return 1; }
    { Item& first = records.at_mut(1); first.value = 42; }
    if (records.get(1).value != 42) { return 1; }
    return 0;
}
'@
$build = Invoke-Captured $crust ('build --release --project "' + $work + '"')
if ($build.Code) { throw $build.Text }
$run = Invoke-Captured $exe ''
if ($run.Code) { throw "Store growth/borrow test failed: $($run.Text)" }
Write-Host 'PASS 10000 stable record IDs across store growth and checked mutation'

foreach ($operation in @('records.get(0)', 'records.get(2)', 'records.put(0, 7)', 'records.at_mut(999)')) {
    Write-Case ('int main() { Store<int> records = Store<int>::create(); records.add(42); ' + $operation + '; return 0; }')
    $build = Invoke-Captured $crust ('build --release --project "' + $work + '"')
    if ($build.Code) { throw $build.Text }
    $run = Invoke-Captured $exe ''
    if ($run.Code -ne 1 -or !$run.Text.Contains('invalid store ID')) { throw "Expected checked bounds failure: $operation`n$($run.Text)" }
}
Write-Host 'PASS zero and out-of-range IDs fail before accessing storage'

$rejects = @(
    @{ Source = 'int main() { Store<int> first = Store<int>::create(); Store<int> second = first; return 0; }'; Error = 'move' },
    @{ Source = 'int main() { Store<int> records = Store<int>::create(); records.add(1); int& held = records.at_mut(1); records.add(2); print(held); return 0; }'; Error = 'borrow' },
    @{ Source = 'int main() { Output output = { .file = 0, .owned = true }; return 0; }'; Error = 'private' },
    @{ Source = 'int main() { return fclose(0); }'; Error = 'unsafe' }
)
foreach ($case in $rejects) {
    Write-Case $case.Source
    $check = Invoke-Captured $crust ('check --project "' + $work + '"')
    if ($check.Code -eq 0 -or $check.Text -notmatch $case.Error) { throw "Expected ownership rejection: $($case.Source)`n$($check.Text)" }
}
Write-Host 'PASS store ownership, active borrows, private file ownership, and unsafe C boundaries'

$compiler = Join-Path $root 'target/crust/tsnetc/tsnetc.exe'
$inputFile = Join-Path $work 'boundary.ts'
$outputFile = Join-Path $work 'boundary.dll'
foreach ($case in @(
    @{ Source = '//' + ('x' * (4 * 1024 * 1024 - 2)); Succeeds = $true; Message = '' },
    @{ Source = '//' + ('x' * (4 * 1024 * 1024 - 1)); Succeeds = $false; Message = '4 MiB' },
    @{ Source = 'const x = 1;' + [char]0; Succeeds = $false; Message = 'NUL' }
)) {
    [IO.File]::WriteAllText($inputFile, $case.Source, $utf8)
    [IO.File]::WriteAllText($outputFile, '// sentinel', $utf8)
    $run = Invoke-Captured $compiler ('"' + $inputFile + '" "' + $outputFile + '"')
    if ($case.Succeeds) {
        if ($run.Code) { throw "Exact-limit input failed: $($run.Text)" }
    }
    elseif ($run.Code -ne 1 -or !$run.Text.Contains($case.Message) -or [IO.File]::ReadAllText($outputFile) -ne '// sentinel') {
        throw "Expected bounded input failure with output preservation: $($run.Text)"
    }
}
Write-Host 'PASS exact input limit, oversized input, NUL rejection, and output preservation'
