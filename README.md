![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/porrey/System.DisposableObject/.github%2Fworkflows%2Fdotnet.yml?style=for-the-badge&label=Build%20and%20Test) ![GitHub License](https://img.shields.io/github/license/porrey/System.DisposableObject?style=for-the-badge) ![.NET](https://img.shields.io/badge/.NET-10-purple?style=for-the-badge)

[![Nuget](https://img.shields.io/nuget/v/System.DisposableObject?label=System.DisposableObject%20-%20NuGet&style=for-the-badge)![Nuget](https://img.shields.io/nuget/dt/System.DisposableObject?label=Downloads&style=for-the-badge)](https://www.nuget.org/packages/System.DisposableObject/)
[![License: LGPL v3](https://img.shields.io/badge/License-LGPL%20v3-blue.svg?style=for-the-badge)](https://www.gnu.org/licenses/lgpl-3.0)

# System.DisposableObject

**System.DisposableObject** is a lightweight .NET base-class library that implements the [Dispose Pattern](https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose) so you don't have to. Inherit from one of two abstract base classes and override a pair of simple hook methods — the boilerplate is handled for you.

- `DisposableObject` — implements `IDisposable` with a full finalizer, double-dispose guard, and re-entrancy guard.
- `AsyncDisposableObject` — extends `DisposableObject` with `IAsyncDisposable`, enabling `await using` while keeping synchronous callers working.

---

## Table of Contents

1. [Installation](#installation)
2. [Quick Start](#quick-start)
   - [Synchronous disposal — `DisposableObject`](#synchronous-disposal--disposableobject)
   - [Async disposal — `AsyncDisposableObject`](#async-disposal--asyncdisposableobject)
3. [API Reference](#api-reference)
   - [`DisposableObject`](#disposableobject)
   - [`AsyncDisposableObject`](#asyncdisposableobject)
4. [Advanced Usage](#advanced-usage)
   - [Guarding methods against use after disposal](#guarding-methods-against-use-after-disposal)
   - [Debugging — detect objects not disposed properly](#debugging--detect-objects-not-disposed-properly)
   - [Custom class name in diagnostic messages](#custom-class-name-in-diagnostic-messages)
5. [How Disposal Works](#how-disposal-works)
6. [Contributing](#contributing)
7. [License](#license)
8. [References](#references)

---

## Installation

Install the package from NuGet:

```shell
dotnet add package System.DisposableObject
```

Or via the Package Manager Console in Visual Studio:

```powershell
Install-Package System.DisposableObject
```

---

## Quick Start

### Synchronous disposal — `DisposableObject`

Inherit from `DisposableObject` and override whichever hook methods your class needs.

```csharp
using System;

public class DatabaseConnection : DisposableObject
{
    private SqlConnection _connection;

    public DatabaseConnection(string connectionString)
    {
        _connection = new SqlConnection(connectionString);
        _connection.Open();
    }

    protected override void OnDisposeManagedObjects()
    {
        // Dispose CLR-managed objects (objects that implement IDisposable).
        _connection?.Dispose();
        _connection = null;
    }

    protected override void OnDisposeUnmanagedObjects()
    {
        // Release native handles, file descriptors, COM objects, etc.
        // Do NOT reference other .NET objects here — this method can be
        // called by the GC finalizer after managed objects have been collected.
    }
}
```

Use it with a `using` statement (recommended):

```csharp
using (var db = new DatabaseConnection(connectionString))
{
    // Work with db here.
} // OnDisposeManagedObjects() and OnDisposeUnmanagedObjects() are called here.
```

Or call `Dispose()` explicitly:

```csharp
var db = new DatabaseConnection(connectionString);
// ... use db ...
db.Dispose();
```

---

### Async disposal — `AsyncDisposableObject`

Inherit from `AsyncDisposableObject` when your class wraps async-capable resources or when callers may use `await using`.

```csharp
using System;
using System.Threading.Tasks;

public class AsyncDatabaseConnection : AsyncDisposableObject
{
    private SqlConnection _connection;

    public AsyncDatabaseConnection(string connectionString)
    {
        _connection = new SqlConnection(connectionString);
    }

    protected override void OnDisposeManagedObjects()
    {
        // Synchronous cleanup of managed resources.
        _connection?.Dispose();
        _connection = null;
    }

    protected override void OnDisposeUnmanagedObjects()
    {
        // Native resource cleanup.
    }
}
```

Use it with `await using` (recommended for async code):

```csharp
await using (var db = new AsyncDatabaseConnection(connectionString))
{
    // Work with db here.
} // DisposeAsync() → Dispose() → hooks called here.
```

Synchronous callers still work as before:

```csharp
using (var db = new AsyncDatabaseConnection(connectionString))
{
    // Works fine — AsyncDisposableObject inherits DisposableObject.
}
```

---

## API Reference

### `DisposableObject`

**Namespace:** `System`  
**Assembly:** `System.DisposableObject`  
**Inherits:** `System.Dynamic.DynamicObject`  
**Implements:** `IDisposable`

`DisposableObject` is abstract. You cannot instantiate it directly; create a class that inherits from it.

#### Protected properties

| Property | Type | Description |
|---|---|---|
| `IsDisposed` | `bool` | `true` after `Dispose()` has been called. Read-only to subclasses; set internally. |
| `InProcessOfDisposing` | `bool` (virtual) | `true` while disposal is in progress. Used as a re-entrancy guard. |
| `AssertWhenNotDisposed` | `bool` (virtual) | When `true`, a `Trace.Assert` or warning is emitted if the finalizer fires before `Dispose()`. Defaults to `false`. |

#### Public methods

| Method | Description |
|---|---|
| `void Dispose()` | Disposes the object. Calls `OnDisposeManagedObjects()` and `OnDisposeUnmanagedObjects()`, then calls `GC.SuppressFinalize(this)`. Safe to call multiple times. |
| `bool TryInvokeMember(...)` | Overrides `DynamicObject.TryInvokeMember`. Calls `AccessMethod()` before delegating, ensuring that dynamic member invocations throw `ObjectDisposedException` on disposed instances. |

#### Protected virtual methods (hooks)

Override any of the following in your subclass:

| Method | When called | Notes |
|---|---|---|
| `OnDisposeManagedObjects()` | During an explicit `Dispose()` call only | Safe to access other managed (.NET) objects here. |
| `OnDisposeUnmanagedObjects()` | During both explicit `Dispose()` and GC finalization | **Do not** reference managed objects here — they may already have been collected. |
| `AccessMethod()` | Call at the start of any public method in your subclass | Throws `ObjectDisposedException` if `IsDisposed` is `true`. |
| `OnGetClassName()` | Called when a diagnostic message is generated | Override to return a meaningful class name (defaults to `this.ToString()`). |
| `OnNotDisposedProperly()` | Called from the finalizer when `AssertWhenNotDisposed` is `true` | Return `true` to suppress the base-class warning/assertion and handle it yourself. |

---

### `AsyncDisposableObject`

**Namespace:** `System`  
**Assembly:** `System.DisposableObject`  
**Inherits:** `DisposableObject`  
**Implements:** `IDisposable`, `IAsyncDisposable`

Adds a `DisposeAsync()` method on top of everything provided by `DisposableObject`. The hook methods (`OnDisposeManagedObjects`, `OnDisposeUnmanagedObjects`) are identical — there is no separate async hook.

#### Public methods (in addition to `DisposableObject`)

| Method | Description |
|---|---|
| `ValueTask DisposeAsync()` | Calls the synchronous `Dispose()` and returns `ValueTask.CompletedTask`. Safe to call multiple times and to mix with synchronous `Dispose()` calls. |

---

## Advanced Usage

### Guarding methods against use after disposal

Call `AccessMethod()` at the start of any public or internal method in your subclass to ensure the object has not been disposed:

```csharp
public class MyResource : DisposableObject
{
    private byte[] _buffer = new byte[1024];

    public int ReadData(byte[] destination, int count)
    {
        // Throws ObjectDisposedException if Dispose() was already called.
        this.AccessMethod();

        // ... perform the read ...
        return count;
    }

    protected override void OnDisposeManagedObjects()
    {
        _buffer = null;
    }
}
```

After `Dispose()` is called, any subsequent call to `ReadData` will throw `ObjectDisposedException` with the name of the type.

---

### Debugging — detect objects not disposed properly

Enable `AssertWhenNotDisposed` in your subclass constructor during development to catch objects that are garbage-collected without an explicit `Dispose()`:

```csharp
public class LeakDetectingResource : DisposableObject
{
    public LeakDetectingResource()
    {
        // Enable only in DEBUG builds to help catch disposal leaks.
#if DEBUG
        this.AssertWhenNotDisposed = true;
#endif
    }

    protected override void OnDisposeManagedObjects() { /* ... */ }
}
```

When `AssertWhenNotDisposed` is `true` and the GC finalizer runs without a prior `Dispose()`:

- If `OnNotDisposedProperly()` returns `false` (the default), a `Trace.Assert` fires.
- If `OnNotDisposedProperly()` returns `true`, a `Trace.TraceWarning` is emitted instead and no assertion fires.

---

### Custom class name in diagnostic messages

Override `OnGetClassName()` to provide a more readable name in trace/assert messages:

```csharp
public class MyService : DisposableObject
{
    protected override string OnGetClassName() => nameof(MyService);

    protected override void OnDisposeManagedObjects() { /* ... */ }
}
```

---

## How Disposal Works

The diagram below shows the code paths during object lifetime.

```
Explicit call:  obj.Dispose()
                    │
                    ▼
        DisposableObject.Dispose()
                    │  ── calls GC.SuppressFinalize(this)
                    ▼
        DisposableObject.Dispose(disposing: true)
                    ├─► OnDisposeManagedObjects()    ← override in subclass
                    └─► OnDisposeUnmanagedObjects()  ← override in subclass

GC finalizer:   ~DisposableObject()
                    │  (only reached if Dispose() was never called)
                    ▼
        DisposableObject.Dispose(disposing: false)
                    └─► OnDisposeUnmanagedObjects()  ← managed objects must NOT be touched

Async call:     await obj.DisposeAsync()
                    │
                    ▼
        AsyncDisposableObject.DisposeAsync()
                    │  ── delegates to synchronous Dispose()
                    └─► (same flow as explicit synchronous call above)
```

Key guarantees provided by the base classes:

| Guarantee | How it is enforced |
|---|---|
| Hooks called at most once | `IsDisposed` flag checked before calling hooks |
| No recursive disposal | `InProcessOfDisposing` re-entrancy guard |
| Finalizer suppressed after explicit dispose | `GC.SuppressFinalize(this)` called from `Dispose()` |
| Dynamic member access blocked after dispose | `TryInvokeMember` calls `AccessMethod()` |

---

## Contributing

Contributions are welcome. Please open an issue or submit a pull request on [GitHub](https://github.com/porrey/System.DisposableObject).

---

## License

This library is distributed under the [GNU Lesser General Public License v3.0 or later (LGPL-3.0-or-later)](https://www.gnu.org/licenses/lgpl-3.0.html). See [LICENSE](LICENSE) for the full text.

---

## References

1. [IDisposable Interface — Microsoft Docs](https://docs.microsoft.com/en-us/dotnet/api/system.idisposable)
2. [Implementing a Dispose method — Microsoft Docs](https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose)
3. [Implementing a DisposeAsync method — Microsoft Docs](https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync)
4. [IAsyncDisposable Interface — Microsoft Docs](https://docs.microsoft.com/en-us/dotnet/api/system.iasyncdisposable)
