# ts.net

Work-in-progress TypesScript to IL compiler.


## Building

Build with the .NET 10 SDK, Go 1.27 or newer, and Git: `dotnet build tsnet.sln -c Release`. The first build downloads the pinned Microsoft TypeScript frontend source.

Then run `.\scripts\build.ps1 -Run` to compile and run an example.

Cross-publish for Linux with `dotnet publish compiler/Tsnet.Compiler.csproj -c Release -r linux-x64 --self-contained false`. The output includes the managed compiler and a Go frontend executable; NativeAOT and a C toolchain are not required.

## Current performance

As of [8097a13](https://github.com/Gameknight963/ts.net/commit/8097a1302c8b52e4920dd2f621f321e1a771b9c7):

Runtime performance:

| Workload | ts.net | Node | Result |
|---|---:|---:|---|
| Numeric loop, 20M iterations | 14.50 ms | 75.20 ms | 5.2x faster |
| Typed-object loop, 500K iterations | 0.41 ms | 0.66 ms | 1.6x faster |
| `any` property loop, 350K iterations | 0.27 ms | 0.43 ms | 1.6x faster |
| Mixed array, 100K elements and 5 traversals | 0.98 ms | 1.27 ms | 1.3x faster |
| Numeric `any[]`, 1M iterations | 0.72 ms | 1.66 ms | 2.3x faster |
| Numeric array, 100K elements and 100 reversals | 2.17 ms | 7.92 ms | 3.7x faster |
| Mixed array, 100K elements and 100 reversals | 1.12 ms | 7.79 ms | 6.9x faster |
| Numeric callback, 2M calls (experimental) | 1.43 ms | 0.83 ms | 1.7x slower |
| Captured numeric callback, 2M calls | 5.22 ms | 5.14 ms | 2% slower |
| Local function, 2M direct calls | 0.47 ms | 0.83 ms | 1.8x faster |
| Self-returning function, 2M calls | 5.05 ms | 0.77 ms | 6.5x slower |
| `Object.keys(value).length`, 100K calls | 0.071 ms | 0.923 ms | 13.0x faster |
| `Object.keys(value)`, 100K calls and key reads | 1.35 ms | 1.13 ms | 1.2x slower |
| Local object spread, 100K two-field copies | 0.09 ms | 1.02 ms | 10.7x faster |
| Object spread, 100K copies stored in an array | 2.94 ms | 2.57 ms | 1.1x slower |
| Computed keys, 100K objects and reads | 4.40 ms | 26.58 ms | 6.0x faster |
| `Object.values(value)`, 100K calls and reads | 1.51 ms | 9.56 ms | 6.3x faster |
| `Object.entries(value)`, 100K calls and reads | 6.03 ms | 17.06 ms | 2.8x faster |
| Literal squares, 2M iterations | 1.43 ms | 1.42 ms | 1% slower |
| Small integer bases and varying exponents, 1M iterations | 4.11 ms | 13.99 ms | 3.4x faster |
| Mixed numeric strings, 1M conversions | 27.11 ms | 30.62 ms | 1.1x faster |
| Object valueOf, 1M numeric conversions | 14.77 ms | 24.05 ms | 1.6x faster |
| String ordering, 2M comparisons | 8.87 ms | 8.80 ms | 1% slower |

Compilation times:

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 515.24 ms | 355.95 ms |
| 5,000 variables | 703.15 ms | 392.96 ms |

String ordering now follows JavaScript rules and runs about level with Node in this test. Object conversion remains faster here, while stored copies and self-returning functions still need work.

Cold compilation is slower after the C# port; the larger source takes 1.8x as long as Microsoft’s Go-based TypeScript compiler.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11, TypeScript 7.0.2. Runtime measurements exclude startup and use matching checksums, at least 500 ms of warmup, and 35 samples across five fresh processes. Compilation timings include startup and are medians of seven fresh processes with warm filesystem caches.
