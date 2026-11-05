# ts.net

Work-in-progress TypesScript to IL compiler.


## Building

The compiler has significant portions written in [crust](https://github.com/Gameknight963/crust), my C-like memory-safe programming lanuage. To build it, you'll need to install `crust` and `crustc`.

Then run `.\scripts\build.ps1 -Run`. You might figure out what dependencies you're missing if you run it.

Building on linux is not supported atm.

## Current performance

As of [c2dfae4](https://github.com/Gameknight963/ts.net/commit/c2dfae47781c9b681e06538203f6ae771e265968):

Runtime performance:

| Workload | ts.net | Node | Result |
|---|---:|---:|---|
| Numeric loop, 20M iterations | 14.47 ms | 75.52 ms | 5.2x faster |
| Typed-object loop, 500K iterations | 0.36 ms | 0.65 ms | 1.8x faster |
| `any` property loop, 350K iterations | 0.28 ms | 0.43 ms | 1.5x faster |
| Mixed array, 100K elements and 5 traversals | 1.03 ms | 1.19 ms | 1.2x faster |
| Numeric `any[]`, 1M iterations | 0.71 ms | 1.65 ms | 2.3x faster |
| Numeric array, 100K elements and 100 reversals | 2.02 ms | 7.84 ms | 3.9x faster |
| Mixed array, 100K elements and 100 reversals | 1.09 ms | 7.81 ms | 7.2x faster |
| Numeric callback, 2M calls (experimental) | 1.41 ms | 0.83 ms | 1.7x slower |
| Captured numeric callback, 2M calls | 5.10 ms | 5.03 ms | 1% slower |
| Local function, 2M direct calls | 0.47 ms | 0.83 ms | 1.8x faster |
| Self-returning function, 2M calls | 4.97 ms | 0.77 ms | 6.5x slower |
| `Object.keys(value).length`, 100K calls | 0.071 ms | 0.915 ms | 12.9x faster |
| `Object.keys(value)`, 100K calls and key reads | 1.25 ms | 1.10 ms | 1.1x slower |
| Local object spread, 100K two-field copies | 0.09 ms | 1.01 ms | 10.7x faster |
| Object spread, 100K copies stored in an array | 2.77 ms | 2.45 ms | 1.1x slower |
| Computed keys, 100K objects and reads | 3.65 ms | 26.04 ms | 7.1x faster |
| `Object.values(value)`, 100K calls and reads | 1.44 ms | 9.51 ms | 6.6x faster |
| `Object.entries(value)`, 100K calls and reads | 5.40 ms | 16.69 ms | 3.1x faster |
| Literal squares, 2M iterations | 1.40 ms | 1.41 ms | 0% faster |
| Small integer bases and varying exponents, 1M iterations | 4.66 ms | 13.70 ms | 2.9x faster |
| Mixed numeric strings, 1M conversions | 28.63 ms | 29.86 ms | 4% faster |

Compilation times:

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 330.40 ms | 332.59 ms |
| 5,000 variables | 434.10 ms | 373.29 ms |

Numeric-string conversion now handles decimal, hexadecimal, binary, octal, and JavaScript whitespace; the mixed-format test is about tied with Node. Small integer powers remain faster here, but object-to-number conversion still needs implementation.

Compilation times remain close to Microsoft’s Go-based TypeScript compiler, though the larger source takes about 16% longer.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11, TypeScript 7.0.2
