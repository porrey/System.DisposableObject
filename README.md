[![Nuget](https://img.shields.io/nuget/v/System.DisposableObject?label=System.DisposableObject%20-%20NuGet&style=for-the-badge)
![Nuget](https://img.shields.io/nuget/dt/System.DisposableObject?label=Downloads&style=for-the-badge)](https://www.nuget.org/packages/System.DisposableObject/)

# System.DisposableObject
Base object for disposing managed and unmanaged objects. This object implements the dispose pattern for the .NET Framework.

## Example
Create a class and inherit from **DisposableObject** as shown below:

    using System;
    
    public class SomeObject : DisposableObject
    {
      protected override void OnDisposeManagedObjects()
      {
	    //
	    // Disposed CLR managed objects here.
	    //
      }
    
      protected override void OnDisposeUnmanagedObjects()
      {
	    //
	    // Disposed non-CLR managed objects here.
	    //
      }
    }

Override one or both of the above methods depending on the type of resources your class uses.

When instantiating the class. wrap it in a using statement.

    using (SomeObject obj = new SomeObject())
    {
      //
      // obj will be disposed and the dispose
      // methods will be called.
    }

or call the Dispose() method.

    SomeObject obj = new SomeObject();
    obj.Dispose();

In either case, the **OnDisposeManagedObjects()** and **OnDisposeUnmanagedObjects()** are called.

## References

1. [https://docs.microsoft.com/en-us/dotnet/api/system.idisposable](https://docs.microsoft.com/en-us/dotnet/api/system.idisposable "https://docs.microsoft.com/en-us/dotnet/api/system.idisposable")

2. [https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose](https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose "https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose")
