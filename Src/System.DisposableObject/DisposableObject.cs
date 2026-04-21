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
using System.Dynamic;
using System.Threading;

namespace System
{
	/// <summary>
	/// This class provides base functionality for implementing
	/// <see cref="IDisposable"/>. Any class that inherits from this class
	/// simply needs to override <see cref="OnDisposeManagedObjects"/> and/or
	/// <see cref="OnDisposeUnmanagedObjects"/>.
	/// </summary>
	public abstract class DisposableObject : DynamicObject, IDisposable
	{
		// Disposal state: 0 = not started, 1 = in progress, 2 = completed.
		private int _disposed = 0;

		/// <summary>
		/// Gets a value that specifies if this object has been disposed or not.
		/// </summary>
		public bool IsDisposed => Volatile.Read(ref _disposed) >= 2;

		/// <summary>
		/// Raised when the object has been successfully disposed via <see cref="Dispose()"/>.
		/// This event is not raised when the finalizer disposes the object.
		/// </summary>
		public event EventHandler Disposed;

		/// <summary>
		/// Default constructor for <see cref="DisposableObject"/>.
		/// </summary>
		public DisposableObject()
		{
			//
			// Set this to True for debugging.
			//
			this.AssertWhenNotDisposed = false;
		}

		/// <summary>
		/// Default destructor for <see cref="DisposableObject"/>.
		/// </summary>
		~DisposableObject()
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
		/// Called to perform cleanup of valuable resources and to set the state of
		/// the object to an unusable state.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);

			//
			// This object will be cleaned up by the Dispose method.
			// The call to GC.SupressFinalize will
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			//
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Set to true if the object is currently being disposed.
		/// </summary>
		protected virtual bool InProcessOfDisposing { get; set; } = false;

		/// <summary>
		/// Called internally to dispose
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing)
		{
			//
			// Atomically transition from "not started" (0) to "in progress" (1).
			// Re-entrant calls and concurrent calls from other threads both return
			// early here, preventing double cleanup.
			//
			if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
			{
				return;
			}

			this.InProcessOfDisposing = true;
			try
			{
				//
				// Dispose(bool disposing) executes in two distinct scenarios.
				// If disposing equals true, the method has been called directly
				// from code. Managed and unmanaged resources can be disposed. If
				// disposing equals false, the method has been called by the
				// runtime from inside the finalizer and you should not reference
				// other objects. Only unmanaged resources can be disposed.
				//

				//
				// Dispose managed resources (any objects with a Dispose method).
				//
				if (disposing)
				{
					this.OnDisposeManagedObjects();
				}

				//
				// Cleanup unmanaged resources here (no calls
				// to any .NET objects should be made here).
				//
				this.OnDisposeUnmanagedObjects();
			}
			finally
			{
				//
				// Mark disposal as completed (transitions from "in progress" (1) to "completed" (2)).
				//
				Interlocked.Exchange(ref _disposed, 2);
				this.InProcessOfDisposing = false;
			}

			//
			// Notify listeners that this object has been disposed. Only raised for
			// deterministic (non-finalizer) disposal.
			//
			if (disposing)
			{
				this.Disposed?.Invoke(this, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Set this property to True to inform the class to display a message if the object is destroyed
		/// without the Dispose() method having been called.
		/// </summary>
		protected virtual bool AssertWhenNotDisposed { get; set; }

		/// <summary>
		/// Classes should override this method to perform cleanup of managed objects.
		/// </summary>
		protected virtual void OnDisposeManagedObjects()
		{
		}

		/// <summary>
		/// Classes should override this method to perform cleanup of unmanaged objects.
		/// </summary>
		protected virtual void OnDisposeUnmanagedObjects()
		{
		}

		/// <summary>
		/// Classes should call this method prior to any other method call being invoked to ensure that
		/// the Dispose() has not been called on the class.
		/// </summary>
		protected virtual void AccessMethod()
		{
			//
			// Called in any method of the inherited class. If this
			// object is disposed, it will throw an exception.
			//
			ObjectDisposedException.ThrowIf(this.IsDisposed, this);
		}

		/// <summary>
		/// Classes should override this method to provide a class name that is displayed in the Assert
		/// message. See the property AssertWhenNotDisposed.
		/// </summary>
		protected virtual string OnGetClassName()
		{
			//
			// Override to show the class name
			//
			return this.ToString();
		}

		/// <summary>
		/// Classes should override this method to handle the message when the class is not disposed
		/// properly instead of having the base class handling it. The class should return True
		/// to suppress any messages from the base class.
		/// </summary>
		protected virtual bool OnNotDisposedProperly()
		{
			//
			// Override this member to handle the assertion in the
			// overriding class.
			//
			// Return True if handled
			//
			return false;
		}

		/// <summary>
		/// Provides the implementation for operations that get a member value. Classes derived
		/// from the <see cref="DynamicObject"/> class can override this method to specify
		/// dynamic behavior for operations such as getting a property value.
		/// </summary>
		/// <param name="binder">Provides information about the object that called the dynamic operation.</param>
		/// <param name="result">The result of the get operation.</param>
		/// <returns>true if the operation is successful; otherwise, false. If this method returns
		/// false, the run-time binder of the language determines the behavior. (In most
		/// cases, a language-specific run-time exception is thrown.)</returns>
		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			this.AccessMethod();
			return base.TryGetMember(binder, out result);
		}

		/// <summary>
		/// Provides the implementation for operations that set a member value. Classes derived
		/// from the <see cref="DynamicObject"/> class can override this method to specify
		/// dynamic behavior for operations such as setting a property value.
		/// </summary>
		/// <param name="binder">Provides information about the object that called the dynamic operation.</param>
		/// <param name="value">The value to set to the member.</param>
		/// <returns>true if the operation is successful; otherwise, false. If this method returns
		/// false, the run-time binder of the language determines the behavior. (In most
		/// cases, a language-specific run-time exception is thrown.)</returns>
		public override bool TrySetMember(SetMemberBinder binder, object value)
		{
			this.AccessMethod();
			return base.TrySetMember(binder, value);
		}

		/// <summary>
		/// Provides the implementation for operations that invoke a member. Classes derived
		/// from the <see cref="DynamicObject"/> class can override this method to specify
		/// dynamic behavior for operations such as calling a method.
		/// </summary>
		/// <param name="binder">Provides information about the dynamic operation. The binder.Name property provides
		/// the name of the member on which the dynamic operation is performed. For example,
		/// for the statement sampleObject.SampleMethod(100), where sampleObject is an instance
		/// of the class derived from the System.Dynamic.DynamicObject class, binder.Name
		/// returns "SampleMethod". The binder.IgnoreCase property specifies whether the
		/// member name is case-sensitive.</param>
		/// <param name="args">The arguments that are passed to the object member during the invoke operation.
		/// For example, for the statement sampleObject.SampleMethod(100), where sampleObject
		/// is derived from the System.Dynamic.DynamicObject class, args[0] is equal to 100.</param>
		/// <param name="result">The result of the member invocation.</param>
		/// <returns>true if the operation is successful; otherwise, false. If this method returns
		/// false, the run-time binder of the language determines the behavior. (In most
		/// cases, a language-specific run-time exception is thrown.)</returns>
		public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
		{
			bool returnValue = false;

			this.AccessMethod();
			returnValue = base.TryInvokeMember(binder, args, out result);

			return returnValue;
		}
	}
}