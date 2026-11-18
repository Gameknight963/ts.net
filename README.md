# ts.net

Work-in-progress TypesScript to IL compiler.

## AI disclaimer

This project has significant portions written by AI. This README is perodically updated by AI to mantain factual accuracy, but reviewed by me to remove slop.

## Building

Reqires .NET 10 SDK, Go 1.27 or newer, and Git.

The first build downloads the pinned Microsoft TypeScript frontend source.

Then run `.\scripts\build.ps1 -Run` to compile and run an example.

## Current feature completion

87%.

<details>

| Feature | Points | Missing support |
|---|---|---|
| Values and operators | 9 / 10 | Computed deletion of fixed-layout fields and some Symbol protocols. |
| Control flow | 10 / 10 | Missing nothing. |
| Functions | 9 / 10 | Global properties do not alias top-level bindings; native global bindings are read-only. |
| Objects and arrays | 9 / 10 | General property-definition and object-extensibility APIs are incomplete. |
| Types and generics | 5 / 10 | Missing some runtime type representations. |
| Modules | 4 / 5 | Missing dynamic imports of uncompiled modules. |
| Enums | 5 / 5 | Missing nothing. |
| Classes and inheritance | 14 / 15 | Missing some built-in base classes and complete constructor replacement-object support. |
| Exceptions | 9 / 10 | Stack traces keep CLR formatting and may omit inlined frames. |
| Async | 10 / 10 | Missing nothing. |
| Standard library | 3 / 5 | Some mixed-case RegExp backreferences and repeated captures; remaining Intl and stream APIs. |

</details>

## Current performance

Runtime performance:

<details>
<summary>Click to expand large table</summary>


| Workload | ts.net | Node | C# (idiomatic) | Result |
|---|---|---|---|---|
| copyWithin, 1K copies of 5K entries in a 10K packed array | 1 ms | 269 ms | — | 321x |
| Positive decimal ranges, 10K cached calls across four locales | 10.81 ms | 578.08 ms | — | 53.48x |
| Local object spread, 100K two-field copies | 0.095 ms | 1.02 ms | — | 10.77x |
| `Object.keys(value).length`, 100K calls | 0.095 ms | 0.953 ms | — | 10.05x |
| Digital durations, 10K cached calls across four locales | 51.09 ms | 506.65 ms | — | 9.92x |
| Local class with number, string and boolean fields, 100K instances | 0.102 ms | 0.606 ms | — | 5.92x |
| `Object.values(value)`, 100K calls and reads | 1.73 ms | 9.53 ms | — | 5.51x |
| Numeric loop, 20M iterations | 14.58 ms | 75.45 ms | — | 5.17x |
| Computed keys, 100K objects and reads | 5.97 ms | 29.00 ms | — | 4.86x |
| Parse ten JSON arrays of 1K strings, 896 characters per string | 1.624 ms | 7.520 ms | 25.33 ms (`JsonSerializer`) | 4.63x |
| Cached date formatter, 1K daily UTC dates in 2024, Gregorian calendar | 0.312 ms | 1.22 ms | — | 3.90x |
| Gzip-decompress 1,052 bytes into 1 MiB of repeated ASCII A | 1.19 ms | 4.38 ms | — | 3.68x |
| Small integer bases and varying exponents, 1M iterations | 4.07 ms | 13.85 ms | — | 3.41x |
| Float64Array reduce, 10 sums of 10K values | 0.351 ms | 0.999 ms | 0.067 ms (Aggregate) | 2.85x |
| String includes, 1M searches | 11.19 ms | 28.84 ms | — | 2.58x |
| Mixed array, 100K elements and 100 reversals | 3.07 ms | 7.70 ms | — | 2.51x |
| Float64Array every/some/findIndex, 10 full scans each of 10K values | 1.091 ms | 2.711 ms | 0.314 ms (array methods) | 2.49x |
| Cached list formatters, 10K three-element lists across four locales and all types/styles | 4.31 ms | 10.40 ms | — | 2.41x |
| `Object.entries(value)`, 100K calls and reads | 8.09 ms | 16.71 ms | — | 2.07x |
| Map 100K number/string/undefined values with a default parameter and sum | 0.801 ms | 1.65 ms | — | 2.06x |
| Alternate between two class methods through a helper, 100K calls | 0.0696 ms | 0.1406 ms | — | 2.02x |
| String lastIndexOf, 1M searches | 10.07 ms | 19.54 ms | — | 1.94x |
| Gzip-compress 1 MiB of repeated ASCII A | 3.54 ms | 6.71 ms | — | 1.90x |
| Numeric array, 100K elements and 100 reversals | 4.37 ms | 7.87 ms | — | 1.80x |
| Local function, 2M direct calls | 0.461 ms | 0.825 ms | — | 1.79x |
| Intl.DisplayNames, 6K region-name lookups | 7.64 ms | 13.70 ms | — | 1.79x |
| String indexOf, 1M searches | 10.21 ms | 17.82 ms | — | 1.75x |
| Numeric `any[]`, 1M iterations | 0.954 ms | 1.66 ms | — | 1.74x |
| Typed-object loop, 500K iterations | 0.381 ms | 0.650 ms | — | 1.70x |
| Write and sum a numeric property on `Object.create(null)`, 100K iterations | 0.610 ms | 1.02 ms | 1.43 ms (`Dictionary<string, object>`) | 1.67x |
| `any` property loop, 350K iterations | 0.266 ms | 0.435 ms | — | 1.64x |
| Update typed fields and sum through a helper, 2M iterations | 1.42 ms | 2.33 ms | 1.39 ms | 1.64x |
| Map 10K number/string values to numbers or undefined and reduce, 20 passes | 1.76 ms | 2.80 ms | — | 1.59x |
| WritableStream, queue and complete 1K numeric writes | 0.394 ms | 0.618 ms | — | 1.57x |
| reduce and reduceRight, 10 pairs over 10K integers from 0-255 | 0.775 ms | 1.21 ms | — | 1.56x |
| Count numeric values in a mixed array, 500K type checks | 0.285 ms | 0.440 ms | — | 1.54x |
| forEach sum, every and findIndex, 10 passes over 10K integers from 0-255 | 0.577 ms | 0.865 ms | — | 1.50x |
| Typed-object loop, 500K iterations | 0.5 ms | 0.4 ms | — | 1.49x |
| UTF-8 roundtrip, 1K chunks of 80 UTF-16 code units | 11.23 ms | 16.11 ms | — | 1.43x |
| `==` on mixed values, 1M comparisons | 5.15 ms | 7.26 ms | — | 1.41x |
| join, 20 joins of 10K integers from 0-99 | 1.64 ms | 2.23 ms | — | 1.36x |
| Split 10K comma-separated strings, keep three parts, and sum lengths | 1.542 ms | 1.938 ms | 0.382 ms (`string[]`) | 1.26x |
| Sum 100K local arrays of two numbers using index & 1 | 0.0702 ms | 0.0877 ms | — | 1.25x |
| String `.at(index)`, 1M calls | 4.26 ms | 5.28 ms | — | 1.24x |
| toSorted with numeric comparator and traversal, 10 copies of 10K integers | 10.31 ms | 12.78 ms | — | 1.24x |
| Write and sum a numeric field on a function-constructed object, 100K iterations | 0.574 ms | 0.703 ms | 0.0702 ms (class) | 1.23x |
| 100K numeric writes to a two-element mixed array, after reading its numeric element | 0.0282 ms | 0.0344 ms | 0.414 ms (`object[]`) | 1.22x |
| BigInt add/subtract loop, 100K iterations | 0.440 ms | 0.494 ms | 0.435 ms (`BigInteger`) | 1.12x |
| 100K numeric writes to a four-element mapped array | 0.0378 ms | 0.0419 ms | 0.0377 ms (`double[]`) | 1.11x |
| Map.forEach numeric sum, 10 passes over 10K entries | 0.579 ms | 0.626 ms | — | 1.08x |
| Reduce 100K number/string/undefined values with a default parameter | 0.596 ms | 0.640 ms | — | 1.08x |
| Mixed numeric strings, 1M conversions | 28.60 ms | 30.37 ms | — | 1.06x |
| Set.forEach numeric sum, 10 passes over 10K values | 0.580 ms | 0.608 ms | — | 1.05x |
| Create/read a two-field object in a 100K-iteration loop, with conditional-key deletion elsewhere | 0.0679 ms | 0.0698 ms | 0.0704 ms (struct) | 1.03x |
| Create/read a two-field object in a 100K-iteration loop, with a read-only Reflect alias in the program | 0.0706 ms | 0.0717 ms | — | 1.02x |
| Create/read a two-field object in a 100K-iteration loop, with a constant-key deletion elsewhere in the program | 0.0706 ms | 0.0720 ms | — | 1.02x |
| Masked integer remainder, 1M iterations | 1.10 ms | 1.11 ms | — | 1.01x |
| Sum 100K local two-number array literals by fixed index | 0.0679 ms | 0.0689 ms | — | 1.01x |
| Literal squares, 2M iterations | 1.40 ms | 1.41 ms | — | 1.00x |
| `any` property loop, 350K iterations | 0.3 ms | 0.3 ms | — | 1.00x |
| Numeric array `.at(-1)`, 1M calls | 3.84 ms | 3.82 ms | — | 0.99x |
| Numeric loop, 20M iterations | 19 ms | 19 ms | — | 0.99x |
| Serialize 10K numbers from 0-255, 20 times | 1.73 ms | 1.71 ms | — | 0.99x |
| Captured numeric callback, 2M calls | 5.24 ms | 5.07 ms | — | 0.97x |
| String ordering, 2M comparisons | 8.98 ms | 8.75 ms | — | 0.97x |
| Float64Array traversal, 10K elements and 100 passes | 0.784 ms | 0.718 ms | — | 0.92x |
| Local class constructor parameters and field read, 100K instances | 0.0687 ms | 0.0633 ms | — | 0.92x |
| Update a class field and read its getter, 100K iterations | 0.0675 ms | 0.0624 ms | — | 0.92x |
| Local class field update and read, 100K instances | 0.0694 ms | 0.0631 ms | — | 0.91x |
| Borrow an object method with call, 100K calls | 0.0719 ms | 0.0651 ms | 0.217 ms (delegate) | 0.91x |
| Borrow an object method with apply and a one-number array, 100K calls | 0.0720 ms | 0.0652 ms | 0.220 ms (delegate) | 0.91x |
| Update a sealed class field and read its getter, 100K iterations | 0.0698 ms | 0.0629 ms | — | 0.90x |
| Assign numbers or undefined and check the type, 1M iterations | 1.02 ms | 0.919 ms | — | 0.90x |
| Serialize a 1 MiB ASCII string 10 times | 6.94 ms | 6.19 ms | — | 0.89x |
| Local constructor parameter property and field read, 100K instances | 0.0698 ms | 0.0617 ms | — | 0.88x |
| Class method through a helper, 100K calls | 0.0703 ms | 0.0619 ms | — | 0.88x |
| Object literal method through a helper, 100K calls | 0.0715 ms | 0.0630 ms | 0.0710 ms | 0.88x |
| Response BYOB, read 1 MiB through a reused 16 KiB buffer | 0.957 ms | 0.797 ms | — | 0.83x |
| Construct and traverse 10K class instances with 17 numeric fields | 0.257 ms | 0.213 ms | 0.163 ms (class) | 0.83x |
| Object valueOf, 1M numeric conversions | 27.57 ms | 22.64 ms | — | 0.82x |
| Call a callback with an object and a number, 100K calls | 0.0776 ms | 0.0621 ms | 0.0698 ms (delegate) | 0.80x |
| Regional number formatting, 10K cached calls across four locales | 9.85 ms | 7.54 ms | — | 0.77x |
| `== null` on nullable arrays, 1M comparisons | 1.51 ms | 1.15 ms | — | 0.76x |
| Numeric class property increments, 2M updates | 5.908 ms | 4.505 ms | 1.348 ms | 0.76x |
| String substring and first-character read, 1M calls | 14.27 ms | 10.75 ms | — | 0.75x |
| Sum 1K values in a loop, awaiting each value | 0.0563 ms | 0.0423 ms | — | 0.75x |
| Read varying `keyof` keys through a typed helper from a 64-field object, 100K reads | 1.97 ms | 1.48 ms | — | 0.75x |
| String `charCodeAt(index)`, 1M calls | 2.27 ms | 1.66 ms | — | 0.73x |
| Read varying keys through an `any` helper from a 64-field object, 100K reads | 2.04 ms | 1.49 ms | — | 0.73x |
| Create, retain, and sum 100K two-number objects | 1.62 ms | 1.15 ms | 1.71 ms (`List<Point>`) | 0.71x |
| Float64Array toSorted and traversal, 10 copies of 10K values | 11.56 ms | 8.06 ms | — | 0.70x |
| Int32Array toSorted and traversal, 10 copies of 10K values | 5.74 ms | 4.03 ms | — | 0.70x |
| TransformStream pipeline, double 1K numbers | 5.17 ms | 3.57 ms | — | 0.69x |
| ReadableStream, create and drain 1K numbers | 0.281 ms | 0.189 ms | — | 0.67x |
| map, filter and sum, 10 passes over 10K integers from 0-255 | 0.814 ms | 0.534 ms | 0.266 ms (array methods) | 0.66x |
| Numeric getter on an Object.create object, 100K reads | 1.95 ms | 1.27 ms | — | 0.65x |
| String `codePointAt(index)`, 1M calls | 3.00 ms | 1.90 ms | — | 0.63x |
| Intl.Segmenter, 100 word-segmentation calls and full traversal | 4.14 ms | 2.59 ms | — | 0.63x |
| BigInt64Array toSorted and Number conversion, 10 copies of 10K values | 7.99 ms | 5.00 ms | — | 0.63x |
| Sum 10K numbers, 20 traversals | 0.139 ms | 0.0869 ms | — | 0.63x |
| String slice and first-character read, 1M calls | 14.55 ms | 8.98 ms | — | 0.62x |
| Add a 32-digit BigInt literal 10K times | 0.212 ms | 0.132 ms | 0.221 ms (`BigInteger`) | 0.62x |
| Sum 10K mapped numbers, 20 traversals | 0.142 ms | 0.0864 ms | — | 0.61x |
| Numeric callback, 2M calls (experimental) | 1.38 ms | 0.827 ms | — | 0.60x |
| Traverse 1K array values with for-await-of and sum them | 0.196 ms | 0.118 ms | — | 0.60x |
| Cached date formatter, 1K daily UTC dates in year 10000, Gregorian calendar | 2.10 ms | 1.25 ms | — | 0.59x |
| Sum 100K values in a loop, awaiting only on the final iteration | 0.717 ms | 0.415 ms | — | 0.58x |
| Read varying string keys through an `any` helper from a Float64Array, 100K reads | 1.23 ms | 0.713 ms | — | 0.58x |
| Create a Map, insert and read 10K entries, then delete half | 1.184 ms | 0.658 ms | 0.395 ms (`Dictionary`) | 0.56x |
| Read `arguments[0]`, 100K reads | 0.176 ms | 0.0949 ms | — | 0.54x |
| Parse, filter, group, sort and serialize 1K records, 10 passes | 7.86 ms | 4.12 ms | — | 0.52x |
| Serialize 1,000 records with 16 numeric fields 20 times, without a replacer | 10.55 ms | 5.43 ms | 13.27 ms (JsonElement) | 0.51x |
| ReadableStream tee, drain both branches of 1K numbers | 5.41 ms | 2.69 ms | — | 0.50x |
| Call a numeric void method on a record with 16 numeric fields through a helper, 100K calls | 0.477 ms | 0.233 ms | 0.0711 ms (class) | 0.49x |
| Construct and run a chain of 10K handlers | 0.687 ms | 0.340 ms | 0.665 ms (queued Task continuations) | 0.49x |
| Parse 1,000 three-field records 20 times, without a reviver | 9 ms | 4 ms | — | 0.48x |
| Pipe 1K numbers from a ReadableStream to a WritableStream | 2.06 ms | 0.998 ms | — | 0.48x |
| `Object.keys(value)`, 100K calls and key reads | 2.46 ms | 1.12 ms | — | 0.46x |
| Cached emoji collator, 10K comparisons with numeric and punctuation options | 2.43 ms | 1.11 ms | — | 0.46x |
| forEach with source-array reads, 10 passes over 10K integers from 0-255 | 0.538 ms | 0.248 ms | — | 0.46x |
| Object.assign, 1K copies of a three-number record | 0.125 ms | 0.0563 ms | — | 0.45x |
| Serialize 1,000 three-field records 20 times, without a replacer | 2.28 ms | 0.985 ms | — | 0.43x |
| map and sum, 20 passes over 10K integers from 0-9999 | 1.01 ms | 0.413 ms | — | 0.41x |
| Call a numeric void object-literal method through a helper, 100K calls | 0.571 ms | 0.231 ms | 0.0702 ms (class) | 0.40x |
| Numeric string index and comparison, 1M reads | 4.86 ms | 1.93 ms | — | 0.40x |
| Class getter returning `this.base * 2 + 1`, 100K reads | 0.0682 ms | 0.0274 ms | — | 0.40x |
| Class numeric getter, 100K reads | 0.0702 ms | 0.0274 ms | — | 0.39x |
| Read and sum one numeric element 100K times through a mixed-array helper | 0.0698 ms | 0.0273 ms | 0.0675 ms (`object[]`) | 0.39x |
| ReadableStream BYOB, read 1K one-byte chunks into a reused buffer | 4.46 ms | 1.71 ms | — | 0.38x |
| Response text from 1K four-byte stream chunks | 0.916 ms | 0.343 ms | — | 0.37x |
| Mixed array, 100K elements and 5 traversals | 3.19 ms | 1.15 ms | — | 0.36x |
| Regional number formatting, 1K new formatters with one call each | 90.24 ms | 31.25 ms | — | 0.35x |
| Numeric callback reading `arguments.length`, 100K calls | 1.26 ms | 0.435 ms | — | 0.35x |
| Currency names, 10K cached calls across German, French, Russian and British English | 28.24 ms | 9.55 ms | — | 0.34x |
| Cached compact number formatters, 10K calls across four locales | 39.01 ms | 13.04 ms | — | 0.33x |
| from, 20 copies of 10K packed numbers | 1 ms | 0 ms | — | 0.32x |
| Sum 1K values with an await in try and an increment in finally each iteration | 0.146 ms | 0.0470 ms | — | 0.32x |
| Map 10K number/string values to numbers and sum, 20 passes | 1.69 ms | 0.540 ms | — | 0.32x |
| Call a numeric void class method through a helper, 100K calls | 0.722 ms | 0.228 ms | 0.0702 ms (class) | 0.32x |
| Count string values in a mixed array, 500K type checks | 1.38 ms | 0.429 ms | — | 0.31x |
| Parse ten JSON arrays of 10K integers from -5000 to 4999 | 2.90 ms | 0.885 ms | 4.19 ms (`JsonSerializer`) | 0.31x |
| Object spread, 100K copies stored in an array | 5.51 ms | 1.66 ms | — | 0.30x |
| Add an `any` value to a number, 200K additions | 0.185 ms | 0.0548 ms | — | 0.30x |
| Serialize 10K numbers divided by 7, 20 times | 21.81 ms | 6.30 ms | — | 0.29x |
| Self-returning function, 2M calls | 1.97 ms | 0.572 ms | 1.51 ms (delegate) | 0.29x |
| Cached date formatter, 1K daily UTC dates in 2024, Chinese calendar | 83.9 ms | 23.8 ms | — | 0.28x |
| Construct objects with a numeric argument and sum their field, 100K instances | 7.84 ms | 2.16 ms | 0.278 ms (class) | 0.28x |
| Retained class instances with number, string and boolean fields, 100K instances; one server GC heap for both .NET builds | 4.08 ms | 1.125 ms | 1.100 ms | 0.28x |
| Retained numeric class instances and field traversal, 100K instances | 4.50 ms | 1.165 ms | 1.012 ms | 0.26x |
| Reduce 10K number/string values to a numeric sum, 20 passes | 0.730 ms | 0.179 ms | — | 0.25x |
| Sum 1K values from a generator | 0.0461 ms | 0.0115 ms | — | 0.25x |
| Retained class instances with number, string and boolean fields, 100K instances | 6.26 ms | 1.551 ms | 1.832 ms | 0.25x |
| Attach 10K handlers to one pending promise and sum their results | 3.47 ms | 0.835 ms | 1.03 ms (queued Task continuations) | 0.24x |
| map with source-array reads and sum, 10 passes over 10K integers from 0-255 | 1.20 ms | 0.276 ms | — | 0.23x |
| Create 10K resolved promises, collect with all, and sum results | 2.17 ms | 0.479 ms | — | 0.22x |
| Increment fifth numeric property of a parsed object, 100K times | 0.645 ms | 0.120 ms | 1.361 ms (dictionary), 0.258 ms (class) | 0.19x |
| Map 10K numbers, replace one result with a string, and sum numeric results, 20 passes | 2.85 ms | 0.521 ms | — | 0.18x |
| Construct and traverse 100K zero-argument class instances with a field initializer and method | 5.68 ms | 0.984 ms | 0.864 ms (class) | 0.17x |
| Long meter units, 10K cached calls across German, French, Russian and British English | 51.31 ms | 8.94 ms | — | 0.17x |
| Construct and traverse 100K class instances whose constructor calls a helper | 6.41 ms | 1.09 ms | 1.41 ms (class) | 0.17x |
| Call 100K callbacks stored in an array | 0.798 ms | 0.131 ms | 0.254 ms (delegates) | 0.16x |
| Create and call 100K callbacks stored in an array | 9.81 ms | 1.61 ms | 6.45 ms (fresh delegates) | 0.16x |
| Create and traverse 100K retained objects with a numeric field and a method | 21.3 ms | 2.37 ms | 1.62 ms (class/list), 0.300 ms (struct/array) | 0.11x |
| Run 1K async calls with two awaits each, collect with all, and sum results | 3 ms | 0 ms | — | 0.11x |
| Call a custom apply handler with an object and a one-number array, 100K calls | 0.612 ms | 0.0624 ms | 0.283 ms (`double[]`) | 0.10x |
| Call a custom call handler with an object and a number, 100K calls | 0.650 ms | 0.0651 ms | 0.0724 ms (delegate) | 0.10x |
| Call a zero-argument void class method through a helper, 100K calls | 0.544 ms | 0.0403 ms | 0.0702 ms (`double` field), 0.0233 ms (`int` field) | 0.07x |
| Local year/hour reads, 1K daily timestamp changes on one Date, starting in 2020 | 0.759 ms | 0.0319 ms | — | 0.04x |
| Local year/hour reads, 1K daily timestamp changes on one Date, starting in 20000 | 1.34 ms | 0.0317 ms | — | 0.02x |
| Call `slice(5)` by name on an `any` six-character string, 100K calls | 1.82 ms | 0.0275 ms | 0.354 ms (`Substring`), 0.0707 ms (span length) | 0.015x |

</details>

Compilation times:

| Source | tsnetc | tsc (`--skipDefaultLibCheck`) |
|---|---|---|
| Small numeric benchmark | 277.19 ms | 98.45 ms |
| 5,000 variables | 558.22 ms | 138.49 ms |
| 500 functions with object parameters | 595.69 ms | 150.87 ms |

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10, TypeScript 7.0.2
