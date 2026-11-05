# ts.net

Work-in-progress TypesScript to IL compiler.


## Building

The compiler has significant portions written in [crust](https://github.com/Gameknight963/crust), my C-like memory-safe programming lanuage. To build it, you'll need to install `crust` and `crustc`.

Then run `.\scripts\build.ps1 -Run`. You might figure out what dependencies you're missing if you run it.

Building on linux is not supported atm.

## Current performance

As of [691df7a](https://github.com/Gameknight963/ts.net/commit/691df7a73b65168274da9342e49ac5f12e739073):

Runtime performance:

| Workload | ts.net | Node | Result |
|---|---:|---:|---|
| Numeric loop, 20M iterations | 14.44 ms | 75.13 ms | 5.2x faster |
| Typed-object loop, 500K iterations | 0.36 ms | 0.66 ms | 1.8x faster |
| `any` property loop, 350K iterations | 0.28 ms | 0.43 ms | 1.5x faster |
| Mixed array, 100K elements and 5 traversals | 1.09 ms | 1.21 ms | 10% faster |
| Numeric `any[]`, 1M iterations | 0.71 ms | 1.65 ms | 2.3x faster |
| Numeric array, 100K elements and 100 reversals | 2.02 ms | 7.78 ms | 3.9x faster |
| Mixed array, 100K elements and 100 reversals | 0.98 ms | 7.64 ms | 7.8x faster |
| Numeric callback, 2M calls (experimental) | 1.41 ms | 0.82 ms | 1.7x slower |
| Captured numeric callback, 2M calls | 5.12 ms | 5.04 ms | 2% slower |
| Local function, 2M direct calls | 0.47 ms | 0.83 ms | 1.8x faster |
| Self-returning function, 2M calls | 5.02 ms | 0.77 ms | 6.5x slower |
| `Object.keys(value).length`, 100K calls | 0.071 ms | 0.906 ms | 12.8x faster |
| `Object.keys(value)`, 100K calls and key reads | 1.40 ms | 1.12 ms | 1.3x slower |
| Local object spread, 100K two-field copies | 0.09 ms | 1.01 ms | 10.7x faster |
| Object spread, 100K copies stored in an array | 3.95 ms | 2.45 ms | 1.6x slower |
| Computed keys, 100K objects and reads | 4.15 ms | 26.44 ms | 6.4x faster |
| `Object.values(value)`, 100K calls and reads | 1.49 ms | 9.68 ms | 6.5x faster |
| `Object.entries(value)`, 100K calls and reads | 5.72 ms | 16.84 ms | 2.9x faster |
| Literal squares, 2M iterations | 1.42 ms | 1.42 ms | 0% faster |
| Small integer bases and varying exponents, 1M iterations | 4.70 ms | 13.87 ms | 3.0x faster |
| Mixed numeric strings, 1M conversions | 27.51 ms | 30.06 ms | 8% faster |
| Object valueOf, 1M numeric conversions | 12.94 ms | 23.78 ms | 1.8x faster |

Compilation times:

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 331.05 ms | 345.55 ms |
| 5,000 variables | 457.74 ms | 391.77 ms |

Object-to-number conversion now calls valueOf and toString in order; the valueOf workload runs 1.8x faster than Node here. Stored object copies and self-returning functions remain slower, so allocation and function calls still need work.

Compilation times remain close to Microsoft’s Go-based TypeScript compiler, though the larger source takes about 17% longer.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11, TypeScript 7.0.2
