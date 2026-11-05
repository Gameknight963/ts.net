# ts.net

Work-in-progress TypesScript to IL compiler.


## Building

The compiler has significant portions written in [crust](https://github.com/Gameknight963/crust), my C-like memory-safe programming lanuage. To build it, you'll need to install `crust` and `crustc`.

Then run `.\scripts\build.ps1 -Run`. You might figure out what dependencies you're missing if you run it.

Building on linux is not supported atm.

## Current performance

As of [d1b01d1](https://github.com/Gameknight963/ts.net/commit/d1b01d1c03cecefd0dd11fea48cd756a2a6f3399):

| Workload | ts.net | Node | Result |
|---|---|---|---|
| Numeric loop, 20M iterations | 24.71 ms | 77.41 ms | 3.1× faster |
| Typed-object loop, 500K iterations | 15.42 ms | 0.68 ms | 22.6× slower |
| any property loop, 350K iterations | 22.79 ms | 0.45 ms | 50.9× slower |

Arithmetic seems quite fast, which gives me hope that the rest of the rest can see performance improvments. But object and `any` operations currently need substantial optimization.

Measured on Ryzen 9 3900X, Windows x64, Node 25.9.0, .NET 10.0.11, TypeScript 7.0.2