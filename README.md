# ts.net

Work-in-progress TypesScript to IL compiler.


## Building

The compiler has significant portions written in [crust](https://github.com/Gameknight963/crust), my C-like memory-safe programming lanuage. To build it, you'll need to install `crust` and `crustc`.

Then run `.\scripts\build.ps1 -Run`. You might figure out what dependencies you're missing if you run it.

Building on linux is not supported atm.

## Current performance

As of [582b7d7](https://github.com/Gameknight963/ts.net/commit/582b7d71a7fa7d4db7cfc6e793cb4a365c77e2f8):

Runtime performance:

| Workload | ts.net | Node | Result |
|---|---:|---:|---|
| Numeric loop, 20M iterations | 14.29 ms | 73.99 ms | 5.2x faster |
| Typed-object loop, 500K iterations | 0.36 ms | 0.65 ms | 1.8x faster |
| `any` property loop, 350K iterations | 0.28 ms | 0.43 ms | 1.5x faster |
| Mixed array, 100K elements and 5 traversals | 0.99 ms | 1.16 ms | 1.2x faster |
| Numeric `any[]`, 1M iterations | 0.70 ms | 1.65 ms | 2.3x faster |
| Numeric array, 100K elements and 100 reversals | 2.08 ms | 7.80 ms | 3.7x faster |
| Mixed array, 100K elements and 100 reversals | 1.05 ms | 7.62 ms | 7.2x faster |
| Numeric callback, 2M calls (experimental) | 1.37 ms | 0.82 ms | 1.7x slower |
| Captured numeric callback, 2M calls | 5.15 ms | 4.99 ms | 3% slower |
| Local function, 2M direct calls | 0.48 ms | 0.82 ms | 1.7x faster |
| Self-returning function, 2M calls | 4.88 ms | 0.75 ms | 6.5x slower |
| `Object.keys(value).length`, 100K calls | 0.071 ms | 0.934 ms | 13.2x faster |
| `Object.keys(value)`, 100K calls and key reads | 2.04 ms | 1.10 ms | 1.9x slower |
| Local object spread, 100K two-field copies | 0.09 ms | 1.00 ms | 10.6x faster |
| Object spread, 100K copies stored in an array | 2.82 ms | 2.39 ms | 1.2x slower |

Compilation times:

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 322.93 ms | 328.09 ms |
| 5,000 variables | 429.33 ms | 371.74 ms |

Stored copies improved from 18.10 ms to 2.82 ms after reducing temporary allocations and shrinking numeric fields. They still take 1.2x as long as Node here; local copies remain much faster.

Compilation times remain close to Microsoft’s Go-based TypeScript compiler, though the larger source takes about 15% longer.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11 (SDK 10.0.400), TypeScript 7.0.2. Runtime results use matching sources and checksums, at least 100 warmups and 500 ms of warmup per engine, and 35 timed samples across five fresh processes per engine; compilation results are medians of seven fresh processes with warm filesystem caches. Runtime timings exclude startup and compilation.
