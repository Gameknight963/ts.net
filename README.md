# ts.net

Work-in-progress TypesScript to IL compiler.


## Building

The compiler has significant portions written in [crust](https://github.com/Gameknight963/crust), my C-like memory-safe programming lanuage. To build it, you'll need to install `crust` and `crustc`.

Then run `.\scripts\build.ps1 -Run`. You might figure out what dependencies you're missing if you run it.

Building on linux is not supported atm.

## Current performance

As of [5ee8e21](https://github.com/Gameknight963/ts.net/commit/5ee8e2188977b5ca5983a65ffcc91612d86207cf):

Runtime performance:

| Workload | ts.net | Node | Result |
|---|---:|---:|---|
| Numeric loop, 20M iterations | 14.33 ms | 73.88 ms | 5.2x faster |
| Typed-object loop, 500K iterations | 0.41 ms | 0.65 ms | 1.6x faster |
| `any` property loop, 350K iterations | 0.27 ms | 0.43 ms | 1.6x faster |
| Mixed array, 100K elements and 5 traversals | 0.94 ms | 1.15 ms | 1.2x faster |
| Numeric `any[]`, 1M iterations | 0.70 ms | 1.64 ms | 2.4x faster |
| Numeric array, 100K elements and 100 reversals | 2.03 ms | 7.73 ms | 3.8x faster |
| Mixed array, 100K elements and 100 reversals | 1.02 ms | 7.64 ms | 7.5x faster |
| Numeric callback, 2M calls (experimental) | 1.40 ms | 0.82 ms | 1.7x slower |
| Captured numeric callback, 2M calls | 5.10 ms | 4.97 ms | 3% slower |
| Local function, 2M direct calls | 0.47 ms | 0.82 ms | 1.8x faster |
| Self-returning function, 2M calls | 4.91 ms | 0.76 ms | 6.5x slower |
| `Object.keys(value).length`, 100K calls | 0.070 ms | 0.910 ms | 13.0x faster |
| `Object.keys(value)`, 100K calls and key reads | 1.93 ms | 1.11 ms | 1.7x slower |
| Local object spread, 100K two-field copies | 0.07 ms | 1.01 ms | 14.4x faster |
| Object spread, 100K copies stored in an array | 18.10 ms | 2.29 ms | 7.9x slower |

Compilation times:

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 326.39 ms | 326.46 ms |
| 5,000 variables | 432.65 ms | 370.74 ms |

Local spread copies now avoid per-iteration allocation and run 14.4x faster than Node here. Storing the copies in an array still takes 7.9x as long as Node; escaping objects and callback dispatch need more work.

Compilation times remain close to Microsoft’s Go-based TypeScript compiler, though the larger source takes about 17% longer.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11 (SDK 10.0.400), TypeScript 7.0.2. Runtime results use matching sources and checksums, at least 100 warmups and 500 ms of warmup per engine, and 35 timed samples across five fresh processes per engine; compilation results are medians of seven fresh processes with warm filesystem caches. Runtime timings exclude startup and compilation.
