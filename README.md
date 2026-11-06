# ts.net

Work-in-progress TypesScript to IL compiler.


## Building

Reqires .NET 10 SDK, Go 1.27 or newer, and Git.

The first build downloads the pinned Microsoft TypeScript frontend source.

Then run `.\scripts\build.ps1 -Run` to compile and run an example.

## Current performance

As of [90ced6f](https://github.com/Gameknight963/ts.net/commit/90ced6f8ad9e15fbd99f57cccc6f92bb0f593211):

Runtime performance:

| Workload | ts.net | Node | Result |
|---|---|---|---|
| Numeric loop, 20M iterations | 14.56 ms | 75.98 ms | 5.2x faster |
| Typed-object loop, 500K iterations | 0.36 ms | 0.65 ms | 1.8x faster |
| `any` property loop, 350K iterations | 0.28 ms | 0.43 ms | 1.5x faster |
| Mixed array, 100K elements and 5 traversals | 1.03 ms | 1.21 ms | 1.2x faster |
| Numeric `any[]`, 1M iterations | 0.71 ms | 1.66 ms | 2.3x faster |
| Numeric array, 100K elements and 100 reversals | 2.08 ms | 7.90 ms | 3.8x faster |
| Mixed array, 100K elements and 100 reversals | 0.99 ms | 7.67 ms | 7.7x faster |
| Numeric callback, 2M calls (experimental) | 1.41 ms | 0.83 ms | 1.7x slower |
| Captured numeric callback, 2M calls | 5.25 ms | 5.09 ms | 3% slower |
| Local function, 2M direct calls | 0.47 ms | 0.83 ms | 1.8x faster |
| Self-returning function, 2M calls | 4.99 ms | 0.77 ms | 6.5x slower |
| `Object.keys(value).length`, 100K calls | 0.071 ms | 0.916 ms | 12.9x faster |
| `Object.keys(value)`, 100K calls and key reads | 1.33 ms | 1.12 ms | 1.2x slower |
| Local object spread, 100K two-field copies | 0.09 ms | 1.06 ms | 11.3x faster |
| Object spread, 100K copies stored in an array | 3.26 ms | 2.39 ms | 1.4x slower |
| Computed keys, 100K objects and reads | 4.16 ms | 26.22 ms | 6.3x faster |
| `Object.values(value)`, 100K calls and reads | 1.51 ms | 9.68 ms | 6.4x faster |
| `Object.entries(value)`, 100K calls and reads | 5.71 ms | 16.91 ms | 3.0x faster |
| Literal squares, 2M iterations | 1.43 ms | 1.42 ms | 1% slower |
| Small integer bases and varying exponents, 1M iterations | 4.11 ms | 13.92 ms | 3.4x faster |
| Mixed numeric strings, 1M conversions | 29.04 ms | 30.43 ms | 5% faster |
| Object valueOf, 1M numeric conversions | 13.22 ms | 23.82 ms | 1.8x faster |
| String ordering, 2M comparisons | 8.94 ms | 8.79 ms | 2% slower |
| `==` on mixed values, 1M comparisons | 5.21 ms | 7.25 ms | 1.4x faster |
| `== null` on nullable arrays, 1M comparisons | 1.53 ms | 1.23 ms | 1.2x slower |
| Numeric array `.at(-1)`, 1M calls | 3.73 ms | 3.83 ms | 3% faster |
| String `.at(index)`, 1M calls | 4.32 ms | 5.28 ms | 1.2x faster |
| String `charCodeAt(index)`, 1M calls | 2.25 ms | 1.67 ms | 1.3x slower |
| String `codePointAt(index)`, 1M calls | 2.89 ms | 1.90 ms | 1.5x slower |
| Masked integer remainder, 1M iterations | 1.11 ms | 1.12 ms | 1% faster |
| String slice and first-character read, 1M calls | 14.96 ms | 8.98 ms | 1.7x slower |
| String substring and first-character read, 1M calls | 14.69 ms | 10.97 ms | 1.3x slower |
| Numeric string index and comparison, 1M reads | 4.93 ms | 1.91 ms | 2.6x slower |

Compilation times:

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 496.80 ms | 356.76 ms |
| 5,000 variables | 682.69 ms | 390.92 ms |

The numeric string index loop fell from 67.13 ms to 4.93 ms per million reads, though Node still takes 1.91 ms. String ranges improved too, but larger partial results still allocate a string.

The compiler used to be written in [crust](https://github.com/Gameknight963/crust), my C-like memory safe language. It was ported to C# for various reasons. The point is, compilation is slower after the C# port. The larger source takes 1.7x as long as Microsoft’s Go-based TypeScript compiler. It used to be more or less on par.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11, TypeScript 7.0.2
