# ts.net

Work-in-progress TypesScript to IL compiler.


## Building

The compiler has significant portions written in [crust](https://github.com/Gameknight963/crust), my C-like memory-safe programming lanuage. To build it, you'll need to install `crust` and `crustc`.

Then run `.\scripts\build.ps1 -Run`. You might figure out what dependencies you're missing if you run it.

Building on linux is not supported atm.

## Current performance

As of [03f8cf3](https://github.com/Gameknight963/ts.net/commit/03f8cf3056db4877740e5642a81705ab2281855e):

Runtime performance:

| Workload | ts.net | Node | Result |
|---|---:|---:|---|
| Numeric loop, 20M iterations | 14.32 ms | 73.79 ms | 5.2x faster |
| Typed-object loop, 500K iterations | 0.35 ms | 0.65 ms | 1.8x faster |
| `any` property loop, 350K iterations | 0.28 ms | 0.43 ms | 1.5x faster |
| Mixed array, 100K elements and 5 traversals | 0.99 ms | 1.29 ms | 1.3x faster |
| Numeric `any[]`, 1M iterations | 0.71 ms | 1.64 ms | 2.3x faster |
| Numeric array, 100K elements and 100 reversals | 2.03 ms | 7.73 ms | 3.8x faster |
| Mixed array, 100K elements and 100 reversals | 1.00 ms | 7.59 ms | 7.6x faster |
| Numeric callback, 2M calls (experimental) | 1.40 ms | 0.82 ms | 1.7x slower |
| Captured numeric callback, 2M calls | 5.08 ms | 5.01 ms | 1% slower |
| Local function, 2M direct calls | 0.47 ms | 0.82 ms | 1.8x faster |
| Self-returning function, 2M calls | 4.93 ms | 0.76 ms | 6.5x slower |
| `Object.keys(value).length`, 100K calls | 0.071 ms | 0.916 ms | 13.0x faster |
| `Object.keys(value)`, 100K calls and key reads | 1.87 ms | 1.10 ms | 1.7x slower |
| Object spread, 100K two-field copies | 3.02 ms | 1.01 ms | 3.0x slower |

Compilation times:

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 329.41 ms | 329.46 ms |
| 5,000 variables | 425.33 ms | 371.28 ms |

Numeric loops and key counting remain encouraging. Object spread now works, but copying even a small object is 3.0x slower than Node here; object copying and callback dispatch still need work.

Compilation times remain close to Microsoft’s Go-based TypeScript compiler, though the larger source takes about 15% longer.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11 (SDK 10.0.400), TypeScript 7.0.2. Runtime results use matching sources and checksums, at least 100 warmups and 500 ms of warmup per engine, and 35 timed samples across five fresh processes per engine; compilation results are medians of seven fresh processes with warm filesystem caches. Runtime timings exclude startup and compilation.
