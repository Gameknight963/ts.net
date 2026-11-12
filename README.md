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


| Workload | ts.net | Node | Result |
|---|---|---|---|
| copyWithin, 1K copies of 5K entries in a 10K packed array | 1 ms | 269 ms | 321x |
| Positive decimal ranges, 10K cached calls across four locales | 10.81 ms | 578.08 ms | 53.48x |
| Local object spread, 100K two-field copies | 0.095 ms | 1.02 ms | 10.77x |
| `Object.keys(value).length`, 100K calls | 0.095 ms | 0.953 ms | 10.05x |
| Digital durations, 10K cached calls across four locales | 51.09 ms | 506.65 ms | 9.92x |
| `Object.values(value)`, 100K calls and reads | 1.73 ms | 9.53 ms | 5.51x |
| Numeric loop, 20M iterations | 14.58 ms | 75.45 ms | 5.17x |
| Computed keys, 100K objects and reads | 5.97 ms | 29.00 ms | 4.86x |
| Cached date formatter, 1K daily UTC dates in 2024, Gregorian calendar | 0.312 ms | 1.22 ms | 3.90x |
| Gzip-decompress 1,052 bytes into 1 MiB of repeated ASCII A | 1.19 ms | 4.38 ms | 3.68x |
| Small integer bases and varying exponents, 1M iterations | 4.07 ms | 13.85 ms | 3.41x |
| String includes, 1M searches | 11.19 ms | 28.84 ms | 2.58x |
| Mixed array, 100K elements and 100 reversals | 3.07 ms | 7.70 ms | 2.51x |
| Cached list formatters, 10K three-element lists across four locales and all types/styles | 4.31 ms | 10.40 ms | 2.41x |
| `Object.entries(value)`, 100K calls and reads | 8.09 ms | 16.71 ms | 2.07x |
| String lastIndexOf, 1M searches | 10.07 ms | 19.54 ms | 1.94x |
| Gzip-compress 1 MiB of repeated ASCII A | 3.54 ms | 6.71 ms | 1.90x |
| Numeric array, 100K elements and 100 reversals | 4.37 ms | 7.87 ms | 1.80x |
| Local function, 2M direct calls | 0.461 ms | 0.825 ms | 1.79x |
| Intl.DisplayNames, 6K region-name lookups | 7.64 ms | 13.70 ms | 1.79x |
| String indexOf, 1M searches | 10.21 ms | 17.82 ms | 1.75x |
| Numeric `any[]`, 1M iterations | 0.954 ms | 1.66 ms | 1.74x |
| Typed-object loop, 500K iterations | 0.381 ms | 0.650 ms | 1.70x |
| `any` property loop, 350K iterations | 0.266 ms | 0.435 ms | 1.64x |
| WritableStream, queue and complete 1K numeric writes | 0.394 ms | 0.618 ms | 1.57x |
| reduce and reduceRight, 10 pairs over 10K integers from 0-255 | 0.775 ms | 1.21 ms | 1.56x |
| forEach sum, every and findIndex, 10 passes over 10K integers from 0-255 | 0.577 ms | 0.865 ms | 1.50x |
| Typed-object loop, 500K iterations | 0.5 ms | 0.4 ms | 1.49x |
| UTF-8 roundtrip, 1K chunks of 80 UTF-16 code units | 11.23 ms | 16.11 ms | 1.43x |
| `==` on mixed values, 1M comparisons | 5.15 ms | 7.26 ms | 1.41x |
| join, 20 joins of 10K integers from 0-99 | 1.64 ms | 2.23 ms | 1.36x |
| String `.at(index)`, 1M calls | 4.26 ms | 5.28 ms | 1.24x |
| Map.forEach numeric sum, 10 passes over 10K entries | 0.579 ms | 0.626 ms | 1.08x |
| Mixed numeric strings, 1M conversions | 28.60 ms | 30.37 ms | 1.06x |
| Set.forEach numeric sum, 10 passes over 10K values | 0.580 ms | 0.608 ms | 1.05x |
| Create/read a two-field object in a 100K-iteration loop, with a read-only Reflect alias in the program | 0.0706 ms | 0.0717 ms | 1.02x |
| Masked integer remainder, 1M iterations | 1.10 ms | 1.11 ms | 1.01x |
| Literal squares, 2M iterations | 1.40 ms | 1.41 ms | 1.00x |
| `any` property loop, 350K iterations | 0.3 ms | 0.3 ms | 1.00x |
| Numeric array `.at(-1)`, 1M calls | 3.84 ms | 3.82 ms | 0.99x |
| Numeric loop, 20M iterations | 19 ms | 19 ms | 0.99x |
| Object valueOf, 1M numeric conversions | 121 ms | 122 ms | 0.99x |
| Serialize 10K numbers from 0-255, 20 times | 1.73 ms | 1.71 ms | 0.99x |
| Captured numeric callback, 2M calls | 5.24 ms | 5.07 ms | 0.97x |
| String ordering, 2M comparisons | 8.98 ms | 8.75 ms | 0.97x |
| Float64Array traversal, 10K elements and 100 passes | 0.784 ms | 0.718 ms | 0.92x |
| Response BYOB, read 1 MiB through a reused 16 KiB buffer | 0.957 ms | 0.797 ms | 0.83x |
| Regional number formatting, 10K cached calls across four locales | 9.85 ms | 7.54 ms | 0.77x |
| `== null` on nullable arrays, 1M comparisons | 1.51 ms | 1.15 ms | 0.76x |
| String substring and first-character read, 1M calls | 14.27 ms | 10.75 ms | 0.75x |
| String `charCodeAt(index)`, 1M calls | 2.27 ms | 1.66 ms | 0.73x |
| TransformStream pipeline, double 1K numbers | 5.17 ms | 3.57 ms | 0.69x |
| Sum 1K values in a loop, awaiting each value | 0.0633 ms | 0.0434 ms | 0.69x |
| ReadableStream, create and drain 1K numbers | 0.281 ms | 0.189 ms | 0.67x |
| Numeric getter on an Object.create object, 100K reads | 1.95 ms | 1.27 ms | 0.65x |
| String `codePointAt(index)`, 1M calls | 3.00 ms | 1.90 ms | 0.63x |
| Intl.Segmenter, 100 word-segmentation calls and full traversal | 4.14 ms | 2.59 ms | 0.63x |
| String slice and first-character read, 1M calls | 14.55 ms | 8.98 ms | 0.62x |
| Numeric callback, 2M calls (experimental) | 1.38 ms | 0.827 ms | 0.60x |
| Traverse 1K array values with for-await-of and sum them | 0.196 ms | 0.118 ms | 0.60x |
| Cached date formatter, 1K daily UTC dates in year 10000, Gregorian calendar | 2.10 ms | 1.25 ms | 0.59x |
| Sum 100K values in a loop, awaiting only on the final iteration | 0.717 ms | 0.415 ms | 0.58x |
| Read `arguments[0]`, 100K reads | 0.176 ms | 0.0949 ms | 0.54x |
| Split 10K comma-separated strings, keep three parts, and sum lengths | 4 ms | 2 ms | 0.50x |
| ReadableStream tee, drain both branches of 1K numbers | 5.41 ms | 2.69 ms | 0.50x |
| Object valueOf, 1M numeric conversions | 48.94 ms | 23.64 ms | 0.48x |
| Parse 1,000 three-field records 20 times, without a reviver | 9 ms | 4 ms | 0.48x |
| Pipe 1K numbers from a ReadableStream to a WritableStream | 2.06 ms | 0.998 ms | 0.48x |
| `Object.keys(value)`, 100K calls and key reads | 2.46 ms | 1.12 ms | 0.46x |
| Cached emoji collator, 10K comparisons with numeric and punctuation options | 2.43 ms | 1.11 ms | 0.46x |
| forEach with source-array reads, 10 passes over 10K integers from 0-255 | 0.538 ms | 0.248 ms | 0.46x |
| Object.assign, 1K copies of a three-number record | 0.125 ms | 0.0563 ms | 0.45x |
| Numeric string index and comparison, 1M reads | 4.86 ms | 1.93 ms | 0.40x |
| Create a Map, insert and read 10K entries, then delete half | 1.76 ms | 0.695 ms | 0.40x |
| ReadableStream BYOB, read 1K one-byte chunks into a reused buffer | 4.46 ms | 1.71 ms | 0.38x |
| map, filter and sum, 10 passes over 10K integers from 0-255 | 5.60 ms | 2.05 ms | 0.37x |
| Response text from 1K four-byte stream chunks | 0.916 ms | 0.343 ms | 0.37x |
| Construct and run a chain of 10K handlers | 0.798 ms | 0.296 ms | 0.37x |
| Mixed array, 100K elements and 5 traversals | 3.19 ms | 1.15 ms | 0.36x |
| Regional number formatting, 1K new formatters with one call each | 90.24 ms | 31.25 ms | 0.35x |
| Numeric callback reading `arguments.length`, 100K calls | 1.26 ms | 0.435 ms | 0.35x |
| Currency names, 10K cached calls across German, French, Russian and British English | 28.24 ms | 9.55 ms | 0.34x |
| Cached compact number formatters, 10K calls across four locales | 39.01 ms | 13.04 ms | 0.33x |
| from, 20 copies of 10K packed numbers | 1 ms | 0 ms | 0.32x |
| Sum 1K values with an await in try and an increment in finally each iteration | 0.146 ms | 0.0470 ms | 0.32x |
| Cached date formatter, 1K daily UTC dates in 2024, Chinese calendar | 83.9 ms | 23.8 ms | 0.28x |
| Serialize 1,000 three-field records 20 times, without a replacer | 3.50 ms | 0.940 ms | 0.27x |
| Serialize 10K numbers divided by 7, 20 times | 26.70 ms | 6.70 ms | 0.25x |
| Self-returning function, 2M calls | 3.57 ms | 0.813 ms | 0.23x |
| map with source-array reads and sum, 10 passes over 10K integers from 0-255 | 1.20 ms | 0.276 ms | 0.23x |
| Create 10K resolved promises, collect with all, and sum results | 2.47 ms | 0.495 ms | 0.20x |
| Object spread, 100K copies stored in an array | 9.51 ms | 1.65 ms | 0.17x |
| Long meter units, 10K cached calls across German, French, Russian and British English | 51.31 ms | 8.94 ms | 0.17x |
| Object literal method through a helper, 100K calls | 0.447 ms | 0.0616 ms | 0.14x |
| 100K numeric writes to a two-element mixed array, after reading its numeric element | 0.284 ms | 0.0358 ms | 0.13x |
| Read and sum 100K numbers from a mixed array through a helper | 0.541 ms | 0.0625 ms | 0.12x |
| Run 1K async calls with two awaits each, collect with all, and sum results | 3 ms | 0 ms | 0.11x |
| Sum 1K values from a generator | 0.122 ms | 0.0115 ms | 0.09x |
| Class method through a helper, 100K calls | 1.10 ms | 0.0619 ms | 0.06x |
| Local year/hour reads, 1K daily timestamp changes on one Date, starting in 2020 | 0.759 ms | 0.0319 ms | 0.04x |
| Local year/hour reads, 1K daily timestamp changes on one Date, starting in 20000 | 1.34 ms | 0.0317 ms | 0.02x |
| Class numeric getter, 100K reads | 2.38 ms | 0.0273 ms | 0.01x |

</details>

Compilation times:

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 503.72 ms | 354.32 ms |
| 5,000 variables | 754.27 ms | 391.27 ms |

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11, TypeScript 7.0.2
