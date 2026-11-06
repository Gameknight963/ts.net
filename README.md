# ts.net

Work-in-progress TypesScript to IL compiler.


## Building

Build with the .NET 10 SDK, Go 1.27 or newer, and Git: `dotnet build tsnet.sln -c Release`. The first build downloads the pinned Microsoft TypeScript frontend source.

Then run `.\scripts\build.ps1 -Run` to compile and run an example.

Cross-publish for Linux with `dotnet publish compiler/Tsnet.Compiler.csproj -c Release -r linux-x64 --self-contained false`. The output includes the managed compiler and a Go frontend executable; NativeAOT and a C toolchain are not required.

## Current performance

As of [90731bc](https://github.com/Gameknight963/ts.net/commit/90731bc4a6b9529f20acde79e5d0471e59727275):

Runtime performance:

| Workload | ts.net | Node | Result |
|---|---:|---:|---|
| Numeric loop, 20M iterations | 14.34 ms | 74.30 ms | 5.2x faster |
| Typed-object loop, 500K iterations | 0.36 ms | 0.65 ms | 1.8x faster |
| `any` property loop, 350K iterations | 0.29 ms | 0.43 ms | 1.5x faster |
| Mixed array, 100K elements and 5 traversals | 1.16 ms | 1.19 ms | 3% faster |
| Numeric `any[]`, 1M iterations | 0.72 ms | 1.67 ms | 2.3x faster |
| Numeric array, 100K elements and 100 reversals | 2.03 ms | 7.71 ms | 3.8x faster |
| Mixed array, 100K elements and 100 reversals | 0.99 ms | 7.63 ms | 7.7x faster |
| Numeric callback, 2M calls (experimental) | 1.39 ms | 0.82 ms | 1.7x slower |
| Captured numeric callback, 2M calls | 5.23 ms | 5.03 ms | 4% slower |
| Local function, 2M direct calls | 0.46 ms | 0.82 ms | 1.8x faster |
| Self-returning function, 2M calls | 4.97 ms | 0.75 ms | 6.6x slower |
| `Object.keys(value).length`, 100K calls | 0.071 ms | 0.929 ms | 13.2x faster |
| `Object.keys(value)`, 100K calls and key reads | 1.31 ms | 1.11 ms | 1.2x slower |
| Local object spread, 100K two-field copies | 0.09 ms | 0.99 ms | 10.6x faster |
| Object spread, 100K copies stored in an array | 3.24 ms | 2.47 ms | 1.3x slower |
| Computed keys, 100K objects and reads | 3.81 ms | 26.43 ms | 6.9x faster |
| `Object.values(value)`, 100K calls and reads | 1.46 ms | 9.38 ms | 6.4x faster |
| `Object.entries(value)`, 100K calls and reads | 5.29 ms | 16.83 ms | 3.2x faster |
| Literal squares, 2M iterations | 1.41 ms | 1.39 ms | 1% slower |
| Small integer bases and varying exponents, 1M iterations | 4.08 ms | 13.69 ms | 3.4x faster |
| Mixed numeric strings, 1M conversions | 29.31 ms | 29.95 ms | 2% faster |
| Object valueOf, 1M numeric conversions | 14.32 ms | 23.52 ms | 1.6x faster |
| String ordering, 2M comparisons | 8.77 ms | 8.60 ms | 2% slower |
| `==` on mixed values, 1M comparisons | 5.14 ms | 7.21 ms | 1.4x faster |
| `== null` on nullable arrays, 1M comparisons | 1.52 ms | 1.15 ms | 1.3x slower |
| Numeric array `.at(-1)`, 1M calls | 3.48 ms | 3.78 ms | 8% faster |
| String `.at(index)`, 1M calls | 5.54 ms | 5.24 ms | 6% slower |

Compilation times:

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 607.18 ms | 445.71 ms |
| 5,000 variables | 806.92 ms | 454.21 ms |

Numeric array `.at()` calls are close to Node here, while string `.at()` is slightly slower. The numeric `.at()` allocation check measured 152 bytes total for one million calls, with no per-call boxing.

Cold compilation is slower after the C# port; the larger source takes 1.8x as long as Microsoft’s Go-based TypeScript compiler.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11, TypeScript 7.0.2. Runtime measurements exclude startup and use matching checksums, at least 500 ms of warmup, and 35 samples across five fresh processes. Compilation timings include startup and are medians of seven fresh processes with warm filesystem caches.
