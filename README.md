# ts.net

Work-in-progress TypesScript to IL compiler.


## Building

The compiler has significant portions written in [crust](https://github.com/Gameknight963/crust), my C-like memory-safe programming lanuage. To build it, you'll need to install `crust` and `crustc`.

Then run `.\scripts\build.ps1 -Run`. You might figure out what dependencies you're missing if you run it.

Building on linux is not supported atm.

## Current performance

As of [05ff842](https://github.com/Gameknight963/ts.net/commit/05ff84238a3a9dc7d8ddb3821d62358ccb363d9a):

Runtime performance:

| Workload | ts.net | Node | Result |
|---|---:|---:|---|
| Numeric loop, 20M iterations | 14.41 ms | 74.63 ms | 5.2x faster |
| Typed-object loop, 500K iterations | 0.43 ms | 0.66 ms | 1.5x faster |
| `any` property loop, 350K iterations | 0.29 ms | 0.83 ms | 2.8x faster |
| Mixed array, 100K elements and 5 traversals | 1.05 ms | 1.12 ms | 1.1x faster |
| Numeric `any[]`, 1M iterations | 0.76 ms | 1.65 ms | 2.2x faster |
| Numeric array, 100K elements and 100 reversals | 2.09 ms | 7.82 ms | 3.7x faster |
| Mixed array, 100K elements and 100 reversals | 1.15 ms | 7.58 ms | 6.6x faster |
| Numeric callback, 2M calls (experimental) | 1.46 ms | 0.83 ms | 1.8x slower |
| Captured numeric callback, 2M calls | 5.11 ms | 5.01 ms | 2% slower |
| Local function, 2M direct calls | 1.45 ms | 0.82 ms | 1.8x slower |

Compilation times:

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 323.90 ms | 327.22 ms |
| 5,000 variables | 432.98 ms | 374.02 ms |

The numeric and small-object loops remain encouraging. Captured callbacks are close to Node here; direct local calls and ordinary numeric callbacks still trail it by about 1.8x.

Compilation times remain close to Microsoft's Go-based TypeScript compiler, though the larger source takes about 16% longer.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11 (SDK 10.0.400), TypeScript 7.0.2. Runtime results use matching sources and checksums, 100 warmups, and 35 timed samples across five fresh processes per engine; compilation results are medians of seven fresh processes with warm filesystem caches. Runtime timings exclude startup and compilation.
