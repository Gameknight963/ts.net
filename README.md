# ts.net

Work-in-progress TypesScript to IL compiler.


## Building

The compiler has significant portions written in [crust](https://github.com/Gameknight963/crust), my C-like memory-safe programming lanuage. To build it, you'll need to install `crust` and `crustc`.

Then run `.\scripts\build.ps1 -Run`. You might figure out what dependencies you're missing if you run it.

Building on linux is not supported atm.

## Current performance

As of [f6024a0](https://github.com/Gameknight963/ts.net/commit/f6024a04ecee21745eda0542e6836bc3570295fe):

Runtime performance:

| Workload | ts.net | Node | Result |
|---|---:|---:|---|
| Numeric loop, 20M iterations | 14.26 ms | 73.93 ms | 5.2x faster |
| Typed-object loop, 500K iterations | 0.36 ms | 0.65 ms | 1.8x faster |
| `any` property loop, 350K iterations | 0.27 ms | 0.43 ms | 1.6x faster |
| Mixed array, 100K elements and 5 traversals | 1.01 ms | 1.14 ms | 1.1x faster |
| Numeric `any[]`, 1M iterations | 0.70 ms | 1.64 ms | 2.3x faster |
| Numeric array, 100K elements and 100 reversals | 2.04 ms | 7.89 ms | 3.9x faster |
| Mixed array, 100K elements and 100 reversals | 1.00 ms | 7.53 ms | 7.5x faster |
| Numeric callback, 2M calls (experimental) | 1.41 ms | 0.82 ms | 1.7x slower |
| Captured numeric callback, 2M calls | 5.13 ms | 4.98 ms | 3% slower |
| Local function, 2M direct calls | 0.47 ms | 0.81 ms | 1.7x faster |
| Self-returning function, 2M calls | 4.92 ms | 0.75 ms | 6.5x slower |
| `Object.keys(value).length`, 100K calls | 0.071 ms | 0.906 ms | 12.8x faster |
| `Object.keys(value)`, 100K calls and key reads | 1.89 ms | 1.10 ms | 1.7x slower |

Compilation times:

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 332.31 ms | 326.98 ms |
| 5,000 variables | 433.24 ms | 367.93 ms |

Counting keys now avoids a temporary array and wins clearly here. Returning and reading the actual keys is still slower, as are callbacks; these remain useful places to improve.

Compilation times remain close to Microsoft's Go-based TypeScript compiler, though the larger source takes about 18% longer.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11 (SDK 10.0.400), TypeScript 7.0.2. Runtime results use matching sources and checksums, at least 100 warmups and 500 ms of warmup per engine, and 35 timed samples across five fresh processes per engine; compilation results are medians of seven fresh processes with warm filesystem caches. Runtime timings exclude startup and compilation.
