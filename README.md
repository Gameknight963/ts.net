# ts.net

Work-in-progress TypesScript to IL compiler.


## Building

The compiler has significant portions written in [crust](https://github.com/Gameknight963/crust), my C-like memory-safe programming lanuage. To build it, you'll need to install `crust` and `crustc`.

Then run `.\scripts\build.ps1 -Run`. You might figure out what dependencies you're missing if you run it.

Building on linux is not supported atm.

## Current performance

As of [274dd3d](https://github.com/Gameknight963/ts.net/commit/274dd3d5493bfd671f1216e539a9dfc23c65a330):

Runtime performance:

| Workload | ts.net | Node | Result |
|---|---:|---:|---|
| Numeric loop, 20M iterations | 14.30 ms | 74.24 ms | 5.2x faster |
| Typed-object loop, 500K iterations | 0.36 ms | 0.64 ms | 1.8x faster |
| `any` property loop, 350K iterations | 0.26 ms | 0.43 ms | 1.7x faster |
| Mixed array, 100K elements and 5 traversals | 0.99 ms | 1.14 ms | 1.1x faster |
| Numeric `any[]`, 1M iterations | 0.70 ms | 1.65 ms | 2.4x faster |
| Numeric array, 100K elements and 100 reversals | 2.05 ms | 7.70 ms | 3.8x faster |
| Mixed array, 100K elements and 100 reversals | 1.03 ms | 7.69 ms | 7.5x faster |
| Numeric callback, 2M calls (experimental) | 1.41 ms | 0.82 ms | 1.7x slower |
| Captured numeric callback, 2M calls | 5.08 ms | 4.97 ms | 2% slower |
| Local function, 2M direct calls | 0.47 ms | 0.81 ms | 1.7x faster |
| Self-returning function, 2M calls | 4.93 ms | 0.76 ms | 6.5x slower |
| `Object.keys(value).length`, 100K calls | 0.070 ms | 0.910 ms | 12.9x faster |
| `Object.keys(value)`, 100K calls and key reads | 1.93 ms | 1.10 ms | 1.8x slower |
| Local object spread, 100K two-field copies | 0.09 ms | 0.99 ms | 10.5x faster |
| Object spread, 100K copies stored in an array | 4.05 ms | 2.37 ms | 1.7x slower |
| Computed keys, 100K objects and reads | 4.37 ms | 26.22 ms | 6.0x faster |
| `Object.values(value)`, 100K calls and reads | 1.42 ms | 9.84 ms | 6.9x faster |
| `Object.entries(value)`, 100K calls and reads | 5.62 ms | 16.64 ms | 3.0x faster |

Compilation times:

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 329.01 ms | 330.58 ms |
| 5,000 variables | 434.87 ms | 369.56 ms |

Object.values and Object.entries run 6.9x and 3.0x faster than Node in these tests. Stored object copies and self-returning functions remain slower, so allocation-heavy code still needs work.

Compilation times remain close to Microsoft’s Go-based TypeScript compiler, though the larger source takes about 18% longer.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11 (SDK 10.0.400), TypeScript 7.0.2. Runtime results use matching sources and checksums, at least 100 warmups and 500 ms of warmup per engine, and 35 timed samples across five fresh processes per engine; compilation results are medians of seven fresh processes with warm filesystem caches. Runtime timings exclude startup and compilation.
