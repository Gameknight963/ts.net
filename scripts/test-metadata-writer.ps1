$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$work = Join-Path $root 'artifacts/metadata-writer-tests'
[IO.Directory]::CreateDirectory($work) | Out-Null
$entry = (Join-Path $work 'case.cr').Replace('\','/')
$manifest = (Get-Content "$root/crust.toml" -Raw).
    Replace('entry = "src/main.cr"', "entry = `"$entry`"").
    Replace('"backend/metadata/link-options.c"', '"../../backend/metadata/link-options.c"').
    Replace('"artifacts/metadata-writer/', '"../../artifacts/metadata-writer/')
$manifest = $manifest.Replace('"artifacts/native-frontend', '"../../artifacts/native-frontend')
$manifest | Set-Content "$work/crust.toml"
$import = '../../src/assembly-plan.cr'
@"
import "$import";
int main() {
    AssemblyPlan plan=AssemblyPlan::create();
    // A missing base type must produce an error, not replace an existing file.
    plan.type("Invalid",999,1);
    plan.finish(999,"preserved.dll");
    return 0;
}
"@ | Set-Content "$work/case.cr"
& crust build --project $work --release
if ($LASTEXITCODE) { throw 'Writer test build failed' }
$sentinel = 'existing assembly sentinel'
[IO.File]::WriteAllText("$work/preserved.dll",$sentinel)
Push-Location $work
try {
    $output = @(& "$work/target/crust/tsnetc/tsnetc.exe") -join "`n"
    if ($LASTEXITCODE -ne 1 -or !$output.Contains('assembly writer failed')) { throw "Expected assembly writer error: $output" }
    if ([IO.File]::ReadAllText("$work/preserved.dll") -ne $sentinel) { throw 'Failed assembly emission replaced output' }
}
finally { Pop-Location }
Write-Host 'PASS writer errors remain inside C ABI and preserve existing output'
$global:LASTEXITCODE = 0
