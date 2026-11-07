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
|---|---:|---|
| Values and operators | 9 / 10 | Missing some Symbol protocols and operators. |
| Control flow | 10 / 10 | Missing nothing. |
| Functions | 9 / 10 | Global properties do not alias top-level bindings; native global bindings are read-only. |
| Objects and arrays | 9 / 10 | Missing complete general prototype behavior. |
| Types and generics | 5 / 10 | Missing some runtime type representations. |
| Modules | 4 / 5 | Missing dynamic imports of uncompiled modules. |
| Enums | 5 / 5 | Missing nothing. |
| Classes and inheritance | 14 / 15 | Missing some legacy decorator descriptor changes, some built-in base classes, and complete constructor replacement-object support. |
| Exceptions | 9 / 10 | Missing TypeScript source locations in stack traces. |
| Async | 10 / 10 | Missing nothing. |
| Standard library | 3 / 5 | Missing some Symbol protocols, Reflect methods, RegExp features, and built-ins. |

</details>

## Current performance

Recent additions, measured at [682c077](https://github.com/Gameknight963/ts.net/commit/682c077):

| Workload | ts.net | Node |
|---|---:|---:|
| Sum a 10K-element Float64Array 100 times | 42.38 ms | 0.70 ms |
| Consume ten generators yielding 10K numbers each | 111.74 ms | 1.53 ms |
| Consume 10K values from an async generator | 24.99 ms | 1.87 ms |
| UTF-8 encode/decode, 100 round trips of a 28,672-byte payload | 6.71 ms | 5.49 ms |
| Resolve 1K URLs and update each query through searchParams | 6.98 ms | 2.91 ms |

Structured cloning, measured at [c59dc05](https://github.com/Gameknight963/ts.net/commit/c59dc05):

| Workload | ts.net | Node |
|---|---:|---:|
| Clone a cyclic graph with 1K rows and a shared child, 100 times | 154.13 ms | 56.81 ms |

Medians of seven fresh processes after three warmup runs per process, using Release builds on Windows x64, .NET 10.0.11, Node 25.9.0, and TypeScript 7.0.2. Results match Node. Timings exclude startup, compilation, and checksum printing; raw results and benchmark programs are under `artifacts`.

Typed-array and generator loops are still painfully slow here, while text encoding is much closer to Node. URL handling and graph cloning take about 2.4 and 2.7 times as long respectively, so these built-ins still need work. These individual workloads do not establish overall application performance.

Earlier measurements, from [9c5d2ef](https://github.com/Gameknight963/ts.net/commit/9c5d2efb1f0ece3314753f3a44fb1ea74aa770b9):

Runtime performance:

<details>
<summary>Click to expand large table</summary>

| Workload | ts.net | Node | Result |
|---|---:|---:|---:|
| Numeric loop, 20M iterations | 14.44 ms | 74.71 ms | 5.17x |
| Typed-object loop, 500K iterations | 0.38 ms | 0.66 ms | 1.74x |
| `any` property loop, 350K iterations | 0.27 ms | 0.43 ms | 1.59x |
| Mixed array, 100K elements and 5 traversals | 1.05 ms | 1.16 ms | 1.10x |
| Numeric `any[]`, 1M iterations | 0.81 ms | 1.66 ms | 2.05x |
| Numeric array, 100K elements and 100 reversals | 2.07 ms | 7.98 ms | 3.86x |
| Mixed array, 100K elements and 100 reversals | 1.06 ms | 7.74 ms | 7.30x |
| Numeric callback, 2M calls (experimental) | 1.42 ms | 0.83 ms | 0.58x |
| Captured numeric callback, 2M calls | 5.24 ms | 5.10 ms | 0.97x |
| Local function, 2M direct calls | 0.47 ms | 0.83 ms | 1.77x |
| Self-returning function, 2M calls | 5.04 ms | 0.77 ms | 0.15x |
| `Object.keys(value).length`, 100K calls | 0.071 ms | 0.949 ms | 13.37x |
| `Object.keys(value)`, 100K calls and key reads | 1.30 ms | 1.12 ms | 0.86x |
| Local object spread, 100K two-field copies | 0.09 ms | 1.00 ms | 11.11x |
| Object spread, 100K copies stored in an array | 3.17 ms | 2.41 ms | 0.76x |
| Computed keys, 100K objects and reads | 3.85 ms | 26.06 ms | 6.77x |
| `Object.values(value)`, 100K calls and reads | 1.44 ms | 9.41 ms | 6.53x |
| `Object.entries(value)`, 100K calls and reads | 5.53 ms | 16.69 ms | 3.02x |
| Literal squares, 2M iterations | 1.40 ms | 1.42 ms | 1.01x |
| Small integer bases and varying exponents, 1M iterations | 4.04 ms | 13.72 ms | 3.40x |
| Mixed numeric strings, 1M conversions | 27.14 ms | 29.88 ms | 1.10x |
| Object valueOf, 1M numeric conversions | 14.44 ms | 23.51 ms | 1.63x |
| String ordering, 2M comparisons | 8.85 ms | 8.75 ms | 0.99x |
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
| copyWithin, 1K copies of 5K entries in a 10K packed array | 1 ms | 269 ms | 0.0031x |
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
| Small numeric benchmark | 519.18 ms | 340.84 ms |
| 5,000 variables | 726.64 ms | 388.73 ms |

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11, TypeScript 7.0.2
