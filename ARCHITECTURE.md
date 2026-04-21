# Architecture: System.DisposableObject

## Overview

`System.DisposableObject` is a lightweight .NET class library that provides ready-to-use base classes implementing the [Dispose Pattern](https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose) for both synchronous (`IDisposable`) and asynchronous (`IAsyncDisposable`) resource cleanup. Consuming code inherits from one of the two base classes and overrides hook methods rather than re-implementing the full dispose pattern from scratch.

The library is distributed as a NuGet package (`System.DisposableObject`) and targets **net10.0**.

---

## Repository Layout

```
System.DisposableObject/          ← repository root
├── Image/
│   └── dispose.png               ← NuGet package icon
├── README.md                     ← Quick-start documentation (also bundled in the NuGet package)
├── LICENSE                       ← LGPL-3.0-or-later
└── Src/
    ├── System.DisposableObject Solution.sln   ← Visual Studio solution
    ├── NuGet.Publish.cmd                      ← Helper script to push *.nupkg files to nuget.org
    ├── System.DisposableObject/               ← Library project (produces NuGet package)
    │   ├── System.DisposableObject.csproj
    │   ├── DisposableObject.cs
    │   ├── AsyncDisposableObject.cs
    │   └── System.DisposableObject.xml        ← Generated XML documentation
    └── System.DisposableObject.Example/       ← Console application demonstrating usage
        ├── System.DisposableObject.Example.csproj
        ├── Program.cs
        ├── SomeObject.cs
        └── SomeAsyncObject.cs
```

---

## Key Technologies

| Technology | Role |
|---|---|
| **C# / .NET 10** | Implementation language and target runtime |
| **Visual Studio 2019+ solution format** | IDE project organisation (`Format Version 12.00`) |
| **`IDisposable`** | Standard synchronous resource-cleanup interface |
| **`IAsyncDisposable`** | Asynchronous resource-cleanup interface (C# 8 / .NET Core 3+) |
| **`System.Dynamic.DynamicObject`** | Base class for `DisposableObject`; enables dynamic dispatch and validates that the object has not been disposed before any member invocation |
| **`System.Diagnostics.Trace`** | Emits warnings and optional assertions when an object is finalised without having been explicitly disposed |
| **NuGet** | Package distribution; the library project generates both a `.nupkg` and a `.snupkg` (symbols) on build |
| **LGPL-3.0-or-later** | Open-source licence |

---

## Class Hierarchy

```
System.Dynamic.DynamicObject          (BCL)
    └── System.DisposableObject       (abstract) — implements IDisposable
            └── System.AsyncDisposableObject  (abstract) — also implements IAsyncDisposable
```

### `DisposableObject` (`DisposableObject.cs`)

The core base class. Key design decisions:

* **Inherits `DynamicObject`** — `TryInvokeMember` is overridden to call `AccessMethod()` before every dynamic dispatch, guaranteeing that calling a method on an already-disposed instance throws `ObjectDisposedException`.
* **Standard dispose pattern** — implements the canonical `Dispose()` / `Dispose(bool disposing)` / finalizer trio.
* **Re-entrancy guard** — `InProcessOfDisposing` flag prevents recursive disposal.
* **Double-disposal guard** — `IsDisposed` flag ensures `OnDisposeManagedObjects` and `OnDisposeUnmanagedObjects` are called at most once.
* **`GC.SuppressFinalize`** — called from `Dispose()` to remove the object from the finalisation queue after an explicit dispose.
* **Debug assistance** — `AssertWhenNotDisposed` (defaults to `false`) causes a `Trace.Assert` or warning when the finaliser fires without a prior `Dispose()` call; `OnGetClassName()` and `OnNotDisposedProperly()` are overridable hooks for customising the diagnostic output.

#### Overridable hooks for consumers

| Method | Purpose |
|---|---|
| `OnDisposeManagedObjects()` | Clean up CLR-managed objects (called only when disposing explicitly) |
| `OnDisposeUnmanagedObjects()` | Clean up native/unmanaged handles (called on both explicit dispose and GC finalisation) |
| `OnGetClassName()` | Return a human-readable class name for diagnostic messages |
| `OnNotDisposedProperly()` | Custom handling when GC finalises a non-disposed instance; return `true` to suppress the base-class warning |
| `AccessMethod()` | Guard method to call at the start of any public method to enforce "not disposed" invariant |

### `AsyncDisposableObject` (`AsyncDisposableObject.cs`)

Extends `DisposableObject` with `IAsyncDisposable` support. The `DisposeAsync()` implementation delegates to the synchronous `Dispose()` path and returns a completed `ValueTask`, making it safe to use in `await using` statements while maintaining full backwards compatibility with synchronous callers.

---

## Data / Control Flow During Disposal

```
Explicit call:  obj.Dispose()
                    │
                    ▼
            DisposableObject.Dispose()
                    │  calls GC.SuppressFinalize(this)
                    ▼
            DisposableObject.Dispose(disposing: true)
                    ├─► OnDisposeManagedObjects()   ← override in subclass
                    └─► OnDisposeUnmanagedObjects()  ← override in subclass

GC finaliser:   ~DisposableObject()
                    │  (object was never explicitly disposed)
                    ▼
            DisposableObject.Dispose(disposing: false)
                    └─► OnDisposeUnmanagedObjects()  ← managed objects must NOT be touched

Async call:     await obj.DisposeAsync()
                    │
                    ▼
            AsyncDisposableObject.DisposeAsync()
                    │  delegates to synchronous Dispose()
                    └─► (same flow as explicit synchronous call above)
```

---

## Example Project (`System.DisposableObject.Example`)

A minimal console application (`net10.0`) that exercises both base classes:

* **`SomeObject`** — inherits `DisposableObject`; overrides both hook methods.
* **`SomeAsyncObject`** — inherits `AsyncDisposableObject`; overrides both hook methods.
* **`Program.Main`** — instantiates each class inside a `using` block to trigger disposal.

The example project references the library via a `ProjectReference` (not a NuGet reference), so it always builds against the local source.

---

## Build & Publish

| Task | Command / File |
|---|---|
| Build library | `dotnet build` inside `Src/System.DisposableObject/` |
| Generate NuGet package | Automatic on build (`<GeneratePackageOnBuild>true</GeneratePackageOnBuild>`) |
| Publish to nuget.org | Run `Src/NuGet.Publish.cmd` (Windows only; requires NuGet API key) |

The package includes:
* The compiled assembly and XML documentation.
* Debug symbols as a separate `.snupkg` file.
* `README.md` (displayed on nuget.org).
* The `dispose.png` icon.
