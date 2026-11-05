# ts.net

Work-in-progress TypesScript to IL compiler.


## Building

The compiler has significant portions written in [crust](https://github.com/Gameknight963/crust), my C-like memory-safe programming lanuage. To build it, you'll need to install `crust` and `crustc`.

Then run `.\scripts\build.ps1 -Run`. You might figure out what dependencies you're missing if you run it.

Building on linux is not supported atm.

## Current performance

As of [eb01eac](https://github.com/Gameknight963/ts.net/commit/eb01eac201312bf4e4da0a50cdcb8906ff6bc349):

Runtime performance:

| Workload | ts.net | Node | Result |
|---|---:|---:|---|
| Numeric loop, 20M iterations | 14.42 ms | 75.09 ms | 5.2x faster |
| Typed-object loop, 500K iterations | 0.36 ms | 0.65 ms | 1.8x faster |
| `any` property loop, 350K iterations | 0.28 ms | 0.43 ms | 1.5x faster |
| Mixed array, 100K elements and 5 traversals | 0.98 ms | 1.11 ms | 1.1x faster |
| Numeric `any[]`, 1M iterations | 0.71 ms | 1.67 ms | 2.3x faster |
| Numeric array, 100K elements and 100 reversals | 2.12 ms | 7.84 ms | 3.7x faster |
| Mixed array, 100K elements and 100 reversals | 1.02 ms | 7.69 ms | 7.5x faster |
| Numeric callback, 2M calls (experimental) | 1.41 ms | 0.83 ms | 1.7x slower |
| Captured numeric callback, 2M calls | 5.16 ms | 5.08 ms | 2% slower |
| Local function, 2M direct calls | 0.47 ms | 0.82 ms | 1.8x faster |
| Self-returning function, 2M calls | 5.04 ms | 0.77 ms | 6.6x slower |
| `Object.keys(value).length`, 100K calls | 0.071 ms | 0.928 ms | 13.1x faster |
| `Object.keys(value)`, 100K calls and key reads | 2.04 ms | 1.12 ms | 1.8x slower |
| Local object spread, 100K two-field copies | 0.09 ms | 1.01 ms | 10.7x faster |
| Object spread, 100K copies stored in an array | 3.30 ms | 2.33 ms | 1.4x slower |
| Computed keys, 100K objects and reads | 4.61 ms | 26.40 ms | 5.7x faster |

Compilation times:

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 329.64 ms | 343.85 ms |
| 5,000 variables | 446.37 ms | 384.30 ms |

Computed keys work and run 5.7x faster than Node in this test; keeping small property sets out of dictionaries helped. Stored copies still take 1.4x as long as Node, so allocation-heavy code needs more work.

Compilation times remain close to Microsoft’s Go-based TypeScript compiler, though the larger source takes about 16% longer.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11 (SDK 10.0.400), TypeScript 7.0.2. Runtime results use matching sources and checksums, at least 100 warmups and 500 ms of warmup per engine, and 35 timed samples across five fresh processes per engine; compilation results are medians of seven fresh processes with warm filesystem caches. Runtime timings exclude startup and compilation.
