# ts.net

Work-in-progress TypesScript to IL compiler.


## Building

The compiler has significant portions written in [crust](https://github.com/Gameknight963/crust), my C-like memory-safe programming lanuage. To build it, you'll need to install `crust` and `crustc`.

Then run `.\scripts\build.ps1 -Run`. You might figure out what dependencies you're missing if you run it.

Building on linux is not supported atm.

## Current performance

As of [f385b5c](https://github.com/Gameknight963/ts.net/commit/f385b5c99cc3d0d4e3888daeee41b87d076fbd0f):

Runtime performance:

| Workload | ts.net | Node | Result |
|---|---:|---:|---|
| Numeric loop, 20M iterations | 14.46 ms | 75.99 ms | 5.3x faster |
| Typed-object loop, 500K iterations | 0.36 ms | 0.66 ms | 1.8x faster |
| `any` property loop, 350K iterations | 0.28 ms | 0.43 ms | 1.5x faster |
| Mixed array, 100K elements and 5 traversals | 1.00 ms | 1.19 ms | 1.2x faster |
| Numeric `any[]`, 1M iterations | 0.72 ms | 1.67 ms | 2.3x faster |
| Numeric array, 100K elements and 100 reversals | 2.01 ms | 7.93 ms | 3.9x faster |
| Mixed array, 100K elements and 100 reversals | 1.06 ms | 7.76 ms | 7.3x faster |
| Numeric callback, 2M calls (experimental) | 1.43 ms | 0.84 ms | 1.7x slower |
| Captured numeric callback, 2M calls | 5.31 ms | 5.15 ms | 3% slower |
| Local function, 2M direct calls | 0.48 ms | 0.85 ms | 1.8x faster |
| Self-returning function, 2M calls | 5.19 ms | 0.78 ms | 6.6x slower |
| `Object.keys(value).length`, 100K calls | 0.071 ms | 0.952 ms | 13.4x faster |
| `Object.keys(value)`, 100K calls and key reads | 1.51 ms | 1.13 ms | 1.3x slower |
| Local object spread, 100K two-field copies | 0.09 ms | 1.03 ms | 10.9x faster |
| Object spread, 100K copies stored in an array | 4.01 ms | 2.49 ms | 1.6x slower |
| Computed keys, 100K objects and reads | 4.68 ms | 26.83 ms | 5.7x faster |
| `Object.values(value)`, 100K calls and reads | 1.66 ms | 9.65 ms | 5.8x faster |
| `Object.entries(value)`, 100K calls and reads | 6.17 ms | 17.13 ms | 2.8x faster |

Compilation times:

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 328.46 ms | 333.98 ms |
| 5,000 variables | 448.68 ms | 378.37 ms |

Key enumeration improved, though Node is still faster in that test. Object.values and Object.entries remain faster here; stored object copies and self-returning functions still need work.

Compilation times remain close to Microsoft’s Go-based TypeScript compiler, though the larger source takes about 19% longer.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11, TypeScript 7.0.2
