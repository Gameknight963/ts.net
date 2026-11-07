# ts.net

Work-in-progress TypesScript to IL compiler.


## Building

Reqires .NET 10 SDK, Go 1.27 or newer, and Git.

The first build downloads the pinned Microsoft TypeScript frontend source.

Then run `.\scripts\build.ps1 -Run` to compile and run an example.

## Current feature completion

84%.

<details>

| Feature | Points | Current support |
|---|---|---|
| Values and operators | 9 / 10 | Numbers, strings, booleans, null, undefined, arithmetic, comparisons, bitwise operators, optional chaining, in and delete. <br> Symbol values, descriptions, property keys, well-known symbol identities and the global symbol registry are supported. Symbol keys work with copying, descriptors and reflection, and are excluded from string-key enumeration and JSON. Object.prototype.toString honors custom Symbol.toStringTag values and getters. Symbol.toPrimitive controls object conversion with string, number and default hints. Custom Symbol.hasInstance methods control instanceof, with the ordinary function check available for delegation. BigInt literals and construction use CLR BigInteger, with boxing, comparisons, arithmetic, bitwise operators, compound assignments, increment/decrement, radix and native locale formatting, and asIntN/asUintN (bit counts must fit a signed 32-bit integer). Remaining symbol protocols and some operators are missing. |
| Control flow | 10 / 10 | Conditions, loops, for-of, for-in (including inherited enumerable keys), switch, labels, break, continue and debugger statements. Synchronous using declarations clean up block and module resources through Symbol.dispose, including in async functions. Traditional for initializers and for-of bindings also support using, including for-await-of loops; await using and using declarations in namespaces remain unsupported. |
| Functions | 9 / 10 | Closures, strict arguments objects (including arrow captures and iteration), callbacks, overloads, default/optional/rest parameters, destructuring, function/method receivers, arrow this capture, ordinary function constructors, new.target (including arrow captures), instanceof and call/apply/bind. Function.toString uses native-style text rather than preserving user source. Tagged templates support cached, read-only cooked/raw strings arrays and method receivers. Top-level this is undefined in ES modules, including lexical arrow captures; script-global this and generators are missing. |
| Objects and arrays | 9 / 10 | Structural objects, methods, getters/setters, computed properties, spreads, destructuring, object rest bindings and sparse arrays. General prototype behavior, including function and array intrinsic prototypes, remains incomplete. |
| Types and generics | 5 / 10 | Microsoft's type checking, interfaces, aliases, basic generics and union/nullable storage. Runtime representations still have gaps. |
| Modules | 4 / 5 | Imports, exports, default exports, namespace values, live bindings and multiple files. import.meta provides a cached, mutable per-module object with the original TypeScript source file URL; host-specific properties and resolve are unsupported. Dynamic import is missing. |
| Enums | 5 / 5 | Numeric, string, mixed, computed and const enums; reverse mappings, merged enums, and imports. Namespace merging supports nested/dotted namespaces, exported destructuring, functions, classes and enums; __proto__ members follow object prototype assignment. Import aliases (`import Alias = E.Nested`) retain their original namespace object. |
| Classes and inheritance | 14 / 15 | Constructors, fields, methods, static blocks, parameter properties, class index signatures, class expressions, numeric and computed member names, generics, structural interfaces, inheritance (including ordinary and bound constructor functions), super, getters/setters, auto-accessors and #private fields/methods/accessors with brand checks. Object, Number, String, Boolean, Array, Promise, Map, Set, Error, native error, AggregateError and SuppressedError base classes are supported; other built-in base classes and decorators are missing. Some generated auto-accessor initializer names differ from tsc; base constructors returning replacement objects still have gaps. |
| Exceptions | 9 / 10 | throw, try/catch/finally, standard error objects, cause, and instanceof. Error prototypes, prototype mutation and custom error subclasses are supported. TypeScript source locations in stack traces are missing. |
| Async | 8 / 10 | Promise construction, resolve/reject/withResolvers/try, all/allSettled/race/any over supported iterables (arrays, strings, arguments, Map/Set, their iterators and custom Symbol.iterator iterables), then/catch/finally, thenable assimilation, subclasses, queueMicrotask and async declarations/expressions/arrows/methods, and await in return expressions (operators, assignments, optional chains, calls, literals and constructors). Await statements, local initializers, destructuring defaults/computed keys (including nested patterns and rest), blocks, if branches, switch statements (including case expressions and fallthrough), labeled blocks, while/do/for/for-of/for-in loops, and try/catch/finally are supported. For-await-of supports arrays, strings, arguments, Map/Set, their iterators and custom synchronous/asynchronous iterables, including promise/thenable values and awaited cleanup on early exit. Custom Symbol.asyncIterator iterables are also supported by Array.fromAsync. Generators and custom promise species are missing. |
| Standard library | 3 / 5 | Map/Set construction, Map.groupBy and Object.groupBy (supported iterables and string/symbol property keys), get/set/add/has/delete/clear/size, live forEach/keys/values/entries, collection subclasses, and iteration through loops, spreads, destructuring, Array.from and Promise combinators. Set union/intersection/difference/symmetricDifference and isSubsetOf/isSupersetOf/isDisjointFrom accept Sets, Maps and custom set-like objects. WeakRef supports object and non-registered symbol targets, deref, subclasses and per-job target retention using .NET weak references; collection timing follows the CLR. WeakMap/WeakSet support object and non-registered symbol keys, construction, subclasses, get/set/add/has/delete and weak key lifetimes. JSON.parse supports revivers and context.source. JSON.stringify supports replacers, property lists, indentation, toJSON and cycle checks. JSON.rawJSON and JSON.isRawJSON support validated primitive text and raw serialization. Reflect.apply/construct/get/set/has/ownKeys/getPrototypeOf/setPrototypeOf/getOwnPropertyDescriptor/deleteProperty are supported for string and symbol property keys; construct supports alternate new.target. Reflect deletion of fixed-layout fields requires a direct literal-key call; aliases and computed keys require dictionary-backed objects. Remaining Reflect methods are missing. RegExp.escape, literals and construction, test/exec, lookbehind, captures, lastIndex, string match/search/matchAll (lazy iteration), split (including captures), replace/replaceAll (including captures and callbacks), and flags d/g/i/m/s/y (including capture indices) use .NET?s ECMAScript regex mode. Flags u/v, named groups, custom exec overrides are unsupported; Unicode and newline edge cases can differ from JavaScript. Date supports timestamp/string/calendar-part construction, copying, now/parse/UTC, getTime/setTime/valueOf, UTC/local component getters/setters and timezone offsets, ISO/UTC/JSON output and subclasses. Date parsing uses .NET invariant parsing; Date(), toString, toDateString and toTimeString use local time and platform time-zone names. Date toLocaleString/toLocaleDateString/toLocaleTimeString use .NET culture formats rather than Intl patterns; their formatting options are unsupported. Calling toJSON directly on an invalid Date requires an any cast because the TypeScript library declares a string return. Calendar construction uses .NET?s year range 1?9999. Time-zone results use platform data, and calendar formatting outside years 1?9999 is rejected. Error.isError checks native error instances, including subclasses. Iterator.from wraps iterator-like objects and supports arrays, strings, collections and native iterators. Iterator subclasses work with loops, spreads, Array.from and Array.fromAsync. Iterator toArray/forEach/reduce/some/every/find consume values and close on early exit or callback failure. Iterator map/filter/flatMap/take/drop are lazy and forward cleanup through chains; flatMap accepts supported iterables and iterator-like objects. Symbol property keys and well-known-symbol protocols remain missing. Object/Number/String/Boolean/Array construction, Number.prototype.toString supports radix 10, all finite numbers in radices 2/4/8/16/32, and safe integers in other radices 2?36. Number.prototype.toFixed/toExponential/toPrecision and toLocaleString (native .NET decimal formatting with locale selection, grouping and fraction-digit options; native rounding/patterns can differ from Intl and other options are unsupported), Array.isArray/of/from (supported iterables and array-like objects), Array.fromAsync (supported sync/async iterables and array-like objects, serial consumption and mapper awaiting, subclasses and rejection cleanup), prototype get/set, Math constants and common numeric/trigonometric functions including f16round, selected Number/Object helpers, parseInt/parseFloat (global and Number methods), global isNaN/isFinite, encodeURI/encodeURIComponent/decodeURI/decodeURIComponent, legacy escape/unescape, Object.prototype.toString/toLocaleString/valueOf/hasOwnProperty/isPrototypeOf/propertyIsEnumerable/__lookupGetter__/__lookupSetter__, Object.getOwnPropertyNames/getOwnPropertyDescriptor/getOwnPropertyDescriptors, Object.is, Object.assign, Object.create (including data/accessor descriptors), Object.fromEntries (supported iterables and string/symbol keys), String.fromCharCode/fromCodePoint, String.raw (direct and tagged calls), localeCompare using .NET cultures with numeric ordering and base/accent/case/variant sensitivity (other comparison options are unsupported and platform collation can differ), toLowerCase/toUpperCase using .NET invariant casing and toLocaleLowerCase/toLocaleUpperCase using .NET culture names or locale lists (locale validation, Unicode expansions and contextual mappings can differ from JavaScript), string formatting and legacy HTML methods, normalize (NFC/NFD/NFKC/NFKD), isWellFormed/toWellFormed, split/replace/replaceAll with string patterns, prefix/suffix checks, concatenation and array/string methods, including element-wise toLocaleString (forwarding locale/options to native element formatters), generic join, map, filter, forEach, every, some, reduce, reduceRight, find, findIndex, findLast, findLastIndex, flat, flatMap, lastIndexOf, fill, copyWithin, sort, shift, unshift, splice, concat (including Symbol.isConcatSpreadable), generic push/pop/slice/reverse/indexOf/includes, keys/values/entries, toReversed, toSorted, toSpliced and with. Calling fill or copyWithin on a non-array currently requires casting the function to `any`; some Object.assign calls involving primitives or arrays also need this. Custom array species and many built-ins are missing. |

</details>

## Current performance

As of [9c5d2ef](https://github.com/Gameknight963/ts.net/commit/9c5d2efb1f0ece3314753f3a44fb1ea74aa770b9):

Runtime performance:

<details>
<summary>Click to expand large table</summary>

| Workload | ts.net | Node | Result |
|---|---|---|---|
| Numeric loop, 20M iterations | 14.44 ms | 74.71 ms | 5.2x faster |
| Typed-object loop, 500K iterations | 0.38 ms | 0.66 ms | 1.8x faster |
| `any` property loop, 350K iterations | 0.27 ms | 0.43 ms | 1.6x faster |
| Mixed array, 100K elements and 5 traversals | 1.05 ms | 1.16 ms | 10% faster |
| Numeric `any[]`, 1M iterations | 0.81 ms | 1.66 ms | 2.1x faster |
| Numeric array, 100K elements and 100 reversals | 2.07 ms | 7.98 ms | 3.9x faster |
| Mixed array, 100K elements and 100 reversals | 1.06 ms | 7.74 ms | 7.3x faster |
| Numeric callback, 2M calls (experimental) | 1.42 ms | 0.83 ms | 1.7x slower |
| Captured numeric callback, 2M calls | 5.24 ms | 5.10 ms | 3% slower |
| Local function, 2M direct calls | 0.47 ms | 0.83 ms | 1.8x faster |
| Self-returning function, 2M calls | 5.04 ms | 0.77 ms | 6.5x slower |
| `Object.keys(value).length`, 100K calls | 0.071 ms | 0.949 ms | 13.4x faster |
| `Object.keys(value)`, 100K calls and key reads | 1.30 ms | 1.12 ms | 1.2x slower |
| Local object spread, 100K two-field copies | 0.09 ms | 1.00 ms | 10.6x faster |
| Object spread, 100K copies stored in an array | 3.17 ms | 2.41 ms | 1.3x slower |
| Computed keys, 100K objects and reads | 3.85 ms | 26.06 ms | 6.8x faster |
| `Object.values(value)`, 100K calls and reads | 1.44 ms | 9.41 ms | 6.5x faster |
| `Object.entries(value)`, 100K calls and reads | 5.53 ms | 16.69 ms | 3.0x faster |
| Literal squares, 2M iterations | 1.40 ms | 1.42 ms | 1% faster |
| Small integer bases and varying exponents, 1M iterations | 4.04 ms | 13.72 ms | 3.4x faster |
| Mixed numeric strings, 1M conversions | 27.14 ms | 29.88 ms | 9% faster |
| Object valueOf, 1M numeric conversions | 14.44 ms | 23.51 ms | 1.6x faster |
| String ordering, 2M comparisons | 8.85 ms | 8.75 ms | 1% slower |
| `==` on mixed values, 1M comparisons | 5.15 ms | 7.26 ms | 1.4x faster |
| `== null` on nullable arrays, 1M comparisons | 1.51 ms | 1.15 ms | 1.3x slower |
| Numeric array `.at(-1)`, 1M calls | 3.84 ms | 3.82 ms | About equal |
| String `.at(index)`, 1M calls | 4.26 ms | 5.28 ms | 1.2x faster |
| String `charCodeAt(index)`, 1M calls | 2.27 ms | 1.66 ms | 1.4x slower |
| String `codePointAt(index)`, 1M calls | 3.00 ms | 1.90 ms | 1.6x slower |
| Masked integer remainder, 1M iterations | 1.10 ms | 1.11 ms | 1% faster |
| String slice and first-character read, 1M calls | 14.55 ms | 8.98 ms | 1.6x slower |
| String substring and first-character read, 1M calls | 14.27 ms | 10.75 ms | 1.3x slower |
| Numeric string index and comparison, 1M reads | 4.86 ms | 1.93 ms | 2.5x slower |
| String indexOf, 1M searches | 10.21 ms | 17.82 ms | 1.7x faster |
| String includes, 1M searches | 11.19 ms | 28.84 ms | 2.6x faster |
| String lastIndexOf, 1M searches | 10.07 ms | 19.54 ms | 1.9x faster |

</details>

Array and object methods, measured on 2026-11-06:

| Workload | ts.net | Node |
|---|---|---|
| join, 20 joins of 10K numbers | 26.07 ms | 2.36 ms |
| map, filter and sum, 10 passes over 10K numbers | 22.71 ms | 2.12 ms |
| reduce and reduceRight, 10 pairs over 10K numbers | 6.75 ms | 1.25 ms |
| from, 20 copies of 10K packed numbers | 1.14 ms | 0.36 ms |
| copyWithin, 1K copies of 5K entries in a 10K packed array | 0.84 ms | 268.92 ms |
| Object.assign, create 1K objects with 3 fields | 0.59 ms | 0.06 ms |

Execution time only; source input creation, startup and compilation are excluded. Output allocation is included. Release builds, five warmup passes, and five timed samples in each of three fresh processes; the table reports the median of 15 samples. Results matched Node, and .NET used `DOTNET_TieredCompilation=0`. The copyWithin workload changes the first input value before every copy and checks its copied value afterward.

Map, measured on 2026-11-06:

| Workload | ts.net | Node |
|---|---|---|
| Create a Map, insert and read 10K entries, then delete half | 4.38 ms | 0.80 ms |

Includes construction and allocation, excluding startup and compilation. The same warmup, fresh-process counts and .NET settings apply; all samples returned the checksum 50,010,000.

String splitting, measured on 2026-11-06:

| Workload | ts.net | Node |
|---|---|---|
| Split 10K comma-separated strings, keep three parts, and sum lengths | 4.20 ms | 2.11 ms |

Two four-field input strings alternate between iterations. Includes output arrays and substring allocation, excluding startup and compilation; inputs are prepared before timing. The same warmup, fresh-process counts and .NET settings apply, and every sample returned 120,000. Source and raw results are under `artifacts/string-split-benchmark`.

Read-only reflection, measured on 2026-11-06:

| Workload | Before | After | Node |
|---|---|---|---|
| Create/read a two-field object in a 100K-iteration loop, with a read-only Reflect alias in the program | 8.14 ms | 0.077 ms | 0.105 ms |

Execution time only, with the same warmup, process counts and .NET settings as above; all samples returned 10,000,000,000. Removing the program-wide dynamic-storage fallback removed object construction from the loop's IL, and managed allocation fell from 19,200,024 to 24 bytes including the reflection invocation used to measure allocation. That was a substantial penalty for an unused deletion capability; dynamic deletion now requires dictionary-backed objects instead. Raw measurements and IL/allocation inspection are under `artifacts/reflect-layout-before`, `artifacts/reflect-layout-after` and `artifacts/reflect-layout-il.log`.

JSON parsing and serialization, measured on 2026-11-06:

| Workload | ts.net | Node |
|---|---|---|
| Parse 1,000 three-field records 20 times, without a reviver | 9.32 ms | 4.46 ms |
| Serialize 1,000 three-field records 20 times, without a replacer | 20.44 ms | 1.59 ms |

Input creation, startup and compilation are excluded; parsed objects, arrays and serialized output strings are included. The same warmup, fresh-process counts and .NET settings apply. Parsing returned 20,100 and serialization returned 760,020 in every sample. Raw results are under `artifacts/json-parse-benchmark` and `artifacts/json-stringify-benchmark`.

Promises, measured on 2026-11-06:

| Workload | ts.net | Node |
|---|---|---|
| Construct and run a chain of 10K handlers | 11.66 ms | 0.97 ms |
| Create 10K resolved promises, collect with all, and sum results | 39.11 ms | 1.22 ms |
| Run 1K async calls with two awaits each, collect with all, and sum results | 3.30 ms | 0.37 ms |
| Sum 1K values in a loop, awaiting each value | 0.384 ms | 0.047 ms |
| Sum 1K values with an await in try and an increment in finally each iteration | 0.860 ms | 0.092 ms |
| Traverse 1K array values with for-await-of and sum them | 0.737 ms | 0.126 ms |
| Sum 100K values in a loop, awaiting only on the final iteration | 7.88 ms | 0.53 ms |

These include promise/chain construction and all queued handlers, excluding startup and compilation. The for-await input array is prepared before timing. Warmup, fresh-process counts and .NET settings match the array-method measurements above; every chain returned 10,000, the 10K-promise result summed to 49,995,000, and the async-call result summed to 500,500. The plain and for-await loop checksums were 499,500; the try/finally and rare-await loops returned 500,500 and 4,999,950,000 respectively.

Regression check after BigInt support, measured on 2026-11-07 ([38760ba](https://github.com/Gameknight963/ts.net/commit/38760ba39d9647602c255fbffaa3b01a14ae7d54) against [b8e2f28](https://github.com/Gameknight963/ts.net/commit/b8e2f28aed78af458e7ec080fea08dba7c989f73), before BigInt support):

| Workload | Before BigInt | Current | Node |
|---|---|---|---|
| Numeric loop, 20M iterations | 19.20 ms | 18.96 ms | 74.79 ms |
| Typed-object loop, 500K iterations | 0.52 ms | 0.35 ms | 0.65 ms |
| `any` property loop, 350K iterations | 0.25 ms | 0.25 ms | 0.43 ms |
| Object valueOf, 1M numeric conversions | 120.54 ms | 121.66 ms | 23.46 ms |

The same source ran in five fresh processes per engine, with at least 100 warmup calls and 500 ms of warmup; these are medians of 35 timed kernel samples. Startup and compilation are excluded, results matched, and .NET used `DOTNET_TieredCompilation=0`. Raw timings and checksums are under `artifacts/benchmarks/bigint-guard-after-2026-11-07`; allocation measurements are under `artifacts/bigint-dispatch-allocation`.

The object-conversion loop returned near its pre-BigInt time and now allocates 48 bytes per iteration, down from 192 during the regression. It is still about five times slower than Node here.

Compilation times, measured on 2026-11-07 (medians of seven fresh compiler processes with warm filesystem caches):

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 519.18 ms | 340.84 ms |
| 5,000 variables | 726.64 ms | 388.73 ms |

The packed copyWithin result is encouraging; [V8?s published per-property implementation](https://chromium.googlesource.com/v8/v8/+/refs/heads/main/src/builtins/array-copywithin.tq) is consistent with the large gap against our guarded bulk copy. Split and JSON parsing are about twice as slow as Node; serialization is about thirteen times slower, and Map, joins, callbacks, object copies and Promise workloads also need substantial work. These microbenchmarks do not establish overall application performance.

The compiler used to be written in [crust](https://github.com/Gameknight963/crust), my C-like memory safe language. It was ported to C# for various reasons. The point is, compilation is slower after the C# port. The larger source takes 1.9x as long as Microsoft’s Go-based TypeScript compiler. It used to be more or less on par.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11, TypeScript 7.0.2
