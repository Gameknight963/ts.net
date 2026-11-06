# ts.net

Work-in-progress TypesScript to IL compiler.


## Building

Reqires .NET 10 SDK, Go 1.27 or newer, and Git.

The first build downloads the pinned Microsoft TypeScript frontend source.

Then run `.\scripts\build.ps1 -Run` to compile and run an example.

## Current feature completion

68%.

<details>

| Feature | Points | Current support |
|---|---|---|
| Values and operators | 7 / 10 | Numbers, strings, booleans, null, undefined, arithmetic, comparisons and bitwise operators. <br> BigInt, symbols and some operators are missing. |
| Control flow | 9 / 10 | Conditions, loops, for-of, switch, labels, break and continue. For-in is missing. |
| Functions | 9 / 10 | Closures, callbacks, overloads, default/optional/rest parameters, destructuring, function/method receivers and arrow this capture. call/apply/bind, top-level this and generators are missing. |
| Objects and arrays | 8 / 10 | Structural objects, methods, getters/setters, computed properties, spreads, destructuring and sparse arrays. Object rest bindings and general prototype behavior are incomplete. |
| Types and generics | 5 / 10 | Microsoft's type checking, interfaces, aliases, basic generics and union/nullable storage. Runtime representations still have gaps. |
| Modules | 4 / 5 | Imports, exports, default exports and multiple files. Dynamic import is missing. |
| Enums | 4 / 5 | Numeric, string, mixed, computed and const enums; reverse mappings, merged enums, and imports. Namespace merging and __proto__ members are missing. |
| Classes and inheritance | 12 / 15 | Constructors, fields, methods, static blocks, parameter properties, class expressions, generics, structural interfaces, inheritance, super and getters/setters. Built-in base classes, auto-accessors, #private fields, dynamic field keys and decorators are missing. Base constructors returning replacement objects still have gaps. |
| Exceptions | 8 / 10 | throw, try/catch/finally, standard error objects, cause, and instanceof. Error prototypes and TypeScript source locations in stack traces are missing. |
| Async | 0 / 10 | Not implemented |
| Standard library | 2 / 5 | Selected Number/Object helpers and array/string methods. Many built-ins are missing. |

</details>

## Current performance

As of [9c5d2ef](https://github.com/Gameknight963/ts.net/commit/9c5d2efb1f0ece3314753f3a44fb1ea74aa770b9):

Runtime performance:

<details>
<summary>Click to expand large table</summary>

| Workload | ts.net | Node | Result |
|---|---|---|---|
| Numeric loop, 20M iterations | 14.44 ms | 74.71 ms | 5.2x faster |
| Typed-object loop, 500K iterations | 0.38 ms | 0.66 ms | 1.8x faster |
| `any` property loop, 350K iterations | 0.27 ms | 0.43 ms | 1.6x faster |
| Mixed array, 100K elements and 5 traversals | 1.05 ms | 1.16 ms | 10% faster |
| Numeric `any[]`, 1M iterations | 0.81 ms | 1.66 ms | 2.1x faster |
| Numeric array, 100K elements and 100 reversals | 2.07 ms | 7.98 ms | 3.9x faster |
| Mixed array, 100K elements and 100 reversals | 1.06 ms | 7.74 ms | 7.3x faster |
| Numeric callback, 2M calls (experimental) | 1.42 ms | 0.83 ms | 1.7x slower |
| Captured numeric callback, 2M calls | 5.24 ms | 5.10 ms | 3% slower |
| Local function, 2M direct calls | 0.47 ms | 0.83 ms | 1.8x faster |
| Self-returning function, 2M calls | 5.04 ms | 0.77 ms | 6.5x slower |
| `Object.keys(value).length`, 100K calls | 0.071 ms | 0.949 ms | 13.4x faster |
| `Object.keys(value)`, 100K calls and key reads | 1.30 ms | 1.12 ms | 1.2x slower |
| Local object spread, 100K two-field copies | 0.09 ms | 1.00 ms | 10.6x faster |
| Object spread, 100K copies stored in an array | 3.17 ms | 2.41 ms | 1.3x slower |
| Computed keys, 100K objects and reads | 3.85 ms | 26.06 ms | 6.8x faster |
| `Object.values(value)`, 100K calls and reads | 1.44 ms | 9.41 ms | 6.5x faster |
| `Object.entries(value)`, 100K calls and reads | 5.53 ms | 16.69 ms | 3.0x faster |
| Literal squares, 2M iterations | 1.40 ms | 1.42 ms | 1% faster |
| Small integer bases and varying exponents, 1M iterations | 4.04 ms | 13.72 ms | 3.4x faster |
| Mixed numeric strings, 1M conversions | 27.14 ms | 29.88 ms | 9% faster |
| Object valueOf, 1M numeric conversions | 14.44 ms | 23.51 ms | 1.6x faster |
| String ordering, 2M comparisons | 8.85 ms | 8.75 ms | 1% slower |
| `==` on mixed values, 1M comparisons | 5.15 ms | 7.26 ms | 1.4x faster |
| `== null` on nullable arrays, 1M comparisons | 1.51 ms | 1.15 ms | 1.3x slower |
| Numeric array `.at(-1)`, 1M calls | 3.84 ms | 3.82 ms | About equal |
| String `.at(index)`, 1M calls | 4.26 ms | 5.28 ms | 1.2x faster |
| String `charCodeAt(index)`, 1M calls | 2.27 ms | 1.66 ms | 1.4x slower |
| String `codePointAt(index)`, 1M calls | 3.00 ms | 1.90 ms | 1.6x slower |
| Masked integer remainder, 1M iterations | 1.10 ms | 1.11 ms | 1% faster |
| String slice and first-character read, 1M calls | 14.55 ms | 8.98 ms | 1.6x slower |
| String substring and first-character read, 1M calls | 14.27 ms | 10.75 ms | 1.3x slower |
| Numeric string index and comparison, 1M reads | 4.86 ms | 1.93 ms | 2.5x slower |
| String indexOf, 1M searches | 10.21 ms | 17.82 ms | 1.7x faster |
| String includes, 1M searches | 11.19 ms | 28.84 ms | 2.6x faster |
| String lastIndexOf, 1M searches | 10.07 ms | 19.54 ms | 1.9x faster |

</details>

Compilation times:

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 497.09 ms | 338.65 ms |
| 5,000 variables | 678.81 ms | 375.20 ms |

All three search benchmarks beat Node without allocating during the loop. String slicing and numeric character reads still need work; they remain slower than Node here.

The compiler used to be written in [crust](https://github.com/Gameknight963/crust), my C-like memory safe language. It was ported to C# for various reasons. The point is, compilation is slower after the C# port. The larger source takes 1.8x as long as Microsoft’s Go-based TypeScript compiler. It used to be more or less on par.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11, TypeScript 7.0.2
