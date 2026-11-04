$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    & "$PSScriptRoot/build-metadata-writer.ps1"
    & crust check
    if ($LASTEXITCODE) { throw 'Crust check failed' }
    & crust build --release
    if ($LASTEXITCODE) { throw 'Crust build failed' }
    $compiler = "$root/target/crust/tsnetc/tsnetc.exe"
    $utf8 = New-Object System.Text.UTF8Encoding($false)
    $testRoot = "$root/artifacts/tests"
    [IO.Directory]::CreateDirectory($testRoot) | Out-Null

    # Cross read-buffer boundaries, retain IDs through repeated vector growth,
    # and preserve UTF-8 source bytes, a BOM, and CRLF line endings.
    $boundary = New-Object System.Text.StringBuilder
    [void]$boundary.AppendLine('//' + ('x' * 5000))
    [void]$boundary.AppendLine('function increment(n: number): number { return n + 1; }')
    for ($i = 0; $i -lt 1500; $i++) { [void]$boundary.AppendLine("const v$i = increment($i);") }
    $unicode = [string][char]0x96EA + ' ' + [char]::ConvertFromUtf32(0x1F980)
    [void]$boundary.AppendLine('const banner = "' + $unicode + '";')
    [void]$boundary.AppendLine('console.log(v0, v127, v511, v1499, banner, banner.length);')
    $bom = New-Object System.Text.UTF8Encoding($true)
    [IO.File]::WriteAllText("$testRoot/boundaries.ts", $boundary.ToString(), $bom)

    # Differential execution checks against the actual TypeScript compiler.
    foreach ($fixture in @('tests/behavior.ts', 'tests/il-backend.ts', 'examples/structural.ts', 'examples/numeric.ts', 'artifacts/tests/boundaries.ts')) {
        $name = [IO.Path]::GetFileNameWithoutExtension($fixture)
        & "$PSScriptRoot/build.ps1" -InputFile $fixture -OutputDirectory "artifacts/tests/$name" -SkipCompilerBuild | Out-Host
        & tsc.cmd --strict --target es2022 --module commonjs --outDir "$testRoot/node/$name" $fixture
        if ($LASTEXITCODE) { throw "Reference TypeScript compilation failed: $name" }
        $reference = "$testRoot/node/$name/$name.js"
        $expected = @(& node $reference)
        if ($LASTEXITCODE) { throw "Node failed: $name" }
        $actual = @(& dotnet "$testRoot/$name/bin/Release/net10.0/App.dll")
        if ($LASTEXITCODE) { throw ".NET failed: $name" }
        if (($expected -join "`n") -cne ($actual -join "`n")) {
            throw "Output mismatch for $name`nExpected: $expected`nActual: $actual"
        }
        Write-Host "PASS differential: $name"
    }

    foreach ($case in @(
        @{ Name = 'bad-assertion'; Message = 'checked conversion failed' },
        @{ Name = 'bad-write'; Message = 'property write violates static field type' },
        @{ Name = 'bad-structural-cast'; Message = 'checked conversion failed' }
    )) {
        $name = $case.Name
        & "$PSScriptRoot/build.ps1" -InputFile "tests/$name.ts" -OutputDirectory "artifacts/tests/$name" -SkipCompilerBuild | Out-Host
        # Process captures expected stderr failures without PowerShell's native
        # stderr-to-error conversion. Argument list contains only a quoted path.
        $start = New-Object System.Diagnostics.ProcessStartInfo
        $start.FileName = (Get-Command dotnet).Source
        $start.Arguments = '"' + "$testRoot/$name/bin/Release/net10.0/App.dll" + '"'
        $start.UseShellExecute = $false
        $start.CreateNoWindow = $true
        $start.RedirectStandardError = $true
        $process = [Diagnostics.Process]::Start($start)
        $errorText = $process.StandardError.ReadToEnd()
        $process.WaitForExit()
        if ($process.ExitCode -eq 0 -or !$errorText.Contains($case.Message)) { throw "Expected checked failure: $name`n$errorText" }
        $process.Dispose()
        Write-Host "PASS runtime check: $name"
    }

    $rejects = @(
        @{ Source = 'const n: number = "wrong";'; Error = 'incompatible variable initializer' },
        @{ Source = 'const n = 1; n = 2;'; Error = 'cannot assign to const' },
        @{ Source = 'const n: unknown = 1; console.log(n + 1);'; Error = 'numeric operand required' },
        @{ Source = 'function f(): number { const x = 1; }'; Error = 'return on every path' },
        @{ Source = 'function f(n: number): number { return n; } f("bad");'; Error = 'incompatible function argument' },
        @{ Source = 'const p = { x: 1 }; console.log(p.y);'; Error = 'no such property' },
        @{ Source = 'const x = [1, 2];'; Error = 'expected expression' },
        @{ Source = 'const s = "unterminated'; Error = 'unterminated string' },
        @{ Source = '/* unterminated'; Error = 'unterminated comment' },
        @{ Source = 'let x = 1; let x = 2;'; Error = 'duplicate binding' },
        @{ Source = 'console.log(missing);'; Error = 'unknown binding' },
        @{ Source = 'type A = { x: number }; type B = { x: string }; const a: A = { x: 1 }; const b: B = a;'; Error = 'incompatible variable initializer' }
    )
    $index = 0
    foreach ($case in $rejects) {
        $inputPath = "$testRoot/reject-$index.ts"
        $outputPath = "$testRoot/reject-$index.dll"
        [IO.File]::WriteAllText($inputPath, $case.Source, $utf8)
        # Existing output must survive a failed frontend compilation.
        [IO.File]::WriteAllText($outputPath, '// sentinel', $utf8)
        $message = @(& $compiler $inputPath $outputPath) -join "`n"
        if ($LASTEXITCODE -eq 0 -or !$message.Contains($case.Error) -or !$message.Contains('(1,')) {
            throw "Expected frontend diagnostic: $($case.Source)`n$message"
        }
        if ([IO.File]::ReadAllText($outputPath) -ne '// sentinel') { throw 'Frontend failure overwrote output' }
        $index++
    }
    Write-Host "PASS $index frontend rejection and output-preservation checks"
    & "$PSScriptRoot/test-compiler-storage.ps1"
    & "$PSScriptRoot/test-metadata-writer.ps1"
    & dotnet msbuild tsnet.crproj -t:Check -nologo -v:minimal
    if ($LASTEXITCODE) { throw 'Visual Studio project check failed' }
    Write-Host 'All tests passed.'
}
finally { Pop-Location }
