# ts.net

Work-in-progress TypesScript to IL compiler.


## Building

The compiler has significant portions written in [crust](https://github.com/Gameknight963/crust), my C-like memory-safe programming lanuage. To build it, you'll need to install `crust` and `crustc`.

Then run `.\scripts\build.ps1 -Run`. You might figure out what dependencies you're missing if you run it.

Building on linux is not supported atm.

## Current performance

As of [7df3dc6](https://github.com/Gameknight963/ts.net/commit/7df3dc652dcc5cc91653028cb0074a218ce74473):

Runtime performance:

| Workload | ts.net | Node | Result |
|---|---:|---:|---|
| Numeric loop, 20M iterations | 14.35 ms | 74.46 ms | 5.2x faster |
| Typed-object loop, 500K iterations | 0.41 ms | 0.65 ms | 1.6x faster |
| `any` property loop, 350K iterations | 0.26 ms | 0.43 ms | 1.7x faster |
| Mixed array, 100K elements and 5 traversals | 1.01 ms | 1.30 ms | 1.3x faster |
| Numeric `any[]`, 1M iterations | 0.70 ms | 1.66 ms | 2.4x faster |
| Numeric array, 100K elements and 100 reversals | 2.03 ms | 7.90 ms | 3.9x faster |
| Mixed array, 100K elements and 100 reversals | 1.06 ms | 7.66 ms | 7.2x faster |
| Numeric callback, 2M calls (experimental) | 1.42 ms | 0.82 ms | 1.7x slower |
| Captured numeric callback, 2M calls | 5.20 ms | 5.04 ms | 3% slower |
| Local function, 2M direct calls | 0.47 ms | 0.83 ms | 1.8x faster |
| Self-returning function, 2M calls | 4.96 ms | 0.77 ms | 6.5x slower |

Compilation times:

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 335.80 ms | 333.51 ms |
| 5,000 variables | 442.01 ms | 381.75 ms |

Numeric and direct local-function loops remain faster in these tests. Self-returning functions now work without per-call allocation, but their dispatch and identity checks remain much slower than Node.

Compilation times remain close to Microsoft's Go-based TypeScript compiler, though the larger source takes about 16% longer.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11 (SDK 10.0.400), TypeScript 7.0.2. Runtime results use matching sources and checksums, at least 100 warmups and 500 ms of warmup per engine, and 35 timed samples across five fresh processes per engine; compilation results are medians of seven fresh processes with warm filesystem caches. Runtime timings exclude startup and compilation.
