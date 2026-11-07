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
| Values and operators | 8 / 10 | Numbers, strings, booleans, null, undefined, arithmetic, comparisons, bitwise operators, optional chaining and in. <br> BigInt, symbols and some operators are missing. |
| Control flow | 10 / 10 | Conditions, loops, for-of, for-in (including inherited enumerable keys), switch, labels, break and continue. |
| Functions | 9 / 10 | Closures, strict arguments objects (including arrow captures and iteration), callbacks, overloads, default/optional/rest parameters, destructuring, function/method receivers, arrow this capture, ordinary function constructors, new.target (including arrow captures), instanceof and call/apply/bind. Top-level this and generators are missing. |
| Objects and arrays | 9 / 10 | Structural objects, methods, getters/setters, computed properties, spreads, destructuring, object rest bindings and sparse arrays. General prototype behavior, including function and array intrinsic prototypes, remains incomplete. |
| Types and generics | 5 / 10 | Microsoft's type checking, interfaces, aliases, basic generics and union/nullable storage. Runtime representations still have gaps. |
| Modules | 4 / 5 | Imports, exports, default exports, namespace values, live bindings and multiple files. Dynamic import is missing. |
| Enums | 5 / 5 | Numeric, string, mixed, computed and const enums; reverse mappings, merged enums, and imports. Namespace merging supports nested/dotted namespaces, exported destructuring, functions, classes and enums; __proto__ members follow object prototype assignment. Import aliases (`import Alias = E.Nested`) retain their original namespace object. |
| Classes and inheritance | 14 / 15 | Constructors, fields, methods, static blocks, parameter properties, class expressions, numeric and computed member names, generics, structural interfaces, inheritance (including ordinary and bound constructor functions), super, getters/setters, auto-accessors and #private fields/methods/accessors with brand checks. Object, String, Boolean, Array, Promise, Map, Set, Error, native error and AggregateError base classes are supported; other built-in base classes and decorators are missing. Some generated auto-accessor initializer names differ from tsc; base constructors returning replacement objects still have gaps. |
| Exceptions | 9 / 10 | throw, try/catch/finally, standard error objects, cause, and instanceof. Error prototypes, prototype mutation and custom error subclasses are supported. TypeScript source locations in stack traces are missing. |
| Async | 8 / 10 | Promise construction, resolve/reject/withResolvers/try, all/allSettled/race/any over supported iterables (arrays, strings, arguments, Map/Set and their iterators), then/catch/finally, thenable assimilation, subclasses, queueMicrotask and async declarations/expressions/arrows/methods, and await in return expressions (operators, assignments, optional chains, calls, literals and constructors). Await statements, local initializers, destructuring defaults/computed keys (including nested patterns and rest), blocks, if branches, switch statements (including case expressions and fallthrough), labeled blocks, while/do/for/for-of/for-in loops, and try/catch/finally are supported. For-await-of supports arrays, strings, arguments, Map/Set and their iterators, including promise/thenable values and awaited cleanup on early exit. Custom sync/async iterators, generators and custom promise species are missing. |
| Standard library | 3 / 5 | Map/Set construction, Map.groupBy, get/set/add/has/delete/clear/size, live forEach/keys/values/entries, collection subclasses, and iteration through loops, spreads, destructuring, Array.from and Promise combinators. Newer Set composition methods, weak collections, symbols and iterator helpers remain missing. Object/String/Boolean/Array construction, Array.isArray/of/from (supported iterables and array-like objects), prototype get/set, Math constants and common numeric/trigonometric functions, selected Number/Object helpers, Object.is, Object.assign, string formatting, prefix/suffix checks, concatenation and array/string methods, including generic join, map, filter, forEach, every, some, reduce, reduceRight, find, findIndex, findLast, findLastIndex, flat, flatMap, lastIndexOf, fill, copyWithin, toReversed, toSorted, toSpliced and with. Calling fill or copyWithin on a non-array currently requires casting the function to `any`; some Object.assign calls involving primitives or arrays also need this. Custom array species and many built-ins are missing. |

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

Compilation times:

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 497.09 ms | 338.65 ms |
| 5,000 variables | 678.81 ms | 375.20 ms |

The packed copyWithin result is encouraging; [V8?s published per-property implementation](https://chromium.googlesource.com/v8/v8/+/refs/heads/main/src/builtins/array-copywithin.tq) is consistent with the large gap against our guarded bulk copy. The Map workload and the loop that rarely awaits are disappointing too; callbacks, joins, object copying and Promise workloads remain substantially slower. These microbenchmarks do not establish overall application performance.

The compiler used to be written in [crust](https://github.com/Gameknight963/crust), my C-like memory safe language. It was ported to C# for various reasons. The point is, compilation is slower after the C# port. The larger source takes 1.8x as long as Microsoft’s Go-based TypeScript compiler. It used to be more or less on par.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11, TypeScript 7.0.2
