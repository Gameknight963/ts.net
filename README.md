# ts.net

Work-in-progress TypesScript to IL compiler.


## Building

The compiler has significant portions written in [crust](https://github.com/Gameknight963/crust), my C-like memory-safe programming lanuage. To build it, you'll need to install `crust` and `crustc`.

Then run `.\scripts\build.ps1 -Run`. You might figure out what dependencies you're missing if you run it.

Building on linux is not supported atm.

## Current performance

Most measurements are from [5a0ebda](https://github.com/Gameknight963/ts.net/commit/5a0ebda3061c267298b5b715f63e874fea70ce84). The callback measurement is from [b46c76b](https://github.com/Gameknight963/ts.net/commit/b46c76b612b6d0ca905cb4cbef2f97370e206af7):

Runtime performance:

| Workload | ts.net | Node | Result |
|---|---:|---:|---|
| Numeric loop, 20M iterations | 14.55 ms | 75.29 ms | 5.2x faster |
| Typed-object loop, 500K iterations | 0.43 ms | 0.66 ms | 1.5x faster |
| `any` property loop, 350K iterations | 0.30 ms | 0.83 ms | 2.7x faster |
| Mixed array, 100K elements and 5 traversals | 1.66 ms | 1.13 ms | 1.5x slower |
| Numeric `any[]`, 1M iterations | 0.76 ms | 1.67 ms | 2.2x faster |
| Numeric array, 100K elements and 100 reversals | 2.18 ms | 7.89 ms | 3.6x faster |
| Mixed array, 100K elements and 100 reversals | 1.30 ms | 7.92 ms | 6.1x faster |
| Numeric callback, 2M calls (experimental) | 1.47 ms | 0.82 ms | 1.8x slower |

Compilation times:

| Source | tsnetc | tsc |
|---|---|---|
| Small numeric benchmark | 338.66 ms | 343.57 ms |
| 5,000 variables | 446.63 ms | 392.42 ms |

The numeric and small-object loops are encouraging. Numeric callbacks improved substantially after letting the CLR inline their bodies, but remain slower than Node in this measurement; mixed-array traversal also needs work.

The similar compilation times make sense since this is using much of Microsoft's Go compiler components, and IL emission isn't really a bottleneck.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11 (SDK 10.0.400), TypeScript 7.0.2
