using System;

namespace Sample
{
	public class SomeAsyncObject : AsyncDisposableObject
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
}