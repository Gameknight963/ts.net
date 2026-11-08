# ts.net

Work-in-progress TypesScript to IL compiler.

## AI disclaimer

This project has significant portions written by AI. This README is perodically updated by AI to mantain factual accuracy, but reviewed by me to remove slop.

## Building

Reqires .NET 10 SDK, Go 1.27 or newer, and Git.

The first build downloads the pinned Microsoft TypeScript frontend source.

Then run `.\scripts\build.ps1 -Run` to compile and run an example.

## Current feature completion

87%.

<details>

| Feature | Points | Missing support |
|---|---|---|
| Values and operators | 9 / 10 | Computed deletion of fixed-layout fields and some Symbol protocols. |
| Control flow | 10 / 10 | Missing nothing. |
| Functions | 9 / 10 | Global properties do not alias top-level bindings; native global bindings are read-only. |
| Objects and arrays | 9 / 10 | General property-definition and object-extensibility APIs are incomplete. |
| Types and generics | 5 / 10 | Missing some runtime type representations. |
| Modules | 4 / 5 | Missing dynamic imports of uncompiled modules. |
| Enums | 5 / 5 | Missing nothing. |
| Classes and inheritance | 14 / 15 | Missing some built-in base classes and complete constructor replacement-object support. |
| Exceptions | 9 / 10 | Stack traces keep CLR formatting and may omit inlined frames. |
| Async | 10 / 10 | Missing nothing. |
| Standard library | 3 / 5 | Remaining RegExp u/v support and other built-ins. |

</details>

## Current performance

Rows from the first Numeric loop through String ordering, and compilation times, measured at [395e8ad](https://github.com/Gameknight963/ts.net/commit/395e8ade44a27da41ba17b08a7dbe7baa4862c0e). Other measurements remain from [9c5d2ef](https://github.com/Gameknight963/ts.net/commit/9c5d2efb1f0ece3314753f3a44fb1ea74aa770b9), except typed-array traversal, measured at [e3c9b8a](https://github.com/Gameknight963/ts.net/commit/e3c9b8a):

Updated execution rows use Release builds and the median of 35 samples across five fresh processes, after at least 100 warmup calls and 500 ms of warmup per process; startup is excluded. Compilation uses medians of seven fresh processes and includes startup; the full test suite ran concurrently.

Runtime performance:

<details>
<summary>Click to expand large table</summary>

| Workload | ts.net | Node | Result |
|---|---|---|---|
| Float64Array traversal, 10K elements and 100 passes | 0.784 ms | 0.718 ms | 0.92x |
| Numeric loop, 20M iterations | 14.58 ms | 75.45 ms | 5.17x |
| Typed-object loop, 500K iterations | 0.381 ms | 0.650 ms | 1.70x |
| `any` property loop, 350K iterations | 0.266 ms | 0.435 ms | 1.64x |
| Mixed array, 100K elements and 5 traversals | 3.19 ms | 1.15 ms | 0.36x |
| Numeric `any[]`, 1M iterations | 0.954 ms | 1.66 ms | 1.74x |
| Numeric array, 100K elements and 100 reversals | 4.37 ms | 7.87 ms | 1.80x |
| Mixed array, 100K elements and 100 reversals | 3.07 ms | 7.70 ms | 2.51x |
| Numeric callback, 2M calls (experimental) | 1.38 ms | 0.827 ms | 0.60x |
| Captured numeric callback, 2M calls | 5.24 ms | 5.07 ms | 0.97x |
| Local function, 2M direct calls | 0.461 ms | 0.825 ms | 1.79x |
| Self-returning function, 2M calls | 8.39 ms | 0.760 ms | 0.09x |
| `Object.keys(value).length`, 100K calls | 0.095 ms | 0.953 ms | 10.05x |
| `Object.keys(value)`, 100K calls and key reads | 2.46 ms | 1.12 ms | 0.46x |
| Local object spread, 100K two-field copies | 0.095 ms | 1.02 ms | 10.77x |
| Object spread, 100K copies stored in an array | 11.34 ms | 2.42 ms | 0.21x |
| Computed keys, 100K objects and reads | 5.97 ms | 29.00 ms | 4.86x |
| `Object.values(value)`, 100K calls and reads | 1.73 ms | 9.53 ms | 5.51x |
| `Object.entries(value)`, 100K calls and reads | 8.09 ms | 16.71 ms | 2.07x |
| Literal squares, 2M iterations | 1.40 ms | 1.41 ms | 1.00x |
| Small integer bases and varying exponents, 1M iterations | 4.07 ms | 13.85 ms | 3.41x |
| Mixed numeric strings, 1M conversions | 28.60 ms | 30.37 ms | 1.06x |
| Object valueOf, 1M numeric conversions | 48.94 ms | 23.64 ms | 0.48x |
| String ordering, 2M comparisons | 8.98 ms | 8.75 ms | 0.97x |
| `==` on mixed values, 1M comparisons | 5.15 ms | 7.26 ms | 1.41x |
| `== null` on nullable arrays, 1M comparisons | 1.51 ms | 1.15 ms | 0.76x |
| Numeric array `.at(-1)`, 1M calls | 3.84 ms | 3.82 ms | 0.99x |
| String `.at(index)`, 1M calls | 4.26 ms | 5.28 ms | 1.24x |
| String `charCodeAt(index)`, 1M calls | 2.27 ms | 1.66 ms | 0.73x |
| String `codePointAt(index)`, 1M calls | 3.00 ms | 1.90 ms | 0.63x |
| Masked integer remainder, 1M iterations | 1.10 ms | 1.11 ms | 1.01x |
| String slice and first-character read, 1M calls | 14.55 ms | 8.98 ms | 0.62x |
| String substring and first-character read, 1M calls | 14.27 ms | 10.75 ms | 0.75x |
| Numeric string index and comparison, 1M reads | 4.86 ms | 1.93 ms | 0.40x |
| String indexOf, 1M searches | 10.21 ms | 17.82 ms | 1.75x |
| String includes, 1M searches | 11.19 ms | 28.84 ms | 2.58x |
| String lastIndexOf, 1M searches | 10.07 ms | 19.54 ms | 1.94x |
| Create a Map, insert and read 10K entries, then delete half | 4.38 ms | 0.80 ms | 0.18x |
| join, 20 joins of 10K numbers | 26 ms | 2 ms | 0.09x |
| map, filter and sum, 10 passes over 10K numbers | 23 ms | 2 ms | 0.09x |
| reduce and reduceRight, 10 pairs over 10K numbers | 7 ms | 1 ms | 0.19x |
| from, 20 copies of 10K packed numbers | 1 ms | 0 ms | 0.32x |
| copyWithin, 1K copies of 5K entries in a 10K packed array | 1 ms | 269 ms | 321x |
| Object.assign, create 1K objects with 3 fields | 1 ms | 0 ms | 0.10x |
| Split 10K comma-separated strings, keep three parts, and sum lengths | 4 ms | 2 ms | 0.50x |
| Create/read a two-field object in a 100K-iteration loop, with a read-only Reflect alias in the program | 8 ms | 0.077 ms | 0.01x |
| Parse 1,000 three-field records 20 times, without a reviver | 9 ms | 4 ms | 0.48x |
| Serialize 1,000 three-field records 20 times, without a replacer | 20 ms | 2 ms | 0.08x |
| Construct and run a chain of 10K handlers | 12 ms | 1 ms | 0.08x |
| Create 10K resolved promises, collect with all, and sum results | 39 ms | 1 ms | 0.03x |
| Run 1K async calls with two awaits each, collect with all, and sum results | 3 ms | 0 ms | 0.11x |
| Sum 1K values in a loop, awaiting each value | 0.384 ms | 0.047 ms | 0.12x |
| Sum 1K values with an await in try and an increment in finally each iteration | 0.860 ms | 0.092 ms | 0.11x |
| Traverse 1K array values with for-await-of and sum them | 0.737 ms | 0.126 ms | 0.17x |
| Sum 100K values in a loop, awaiting only on the final iteration | 8 ms | 1 ms | 0.07x |
| Numeric loop, 20M iterations | 19 ms | 19 ms | 0.99x |
| Typed-object loop, 500K iterations | 0.5 ms | 0.4 ms | 1.49x |
| `any` property loop, 350K iterations | 0.3 ms | 0.3 ms | 1.00x |
| Object valueOf, 1M numeric conversions | 121 ms | 122 ms | 0.99x |

</details>

Compilation times:

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 503.72 ms | 354.32 ms |
| 5,000 variables | 754.27 ms | 391.27 ms |

Arithmetic and small typed-object loops look healthy; mixed arrays and stored object copies still need work.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11, TypeScript 7.0.2
