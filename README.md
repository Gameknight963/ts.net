# ts.net

Work-in-progress TypesScript to IL compiler.


## Building

The compiler has significant portions written in [crust](https://github.com/Gameknight963/crust), my C-like memory-safe programming lanuage. To build it, you'll need to install `crust` and `crustc`.

Then run `.\scripts\build.ps1 -Run`. You might figure out what dependencies you're missing if you run it.

Building on linux is not supported atm.

## Current performance

As of [f0dc5bc](https://github.com/Gameknight963/ts.net/commit/f0dc5bc47bb8364e7304b198ff27698dbb113048):

Runtime performance:

| Workload | ts.net | Node | Result |
|---|---:|---:|---|
| Numeric loop, 20M iterations | 14.29 ms | 73.79 ms | 5.2x faster |
| Typed-object loop, 500K iterations | 0.35 ms | 0.65 ms | 1.8x faster |
| `any` property loop, 350K iterations | 0.28 ms | 0.43 ms | 1.5x faster |
| Mixed array, 100K elements and 5 traversals | 0.97 ms | 1.11 ms | 1.1x faster |
| Numeric `any[]`, 1M iterations | 0.70 ms | 1.64 ms | 2.3x faster |
| Numeric array, 100K elements and 100 reversals | 1.99 ms | 7.76 ms | 3.9x faster |
| Mixed array, 100K elements and 100 reversals | 1.01 ms | 7.69 ms | 7.6x faster |
| Numeric callback, 2M calls (experimental) | 1.37 ms | 0.82 ms | 1.7x slower |
| Captured numeric callback, 2M calls | 5.10 ms | 4.99 ms | 2% slower |
| Local function, 2M direct calls | 0.47 ms | 0.82 ms | 1.8x faster |
| Self-returning function, 2M calls | 4.90 ms | 0.76 ms | 6.4x slower |
| `Object.keys(value).length`, 100K calls | 0.071 ms | 0.920 ms | 13.0x faster |
| `Object.keys(value)`, 100K calls and key reads | 1.23 ms | 1.11 ms | 1.1x slower |
| Local object spread, 100K two-field copies | 0.09 ms | 1.03 ms | 10.9x faster |
| Object spread, 100K copies stored in an array | 2.98 ms | 2.42 ms | 1.2x slower |
| Computed keys, 100K objects and reads | 3.76 ms | 26.00 ms | 6.9x faster |
| `Object.values(value)`, 100K calls and reads | 1.42 ms | 9.39 ms | 6.6x faster |
| `Object.entries(value)`, 100K calls and reads | 5.27 ms | 16.70 ms | 3.2x faster |
| Literal squares, 2M iterations | 1.39 ms | 1.41 ms | 1% faster |
| Small integer bases and varying exponents, 1M iterations | 4.67 ms | 13.84 ms | 3.0x faster |

Compilation times:

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 326.42 ms | 327.06 ms |
| 5,000 variables | 436.63 ms | 372.17 ms |

Small integer bases with varying exponents now run 3.0x faster than Node in this test; the runtime avoids general power calls when every multiplication is exact. Literal squares remain about tied, and this improvement does not establish a speedup for fractional powers.

Compilation times remain close to Microsoft’s Go-based TypeScript compiler, though the larger source takes about 17% longer.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11, TypeScript 7.0.2
