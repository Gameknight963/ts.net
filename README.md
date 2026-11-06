# ts.net

Work-in-progress TypesScript to IL compiler.


## Building

Reqires .NET 10 SDK, Go 1.27 or newer, and Git.

The first build downloads the pinned Microsoft TypeScript frontend source.

Then run `.\scripts\build.ps1 -Run` to compile and run an example.

## Current performance

As of [e87ce11](https://github.com/Gameknight963/ts.net/commit/e87ce11b64bb936dd29ae68adc9cfa27d50d8408):

Runtime performance:

| Workload | ts.net | Node | Result |
|---|---|---|---|
| Numeric loop, 20M iterations | 14.42 ms | 74.03 ms | 5.1x faster |
| Typed-object loop, 500K iterations | 0.41 ms | 0.67 ms | 1.6x faster |
| `any` property loop, 350K iterations | 0.28 ms | 0.43 ms | 1.5x faster |
| Mixed array, 100K elements and 5 traversals | 0.97 ms | 1.19 ms | 1.2x faster |
| Numeric `any[]`, 1M iterations | 0.70 ms | 1.67 ms | 2.4x faster |
| Numeric array, 100K elements and 100 reversals | 2.11 ms | 7.84 ms | 3.7x faster |
| Mixed array, 100K elements and 100 reversals | 1.04 ms | 7.85 ms | 7.5x faster |
| Numeric callback, 2M calls (experimental) | 1.40 ms | 0.82 ms | 1.7x slower |
| Captured numeric callback, 2M calls | 5.19 ms | 5.04 ms | 3% slower |
| Local function, 2M direct calls | 0.47 ms | 0.83 ms | 1.7x faster |
| Self-returning function, 2M calls | 5.00 ms | 0.77 ms | 6.5x slower |
| `Object.keys(value).length`, 100K calls | 0.071 ms | 0.924 ms | 13.1x faster |
| `Object.keys(value)`, 100K calls and key reads | 1.25 ms | 1.12 ms | 1.1x slower |
| Local object spread, 100K two-field copies | 0.09 ms | 0.99 ms | 10.6x faster |
| Object spread, 100K copies stored in an array | 3.23 ms | 2.34 ms | 1.4x slower |
| Computed keys, 100K objects and reads | 4.25 ms | 25.89 ms | 6.1x faster |
| `Object.values(value)`, 100K calls and reads | 1.42 ms | 9.47 ms | 6.6x faster |
| `Object.entries(value)`, 100K calls and reads | 5.30 ms | 16.73 ms | 3.2x faster |
| Literal squares, 2M iterations | 1.40 ms | 1.40 ms | About equal |
| Small integer bases and varying exponents, 1M iterations | 4.06 ms | 13.66 ms | 3.4x faster |
| Mixed numeric strings, 1M conversions | 27.44 ms | 29.64 ms | 7% faster |
| Object valueOf, 1M numeric conversions | 19.08 ms | 23.49 ms | 1.2x faster |
| String ordering, 2M comparisons | 8.76 ms | 8.64 ms | 1% slower |
| `==` on mixed values, 1M comparisons | 5.14 ms | 7.38 ms | 1.4x faster |
| `== null` on nullable arrays, 1M comparisons | 1.51 ms | 1.15 ms | 1.3x slower |
| Numeric array `.at(-1)`, 1M calls | 3.78 ms | 3.74 ms | 1% slower |
| String `.at(index)`, 1M calls | 4.00 ms | 5.19 ms | 1.3x faster |
| String `charCodeAt(index)`, 1M calls | 2.25 ms | 1.63 ms | 1.4x slower |
| String `codePointAt(index)`, 1M calls | 2.89 ms | 1.88 ms | 1.5x slower |

Compilation times:

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 483.38 ms | 331.03 ms |
| 5,000 variables | 670.62 ms | 368.28 ms |

Character-code loops allocate no memory per call, but Node remains faster in these tests. Adding a direct path for existing strings roughly halved the initial charCodeAt time; the remaining gap still needs investigation.

The compiler used to be written in [crust](https://github.com/Gameknight963/crust), my C-like memory safe language. It was ported to C# for various reasons. The point is, compilation is slower after the C# port. The larger source takes 1.8x as long as Microsoft’s Go-based TypeScript compiler. It used to be more or less on par.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11, TypeScript 7.0.2
