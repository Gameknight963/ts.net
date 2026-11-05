# ts.net

Work-in-progress TypesScript to IL compiler.


## Building

The compiler has significant portions written in [crust](https://github.com/Gameknight963/crust), my C-like memory-safe programming lanuage. To build it, you'll need to install `crust` and `crustc`.

Then run `.\scripts\build.ps1 -Run`. You might figure out what dependencies you're missing if you run it.

Building on linux is not supported atm.

## Current performance

As of [bcf51ee](https://github.com/Gameknight963/ts.net/commit/bcf51eef349af01d91ab5b028afe679cf872c95b):

Runtime performance:

| Workload | ts.net | Node | Result |
|---|---:|---:|---|
| Numeric loop, 20M iterations | 14.38 ms | 74.42 ms | 5.2x faster |
| Typed-object loop, 500K iterations | 0.36 ms | 0.65 ms | 1.8x faster |
| `any` property loop, 350K iterations | 0.28 ms | 0.43 ms | 1.5x faster |
| Mixed array, 100K elements and 5 traversals | 0.99 ms | 1.21 ms | 1.2x faster |
| Numeric `any[]`, 1M iterations | 0.70 ms | 1.66 ms | 2.4x faster |
| Numeric array, 100K elements and 100 reversals | 1.97 ms | 7.75 ms | 3.9x faster |
| Mixed array, 100K elements and 100 reversals | 1.02 ms | 7.60 ms | 7.4x faster |
| Numeric callback, 2M calls (experimental) | 1.40 ms | 0.83 ms | 1.7x slower |
| Captured numeric callback, 2M calls | 5.17 ms | 5.03 ms | 3% slower |
| Local function, 2M direct calls | 0.47 ms | 0.83 ms | 1.7x faster |
| Self-returning function, 2M calls | 4.92 ms | 0.77 ms | 6.4x slower |
| `Object.keys(value).length`, 100K calls | 0.071 ms | 0.904 ms | 12.8x faster |
| `Object.keys(value)`, 100K calls and key reads | 1.27 ms | 1.11 ms | 1.1x slower |
| Local object spread, 100K two-field copies | 0.09 ms | 1.00 ms | 10.6x faster |
| Object spread, 100K copies stored in an array | 3.06 ms | 2.46 ms | 1.2x slower |
| Computed keys, 100K objects and reads | 3.78 ms | 25.98 ms | 6.9x faster |
| `Object.values(value)`, 100K calls and reads | 1.45 ms | 9.51 ms | 6.6x faster |
| `Object.entries(value)`, 100K calls and reads | 5.49 ms | 16.86 ms | 3.1x faster |
| Literal squares, 2M iterations | 1.40 ms | 1.41 ms | 1% faster |
| Varying exponents, 1M iterations | 18.76 ms | 13.68 ms | 1.4x slower |

Compilation times:

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 327.71 ms | 323.35 ms |
| 5,000 variables | 431.11 ms | 373.90 ms |

Literal squares are about tied with Node, while varying exponents take 1.4x as long. Object.values and Object.entries remain faster in these tests, but general exponentiation and allocation-heavy workloads need more work.

Compilation times remain close to Microsoft’s Go-based TypeScript compiler, though the larger source takes about 15% longer.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11, TypeScript 7.0.2
