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
using System.Threading.Tasks;

namespace System
{
	/// <summary>
	/// This class provides base functionality for implementing
	/// <see cref="IAsyncDisposable"/>. Any class that inherits from this class
	/// simply needs to override <see cref="DisposableObject.OnDisposeManagedObjects"/>,
	/// <see cref="DisposableObject.OnDisposeUnmanagedObjects"/>, and/or <see cref="DisposeAsyncCore"/>.
	/// </summary>
	public abstract class AsyncDisposableObject : DisposableObject, IAsyncDisposable
	{
		/// <summary>
		/// Default constructor for <see cref="AsyncDisposableObject"/>.
		/// </summary>
		public AsyncDisposableObject()
		{
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting
		/// unmanaged resources asynchronously.
		/// </summary>
		/// <returns>A <see cref="ValueTask"/> representing the asynchronous dispose operation.</returns>
		public virtual ValueTask DisposeAsync()
		{
			if (!this.IsDisposed)
			{
				ValueTask core = this.DisposeAsyncCore();

				if (!core.IsCompleted)
				{
					return FinishDisposeAsync(core);
				}

				//
				// Propagate any synchronous exception from the core step before
				// proceeding to the synchronous cleanup.
				//
				core.GetAwaiter().GetResult();
			}

			this.Dispose();
			return ValueTask.CompletedTask;
		}

		/// <summary>
		/// Override this method to perform asynchronous cleanup of managed resources.
		/// The base implementation returns a completed <see cref="ValueTask"/>.
		/// </summary>
		/// <returns>A <see cref="ValueTask"/> representing the asynchronous cleanup operation.</returns>
		protected virtual ValueTask DisposeAsyncCore()
		{
			return ValueTask.CompletedTask;
		}

		private async ValueTask FinishDisposeAsync(ValueTask core)
		{
			await core.ConfigureAwait(false);
			this.Dispose();
		}
	}
}