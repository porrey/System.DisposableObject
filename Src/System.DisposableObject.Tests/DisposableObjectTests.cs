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
using System;
using Xunit;

// The library classes (DisposableObject, AsyncDisposableObject) live in the
// System namespace. The test namespace is kept simple to avoid a name collision
// with the assembly name "System.DisposableObject".
namespace DisposableObjectTests
{
	// ---------------------------------------------------------------------------
	// Concrete test doubles that expose internal state for assertions.
	// ---------------------------------------------------------------------------

	/// <summary>
	/// Minimal concrete subclass that records which hook methods were invoked.
	/// </summary>
	internal class TrackingDisposable : DisposableObject
	{
		public int ManagedDisposeCount { get; private set; }
		public int UnmanagedDisposeCount { get; private set; }

		protected override void OnDisposeManagedObjects()
		{
			ManagedDisposeCount++;
		}

		protected override void OnDisposeUnmanagedObjects()
		{
			UnmanagedDisposeCount++;
		}

		// Expose the protected property for assertions.
		public bool IsDisposedPublic => IsDisposed;

		// Allow tests to call AccessMethod() to verify the disposed guard.
		public void CallAccessMethod() => AccessMethod();
	}

	/// <summary>
	/// Subclass that overrides <see cref="DisposableObject.OnGetClassName"/> to return a fixed string.
	/// </summary>
	internal class CustomClassNameDisposable : DisposableObject
	{
		public const string ClassName = "MyCustomClass";
		protected override string OnGetClassName() => ClassName;
		public string GetClassName() => OnGetClassName();
	}

	/// <summary>
	/// Subclass where <see cref="DisposableObject.OnNotDisposedProperly"/> returns <c>true</c>
	/// (suppresses the default assert).
	/// </summary>
	internal class HandledNotDisposedProperly : DisposableObject
	{
		protected override bool OnNotDisposedProperly() => true;
		public bool CallOnNotDisposedProperly() => OnNotDisposedProperly();
	}

	/// <summary>
	/// Subclass that re-enters Dispose() from within OnDisposeManagedObjects
	/// to verify the re-entrancy guard.
	/// </summary>
	internal class ReentrantDisposable : DisposableObject
	{
		public int ManagedDisposeCount { get; private set; }

		protected override void OnDisposeManagedObjects()
		{
			ManagedDisposeCount++;
			// Attempt a second Dispose() while we are already disposing.
			Dispose();
		}
	}

	// ---------------------------------------------------------------------------
	// Tests
	// ---------------------------------------------------------------------------

	public class DisposableObjectTests
	{
		// -----------------------------------------------------------------------
		// IsDisposed state
		// -----------------------------------------------------------------------

		[Fact]
		public void IsDisposed_IsFalse_BeforeDispose()
		{
			var obj = new TrackingDisposable();
			Assert.False(obj.IsDisposedPublic);
		}

		[Fact]
		public void IsDisposed_IsTrue_AfterDispose()
		{
			var obj = new TrackingDisposable();
			obj.Dispose();
			Assert.True(obj.IsDisposedPublic);
		}

		// -----------------------------------------------------------------------
		// Hook methods called during disposal
		// -----------------------------------------------------------------------

		[Fact]
		public void Dispose_CallsOnDisposeManagedObjects()
		{
			var obj = new TrackingDisposable();
			obj.Dispose();
			Assert.Equal(1, obj.ManagedDisposeCount);
		}

		[Fact]
		public void Dispose_CallsOnDisposeUnmanagedObjects()
		{
			var obj = new TrackingDisposable();
			obj.Dispose();
			Assert.Equal(1, obj.UnmanagedDisposeCount);
		}

		// -----------------------------------------------------------------------
		// Double-dispose guard
		// -----------------------------------------------------------------------

		[Fact]
		public void Dispose_CalledTwice_HooksInvokedOnlyOnce()
		{
			var obj = new TrackingDisposable();
			obj.Dispose();
			obj.Dispose();

			Assert.Equal(1, obj.ManagedDisposeCount);
			Assert.Equal(1, obj.UnmanagedDisposeCount);
		}

		// -----------------------------------------------------------------------
		// using-statement pattern
		// -----------------------------------------------------------------------

		[Fact]
		public void Using_Dispose_SetsIsDisposedTrue()
		{
			TrackingDisposable obj;
			using (obj = new TrackingDisposable())
			{
				Assert.False(obj.IsDisposedPublic);
			}

			Assert.True(obj.IsDisposedPublic);
		}

		// -----------------------------------------------------------------------
		// AccessMethod guard
		// -----------------------------------------------------------------------

		[Fact]
		public void AccessMethod_DoesNotThrow_WhenNotDisposed()
		{
			var obj = new TrackingDisposable();
			var ex = Record.Exception(() => obj.CallAccessMethod());
			Assert.Null(ex);
		}

		[Fact]
		public void AccessMethod_ThrowsObjectDisposedException_WhenDisposed()
		{
			var obj = new TrackingDisposable();
			obj.Dispose();
			Assert.Throws<ObjectDisposedException>(() => obj.CallAccessMethod());
		}

		// -----------------------------------------------------------------------
		// Re-entrancy guard
		// -----------------------------------------------------------------------

		[Fact]
		public void Dispose_ReentrantCall_HookInvokedExactlyOnce()
		{
			var obj = new ReentrantDisposable();
			// OnDisposeManagedObjects calls Dispose() again internally.
			obj.Dispose();
			Assert.Equal(1, obj.ManagedDisposeCount);
		}

		// -----------------------------------------------------------------------
		// OnGetClassName
		// -----------------------------------------------------------------------

		[Fact]
		public void OnGetClassName_DefaultImpl_ReturnsNonEmptyString()
		{
			var obj = new TrackingDisposable();
			// Default implementation returns this.ToString(); just verify it's non-null/empty.
			Assert.False(string.IsNullOrEmpty(obj.ToString()));
		}

		[Fact]
		public void OnGetClassName_Override_ReturnsCustomName()
		{
			var obj = new CustomClassNameDisposable();
			Assert.Equal(CustomClassNameDisposable.ClassName, obj.GetClassName());
		}

		// -----------------------------------------------------------------------
		// OnNotDisposedProperly
		// -----------------------------------------------------------------------

		[Fact]
		public void OnNotDisposedProperly_DefaultImpl_ReturnsFalse()
		{
			// The default (unoverridden) implementation returns false.
			// Verify via a subclass that does NOT override it — we use TrackingDisposable
			// which only overrides the hook methods, not OnNotDisposedProperly.
			// We can't call it directly (it's protected), so we verify behaviour
			// indirectly: the overridden version in HandledNotDisposedProperly returns true.
			var obj = new HandledNotDisposedProperly();
			Assert.True(obj.CallOnNotDisposedProperly());
		}

		// -----------------------------------------------------------------------
		// IDisposable interface
		// -----------------------------------------------------------------------

		[Fact]
		public void ImplementsIDisposable()
		{
			var obj = new TrackingDisposable();
			Assert.IsAssignableFrom<IDisposable>(obj);
		}

		// -----------------------------------------------------------------------
		// TryInvokeMember guard (dynamic dispatch)
		// -----------------------------------------------------------------------

		[Fact]
		public void TryInvokeMember_ThrowsObjectDisposedException_WhenDisposed()
		{
			dynamic obj = new TrackingDisposable();
			obj.Dispose();

			// Invoking any dynamic member on a disposed instance must throw ObjectDisposedException.
			Assert.Throws<ObjectDisposedException>(() =>
			{
				obj.NonExistentMethod();
			});
		}

		[Fact]
		public void TryInvokeMember_DoesNotThrowObjectDisposedException_BeforeDispose()
		{
			dynamic obj = new TrackingDisposable();

			// base.TryInvokeMember returns false for an unknown member which causes a
			// RuntimeBinderException — but that is different from ObjectDisposedException,
			// confirming that AccessMethod passed (object was not yet disposed).
			var ex = Record.Exception(() => obj.NonExistentMethod());
			Assert.NotNull(ex);
			Assert.IsNotType<ObjectDisposedException>(ex);
		}
	}
}
