# Static Provider

An F# type provider that executes your code directly.

The goal of this type provider is to make it possible to create other type providers
right inside your code, without the need for an external library.

Eventually, it should also provide some compile-time metaprogramming features.

## Current state
Development is indefinitely halted, since it is currently impossible to execute F# code
using FSI on .NET Core, the targeted platform.

See: https://github.com/Microsoft/visualfsharp/issues/2407, https://github.com/fsharp/FSharp.Compiler.Service/issues/807
