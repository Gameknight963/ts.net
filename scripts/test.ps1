$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    & "$PSScriptRoot/native-frontend.ps1"
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
    foreach ($fixture in @('tests/behavior.ts', 'tests/globals.ts', 'tests/modules.ts', 'tests/default-modules.ts', 'tests/structural-fields.ts', 'tests/optional-context.ts', 'tests/has-own.ts', 'tests/string-index.ts', 'tests/has-own-fastpath.ts', 'tests/il-backend.ts', 'tests/frontend.ts', 'tests/straightforward.ts', 'tests/logical-assignment.ts', 'tests/dynamic-arithmetic.ts', 'tests/nullable-values.ts', 'tests/numeric-fastpaths.ts', 'tests/bitwise.ts', 'tests/scalar-objects.ts', 'tests/scalar-dynamic.ts', 'tests/scalar-references.ts', 'tests/integer-loops.ts', 'tests/arrays.ts', 'tests/mixed-index.ts', 'tests/dense-read.ts', 'tests/typeof-number.ts', 'tests/slice.ts', 'tests/packed-arrays.ts', 'tests/for-of.ts', 'tests/array-spread.ts', 'tests/array-destructuring.ts', 'tests/object-destructuring.ts', 'tests/destructuring-fastpath.ts', 'tests/destructured-parameters.ts', 'tests/rest-parameters.ts', 'tests/call-spread.ts', 'examples/structural.ts', 'examples/numeric.ts', 'artifacts/tests/boundaries.ts')) {
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

    & dotnet build tests/inspect-nullable/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Nullable metadata inspector build failed' }
    & dotnet tests/inspect-nullable/bin/Release/net10.0/Inspect.dll "$testRoot/nullable-values/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Nullable storage was not emitted' }

    & dotnet build tests/inspect-scalars/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Scalar replacement inspector build failed' }
    & dotnet tests/inspect-scalars/bin/Release/net10.0/Inspect.dll "$testRoot/scalar-objects/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Scalar allocation checks failed' }
    & dotnet tests/inspect-scalars/bin/Release/net10.0/Inspect.dll "$testRoot/has-own-fastpath/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Local object allocation was not eliminated' }

    & dotnet build tests/inspect-arrays/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Array runtime checks failed to build' }
    & dotnet tests/inspect-arrays/bin/Release/net10.0/Inspect.dll "$testRoot/packed-arrays/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Array runtime checks failed' }

    & dotnet build tests/inspect-destructuring/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Destructuring inspector build failed' }
    & dotnet tests/inspect-destructuring/bin/Release/net10.0/Inspect.dll "$testRoot/destructuring-fastpath/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Literal destructuring allocated temporary objects' }

    & dotnet build tests/inspect-iteration/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Iteration runtime checks failed to build' }
    & dotnet tests/inspect-iteration/bin/Release/net10.0/Inspect.dll
    if ($LASTEXITCODE) { throw 'Iteration runtime checks failed' }

    foreach ($case in @(
        @{ Name = 'bad-assertion'; Message = 'checked conversion failed' },
        @{ Name = 'default-module-tdz'; Message = 'Cannot access binding before initialization' },
        @{ Name = 'module-tdz'; Message = 'Cannot access binding before initialization' },
        @{ Name = 'global-tdz'; Message = 'Cannot access binding before initialization' },
        @{ Name = 'logical-assignment-tdz'; Message = 'Cannot access binding before initialization' },
        @{ Name = 'global-write-tdz'; Message = 'Cannot access binding before initialization' },
        @{ Name = 'structural-field-bad-write'; Message = 'checked conversion failed' },
        @{ Name = 'structural-reference-bad-write'; Message = 'checked conversion failed' },
        @{ Name = 'has-own-null'; Message = 'Cannot inspect properties of null or undefined' },
        @{ Name = 'bad-write'; Message = 'property write violates static field type' },
        @{ Name = 'scalar-dynamic-bad-write'; Message = 'property write violates static field type' },
        @{ Name = 'scalar-bad-write'; Message = 'checked conversion failed' },
        @{ Name = 'array-wide-bad-write'; Message = 'Array write violates number element type' },
        @{ Name = 'array-bad-write'; Message = 'Array write violates number element type' },
        @{ Name = 'array-bad-length'; Message = 'Invalid array length' },
        @{ Name = 'slice-invalid'; Message = 'slice requires an array or string' },
        @{ Name = 'reference-bad-write'; Message = 'property write violates static field type' },
        @{ Name = 'for-of-invalid'; Message = 'Value is not iterable' },
        @{ Name = 'array-spread-invalid'; Message = 'Value is not iterable' },
        @{ Name = 'call-spread-invalid'; Message = 'Value is not iterable' },
        @{ Name = 'array-destructuring-invalid'; Message = 'Value is not iterable' },
        @{ Name = 'object-destructuring-invalid'; Message = 'Cannot destructure null or undefined' },
        @{ Name = 'destructured-parameter-invalid'; Message = 'Cannot destructure null or undefined' },
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
        @{ Source = 'const n: number = "wrong";'; Error = '"code":2322' },
        @{ Source = 'const n = 1; n = 2;'; Error = '"code":2588' },
        @{ Source = 'const n: unknown = 1; console.log(n + 1);'; Error = '"code":18046' },
        @{ Source = 'function f(): number { const x = 1; }'; Error = '"code":2355' },
        @{ Source = 'function f(n: number): number { return n; } f("bad");'; Error = '"code":2345' },
        @{ Source = 'const p = { x: 1 }; console.log(p.y);'; Error = '"code":2339' },
        @{ Source = 'const x = {...{a: 1}};'; Error = 'unsupported lowering' },
        @{ Source = 'const s = "unterminated'; Error = '"code":1002' },
        @{ Source = '/* unterminated'; Error = '"code":1010' },
        @{ Source = 'let x = 1; let x = 2;'; Error = '"code":2451' },
        @{ Source = 'console.log(missing);'; Error = '"code":2304' },
        @{ Source = 'type A = { x: number }; type B = { x: string }; const a: A = { x: 1 }; const b: B = a;'; Error = '"code":2322' }
    )
    $index = 0
    foreach ($case in $rejects) {
        $inputPath = "$testRoot/reject-$index.ts"
        $outputPath = "$testRoot/reject-$index.dll"
        [IO.File]::WriteAllText($inputPath, $case.Source, $utf8)
        # Existing output must survive a failed frontend compilation.
        [IO.File]::WriteAllText($outputPath, '// sentinel', $utf8)
        $message = @(& $compiler $inputPath $outputPath) -join "`n"
        if ($LASTEXITCODE -eq 0 -or !$message.Contains($case.Error)) {
            throw "Expected frontend diagnostic: $($case.Source)`n$message"
        }
        if ([IO.File]::ReadAllText($outputPath) -ne '// sentinel') { throw 'Frontend failure overwrote output' }
        $index++
    }
    Write-Host "PASS $index frontend rejection and output-preservation checks"
    & "$PSScriptRoot/test-compiler-storage.ps1"
    & "$PSScriptRoot/test-metadata-writer.ps1"
    & dotnet build tests/inspect-writer/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Assembly writer inspector build failed' }
    & dotnet tests/inspect-writer/bin/Release/net10.0/Inspect.dll
    if ($LASTEXITCODE) { throw 'Assembly writer string checks failed' }
    & dotnet msbuild tsnet.crproj -t:Check -nologo -v:minimal
    if ($LASTEXITCODE) { throw 'Visual Studio project check failed' }
    Write-Host 'All tests passed.'
}
finally { Pop-Location }
