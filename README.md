# ts.net

Work-in-progress TypesScript to IL compiler.


## Building

The compiler has significant portions written in [crust](https://github.com/Gameknight963/crust), my C-like memory-safe programming lanuage. To build it, you'll need to install `crust` and `crustc`.

Then run `.\scripts\build.ps1 -Run`. You might figure out what dependencies you're missing if you run it.

Building on linux is not supported atm.

## Current performance

As of [4753fb8](https://github.com/Gameknight963/ts.net/commit/4753fb8b1334aa3db97773307ce66bb4275ce539):

Runtime performance:

| Workload | ts.net | Node | Result |
|---|---:|---:|---|
| Numeric loop, 20M iterations | 14.30 ms | 73.87 ms | 5.2x faster |
| Typed-object loop, 500K iterations | 0.35 ms | 0.64 ms | 1.8x faster |
| `any` property loop, 350K iterations | 0.28 ms | 0.43 ms | 1.5x faster |
| Mixed array, 100K elements and 5 traversals | 1.02 ms | 1.09 ms | 7% faster |
| Numeric `any[]`, 1M iterations | 0.70 ms | 1.64 ms | 2.3x faster |
| Numeric array, 100K elements and 100 reversals | 2.02 ms | 7.67 ms | 3.8x faster |
| Mixed array, 100K elements and 100 reversals | 1.02 ms | 7.54 ms | 7.4x faster |
| Numeric callback, 2M calls (experimental) | 1.38 ms | 0.82 ms | 1.7x slower |
| Captured numeric callback, 2M calls | 5.08 ms | 4.99 ms | 2% slower |
| Local function, 2M direct calls | 0.47 ms | 0.82 ms | 1.7x faster |
| Self-returning function, 2M calls | 4.86 ms | 0.76 ms | 6.4x slower |
| `Object.keys(value).length`, 100K calls | 0.070 ms | 0.911 ms | 13.0x faster |
| `Object.keys(value)`, 100K calls and key reads | 1.91 ms | 1.11 ms | 1.7x slower |
| Local object spread, 100K two-field copies | 0.09 ms | 1.01 ms | 10.8x faster |
| Object spread, 100K copies stored in an array | 3.05 ms | 2.45 ms | 1.2x slower |
| Computed keys, 100K objects and reads | 3.74 ms | 25.96 ms | 6.9x faster |
| `Object.values(value)`, 100K calls and reads | 1.44 ms | 9.37 ms | 6.5x faster |
| `Object.entries(value)`, 100K calls and reads | 6.35 ms | 16.89 ms | 2.7x faster |

Compilation times:

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 334.69 ms | 345.04 ms |
| 5,000 variables | 473.14 ms | 399.42 ms |

Object.values and Object.entries run 6.5x and 2.7x faster than Node in these tests. Stored object copies and self-returning functions remain slower, so allocation-heavy code still needs work.

Compilation times remain close to Microsoft’s Go-based TypeScript compiler, though the larger source takes about 18% longer.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11 (SDK 10.0.400), TypeScript 7.0.2. Runtime results use matching sources and checksums, at least 100 warmups and 500 ms of warmup per engine, and 35 timed samples across five fresh processes per engine; compilation results are medians of seven fresh processes with warm filesystem caches. Runtime timings exclude startup and compilation.
