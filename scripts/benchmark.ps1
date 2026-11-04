$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    & "$PSScriptRoot/build.ps1" -InputFile examples/benchmark.ts -OutputDirectory artifacts/benchmark
    & tsc.cmd --strict --target es2022 --module commonjs --outDir artifacts/benchmark/node examples/benchmark.ts
    if ($LASTEXITCODE) { throw 'Reference TypeScript compilation failed' }
    $node = @(& node artifacts/benchmark/node/benchmark.js)
    if ($LASTEXITCODE) { throw 'Node benchmark failed' }
    $net = @(& dotnet artifacts/benchmark/bin/Release/net10.0/App.dll)
    if ($LASTEXITCODE) { throw '.NET benchmark failed' }
    if ($node[-1] -ne $net[-1]) { throw 'Benchmark checksums differ' }
    $culture = [Globalization.CultureInfo]::InvariantCulture
    $nodeTimes = @($node[0..4] | ForEach-Object { [double]::Parse($_, $culture) } | Sort-Object)
    $netTimes = @($net[0..4] | ForEach-Object { [double]::Parse($_, $culture) } | Sort-Object)
    Write-Host "Node median kernel time: $($nodeTimes[2]) ms"
    Write-Host ".NET median kernel time: $($netTimes[2]) ms"
    Write-Host "Node/.NET ratio: $($nodeTimes[2] / $netTimes[2])"
    Write-Host $net[-1]
    & node --version
    & dotnet --version
    Write-Host 'Five samples after ten warmup calls; execution only, excluding build and process startup.'
    Write-Host 'One arithmetic kernel is not evidence about application performance.'
}
finally { Pop-Location }
