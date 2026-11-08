$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$fetchServer = $null
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
    & dotnet run --project tests/inspect-reference-casts/Inspect.csproj -c Release --no-launch-profile
    if ($LASTEXITCODE) { throw 'Reference cast checks failed' }

    & dotnet run --project tests/inspect-blob-urls/Inspect.csproj -c Release --no-launch-profile
    if ($LASTEXITCODE) { throw 'Blob URL lifetime checks failed' }

    & dotnet build tests/inspect-host-events/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Host event checks failed to build' }
    & dotnet tests/inspect-host-events/bin/Release/net10.0/Inspect.dll
    if ($LASTEXITCODE) { throw 'Host event lifetime checks failed' }

    $testRoot = "$root/artifacts/tests"
    [IO.Directory]::CreateDirectory($testRoot) | Out-Null

    & dotnet build tests/inspect-weak-ref/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'WeakRef inspector build failed' }
    & dotnet tests/inspect-weak-ref/bin/Release/net10.0/Inspect.dll
    if ($LASTEXITCODE) { throw 'WeakRef lifetime checks failed' }

    & dotnet build tests/inspect-finalization-registry/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'FinalizationRegistry inspector build failed' }
    & dotnet tests/inspect-finalization-registry/bin/Release/net10.0/Inspect.dll
    if ($LASTEXITCODE) { throw 'FinalizationRegistry lifetime checks failed' }


    & dotnet build tests/inspect-date-styles/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Date style checks failed to build' }
    & dotnet tests/inspect-date-styles/bin/Release/net10.0/Inspect.dll
    if ($LASTEXITCODE) { throw 'Native date style checks failed' }

    & dotnet build tests/inspect-date-time-format/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'DateTimeFormat checks failed to build' }
    & dotnet tests/inspect-date-time-format/bin/Release/net10.0/Inspect.dll
    if ($LASTEXITCODE) { throw 'Native DateTimeFormat checks failed' }

    & dotnet build tests/inspect-currency-accounting/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Accounting currency checks failed to build' }
    & dotnet tests/inspect-currency-accounting/bin/Release/net10.0/Inspect.dll
    if ($LASTEXITCODE) { throw 'Native accounting currency checks failed' }

    & dotnet build tests/inspect-collation/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Native collation checks failed to build' }
    & dotnet tests/inspect-collation/bin/Release/net10.0/Inspect.dll
    if ($LASTEXITCODE) { throw 'Native collation checks failed' }

    & dotnet build tests/inspect-segmenter/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Native segmenter checks failed to build' }
    & dotnet tests/inspect-segmenter/bin/Release/net10.0/Inspect.dll
    if ($LASTEXITCODE) { throw 'Native segmentation checks failed' }

    & dotnet build tests/inspect-timers/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Timer inspector build failed' }
    & dotnet tests/inspect-timers/bin/Release/net10.0/Inspect.dll
    if ($LASTEXITCODE) { throw 'Timer checks failed' }

    & dotnet $compiler tests/function-source-exact.ts "$testRoot/function-source-exact.dll"
    if ($LASTEXITCODE) { throw 'Exact function source compilation failed' }
    & dotnet build tests/inspect-function-source/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Function source inspector build failed' }
    & dotnet tests/inspect-function-source/bin/Release/net10.0/Inspect.dll "$testRoot/function-source-exact.dll" tests/function-source-exact.ts
    if ($LASTEXITCODE) { throw 'Exact function source checks failed' }

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

    & dotnet build tests/inspect-descriptor-values/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Descriptor value inspector build failed' }
    & dotnet tests/inspect-descriptor-values/bin/Release/net10.0/Inspect.dll
    if ($LASTEXITCODE) { throw 'Descriptors retained overwritten values' }


    & dotnet build tests/inspect-weak-collections/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Weak collection inspector build failed' }
    & dotnet tests/inspect-weak-collections/bin/Release/net10.0/Inspect.dll
    if ($LASTEXITCODE) { throw 'Weak collection garbage-collection checks failed' }

    & "$PSScriptRoot/build.ps1" -InputFile tests/global-bindings.ts -OutputDirectory artifacts/tests/global-bindings -SkipCompilerBuild -SkipRuntimeBuild | Out-Host
    if ($LASTEXITCODE) { throw 'Native global binding fixture failed to build' }
    & dotnet "$testRoot/global-bindings/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Native global binding checks failed' }

    & "$PSScriptRoot/build.ps1" -InputFile tests/console.ts -OutputDirectory artifacts/tests/console -SkipCompilerBuild -SkipRuntimeBuild | Out-Host
    if ($LASTEXITCODE) { throw 'Console fixture failed to build' }
    & tsc.cmd --strict --target es2022 --module commonjs --outDir "$testRoot/node/console" tests/console.ts
    if ($LASTEXITCODE) { throw 'Console reference failed to compile' }
    '{"type":"commonjs"}' | Set-Content "$testRoot/node/console/package.json"
    function Get-ConsoleOutput($Executable, $Argument) {
        $start = New-Object System.Diagnostics.ProcessStartInfo
        $start.FileName = $Executable
        $start.Arguments = '"' + $Argument + '"'
        $start.UseShellExecute = $false
        $start.CreateNoWindow = $true
        $start.RedirectStandardOutput = $true
        $start.RedirectStandardError = $true
        $process = [System.Diagnostics.Process]::Start($start)
        try {
            $output = $process.StandardOutput.ReadToEndAsync()
            $errors = $process.StandardError.ReadToEndAsync()
            $process.WaitForExit()
            if ($process.ExitCode) { throw "$Executable failed: $($errors.Result)" }
            [PSCustomObject]@{ Out = $output.Result.Replace("`r`n", "`n"); Error = $errors.Result.Replace("`r`n", "`n") }
        } finally { $process.Dispose() }
    }
    $expectedConsole = Get-ConsoleOutput node "$testRoot/node/console/console.js"
    $actualConsole = Get-ConsoleOutput dotnet "$testRoot/console/bin/Release/net10.0/App.dll"
    if ($expectedConsole.Out -cne $actualConsole.Out -or $expectedConsole.Error -cne $actualConsole.Error) { throw 'Console stdout or stderr differs from Node' }
    Write-Host 'PASS console standard output, error output, assertions, and detached calls'

    & "$PSScriptRoot/build.ps1" -InputFile tests/console-timer-calls.ts -OutputDirectory artifacts/tests/console-timer-calls -SkipCompilerBuild -SkipRuntimeBuild | Out-Host
    if ($LASTEXITCODE) { throw 'Console timer fixture failed to build' }
    & dotnet build tests/inspect-console-timers/Inspect.csproj -c Release --configfile artifacts/metadata-writer/NuGet.Config --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Console timer inspector failed to build' }
    & dotnet tests/inspect-console-timers/bin/Release/net10.0/Inspect.dll "$testRoot/console-timer-calls/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Console timer checks failed' }

    # Node retains old event flags and targets; check the DOM reset rules directly.
    & "$PSScriptRoot/build.ps1" -InputFile tests/event-initialization-reset.ts -OutputDirectory artifacts/tests/event-initialization-reset -SkipCompilerBuild -SkipRuntimeBuild | Out-Host
    if ($LASTEXITCODE) { throw 'Event initialization checks failed to build' }
    & dotnet "$testRoot/event-initialization-reset/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Event initialization reset checks failed' }

    $fetchStart = New-Object System.Diagnostics.ProcessStartInfo
    $fetchStart.FileName = 'node'
    $fetchStart.Arguments = '"' + "$root/tests/fetch-server.cjs" + '"'
    $fetchStart.UseShellExecute = $false
    $fetchStart.CreateNoWindow = $true
    $fetchStart.RedirectStandardOutput = $true
    $fetchStart.RedirectStandardError = $true
    $fetchServer = [System.Diagnostics.Process]::Start($fetchStart)
    $fetchServerErrors = $fetchServer.StandardError.ReadToEndAsync()
    $fetchPortText = $fetchServer.StandardOutput.ReadLine()
    $fetchPort = 0
    if (![int]::TryParse($fetchPortText, [ref]$fetchPort)) { throw "Fetch test server did not return a port: $fetchPortText" }
    foreach ($fetchFixture in @('fetch-local', 'fetch-integrity')) {
        $fetchSource = [IO.File]::ReadAllText("$root/tests/$fetchFixture.ts").Replace('http://127.0.0.1:0', "http://127.0.0.1:$fetchPort")
        [IO.File]::WriteAllText("$testRoot/$fetchFixture.ts", $fetchSource, $utf8)
    }

    # Differential execution checks against the actual TypeScript compiler.
    foreach ($fixture in @('artifacts/tests/fetch-local.ts', 'artifacts/tests/fetch-integrity.ts', 'tests/fetch-errors.ts', 'tests/fetch-data.ts', 'tests/blob-urls.ts', 'tests/behavior.ts', 'tests/generic-types.ts', 'tests/array-like-types.ts', 'tests/array-like-interfaces.ts', 'tests/thenable-interfaces.ts', 'tests/collection-interfaces.ts', 'tests/mapped-collections.ts', 'tests/native-structural-views.ts', 'tests/native-mapped-views.ts', 'tests/native-generic-rest.ts', 'tests/headers.ts', 'tests/web-collection-descriptors.ts', 'tests/form-data.ts', 'tests/form-data-types.ts', 'tests/form-data-bodies.ts', 'tests/form-data-url-encoded.ts', 'tests/form-data-multipart.ts', 'tests/headers-inputs.ts', 'tests/responses.ts', 'tests/response-readers.ts', 'tests/stream-property-descriptors.ts', 'tests/response-tee.ts', 'tests/response-tee-iteration.ts', 'tests/response-tee-consumption.ts', 'tests/response-errors.ts', 'tests/requests.ts', 'tests/http-property-descriptors.ts', 'tests/request-errors.ts', 'tests/request-signals.ts', 'tests/debugger.ts', 'tests/await-expressions.ts', 'tests/await-destructuring.ts', 'tests/await-switch.ts', 'tests/await-loops.ts', 'tests/await-exceptions.ts', 'tests/for-await.ts', 'tests/for-await-order.ts', 'tests/for-await-result.ts', 'tests/collections.ts', 'tests/collection-constructor-cleanup.ts', 'tests/collection-species.ts', 'tests/collection-tags.ts', 'tests/map-insert.ts', 'tests/weak-collections.ts', 'tests/weak-collection-tags.ts', 'tests/weak-symbols.ts', 'tests/weak-ref.ts', 'tests/finalization-registry.ts', 'tests/finalization-registry-construction.ts', 'tests/promise-weakref-descriptors.ts', 'tests/weak-ref-tag.ts', 'tests/binary-buffers.ts', 'tests/data-view-large-offsets.ts', 'tests/data-view-constructor-order.ts', 'tests/data-view-new-target.ts', 'tests/buffer-property-descriptors.ts', 'tests/buffer-transfer.ts', 'tests/buffer-transfer-clone.ts', 'tests/shared-buffer.ts', 'tests/growable-shared-buffers.ts', 'tests/growable-shared-views.ts', 'tests/shared-buffer-species.ts', 'tests/atomic-integers.ts', 'tests/atomic-wait.ts', 'tests/atomic-pause.ts', 'tests/atomic-wait-async.ts', 'tests/atomic-coercions.ts', 'tests/atomic-wait-conversion-order.ts', 'tests/buffer-transfer-coercions.ts', 'tests/resizable-buffers.ts', 'tests/resizable-buffer-callbacks.ts', 'tests/resizable-buffer-coercions.ts', 'tests/resizable-buffer-iterators.ts', 'tests/array-buffer-species.ts', 'tests/array-buffer-slice-mutations.ts', 'tests/typed-arrays.ts', 'tests/typed-array-tags.ts', 'tests/typed-array-internal-length.ts', 'tests/typed-array-locale-string.ts', 'tests/typed-array-fill-conversions.ts', 'tests/typed-array-with-conversions.ts', 'tests/typed-array-sort-mutations.ts', 'tests/typed-array-search-conversions.ts', 'tests/typed-array-copy-within-conversions.ts', 'tests/typed-array-constants.ts', 'tests/typed-array-hierarchy.ts', 'tests/typed-array-property-descriptors.ts', 'tests/typed-array-copy.ts', 'tests/typed-array-to-string.ts', 'tests/typed-array-set-copy.ts', 'tests/typed-array-set-length-snapshot.ts', 'tests/typed-array-set-validation.ts', 'tests/typed-array-set-large-offsets.ts', 'tests/typed-array-from-order.ts', 'tests/typed-array-constructor-order.ts', 'tests/typed-array-missing-iterator.ts', 'tests/numeric-read-paths.ts', 'tests/typed-array-loop-guards.ts', 'tests/typed-array-read-kernel.ts', 'tests/typed-array-species.ts', 'tests/typed-array-subarray-bounds.ts', 'tests/typed-array-slice-shared-alias.ts', 'tests/byte-encodings.ts', 'tests/crypto.ts', 'tests/crypto-property-descriptors.ts', 'tests/ecdsa.ts', 'tests/rsa-signatures.ts', 'tests/rsa-oaep.ts', 'tests/rsa-oaep-vectors.ts', 'tests/rsa-oaep-wrapping.ts', 'tests/rsa-pss.ts', 'tests/rsa-pss-vectors.ts', 'tests/rsa-signature-vectors.ts', 'tests/rsa-signature-errors.ts', 'tests/ecdh.ts', 'tests/ecdh-errors.ts', 'tests/ecdsa-vectors.ts', 'tests/ecdsa-errors.ts', 'tests/ecdsa-wrapping.ts', 'tests/aes-cbc.ts', 'tests/aes-ctr.ts', 'tests/aes-kw.ts', 'tests/pbkdf2.ts', 'tests/hkdf.ts', 'tests/derivation-inputs.ts', 'tests/derivation-snapshots.ts', 'tests/crypto-wrapping.ts', 'tests/aes-gcm.ts', 'tests/aes-gcm-keys.ts', 'tests/hmac.ts', 'tests/hmac-keys.ts', 'tests/hmac-jwk.ts', 'tests/structured-clone.ts', 'tests/structured-clone-error-accessors.ts', 'tests/structured-clone-property-mutations.ts', 'tests/text-encoding.ts', 'tests/text-encoding-descriptors.ts', 'tests/text-decoder-legacy.ts', 'tests/blobs.ts', 'tests/blob-property-descriptors.ts', 'tests/blob-streams.ts', 'tests/files.ts', 'tests/url-search-params.ts', 'tests/urls.ts', 'tests/url-property-descriptors.ts', 'tests/set-union.ts', 'tests/set-copy-order.ts', 'tests/set-intersection-difference.ts', 'tests/set-relations.ts', 'tests/map-group-by.ts', 'tests/object-group-by.ts', 'tests/object-from-entries.ts', 'tests/object-copy-descriptors.ts', 'tests/object-enumeration-descriptors.ts', 'tests/object-create.ts', 'tests/define-created-property.ts', 'tests/defined-property-deletion.ts', 'tests/define-dictionary-property.ts', 'tests/define-literal-property.ts', 'tests/define-property-views.ts', 'tests/define-property-modules.ts', 'tests/define-properties.ts', 'tests/reflect-define-property.ts', 'tests/prevent-extensions.ts', 'tests/object-seal-freeze.ts', 'tests/object-integrity-aliases.ts', 'tests/define-property-bound-target.ts', 'tests/descriptor-accessor-names.ts', 'tests/object-create-descriptor-order.ts', 'tests/object-literal-prototypes.ts', 'tests/object-method-super.ts', 'tests/delete.ts', 'tests/delete-super.ts', 'tests/delete-modules.ts', 'tests/await-intrinsic-calls.ts', 'tests/collections-modules.ts', 'tests/collection-iterators.ts', 'tests/iterator-from.ts', 'tests/iterator-from-disabled-string.ts', 'tests/disabled-iteration.ts', 'tests/iterator-concat.ts', 'tests/iterator-concat-prototype.ts', 'tests/iterator-disposal.ts', 'tests/iterator-native-next.ts', 'tests/iterator-terminals.ts', 'tests/iterator-invalid-arguments.ts', 'tests/iterator-lazy.ts', 'tests/iterator-property-descriptors.ts', 'tests/iterator-helper-prototype.ts', 'tests/iterator-drop-values.ts', 'tests/iterator-flat-map.ts', 'tests/iterator-flat-map-next.ts', 'tests/sequence-cleanup.ts', 'tests/sequence-cleanup-modules.ts', 'tests/await-statements.ts', 'tests/await-statements-modules.ts', 'tests/await-assignments.ts', 'tests/await-optional-chaining.ts', 'tests/await-return.ts', 'tests/await-return-modules.ts', 'tests/async-functions.ts', 'tests/timers.ts', 'tests/async-functions-modules.ts', 'tests/promises.ts', 'tests/promise-species.ts', 'tests/promise-combinators.ts', 'tests/promise-combinator-cleanup.ts', 'tests/promise-prototype-tag.ts', 'tests/promise-helpers.ts', 'tests/promise-helpers-modules.ts', 'tests/promises-modules.ts', 'tests/exceptions.ts', 'tests/stack-locations.ts', 'tests/stack-location-classes.ts', 'tests/stack-location-unicode.ts', 'tests/dom-exceptions.ts', 'tests/events.ts', 'tests/event-property-descriptors.ts', 'tests/event-initialization.ts', 'tests/abort-signals.ts', 'tests/abort-signal-subclasses.ts', 'tests/abort-listeners.ts', 'tests/abort-timeouts.ts', 'tests/abort-timeout-unused.ts', 'tests/abort-timeout-cancelled.ts', 'tests/error-prototypes.ts', 'tests/error-property-deletion.ts', 'tests/error-property-descriptors.ts', 'tests/suppressed-errors.ts', 'tests/using-declarations.ts', 'tests/await-using.ts', 'tests/disposable-stacks.ts', 'tests/disposable-stack-descriptors.ts', 'tests/async-disposable-stacks.ts', 'tests/disposable-stack-modules.ts', 'tests/await-using-modules.ts', 'tests/using-for.ts', 'tests/using-for-of.ts', 'tests/using-namespaces.ts', 'tests/namespaces.ts', 'tests/namespace-modules.ts', 'tests/namespace-merges.ts', 'tests/namespace-merge-modules.ts', 'tests/using-modules.ts', 'tests/error-is-error.ts', 'tests/enums.ts', 'tests/enum-initialization.ts', 'tests/enums-namespaces.ts', 'tests/enums-nested-namespaces.ts', 'tests/enum-import-aliases.ts', 'tests/enum-alias-only.ts', 'tests/enums-modules.ts', 'tests/enums-const-import.ts', 'tests/enums-large.ts', 'tests/globals.ts', 'tests/console-count.ts', 'tests/console-property-descriptors.ts', 'tests/performance-properties.ts', 'tests/performance-time-origin.ts', 'tests/performance-json.ts', 'tests/global-object.ts', 'tests/script-global-this.ts', 'tests/symbol-values.ts', 'tests/symbol-function-names.ts', 'tests/bigint-values.ts', 'tests/bigint-locale-formatting.ts', 'tests/bigint-operators.ts', 'tests/bigint-boundaries.ts', 'tests/bigint-integration.ts', 'tests/bigint-inferred-return.ts', 'tests/symbol-keys.ts', 'tests/symbol-to-string-tag.ts', 'tests/symbol-to-primitive.ts', 'tests/symbol-has-instance.ts', 'tests/custom-iterables.ts', 'tests/custom-async-iterables.ts', 'tests/dates.ts', 'tests/date-extended-utc.ts', 'tests/date-extended-utc-string.ts', 'tests/date-iso-parsing.ts', 'tests/date-utc-construction.ts', 'tests/date-construction-range.ts', 'tests/date-component-normalization.ts', 'tests/date-local-boundaries.ts', 'tests/date-calendar-range.ts', 'tests/date-getters.ts', 'tests/date-calendar-parts.ts', 'tests/date-setters.ts', 'tests/date-setter-coercions.ts', 'tests/date-local-overflow.ts', 'tests/date-json-null.ts', 'tests/date-formatting.ts', 'tests/date-locale-formatting.ts', 'tests/intl-date-time-format.ts', 'tests/intl-date-extensions.ts', 'tests/date-locale-extensions.ts', 'tests/date-numbering-systems.ts', 'tests/intl-segmenter.ts', 'tests/date-locale-options.ts', 'tests/date-calendar-selection.ts', 'tests/date-locale-styles.ts', 'tests/date-locale-fields.ts', 'tests/date-locale-time-fields.ts', 'tests/date-locale-time-components.ts', 'tests/date-locale-components.ts', 'tests/json-parse.ts', 'tests/json-reviver-descriptors.ts', 'tests/json-stringify.ts', 'tests/json-bigint-hooks.ts', 'tests/raw-json.ts', 'tests/raw-json-integrity.ts', 'tests/object-constructor.ts', 'tests/object-assign.ts', 'tests/object-is.ts', 'tests/object-integrity.ts', 'tests/object-is-modules.ts', 'tests/object-assign-modules.ts', 'tests/object-constructor-modules.ts', 'tests/primitive-constructors.ts', 'tests/native-constructor-conversions.ts', 'tests/native-constructor-validation.ts', 'tests/boxed-property-descriptors.ts', 'tests/boxed-number-prototypes.ts', 'tests/object-prototypes.ts', 'tests/object-prototype-queries.ts', 'tests/reflect.ts', 'tests/reflect-set.ts', 'tests/reflect-query-layout.ts', 'tests/reflect-construct.ts', 'tests/reflect-construct-typed.ts', 'tests/reflect-set-prototype.ts', 'tests/reflect-delete.ts', 'tests/reflect-delete-aliases.ts', 'tests/reflect-delete-call-forms.ts', 'tests/reflect-delete-alias-modules.ts', 'tests/reflect-delete-fixed-alias.ts', 'tests/reflect-delete-typed-key.ts', 'tests/reflect-delete-object-parameter.ts', 'tests/reflect-delete-fixed-key.ts', 'tests/reflect-delete-direct.ts', 'tests/object-value-of.ts', 'tests/object-string-methods.ts', 'tests/regular-expressions.ts', 'tests/regexp-property-descriptors.ts', 'tests/regex-named-groups.ts', 'tests/regex-capture-names.ts', 'tests/regex-modifiers.ts', 'tests/regex-compile.ts', 'tests/regex-constructor-protocol.ts', 'tests/regex-new-target.ts', 'tests/regex-identity-escapes.ts', 'tests/regex-hex-escapes.ts', 'tests/regex-k-escapes.ts', 'tests/regex-control-escapes.ts', 'tests/regex-line-terminators.ts', 'tests/regex-empty-classes.ts', 'tests/regex-whitespace.ts', 'tests/regex-class-brackets.ts', 'tests/regex-test-exec.ts', 'tests/regex-match-exec.ts', 'tests/regex-generic-match.ts', 'tests/regex-replace-exec.ts', 'tests/regex-global-validation.ts', 'tests/string-regex-validation.ts', 'tests/string-search-overrides.ts', 'tests/string-method-prototypes.ts', 'tests/string-iterator-method.ts', 'tests/array-iterator-method.ts', 'tests/arguments-iterator.ts', 'tests/shared-array-iterator-prototype.ts', 'tests/sequence-iterator-tags.ts', 'tests/collection-iterator-methods.ts', 'tests/symbol-prototype-conversion.ts', 'tests/date-prototype-conversion.ts', 'tests/date-prototype-receivers.ts', 'tests/bigint-wide-width.ts', 'tests/bigint-string-whitespace.ts', 'tests/bigint-number-rounding.ts', 'tests/bigint-prototype.ts', 'tests/string-symbol-methods.ts', 'tests/regexp-symbol-methods.ts', 'tests/string-disabled-regex-hooks.ts', 'tests/regex-string-replacement.ts', 'tests/regex-string-split.ts', 'tests/regex-split-species.ts', 'tests/regex-string-match.ts', 'tests/regex-match-all.ts', 'tests/regex-match-all-species.ts', 'tests/regex-escape.ts', 'tests/regex-dot-all.ts', 'tests/regex-indices.ts', 'tests/regex-lookbehind.ts', 'tests/native-function-text.ts', 'tests/function-source.ts', 'tests/modules.ts', 'tests/export-assignments.ts', 'tests/export-assignment-interop.ts', 'tests/default-modules.ts', 'tests/structural-fields.ts', 'tests/empty-structural-types.ts', 'tests/optional-context.ts', 'tests/in-operator.ts', 'tests/in-operator-allocation.ts', 'tests/optional-chaining.ts', 'tests/optional-chaining-modules.ts', 'tests/optional-chaining-allocation.ts', 'tests/has-own.ts', 'tests/object-keys.ts', 'tests/own-property-names.ts', 'tests/property-descriptors.ts', 'tests/lookup-accessors.ts', 'tests/define-accessors.ts', 'tests/object-enumeration.ts', 'tests/object-spread.ts', 'tests/spread-fastpath.ts', 'tests/key-layouts.ts', 'tests/object-key-count.ts', 'tests/string-index.ts', 'tests/string-search.ts', 'tests/string-search-allocation.ts', 'tests/numeric-string-index.ts', 'tests/string-boundaries.ts', 'tests/string-boundaries-modules.ts', 'tests/string-formatting.ts', 'tests/string-case.ts', 'tests/string-locale-case.ts', 'tests/string-locale-compare.ts', 'tests/intl-collator.ts', 'tests/intl-collator-numeric-locale.ts', 'tests/intl-collator-extensions.ts', 'tests/intl-collator-option-order.ts', 'tests/intl-property-descriptors.ts', 'tests/intl-locale.ts', 'tests/intl-locale-text-info.ts', 'tests/intl-locale-first-day.ts', 'tests/intl-locale-hour-cycles.ts', 'tests/intl-locale-calendars.ts', 'tests/intl-locale-extensions.ts', 'tests/intl-duplicate-keywords.ts', 'tests/intl-canonical-locales.ts', 'tests/intl-neutral-extensions.ts', 'tests/intl-locale-fallback.ts', 'tests/intl-irrelevant-extensions.ts', 'tests/collation-options.ts', 'tests/named-collations.ts', 'tests/string-locale-compare-options.ts', 'tests/intl-number-format.ts', 'tests/intl-exact-numeric-strings.ts', 'tests/number-latin-digits.ts', 'tests/number-digit-systems.ts', 'tests/number-digit-locales.ts', 'tests/number-locale-matcher.ts', 'tests/intl-percent-format.ts', 'tests/intl-currency-format.ts', 'tests/intl-currency-accounting.ts', 'tests/intl-rounding.ts', 'tests/intl-rounding-increments.ts', 'tests/intl-grouping-min2.ts', 'tests/intl-significant-digits.ts', 'tests/intl-rounding-priority.ts', 'tests/intl-exponential.ts', 'tests/number-locale-options.ts', 'tests/string-locale-sensitivity.ts', 'tests/string-locale-numeric.ts', 'tests/string-locale-punctuation.ts', 'tests/string-html.ts', 'tests/string-split.ts', 'tests/string-well-formed.ts', 'tests/string-code-points.ts', 'tests/string-raw.ts', 'tests/tagged-templates.ts', 'tests/await-tagged-templates.ts', 'tests/tagged-templates-modules.ts', 'tests/string-normalization.ts', 'tests/string-replacement.ts', 'tests/string-formatting-modules.ts', 'tests/string-ranges.ts', 'tests/string-ranges-allocation.ts', 'tests/has-own-fastpath.ts', 'tests/il-backend.ts', 'tests/frontend.ts', 'tests/functions.ts', 'tests/stack-function-names.ts', 'tests/stack-async-class-names.ts', 'tests/generators.ts', 'tests/generator-control-flow.ts', 'tests/async-generators.ts', 'tests/async-generator-control-flow.ts', 'tests/async-generators-modules.ts', 'tests/generators-modules.ts', 'tests/arguments.ts', 'tests/arguments-descriptors.ts', 'tests/arguments-modules.ts', 'tests/function-prototypes.ts', 'tests/function-property-deletion.ts', 'tests/function-property-definition.ts', 'tests/function-prototype-descriptors.ts', 'tests/native-constructor-descriptors.ts', 'tests/native-object-descriptors.ts', 'tests/native-property-deletion.ts', 'tests/function-constructors.ts', 'tests/new-target.ts', 'tests/new-target-modules.ts', 'tests/function-constructor-cold.ts', 'tests/constructor-namespace-alias.ts', 'tests/constructor-namespace-index.ts', 'tests/function-constructor-allocation.ts', 'tests/receivers.ts', 'tests/function-binding.ts', 'tests/bound-function-prototypes.ts', 'tests/bound-constructor-targets.ts', 'tests/function-bind-metadata-order.ts', 'tests/classes.ts', 'tests/decorators.ts', 'tests/legacy-decorators.ts', 'tests/decorator-private.ts', 'tests/decorator-context.ts', 'tests/decorator-order.ts', 'tests/decorators-modules.ts', 'tests/class-index-signatures.ts', 'tests/numeric-members.ts', 'tests/numeric-members-modules.ts', 'tests/class-replacement.ts', 'tests/class-field-descriptors.ts', 'tests/class-property-definition.ts', 'tests/class-static-descriptors.ts', 'tests/class-replacement-values.ts', 'tests/private-class-factories.ts', 'tests/computed-classes.ts', 'tests/class-spread.ts', 'tests/classes-modules.ts', 'tests/inheritance.ts', 'tests/function-inheritance.ts', 'tests/function-base-no-new.ts', 'tests/inheritance-modules.ts', 'tests/accessors.ts', 'tests/contextual-accessors.ts', 'tests/auto-accessors.ts', 'tests/private-members.ts', 'tests/private-members-modules.ts', 'tests/function-self.ts', 'tests/inferred-function-names.ts', 'tests/iife-captures.ts', 'tests/closures.ts', 'tests/closure-module-access.ts', 'tests/local-functions.ts', 'tests/local-function-loop.ts', 'tests/closure-loop.ts', 'tests/function-dispatch.ts', 'tests/call-arity.ts', 'tests/function-aliases.ts', 'tests/void-values.ts', 'tests/straightforward.ts', 'tests/logical-assignment.ts', 'tests/dynamic-arithmetic.ts', 'tests/nullable-values.ts', 'tests/number-fields.ts', 'tests/push-values.ts', 'tests/push-local-arrays.ts', 'tests/numeric-fastpaths.ts', 'tests/masked-remainder.ts', 'tests/number-text.ts', 'tests/numeric-constants.ts', 'tests/math.ts', 'tests/math-property-descriptors.ts', 'tests/namespace-tags.ts', 'tests/builtin-tag-descriptors.ts', 'tests/math-f16round.ts', 'tests/math-sum-precise.ts', 'tests/math-modules.ts', 'tests/number-predicates.ts', 'tests/number-parsing.ts', 'tests/number-fixed.ts', 'tests/number-radix.ts', 'tests/number-locale-formatting.ts', 'tests/number-locale-rounding.ts', 'tests/number-sign-display.ts', 'tests/number-integer-digits.ts', 'tests/number-precision.ts', 'tests/global-number-predicates.ts', 'tests/uri-encoding.ts', 'tests/base64.ts', 'tests/legacy-escapes.ts', 'tests/number-bindings.ts', 'tests/number-destructuring.ts', 'tests/bitwise.ts', 'tests/exponentiation.ts', 'tests/number-strings.ts', 'tests/object-number.ts', 'tests/primitive-method-lookup.ts', 'tests/relational.ts', 'tests/equality.ts', 'tests/characters.ts', 'tests/characters-allocation.ts', 'tests/at.ts', 'tests/at-allocation.ts', 'tests/scalar-objects.ts', 'tests/scalar-dynamic.ts', 'tests/scalar-references.ts', 'tests/integer-loops.ts', 'tests/integer-call-loops.ts', 'tests/array-constructor.ts', 'tests/array-from.ts', 'tests/array-from-order.ts', 'tests/array-from-result-properties.ts', 'tests/array-factory-inherited-properties.ts', 'tests/array-from-disabled-iterator.ts', 'tests/array-from-missing-iterator.ts', 'tests/array-property-assignment.ts', 'tests/array-inherited-setters.ts', 'tests/array-inherited-in.ts', 'tests/array-method-membership.ts', 'tests/array-from-async.ts', 'tests/async-iterator-next.ts', 'tests/array-fill.ts', 'tests/array-mutation-prototypes.ts', 'tests/array-copy-within.ts', 'tests/array-copy-within-modules.ts', 'tests/array-fill-modules.ts', 'tests/array-from-modules.ts', 'tests/array-constructor-modules.ts', 'tests/array-prototype-alias.ts', 'tests/array-unscopables.ts', 'tests/arrays.ts', 'tests/array-length-descriptors.ts', 'tests/array-property-descriptors.ts', 'tests/array-element-descriptors.ts', 'tests/array-integrity.ts', 'tests/array-join.ts', 'tests/array-callbacks.ts', 'tests/array-species.ts', 'tests/array-species-inherited-properties.ts', 'tests/array-callback-modules.ts', 'tests/array-reduce.ts', 'tests/array-reduce-modules.ts', 'tests/array-find.ts', 'tests/array-find-modules.ts', 'tests/array-to-sorted.ts', 'tests/array-sort.ts', 'tests/array-shift.ts', 'tests/array-splice.ts', 'tests/array-concat.ts', 'tests/concat-spreadable.ts', 'tests/array-iterators.ts', 'tests/array-push-pop-generic.ts', 'tests/array-push-inherited.ts', 'tests/array-slice-generic.ts', 'tests/array-reverse-generic.ts', 'tests/array-search-generic.ts', 'tests/array-locale-formatting.ts', 'tests/array-to-sorted-modules.ts', 'tests/array-to-spliced.ts', 'tests/array-to-spliced-modules.ts', 'tests/array-copy.ts', 'tests/array-copy-modules.ts', 'tests/array-last-index.ts', 'tests/array-flatten.ts', 'tests/array-flatten-modules.ts', 'tests/mixed-index.ts', 'tests/union-arrays.ts', 'tests/loop-accumulators.ts', 'tests/computed-index.ts', 'tests/computed-properties.ts', 'tests/key-coercion-effects.ts', 'tests/dense-read.ts', 'tests/typeof-number.ts', 'tests/slice.ts', 'tests/reverse.ts', 'tests/array-search.ts', 'tests/packed-arrays.ts', 'tests/array-ranges.ts', 'tests/inferred-arrays.ts', 'tests/fresh-any-arrays.ts', 'tests/for-of.ts', 'tests/for-in.ts', 'tests/for-in-hidden.ts', 'tests/array-spread.ts', 'tests/array-destructuring.ts', 'tests/destructuring-assignment.ts', 'tests/destructuring-holes.ts', 'tests/empty-destructuring.ts', 'tests/destructuring-loop-targets.ts', 'tests/destructuring-assignment-order.ts', 'tests/object-destructuring.ts', 'tests/object-rest.ts', 'tests/object-rest-descriptors.ts', 'tests/destructuring-fastpath.ts', 'tests/destructured-parameters.ts', 'tests/rest-parameters.ts', 'tests/call-spread.ts', 'examples/structural.ts', 'examples/numeric.ts', 'artifacts/tests/boundaries.ts')) {
        $name = [IO.Path]::GetFileNameWithoutExtension($fixture)
        $legacyDecorators = $name -eq 'legacy-decorators'
        & "$PSScriptRoot/build.ps1" -InputFile $fixture -OutputDirectory "artifacts/tests/$name" -SkipCompilerBuild -SkipRuntimeBuild -ExperimentalDecorators:$legacyDecorators | Out-Host
        $moduleKind = if ($name -eq 'delete-modules') { 'es2022' } else { 'commonjs' }
        $referenceOptions = @()
        if ($name.StartsWith('stack-location')) { $referenceOptions += '--sourceMap' }
        if ($legacyDecorators) { $referenceOptions += '--experimentalDecorators' }
        if ($name -eq 'export-assignment-interop') { $referenceOptions += '--esModuleInterop' }
        $referenceTarget = if ($name -eq 'regex-modifiers') { 'esnext' } elseif ($name.StartsWith('resizable-buffer') -or $name.StartsWith('buffer-transfer') -or $name.StartsWith('shared-buffer') -or $name.StartsWith('growable-shared-') -or $name.StartsWith('atomic-') -or $name -eq 'hmac-keys' -or $name -eq 'numeric-read-paths' -or $name -eq 'typed-array-loop-guards') { 'es2024' } else { 'es2022' }
        & tsc.cmd @referenceOptions --strict --target $referenceTarget --module $moduleKind --outDir "$testRoot/node/$name" $fixture
        if ($moduleKind -eq 'es2022') { '{"type":"module"}' | Set-Content "$testRoot/node/$name/package.json" }
        else { '{"type":"commonjs"}' | Set-Content "$testRoot/node/$name/package.json" }
        if ($LASTEXITCODE) { throw "Reference TypeScript compilation failed: $name" }
        $reference = "$testRoot/node/$name/$name.js"
        $expected = if ($name.StartsWith('stack-location')) { @(& node --enable-source-maps $reference) } elseif ($name -eq 'script-global-this') { @(& node tests/run-script.cjs $reference) } else { @(& node $reference) }
        if ($LASTEXITCODE) { throw "Node failed: $name" }
        $actual = @(& dotnet "$testRoot/$name/bin/Release/net10.0/App.dll")
        if ($LASTEXITCODE) { throw ".NET failed: $name" }
        if (($expected -join "`n") -cne ($actual -join "`n")) {
            throw "Output mismatch for $name`nExpected: $expected`nActual: $actual"
        }
        Write-Host "PASS differential: $name"
        if ($name -eq 'fetch-integrity') {
            $fetchServer.Kill()
            $fetchServer.WaitForExit()
            $fetchServer.Dispose()
            $fetchServer = $null
        }
    }

    & dotnet build tests/inspect-source-locations/Inspect.csproj -c Release --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Source location inspector build failed' }
    & dotnet tests/inspect-source-locations/bin/Release/net10.0/Inspect.dll "$testRoot/stack-locations/bin/Release/net10.0/App.dll" "$testRoot/stack-location-classes/bin/Release/net10.0/App.dll" "$testRoot/stack-location-unicode/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Embedded TypeScript source location checks failed' }

    & dotnet build tests/inspect-buffer-transfer/Inspect.csproj -c Release --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Buffer transfer inspector build failed' }
    & dotnet tests/inspect-buffer-transfer/bin/Release/net10.0/Inspect.dll
    if ($LASTEXITCODE) { throw 'Buffer transfer storage checks failed' }

    & dotnet build tests/inspect-function-names/Inspect.csproj -c Release --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Function name inspector build failed' }
    & dotnet tests/inspect-function-names/bin/Release/net10.0/Inspect.dll "$testRoot/stack-function-names/bin/Release/net10.0/App.dll" "$testRoot/stack-async-class-names/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Readable function name checks failed' }

    & dotnet build tests/inspect-events/Inspect.csproj -c Release --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Event inspector build failed' }
    & dotnet tests/inspect-events/bin/Release/net10.0/Inspect.dll
    if ($LASTEXITCODE) { throw 'Event exception and dispatch checks failed' }

    & dotnet build tests/inspect-atomics/Inspect.csproj -c Release --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Atomics checks failed to build' }
    & dotnet tests/inspect-atomics/bin/Release/net10.0/Inspect.dll
    if ($LASTEXITCODE) { throw 'Atomics concurrency checks failed' }

    & dotnet build tests/inspect-abort-signals/Inspect.csproj -c Release --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Abort signal inspector build failed' }
    & dotnet tests/inspect-abort-signals/bin/Release/net10.0/Inspect.dll
    if ($LASTEXITCODE) { throw 'Abort signal lifecycle checks failed' }

    # Namespace semantics need native ES modules as the reference.
    foreach ($name in @('object-integrity-modules', 'global-this-modules', 'timers-modules', 'json-modules', 'decorators-modules', 'async-generators-modules', 'generators-modules', 'await-class-definitions', 'await-class-context', 'top-level-await', 'top-level-await-cycle', 'top-level-await-siblings', 'top-level-await-errors', 'top-level-await-var', 'dynamic-import', 'dynamic-import-bindings', 'disposable-stack-modules', 'await-using-modules', 'namespace-merge-modules', 'namespace-modules', 'using-modules', 'import-meta', 'module-this', 'tagged-templates-modules', 'reflect-delete-modules', 'property-descriptors-modules', 'for-in-modules', 'for-in-cycle', 'module-namespace-order', 'module-namespace-definition', 'in-operator-modules', 'function-constructor-modules')) {
        & "$PSScriptRoot/build.ps1" -InputFile "tests/$name.ts" -OutputDirectory "artifacts/tests/$name" -SkipCompilerBuild -SkipRuntimeBuild | Out-Host
        if ($name -eq 'json-modules') {
            & tsc.cmd --strict --target es2022 --module esnext --moduleResolution bundler --resolveJsonModule --esModuleInterop --outDir "$testRoot/node/$name" "tests/$name.ts"
        } else {
            & tsc.cmd --strict --target es2022 --module es2022 --outDir "$testRoot/node/$name" "tests/$name.ts"
        }
        if ($LASTEXITCODE) { throw "ES module reference compilation failed: $name" }
        '{"type":"module"}' | Set-Content "$testRoot/node/$name/package.json" -Encoding utf8
        $expected = @(& node "$testRoot/node/$name/$name.js")
        if ($LASTEXITCODE) { throw "ES module reference failed: $name" }
        $actual = @(& dotnet "$testRoot/$name/bin/Release/net10.0/App.dll")
        if ($LASTEXITCODE) { throw "Module namespace application failed: $name" }
        if (($expected -join "`n") -cne ($actual -join "`n")) { throw "ES module output mismatch: $name" }
        Write-Host "PASS ES module differential: $name"
    }

    & dotnet build tests/inspect-typed-array-reads/Inspect.csproj -c Release --nologo -v quiet
    if ($LASTEXITCODE) { throw 'Typed-array allocation inspector failed to build' }
    & dotnet tests/inspect-typed-array-reads/bin/Release/net10.0/Inspect.dll "$testRoot/typed-array-read-kernel/bin/Release/net10.0/App.dll"
    if ($LASTEXITCODE) { throw 'Typed-array reads allocated or returned an incorrect result' }

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
        @{ Name = 'string-locale-custom-collation'; Message = 'Custom collations are not supported' },
        @{ Name = 'date-locale-calendar'; Message = 'Date locale formatting option calendar is not supported' },
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
        @{ Name = 'top-level-await-pending'; Message = 'Module initialization did not complete' },
        @{ Name = 'top-level-await-reject'; Message = 'top-level failure' },
        @{ Name = 'bigint-number-return'; Message = 'checked conversion failed' },
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
        @{ Source = 'const n: number = "wrong";'; Error = '"code":2322' },
        @{ Source = 'const n = 1; n = 2;'; Error = '"code":2588' },
        @{ Source = 'const n: unknown = 1; console.log(n + 1);'; Error = '"code":18046' },
        @{ Source = 'function f(): number { const x = 1; }'; Error = '"code":2355' },
        @{ Source = 'function f(n: number): number { return n; } f("bad");'; Error = '"code":2345' },
        @{ Source = 'function f(n: number): number { return n; } f();'; Error = '"code":2554' },
        @{ Source = 'function f(n: number): number { return n; } f(1, 2);'; Error = '"code":2554' },
        @{ Source = 'const p = { x: 1 }; console.log(p.y);'; Error = '"code":2339' },
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
finally {
    if ($fetchServer -ne $null) {
        if (!$fetchServer.HasExited) { $fetchServer.Kill(); $fetchServer.WaitForExit() }
        $fetchServer.Dispose()
    }
    Pop-Location
}
