//
// Copyright(C) 2017-2021, Daniel M. Porrey. All rights reserved.
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with this program. If not, see http://www.gnu.org/licenses/.
//
using System.Diagnostics;
using System.Threading.Tasks;

namespace System
{
	/// <summary>
	/// This class provides base functionality for implementing
	/// <see cref="IAsyncDisposable"/>. Since we are dealing with
	/// database resources, disposing of objects is important. Any
	/// class that inherits from this class simply needs to override
	/// OnDisposeManagedObjects and/or OnDisposeUnmanagedObjects.
	/// </summary>
	public abstract class AsyncDisposableObject : DisposableObject, IAsyncDisposable
	{
		/// <summary>
		/// Default constructor for <see cref="AsyncDisposableObject"/>.
		/// </summary>
		public AsyncDisposableObject()
		{
			//
			// Set this to True for debugging.
			//
			this.AssertWhenNotDisposed = false;
		}

		/// <summary>
		/// Default destructor for <see cref="AsyncDisposableObject"/>.
		/// </summary>
		~AsyncDisposableObject()
		{
			//
			// Write a trace (to the debugger) showing this method was called (it will only
			// get called if this object is not Disposed).
			//
			Trace.TraceWarning("~BaseObject called on {0}", this.OnGetClassName());

			//
			// Give the parent object a chance to respond, if not then this
			// class will assert.
			//
			if (this.AssertWhenNotDisposed)
			{
				if (!this.OnNotDisposedProperly())
				{
					//
					// Assert if this object is destroyed without being disposed. Even though
					// dispose is called here, it is more ideal that it be called by the user
					// of the object. This assert will help catch this instance.
					//
					Trace.Assert(this.IsDisposed, this.OnGetClassName() + " was not disposed properly.");
				}
				else
				{
					Trace.TraceWarning("{0} was not disposed properly.", this.OnGetClassName());
				}
			}

			//
			// This destructor is only called by garbage collection. Because of this, this object
			// can no longer access managed objects. Only unmanaged objects will be cleaned up
			// here.
			//
			this.Dispose(false);
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting
		/// unmanaged resources asynchronously.
		/// </summary>
		/// <returns></returns>
		public virtual ValueTask DisposeAsync()
		{
			this.Dispose();
			return ValueTask.CompletedTask;
		}
	}
}