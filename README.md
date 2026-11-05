# ts.net

Work-in-progress TypesScript to IL compiler.


## Building

The compiler has significant portions written in [crust](https://github.com/Gameknight963/crust), my C-like memory-safe programming lanuage. To build it, you'll need to install `crust` and `crustc`.

Then run `.\scripts\build.ps1 -Run`. You might figure out what dependencies you're missing if you run it.

Building on linux is not supported atm.

## Current performance

As of [2906045](https://github.com/Gameknight963/ts.net/commit/2906045c1dc132e985dbc8a24842787b42606fdf):

Runtime performance:

| Workload | ts.net | Node | Result |
|---|---:|---:|---|
| Numeric loop, 20M iterations | 14.62 ms | 74.75 ms | 5.1x faster |
| Typed-object loop, 500K iterations | 0.43 ms | 0.67 ms | 1.6x faster |
| `any` property loop, 350K iterations | 0.30 ms | 0.84 ms | 2.8x faster |
| Mixed array, 100K elements and 5 traversals | 1.06 ms | 1.33 ms | 1.3x faster |
| Numeric `any[]`, 1M iterations | 0.77 ms | 1.70 ms | 2.2x faster |
| Numeric array, 100K elements and 100 reversals | 2.12 ms | 8.02 ms | 3.8x faster |
| Mixed array, 100K elements and 100 reversals | 1.24 ms | 7.83 ms | 6.3x faster |
| Numeric callback, 2M calls (experimental) | 1.48 ms | 0.85 ms | 1.7x slower |

Compilation times:

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 328.60 ms | 336.81 ms |
| 5,000 variables | 436.54 ms | 381.67 ms |

The numeric and small-object loops remain encouraging, and the mixed-array workload is now faster than Node in this run. Numeric callbacks improved substantially after letting the CLR inline their bodies, but are still 1.7x slower here.

Compilation times remain close to Microsoft's Go-based TypeScript compiler, though the larger source takes about 14% longer.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11 (SDK 10.0.400), TypeScript 7.0.2. Runtime results use matching sources and checksums, 100 warmups, and 35 timed samples across five fresh processes per engine; compilation results are medians of seven fresh processes with warm filesystem caches. Runtime timings exclude startup and compilation.
