# ts.net

Work-in-progress TypesScript to IL compiler.


## Building

Build with the .NET 10 SDK, Go 1.27 or newer, and Git: `dotnet build tsnet.sln -c Release`. The first build downloads the pinned Microsoft TypeScript frontend source.

Then run `.\scripts\build.ps1 -Run` to compile and run an example.

Cross-publish for Linux with `dotnet publish compiler/Tsnet.Compiler.csproj -c Release -r linux-x64 --self-contained false`. The output includes the managed compiler and a Go frontend executable; NativeAOT and a C toolchain are not required.

## Current performance

As of [a013e66](https://github.com/Gameknight963/ts.net/commit/a013e6691459b93e3d43945363576d900e92a0fd):

Runtime performance:

| Workload | ts.net | Node | Result |
|---|---:|---:|---|
| Numeric loop, 20M iterations | 14.35 ms | 73.68 ms | 5.1x faster |
| Typed-object loop, 500K iterations | 0.39 ms | 0.66 ms | 1.7x faster |
| `any` property loop, 350K iterations | 0.29 ms | 0.43 ms | 1.5x faster |
| Mixed array, 100K elements and 5 traversals | 1.07 ms | 1.28 ms | 1.2x faster |
| Numeric `any[]`, 1M iterations | 0.70 ms | 1.65 ms | 2.3x faster |
| Numeric array, 100K elements and 100 reversals | 2.06 ms | 7.82 ms | 3.8x faster |
| Mixed array, 100K elements and 100 reversals | 1.06 ms | 7.72 ms | 7.3x faster |
| Numeric callback, 2M calls (experimental) | 1.40 ms | 0.82 ms | 1.7x slower |
| Captured numeric callback, 2M calls | 5.14 ms | 5.01 ms | 3% slower |
| Local function, 2M direct calls | 0.47 ms | 0.81 ms | 1.7x faster |
| Self-returning function, 2M calls | 5.00 ms | 0.76 ms | 6.6x slower |
| `Object.keys(value).length`, 100K calls | 0.071 ms | 0.914 ms | 12.9x faster |
| `Object.keys(value)`, 100K calls and key reads | 1.31 ms | 1.11 ms | 1.2x slower |
| Local object spread, 100K two-field copies | 0.09 ms | 1.01 ms | 10.7x faster |
| Object spread, 100K copies stored in an array | 3.16 ms | 2.45 ms | 1.3x slower |
| Computed keys, 100K objects and reads | 4.21 ms | 26.30 ms | 6.3x faster |
| `Object.values(value)`, 100K calls and reads | 1.43 ms | 9.57 ms | 6.7x faster |
| `Object.entries(value)`, 100K calls and reads | 5.87 ms | 16.96 ms | 2.9x faster |
| Literal squares, 2M iterations | 1.40 ms | 1.42 ms | 1% faster |
| Small integer bases and varying exponents, 1M iterations | 4.06 ms | 13.96 ms | 3.4x faster |
| Mixed numeric strings, 1M conversions | 28.47 ms | 29.96 ms | 5% faster |
| Object valueOf, 1M numeric conversions | 13.41 ms | 23.52 ms | 1.8x faster |
| String ordering, 2M comparisons | 8.96 ms | 8.72 ms | 3% slower |
| `==` on mixed values, 1M comparisons | 5.09 ms | 7.28 ms | 1.4x faster |
| `== null` on nullable arrays, 1M comparisons | 1.52 ms | 1.15 ms | 1.3x slower |

Compilation times:

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 505.08 ms | 341.52 ms |
| 5,000 variables | 712.72 ms | 395.06 ms |

Mixed-value == comparisons run faster than Node in this test, while nullable-array null checks remain slower. The nullable-loop allocation check measured 248 bytes total for one million iterations.

Cold compilation is slower after the C# port; the larger source takes 1.8x as long as Microsoft’s Go-based TypeScript compiler.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11, TypeScript 7.0.2. Runtime measurements exclude startup and use matching checksums, at least 500 ms of warmup, and 35 samples across five fresh processes. Compilation timings include startup and are medians of seven fresh processes with warm filesystem caches.
