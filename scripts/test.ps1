$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    & dotnet build "$root/compiler/Tsnet.Compiler.csproj" -c Release --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Compiler build failed' }
    $compiler = "$root/compiler/bin/Release/net10.0/tsnetc.dll"
    [IO.Directory]::CreateDirectory("$root/artifacts/metadata-writer") | Out-Null
    '<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content "$root/artifacts/metadata-writer/NuGet.Config"
    & dotnet build "$root/runtime/Tsnet.Runtime.csproj" -c Release --configfile "$root/artifacts/metadata-writer/NuGet.Config" --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Runtime build failed' }
    $utf8 = New-Object System.Text.UTF8Encoding($false)
    $testRoot = "$root/artifacts/tests"
    [IO.Directory]::CreateDirectory($testRoot) | Out-Null

    & dotnet build tests/inspect-weak-ref/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'WeakRef inspector build failed' }
    & dotnet tests/inspect-weak-ref/bin/Release/net10.0/Inspect.dll
    if ($LASTEXITCODE) { throw 'WeakRef lifetime checks failed' }

    & dotnet build tests/inspect-symbol-keys/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Symbol key inspector build failed' }
    & dotnet tests/inspect-symbol-keys/bin/Release/net10.0/Inspect.dll
    if ($LASTEXITCODE) { throw 'Symbol key lifetime checks failed' }

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

    & node tests/number-strings.cjs "$testRoot/number-strings.tsv"
    if ($LASTEXITCODE) { throw 'Numeric string reference generation failed' }
    & dotnet build tests/inspect-number-strings/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Numeric string inspector build failed' }
    & dotnet tests/inspect-number-strings/bin/Release/net10.0/Inspect.dll "$testRoot/number-strings.tsv"
    if ($LASTEXITCODE) { throw 'Numeric string checks failed' }

    & dotnet build tests/inspect-deletion/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Deletion inspector build failed' }
    $expectedDeletion = @(& node tests/deletion.cjs)
    if ($LASTEXITCODE) { throw 'Node deletion reference failed' }
    $actualDeletion = @(& dotnet tests/inspect-deletion/bin/Release/net10.0/Inspect.dll)
    if ($LASTEXITCODE) { throw 'Runtime deletion checks failed' }
    if (($expectedDeletion -join "`n") -cne ($actualDeletion -join "`n")) { throw 'Runtime deletion differs from Node' }
    Write-Host 'PASS runtime deletion matches Node'

    & dotnet build tests/inspect-weak-collections/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Weak collection inspector build failed' }
    & dotnet tests/inspect-weak-collections/bin/Release/net10.0/Inspect.dll
    if ($LASTEXITCODE) { throw 'Weak collection garbage-collection checks failed' }

    # Differential execution checks against the actual TypeScript compiler.
    foreach ($fixture in @('tests/behavior.ts', 'tests/debugger.ts', 'tests/await-expressions.ts', 'tests/await-destructuring.ts', 'tests/await-switch.ts', 'tests/await-loops.ts', 'tests/await-exceptions.ts', 'tests/for-await.ts', 'tests/for-await-order.ts', 'tests/for-await-result.ts', 'tests/collections.ts', 'tests/weak-collections.ts', 'tests/weak-symbols.ts', 'tests/weak-ref.ts', 'tests/set-union.ts', 'tests/set-intersection-difference.ts', 'tests/set-relations.ts', 'tests/map-group-by.ts', 'tests/object-group-by.ts', 'tests/object-from-entries.ts', 'tests/object-create.ts', 'tests/delete.ts', 'tests/delete-super.ts', 'tests/delete-modules.ts', 'tests/await-intrinsic-calls.ts', 'tests/collections-modules.ts', 'tests/collection-iterators.ts', 'tests/iterator-from.ts', 'tests/iterator-terminals.ts', 'tests/iterator-lazy.ts', 'tests/iterator-flat-map.ts', 'tests/sequence-cleanup.ts', 'tests/sequence-cleanup-modules.ts', 'tests/await-statements.ts', 'tests/await-statements-modules.ts', 'tests/await-assignments.ts', 'tests/await-optional-chaining.ts', 'tests/await-return.ts', 'tests/await-return-modules.ts', 'tests/async-functions.ts', 'tests/async-functions-modules.ts', 'tests/promises.ts', 'tests/promise-combinators.ts', 'tests/promise-helpers.ts', 'tests/promise-helpers-modules.ts', 'tests/promises-modules.ts', 'tests/exceptions.ts', 'tests/error-prototypes.ts', 'tests/error-is-error.ts', 'tests/enums.ts', 'tests/enum-initialization.ts', 'tests/enums-namespaces.ts', 'tests/enums-nested-namespaces.ts', 'tests/enum-import-aliases.ts', 'tests/enum-alias-only.ts', 'tests/enums-modules.ts', 'tests/enums-const-import.ts', 'tests/enums-large.ts', 'tests/globals.ts', 'tests/symbol-values.ts', 'tests/symbol-keys.ts', 'tests/custom-iterables.ts', 'tests/custom-async-iterables.ts', 'tests/dates.ts', 'tests/date-getters.ts', 'tests/date-calendar-parts.ts', 'tests/date-setters.ts', 'tests/date-formatting.ts', 'tests/date-locale-formatting.ts', 'tests/json-parse.ts', 'tests/json-stringify.ts', 'tests/raw-json.ts', 'tests/object-constructor.ts', 'tests/object-assign.ts', 'tests/object-is.ts', 'tests/object-is-modules.ts', 'tests/object-assign-modules.ts', 'tests/object-constructor-modules.ts', 'tests/primitive-constructors.ts', 'tests/object-prototypes.ts', 'tests/object-prototype-queries.ts', 'tests/reflect.ts', 'tests/reflect-query-layout.ts', 'tests/reflect-construct.ts', 'tests/reflect-construct-typed.ts', 'tests/reflect-set-prototype.ts', 'tests/reflect-delete.ts', 'tests/reflect-delete-direct.ts', 'tests/object-value-of.ts', 'tests/object-string-methods.ts', 'tests/regular-expressions.ts', 'tests/regex-string-replacement.ts', 'tests/regex-string-split.ts', 'tests/regex-string-match.ts', 'tests/regex-match-all.ts', 'tests/regex-escape.ts', 'tests/regex-dot-all.ts', 'tests/regex-indices.ts', 'tests/regex-lookbehind.ts', 'tests/native-function-text.ts', 'tests/modules.ts', 'tests/default-modules.ts', 'tests/structural-fields.ts', 'tests/empty-structural-types.ts', 'tests/optional-context.ts', 'tests/in-operator.ts', 'tests/in-operator-allocation.ts', 'tests/optional-chaining.ts', 'tests/optional-chaining-modules.ts', 'tests/optional-chaining-allocation.ts', 'tests/has-own.ts', 'tests/object-keys.ts', 'tests/own-property-names.ts', 'tests/property-descriptors.ts', 'tests/lookup-accessors.ts', 'tests/object-enumeration.ts', 'tests/object-spread.ts', 'tests/spread-fastpath.ts', 'tests/key-layouts.ts', 'tests/object-key-count.ts', 'tests/string-index.ts', 'tests/string-search.ts', 'tests/string-search-allocation.ts', 'tests/numeric-string-index.ts', 'tests/string-boundaries.ts', 'tests/string-boundaries-modules.ts', 'tests/string-formatting.ts', 'tests/string-case.ts', 'tests/string-locale-case.ts', 'tests/string-locale-compare.ts', 'tests/string-locale-sensitivity.ts', 'tests/string-locale-numeric.ts', 'tests/string-html.ts', 'tests/string-split.ts', 'tests/string-well-formed.ts', 'tests/string-code-points.ts', 'tests/string-raw.ts', 'tests/tagged-templates.ts', 'tests/await-tagged-templates.ts', 'tests/tagged-templates-modules.ts', 'tests/string-normalization.ts', 'tests/string-replacement.ts', 'tests/string-formatting-modules.ts', 'tests/string-ranges.ts', 'tests/string-ranges-allocation.ts', 'tests/has-own-fastpath.ts', 'tests/il-backend.ts', 'tests/frontend.ts', 'tests/functions.ts', 'tests/arguments.ts', 'tests/arguments-modules.ts', 'tests/function-prototypes.ts', 'tests/function-property-deletion.ts', 'tests/native-property-deletion.ts', 'tests/function-constructors.ts', 'tests/new-target.ts', 'tests/new-target-modules.ts', 'tests/function-constructor-cold.ts', 'tests/constructor-namespace-alias.ts', 'tests/constructor-namespace-index.ts', 'tests/function-constructor-allocation.ts', 'tests/receivers.ts', 'tests/function-binding.ts', 'tests/classes.ts', 'tests/class-index-signatures.ts', 'tests/numeric-members.ts', 'tests/numeric-members-modules.ts', 'tests/class-replacement.ts', 'tests/computed-classes.ts', 'tests/class-spread.ts', 'tests/classes-modules.ts', 'tests/inheritance.ts', 'tests/function-inheritance.ts', 'tests/function-base-no-new.ts', 'tests/inheritance-modules.ts', 'tests/accessors.ts', 'tests/auto-accessors.ts', 'tests/private-members.ts', 'tests/private-members-modules.ts', 'tests/function-self.ts', 'tests/iife-captures.ts', 'tests/closures.ts', 'tests/closure-module-access.ts', 'tests/local-functions.ts', 'tests/local-function-loop.ts', 'tests/closure-loop.ts', 'tests/function-dispatch.ts', 'tests/call-arity.ts', 'tests/function-aliases.ts', 'tests/void-values.ts', 'tests/straightforward.ts', 'tests/logical-assignment.ts', 'tests/dynamic-arithmetic.ts', 'tests/nullable-values.ts', 'tests/number-fields.ts', 'tests/push-values.ts', 'tests/push-local-arrays.ts', 'tests/numeric-fastpaths.ts', 'tests/masked-remainder.ts', 'tests/number-text.ts', 'tests/numeric-constants.ts', 'tests/math.ts', 'tests/math-f16round.ts', 'tests/math-modules.ts', 'tests/number-predicates.ts', 'tests/number-parsing.ts', 'tests/number-fixed.ts', 'tests/number-radix.ts', 'tests/number-locale-formatting.ts', 'tests/number-precision.ts', 'tests/global-number-predicates.ts', 'tests/uri-encoding.ts', 'tests/legacy-escapes.ts', 'tests/number-bindings.ts', 'tests/bitwise.ts', 'tests/exponentiation.ts', 'tests/number-strings.ts', 'tests/object-number.ts', 'tests/relational.ts', 'tests/equality.ts', 'tests/characters.ts', 'tests/characters-allocation.ts', 'tests/at.ts', 'tests/at-allocation.ts', 'tests/scalar-objects.ts', 'tests/scalar-dynamic.ts', 'tests/scalar-references.ts', 'tests/integer-loops.ts', 'tests/integer-call-loops.ts', 'tests/array-constructor.ts', 'tests/array-from.ts', 'tests/array-from-async.ts', 'tests/array-fill.ts', 'tests/array-copy-within.ts', 'tests/array-copy-within-modules.ts', 'tests/array-fill-modules.ts', 'tests/array-from-modules.ts', 'tests/array-constructor-modules.ts', 'tests/array-prototype-alias.ts', 'tests/arrays.ts', 'tests/array-join.ts', 'tests/array-callbacks.ts', 'tests/array-callback-modules.ts', 'tests/array-reduce.ts', 'tests/array-reduce-modules.ts', 'tests/array-find.ts', 'tests/array-find-modules.ts', 'tests/array-to-sorted.ts', 'tests/array-sort.ts', 'tests/array-shift.ts', 'tests/array-splice.ts', 'tests/array-concat.ts', 'tests/array-iterators.ts', 'tests/array-push-pop-generic.ts', 'tests/array-slice-generic.ts', 'tests/array-reverse-generic.ts', 'tests/array-search-generic.ts', 'tests/array-locale-formatting.ts', 'tests/array-to-sorted-modules.ts', 'tests/array-to-spliced.ts', 'tests/array-to-spliced-modules.ts', 'tests/array-copy.ts', 'tests/array-copy-modules.ts', 'tests/array-last-index.ts', 'tests/array-flatten.ts', 'tests/array-flatten-modules.ts', 'tests/mixed-index.ts', 'tests/union-arrays.ts', 'tests/loop-accumulators.ts', 'tests/computed-index.ts', 'tests/computed-properties.ts', 'tests/key-coercion-effects.ts', 'tests/dense-read.ts', 'tests/typeof-number.ts', 'tests/slice.ts', 'tests/reverse.ts', 'tests/array-search.ts', 'tests/packed-arrays.ts', 'tests/array-ranges.ts', 'tests/inferred-arrays.ts', 'tests/fresh-any-arrays.ts', 'tests/for-of.ts', 'tests/for-in.ts', 'tests/for-in-hidden.ts', 'tests/array-spread.ts', 'tests/array-destructuring.ts', 'tests/object-destructuring.ts', 'tests/object-rest.ts', 'tests/destructuring-fastpath.ts', 'tests/destructured-parameters.ts', 'tests/rest-parameters.ts', 'tests/call-spread.ts', 'examples/structural.ts', 'examples/numeric.ts', 'artifacts/tests/boundaries.ts')) {
        $name = [IO.Path]::GetFileNameWithoutExtension($fixture)
        & "$PSScriptRoot/build.ps1" -InputFile $fixture -OutputDirectory "artifacts/tests/$name" -SkipCompilerBuild -SkipRuntimeBuild | Out-Host
        $moduleKind = if ($name -eq 'delete-modules') { 'es2022' } else { 'commonjs' }
        & tsc.cmd --strict --target es2022 --module $moduleKind --outDir "$testRoot/node/$name" $fixture
        if ($moduleKind -eq 'es2022') { '{"type":"module"}' | Set-Content "$testRoot/node/$name/package.json" }
        else { '{"type":"commonjs"}' | Set-Content "$testRoot/node/$name/package.json" }
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

    # Namespace semantics need native ES modules as the reference.
    foreach ($name in @('import-meta', 'module-this', 'tagged-templates-modules', 'reflect-delete-modules', 'property-descriptors-modules', 'for-in-modules', 'for-in-cycle', 'module-namespace-order', 'in-operator-modules', 'function-constructor-modules')) {
        & "$PSScriptRoot/build.ps1" -InputFile "tests/$name.ts" -OutputDirectory "artifacts/tests/$name" -SkipCompilerBuild -SkipRuntimeBuild | Out-Host
        & tsc.cmd --strict --target es2022 --module es2022 --outDir "$testRoot/node/$name" "tests/$name.ts"
        if ($LASTEXITCODE) { throw "ES module reference compilation failed: $name" }
        '{"type":"module"}' | Set-Content "$testRoot/node/$name/package.json" -Encoding utf8
        $expected = @(& node "$testRoot/node/$name/$name.js")
        if ($LASTEXITCODE) { throw "ES module reference failed: $name" }
        $actual = @(& dotnet "$testRoot/$name/bin/Release/net10.0/App.dll")
        if ($LASTEXITCODE) { throw "Module namespace application failed: $name" }
        if (($expected -join "`n") -cne ($actual -join "`n")) { throw "ES module output mismatch: $name" }
        Write-Host "PASS ES module differential: $name"
    }

    & dotnet build tests/inspect-reflect-layout/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Reflect layout inspector build failed' }
    & dotnet tests/inspect-reflect-layout/bin/Release/net10.0/Inspect.dll "$testRoot/reflect-query-layout/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Read-only Reflect alias caused object-loop allocations' }

    # ClassDefinitionEvaluation validates heritage before computing member keys.
    # Node 25.9.0 currently computes keys first for an invalid base.
    # https://tc39.es/ecma262/multipage/ecmascript-language-functions-and-classes.html#sec-runtime-semantics-classdefinitionevaluation
    & "$PSScriptRoot/build.ps1" -InputFile tests/computed-class-invalid-base.ts -OutputDirectory artifacts/tests/computed-class-invalid-base -SkipCompilerBuild -SkipRuntimeBuild | Out-Host
    $actual = @(& dotnet "$testRoot/computed-class-invalid-base/bin/Release/net10.0/App.dll")
    if ($LASTEXITCODE -or ($actual -join "`n") -cne 'true 0') { throw 'Computed keys ran before invalid heritage was rejected' }
    Write-Host 'PASS invalid class heritage is rejected before computed keys'

    # The spec sorts all namespace export names lexically; V8 currently orders
    # integer-looking names numerically, so this case uses explicit expectations.
    & "$PSScriptRoot/build.ps1" -InputFile tests/module-namespace-numeric.ts -OutputDirectory artifacts/tests/module-namespace-numeric -SkipCompilerBuild -SkipRuntimeBuild | Out-Host
    & dotnet "$testRoot/module-namespace-numeric/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Numeric namespace key ordering failed' }

    # Namespace [[Set]] returns false regardless of receiver. V8 currently
    # permits inherited writes, so this check uses the specified result.
    & "$PSScriptRoot/build.ps1" -InputFile tests/namespace-prototype-write.ts -OutputDirectory artifacts/tests/namespace-prototype-write -SkipCompilerBuild -SkipRuntimeBuild | Out-Host
    & dotnet "$testRoot/namespace-prototype-write/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Namespace prototype writes must be rejected' }

    & dotnet build tests/inspect-string-search/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'String search checks failed to build' }
    & dotnet tests/inspect-string-search/bin/Release/net10.0/Inspect.dll "$testRoot/string-search-allocation/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'String search checks failed' }

    & dotnet build tests/inspect-string-ranges/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'String range checks failed to build' }
    & dotnet tests/inspect-string-ranges/bin/Release/net10.0/Inspect.dll "$testRoot/string-ranges-allocation/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'String range checks failed' }

    & dotnet build tests/inspect-remainder/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Remainder checks failed to build' }
    & dotnet tests/inspect-remainder/bin/Release/net10.0/Inspect.dll "$testRoot/masked-remainder/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Remainder checks failed' }

    & dotnet build tests/inspect-characters/Inspect.csproj -c Release --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Character checks failed to build' }
    & dotnet tests/inspect-characters/bin/Release/net10.0/Inspect.dll "$testRoot/characters-allocation/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Character checks failed' }

    & dotnet build tests/inspect-at/Inspect.csproj -c Release --nologo -v quiet
    if ($LASTEXITCODE) { throw 'at checks failed to build' }
    & dotnet tests/inspect-at/bin/Release/net10.0/Inspect.dll "$testRoot/at-allocation/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'at checks failed' }

    & dotnet build tests/inspect-equality/Inspect.csproj -c Release --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Equality inspector build failed' }
    & dotnet tests/inspect-equality/bin/Release/net10.0/Inspect.dll "$testRoot/equality/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Equality storage checks failed' }

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

    & dotnet build tests/inspect-object-keys/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Object key inspector build failed' }
    & dotnet tests/inspect-object-keys/bin/Release/net10.0/Inspect.dll "$testRoot/key-layouts/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Object key runtime checks failed' }

    & dotnet build tests/inspect-spread/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Spread inspector build failed' }
    & dotnet tests/inspect-spread/bin/Release/net10.0/Inspect.dll "$testRoot/spread-fastpath/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Local spread copies allocated per iteration' }

    & dotnet build tests/inspect-push/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Push inspector build failed' }
    & dotnet tests/inspect-push/bin/Release/net10.0/Inspect.dll "$testRoot/push-local-arrays/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Push capacity hints overallocated fresh arrays' }

    & dotnet build tests/inspect-property-keys/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Property key inspector build failed' }
    & dotnet tests/inspect-property-keys/bin/Release/net10.0/Inspect.dll "$testRoot/property-key-numbers.tsv"
    if ($LASTEXITCODE) { throw 'Numeric property keys failed to generate' }
    & node tests/property-key-numbers.cjs "$testRoot/property-key-numbers.tsv"
    if ($LASTEXITCODE) { throw 'Numeric property keys disagree with Node' }

    & dotnet build tests/inspect-property-bag/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Property bag inspector build failed' }
    & dotnet tests/inspect-property-bag/bin/Release/net10.0/Inspect.dll
    if ($LASTEXITCODE) { throw 'Dynamic property storage checks failed' }

    & dotnet build tests/inspect-enumeration/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Enumeration inspector build failed' }
    & dotnet tests/inspect-enumeration/bin/Release/net10.0/Inspect.dll "$testRoot/object-enumeration/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Object enumeration runtime checks failed' }

    & dotnet build tests/inspect-arrays/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Array runtime checks failed to build' }
    & dotnet tests/inspect-arrays/bin/Release/net10.0/Inspect.dll "$testRoot/packed-arrays/bin/Release/net10.0/App.dll" "$testRoot/union-arrays/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Array runtime checks failed' }

    & dotnet build tests/inspect-closures/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Closure inspector build failed' }
    & dotnet tests/inspect-closures/bin/Release/net10.0/Inspect.dll "$testRoot/closure-loop/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Closure storage or allocation check failed' }

    & dotnet build tests/inspect-constructors/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Constructor inspector build failed' }
    & dotnet tests/inspect-constructors/bin/Release/net10.0/Inspect.dll "$testRoot/function-constructor-allocation/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Constructor allocation checks failed' }

    & dotnet build tests/inspect-in/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Membership inspector build failed' }
    & dotnet tests/inspect-in/bin/Release/net10.0/Inspect.dll "$testRoot/in-operator-allocation/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Membership allocation checks failed' }

    & dotnet build tests/inspect-optional/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Optional chaining inspector build failed' }
    & dotnet tests/inspect-optional/bin/Release/net10.0/Inspect.dll "$testRoot/optional-chaining-allocation/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Optional chaining numeric allocation checks failed' }

    & dotnet build tests/inspect-functions/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Function inspector build failed' }
    & dotnet tests/inspect-functions/bin/Release/net10.0/Inspect.dll "$testRoot/functions/bin/Release/net10.0/App.dll" "$testRoot/function-dispatch/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Numeric callback checks failed' }
    & dotnet tests/inspect-functions/bin/Release/net10.0/Inspect.dll "$testRoot/functions/bin/Release/net10.0/App.dll" "$testRoot/local-function-loop/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Local function calls allocated per iteration' }
    & dotnet tests/inspect-functions/bin/Release/net10.0/Inspect.dll "$testRoot/functions/bin/Release/net10.0/App.dll" "$testRoot/function-self/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Self-returning function calls allocated per iteration' }
    & dotnet tests/inspect-functions/bin/Release/net10.0/Inspect.dll "$testRoot/functions/bin/Release/net10.0/App.dll" "$testRoot/iife-captures/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Immediately called closures allocated per iteration' }
    & dotnet tests/inspect-functions/bin/Release/net10.0/Inspect.dll "$testRoot/functions/bin/Release/net10.0/App.dll" "$testRoot/object-key-count/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Object key counts allocated per iteration' }

    & dotnet build tests/inspect-predicates/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Predicate inspector build failed' }
    & dotnet tests/inspect-predicates/bin/Release/net10.0/Inspect.dll "$testRoot/number-predicates/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Stored-predicate allocation check failed' }
    & dotnet tests/inspect-predicates/bin/Release/net10.0/Inspect.dll "$testRoot/function-aliases/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Function alias calls allocated per iteration' }
    & dotnet tests/inspect-predicates/bin/Release/net10.0/Inspect.dll "$testRoot/number-bindings/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Destructured Number checks allocated' }

    & dotnet build tests/inspect-void-values/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Void-value inspector build failed' }
    & dotnet tests/inspect-void-values/bin/Release/net10.0/Inspect.dll "$testRoot/void-values/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Void procedure calls allocated per iteration' }

    & dotnet build tests/inspect-destructuring/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Destructuring inspector build failed' }
    & dotnet tests/inspect-destructuring/bin/Release/net10.0/Inspect.dll "$testRoot/destructuring-fastpath/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Literal destructuring allocated temporary objects' }

    & dotnet build tests/inspect-iteration/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Iteration runtime checks failed to build' }
    & dotnet tests/inspect-iteration/bin/Release/net10.0/Inspect.dll
    if ($LASTEXITCODE) { throw 'Iteration runtime checks failed' }

    foreach ($case in @(
        @{ Name = 'regex-unsupported-flag'; Message = 'Regular-expression flags u and v are not supported' },
        @{ Name = 'string-locale-compare-options'; Message = 'localeCompare option caseFirst is not supported' },
        @{ Name = 'date-calendar-range'; Message = "Date calendar formatting outside .NET's year range 1 through 9999 is not supported" },
        @{ Name = 'date-construction-range'; Message = "Date calendar construction outside .NET's year range 1 through 9999 is not supported" },
        @{ Name = 'date-locale-options'; Message = 'Date locale formatting options are not supported' },
        @{ Name = 'number-locale-options'; Message = 'Number locale formatting option style is not supported' },
        @{ Name = 'number-radix-fraction'; Message = 'Radices other than 2, 4, 8, 10, 16 and 32 require safe integers' },
        @{ Name = 'string-search-unbound'; Message = 'String search method requires a receiver' },
        @{ Name = 'string-search-invalid'; Message = 'Cannot convert object to primitive value' },
        @{ Name = 'string-search-position-invalid'; Message = 'Cannot convert object to primitive value' },
        @{ Name = 'string-ranges-unbound'; Message = 'String range method requires a receiver' },
        @{ Name = 'string-ranges-invalid'; Message = 'Cannot convert object to primitive value' },
        @{ Name = 'characters-unbound'; Message = 'String character method requires a receiver' },
        @{ Name = 'characters-invalid'; Message = 'Cannot convert object to primitive value' },
        @{ Name = 'at-unbound'; Message = 'at requires a receiver' },
        @{ Name = 'at-invalid'; Message = 'Cannot convert object to primitive value' },
        @{ Name = 'equality-invalid'; Message = 'Cannot convert object to primitive value' },
        @{ Name = 'relational-invalid'; Message = 'Cannot convert object to primitive value' },
        @{ Name = 'object-number-invalid'; Message = 'Cannot convert object to primitive value' },
        @{ Name = 'computed-key-invalid'; Message = 'Cannot convert object to primitive value' },
        @{ Name = 'bad-assertion'; Message = 'checked conversion failed' },
        @{ Name = 'closure-default-tdz'; Message = 'Cannot access binding before initialization' },
        @{ Name = 'integer-call-tdz'; Message = 'Cannot access binding before initialization' },
        @{ Name = 'local-function-tdz'; Message = 'Cannot access binding before initialization' },
        @{ Name = 'closure-tdz'; Message = 'Cannot access binding before initialization' },
        @{ Name = 'closure-write-tdz'; Message = 'Cannot access binding before initialization' },
        @{ Name = 'predicate-tdz'; Message = 'Cannot access binding before initialization' },
        @{ Name = 'function-alias-tdz'; Message = 'Cannot access binding before initialization' },
        @{ Name = 'number-binding-tdz'; Message = 'Cannot access binding before initialization' },
        @{ Name = 'predicate-result'; Message = 'Function result violates number type' },
        @{ Name = 'default-module-tdz'; Message = 'Cannot access binding before initialization' },
        @{ Name = 'module-tdz'; Message = 'Cannot access binding before initialization' },
        @{ Name = 'global-tdz'; Message = 'Cannot access binding before initialization' },
        @{ Name = 'logical-assignment-tdz'; Message = 'Cannot access binding before initialization' },
        @{ Name = 'global-write-tdz'; Message = 'Cannot access binding before initialization' },
        @{ Name = 'structural-field-bad-write'; Message = 'checked conversion failed' },
        @{ Name = 'structural-reference-bad-write'; Message = 'checked conversion failed' },
        @{ Name = 'object-key-count-null'; Message = 'Cannot convert null or undefined to object' },
        @{ Name = 'object-values-null'; Message = 'Cannot convert null or undefined to object' },
        @{ Name = 'object-values-missing'; Message = 'Cannot convert null or undefined to object' },
        @{ Name = 'object-entries-null'; Message = 'Cannot convert null or undefined to object' },
        @{ Name = 'object-entries-missing'; Message = 'Cannot convert null or undefined to object' },
        @{ Name = 'object-keys-null'; Message = 'Cannot convert null or undefined to object' },
        @{ Name = 'object-keys-missing'; Message = 'Cannot convert null or undefined to object' },
        @{ Name = 'has-own-null'; Message = 'Cannot inspect properties of null or undefined' },
        @{ Name = 'bad-write'; Message = 'property write violates static field type' },
        @{ Name = 'scalar-dynamic-bad-write'; Message = 'property write violates static field type' },
        @{ Name = 'scalar-bad-write'; Message = 'checked conversion failed' },
        @{ Name = 'array-wide-bad-write'; Message = 'Array write violates number element type' },
        @{ Name = 'array-bad-write'; Message = 'Array write violates number element type' },
        @{ Name = 'array-bad-length'; Message = 'Invalid array length' },
        @{ Name = 'promise-unhandled'; Message = 'unhandled promise fixture' },
        @{ Name = 'microtask-throw'; Message = 'microtask failure fixture' },
        @{ Name = 'slice-invalid'; Message = 'Value is not callable' },
        @{ Name = 'reverse-invalid'; Message = 'Value is not callable' },
        @{ Name = 'reverse-shadow'; Message = 'Value is not callable' },
        @{ Name = 'reverse-null'; Message = 'Cannot read property of undefined' },
        @{ Name = 'reference-bad-write'; Message = 'property write violates static field type' },
        @{ Name = 'for-of-invalid'; Message = 'Value is not iterable' },
        @{ Name = 'array-spread-invalid'; Message = 'Value is not iterable' },
        @{ Name = 'call-spread-invalid'; Message = 'Value is not iterable' },
        @{ Name = 'array-destructuring-invalid'; Message = 'Value is not iterable' },
        @{ Name = 'object-destructuring-invalid'; Message = 'Cannot destructure null or undefined' },
        @{ Name = 'destructured-parameter-invalid'; Message = 'Cannot destructure null or undefined' },
        @{ Name = 'reflect-delete-fixed-alias'; Message = 'Dynamic deletion of a fixed-layout field' },
        @{ Name = 'reflect-delete-fixed-key'; Message = 'Dynamic deletion of a fixed-layout field' },
        @{ Name = 'bad-structural-cast'; Message = 'checked conversion failed' }
    )) {
        $name = $case.Name
        & "$PSScriptRoot/build.ps1" -InputFile "tests/$name.ts" -OutputDirectory "artifacts/tests/$name" -SkipCompilerBuild -SkipRuntimeBuild | Out-Host
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
        @{ Source = 'async function* generator() {}'; Error = 'unsupported lowering' },
        @{ Source = 'const n: number = "wrong";'; Error = '"code":2322' },
        @{ Source = 'const n = 1; n = 2;'; Error = '"code":2588' },
        @{ Source = 'const n: unknown = 1; console.log(n + 1);'; Error = '"code":18046' },
        @{ Source = 'function f(): number { const x = 1; }'; Error = '"code":2355' },
        @{ Source = 'function f(n: number): number { return n; } f("bad");'; Error = '"code":2345' },
        @{ Source = 'function f(n: number): number { return n; } f();'; Error = '"code":2554' },
        @{ Source = 'function f(n: number): number { return n; } f(1, 2);'; Error = '"code":2554' },
        @{ Source = 'const p = { x: 1 }; console.log(p.y);'; Error = '"code":2339' },
        @{ Source = 'const x = {__proto__: {a: 1}};'; Error = 'unsupported lowering' },
        @{ Source = 'NaN = 1;'; Error = 'unsupported lowering' },
        @{ Source = 'Infinity++;'; Error = 'unsupported lowering' },
        @{ Source = '(Infinity) += 1;'; Error = 'unsupported lowering' },
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
        $start = New-Object System.Diagnostics.ProcessStartInfo
        $start.FileName = (Get-Command dotnet).Source
        $start.Arguments = '"' + $compiler + '" "' + $inputPath + '" "' + $outputPath + '"'
        $start.UseShellExecute = $false
        $start.CreateNoWindow = $true
        $start.RedirectStandardOutput = $true
        $start.RedirectStandardError = $true
        $process = [Diagnostics.Process]::Start($start)
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        $message = $stdout.Result + $stderr.Result
        $status = $process.ExitCode
        $process.Dispose()
        if ($status -eq 0 -or !$message.Contains($case.Error)) {
            throw "Expected frontend diagnostic: $($case.Source)`n$message"
        }
        if ([IO.File]::ReadAllText($outputPath) -ne '// sentinel') { throw 'Frontend failure overwrote output' }
        $index++
    }
    Write-Host "PASS $index frontend rejection and output-preservation checks"
    & "$PSScriptRoot/test-compiler-storage.ps1"

    & dotnet build tests/inspect-writer/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Assembly writer inspector build failed' }
    & dotnet tests/inspect-writer/bin/Release/net10.0/Inspect.dll
    if ($LASTEXITCODE) { throw 'Assembly writer string checks failed' }
    & dotnet build tests/inspect-compiler/Inspect.csproj -c Release --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Compiler inspector build failed' }
    & dotnet tests/inspect-compiler/bin/Release/net10.0/Inspect.Compiler.dll
    if ($LASTEXITCODE) { throw 'Compiler storage or writer preservation checks failed' }
    Write-Host 'All tests passed.'
}
finally { Pop-Location }
