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
using System.Threading.Tasks;
using Xunit;

namespace DisposableObjectTests
{
	// ---------------------------------------------------------------------------
	// Concrete test doubles for AsyncDisposableObject
	// ---------------------------------------------------------------------------

	/// <summary>
	/// Minimal async-disposable subclass that records hook invocations.
	/// </summary>
	internal class TrackingAsyncDisposable : AsyncDisposableObject
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

		public bool IsDisposedPublic => IsDisposed;

		public void CallAccessMethod() => AccessMethod();
	}

	/// <summary>
	/// Subclass that re-enters Dispose() from <see cref="AsyncDisposableObject.OnDisposeManagedObjects"/>
	/// to verify the re-entrancy guard survives the async code path.
	/// </summary>
	internal class ReentrantAsyncDisposable : AsyncDisposableObject
	{
		public int ManagedDisposeCount { get; private set; }

		protected override void OnDisposeManagedObjects()
		{
			ManagedDisposeCount++;
			// Attempt to dispose again while already in the dispose path.
			Dispose();
		}
	}

	// ---------------------------------------------------------------------------
	// Tests
	// ---------------------------------------------------------------------------

	public class AsyncDisposableObjectTests
	{
		// -----------------------------------------------------------------------
		// Interface implementation
		// -----------------------------------------------------------------------

		[Fact]
		public void ImplementsIAsyncDisposable()
		{
			var obj = new TrackingAsyncDisposable();
			Assert.IsAssignableFrom<IAsyncDisposable>(obj);
		}

		[Fact]
		public void ImplementsIDisposable()
		{
			var obj = new TrackingAsyncDisposable();
			Assert.IsAssignableFrom<IDisposable>(obj);
		}

		// -----------------------------------------------------------------------
		// DisposeAsync sets IsDisposed
		// -----------------------------------------------------------------------

		[Fact]
		public async Task DisposeAsync_SetsIsDisposedTrue()
		{
			var obj = new TrackingAsyncDisposable();
			await obj.DisposeAsync();
			Assert.True(obj.IsDisposedPublic);
		}

		[Fact]
		public async Task DisposeAsync_ReturnsCompletedValueTask()
		{
			var obj = new TrackingAsyncDisposable();
			ValueTask vt = obj.DisposeAsync();
			Assert.True(vt.IsCompleted);
			await vt; // must not throw
		}

		// -----------------------------------------------------------------------
		// await using pattern
		// -----------------------------------------------------------------------

		[Fact]
		public async Task AwaitUsing_DisposesObject()
		{
			TrackingAsyncDisposable obj;
			await using (obj = new TrackingAsyncDisposable())
			{
				Assert.False(obj.IsDisposedPublic);
			}

			Assert.True(obj.IsDisposedPublic);
		}

		// -----------------------------------------------------------------------
		// Hook methods
		// -----------------------------------------------------------------------

		[Fact]
		public async Task DisposeAsync_CallsOnDisposeManagedObjects()
		{
			var obj = new TrackingAsyncDisposable();
			await obj.DisposeAsync();
			Assert.Equal(1, obj.ManagedDisposeCount);
		}

		[Fact]
		public async Task DisposeAsync_CallsOnDisposeUnmanagedObjects()
		{
			var obj = new TrackingAsyncDisposable();
			await obj.DisposeAsync();
			Assert.Equal(1, obj.UnmanagedDisposeCount);
		}

		// -----------------------------------------------------------------------
		// Double-dispose guard (async)
		// -----------------------------------------------------------------------

		[Fact]
		public async Task DisposeAsync_CalledTwice_HooksInvokedOnlyOnce()
		{
			var obj = new TrackingAsyncDisposable();
			await obj.DisposeAsync();
			await obj.DisposeAsync();

			Assert.Equal(1, obj.ManagedDisposeCount);
			Assert.Equal(1, obj.UnmanagedDisposeCount);
		}

		// -----------------------------------------------------------------------
		// Mixing sync and async disposal
		// -----------------------------------------------------------------------

		[Fact]
		public async Task SyncDispose_ThenDisposeAsync_HooksInvokedOnlyOnce()
		{
			var obj = new TrackingAsyncDisposable();
			obj.Dispose();
			await obj.DisposeAsync();

			Assert.Equal(1, obj.ManagedDisposeCount);
			Assert.Equal(1, obj.UnmanagedDisposeCount);
		}

		[Fact]
		public async Task DisposeAsync_ThenSyncDispose_HooksInvokedOnlyOnce()
		{
			var obj = new TrackingAsyncDisposable();
			await obj.DisposeAsync();
			obj.Dispose();

			Assert.Equal(1, obj.ManagedDisposeCount);
			Assert.Equal(1, obj.UnmanagedDisposeCount);
		}

		// -----------------------------------------------------------------------
		// AccessMethod guard (inherited from DisposableObject)
		// -----------------------------------------------------------------------

		[Fact]
		public async Task AccessMethod_ThrowsObjectDisposedException_AfterDisposeAsync()
		{
			var obj = new TrackingAsyncDisposable();
			await obj.DisposeAsync();
			Assert.Throws<ObjectDisposedException>(() => obj.CallAccessMethod());
		}

		// -----------------------------------------------------------------------
		// Re-entrancy guard
		// -----------------------------------------------------------------------

		[Fact]
		public void Dispose_ReentrantFromHook_HookInvokedExactlyOnce()
		{
			var obj = new ReentrantAsyncDisposable();
			obj.Dispose();
			Assert.Equal(1, obj.ManagedDisposeCount);
		}
	}
}
