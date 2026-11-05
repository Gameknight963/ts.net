# ts.net

Work-in-progress TypesScript to IL compiler.


## Building

The compiler has significant portions written in [crust](https://github.com/Gameknight963/crust), my C-like memory-safe programming lanuage. To build it, you'll need to install `crust` and `crustc`.

Then run `.\scripts\build.ps1 -Run`. You might figure out what dependencies you're missing if you run it.

Building on linux is not supported atm.

## Current performance

As of [004fba2](https://github.com/Gameknight963/ts.net/commit/004fba26d9ee1714ffad2b4ffa312b48fa4b5f2f):

Runtime performance:

| Workload | ts.net | Node | Result |
|---|---:|---:|---|
| Numeric loop, 20M iterations | 14.43 ms | 74.17 ms | 5.1x faster |
| Typed-object loop, 500K iterations | 0.42 ms | 0.65 ms | 1.5x faster |
| `any` property loop, 350K iterations | 0.30 ms | 0.83 ms | 2.7x faster |
| Mixed array, 100K elements and 5 traversals | 1.04 ms | 1.16 ms | 1.1x faster |
| Numeric `any[]`, 1M iterations | 0.76 ms | 1.65 ms | 2.2x faster |
| Numeric array, 100K elements and 100 reversals | 2.07 ms | 7.78 ms | 3.8x faster |
| Mixed array, 100K elements and 100 reversals | 1.15 ms | 7.67 ms | 6.6x faster |
| Numeric callback, 2M calls (experimental) | 1.46 ms | 0.83 ms | 1.8x slower |
| Captured numeric callback, 2M calls | 5.16 ms | 5.00 ms | 3% slower |

Compilation times:

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 330.09 ms | 329.44 ms |
| 5,000 variables | 434.22 ms | 377.15 ms |

The numeric and small-object loops remain encouraging. Capturing callbacks now work and are close to Node here; ordinary numeric callbacks still trail it by about 1.8x.

Compilation times remain close to Microsoft's Go-based TypeScript compiler, though the larger source takes about 15% longer.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11 (SDK 10.0.400), TypeScript 7.0.2. Runtime results use matching sources and checksums, 100 warmups, and 35 timed samples across five fresh processes per engine; compilation results are medians of seven fresh processes with warm filesystem caches. Runtime timings exclude startup and compilation.
