# ts.net

Work-in-progress TypesScript to IL compiler.


## Building

Reqires .NET 10 SDK, Go 1.27 or newer, and Git.

The first build downloads the pinned Microsoft TypeScript frontend source.

Then run `.\scripts\build.ps1 -Run` to compile and run an example.

## Current performance

As of [92e9808](https://github.com/Gameknight963/ts.net/commit/92e98089b3bceee67870905bf52032c339c8ec29):

Runtime performance:

| Workload | ts.net | Node | Result |
|---|---|---|---|
| Numeric loop, 20M iterations | 14.38 ms | 74.38 ms | 5.2x faster |
| Typed-object loop, 500K iterations | 0.36 ms | 0.65 ms | 1.8x faster |
| `any` property loop, 350K iterations | 0.28 ms | 0.43 ms | 1.5x faster |
| Mixed array, 100K elements and 5 traversals | 1.05 ms | 1.39 ms | 1.3x faster |
| Numeric `any[]`, 1M iterations | 0.71 ms | 1.65 ms | 2.3x faster |
| Numeric array, 100K elements and 100 reversals | 2.10 ms | 7.77 ms | 3.7x faster |
| Mixed array, 100K elements and 100 reversals | 1.00 ms | 7.69 ms | 7.7x faster |
| Numeric callback, 2M calls (experimental) | 1.42 ms | 0.83 ms | 1.7x slower |
| Captured numeric callback, 2M calls | 5.15 ms | 5.03 ms | 3% slower |
| Local function, 2M direct calls | 0.47 ms | 0.82 ms | 1.7x faster |
| Self-returning function, 2M calls | 5.02 ms | 0.78 ms | 6.4x slower |
| `Object.keys(value).length`, 100K calls | 0.071 ms | 0.915 ms | 12.9x faster |
| `Object.keys(value)`, 100K calls and key reads | 1.34 ms | 1.14 ms | 1.2x slower |
| Local object spread, 100K two-field copies | 0.09 ms | 1.02 ms | 10.8x faster |
| Object spread, 100K copies stored in an array | 2.91 ms | 2.48 ms | 1.2x slower |
| Computed keys, 100K objects and reads | 4.09 ms | 26.36 ms | 6.4x faster |
| `Object.values(value)`, 100K calls and reads | 1.47 ms | 9.69 ms | 6.6x faster |
| `Object.entries(value)`, 100K calls and reads | 5.56 ms | 16.81 ms | 3.0x faster |
| Literal squares, 2M iterations | 1.41 ms | 1.42 ms | About equal |
| Small integer bases and varying exponents, 1M iterations | 4.07 ms | 13.83 ms | 3.4x faster |
| Mixed numeric strings, 1M conversions | 27.22 ms | 30.15 ms | 10% faster |
| Object valueOf, 1M numeric conversions | 14.40 ms | 23.82 ms | 1.7x faster |
| String ordering, 2M comparisons | 8.87 ms | 8.84 ms | About equal |
| `==` on mixed values, 1M comparisons | 5.18 ms | 7.22 ms | 1.4x faster |
| `== null` on nullable arrays, 1M comparisons | 1.51 ms | 1.15 ms | 1.3x slower |
| Numeric array `.at(-1)`, 1M calls | 3.77 ms | 3.78 ms | About equal |
| String `.at(index)`, 1M calls | 4.02 ms | 5.29 ms | 1.3x faster |
| String `charCodeAt(index)`, 1M calls | 2.25 ms | 1.65 ms | 1.4x slower |
| String `codePointAt(index)`, 1M calls | 2.85 ms | 1.88 ms | 1.5x slower |
| Masked integer remainder, 1M iterations | 1.10 ms | 1.10 ms | About equal |

Compilation times:

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 485.76 ms | 345.25 ms |
| 5,000 variables | 692.55 ms | 392.09 ms |

Masked integer remainder is now close to Node here, down from 7.44 ms to about 1.1 ms per million iterations. This applies only where the compiler proves a nonnegative integer; general numbers keep JavaScript remainder behavior.

The compiler used to be written in [crust](https://github.com/Gameknight963/crust), my C-like memory safe language. It was ported to C# for various reasons. The point is, compilation is slower after the C# port. The larger source takes 1.8x as long as Microsoft’s Go-based TypeScript compiler. It used to be more or less on par.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11, TypeScript 7.0.2
