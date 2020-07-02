using System;
using System.Threading;
using System.Threading.Tasks;

namespace WalletWasabi.Tests.Helpers
{
	/// <summary>
	/// A code part is required to finish in predefined amount of time or be denoted as timed out.
	///
	/// <para>This class helps you avoid using <see cref="Task.Delay(TimeSpan)"/> to give program "room to finish" - e.g. <c>Task.Delay(TimeSpan.FromMilliseconds(100))</c>.</para>
	/// </summary>
	public class TimeBoundedSection : IDisposable
	{
		/// <summary>
		/// Note that timeout starts at the moment of class instantiation.
		/// </summary>
		public TimeBoundedSection(TimeSpan timeout)
		{
			Timeout = timeout;
			Cts = new CancellationTokenSource(Timeout);
		}

		public TimeSpan Timeout { get; }

		/// <summary>
		/// To detect redundant calls.
		/// </summary>
		private bool _disposed = false;

		private CancellationTokenSource Cts { get; }

		public enum Result
		{
			/// <summary>Operation finished in time.</summary>
			Ok,
			/// <summary><see cref="Timeout"/> was hit.</summary>
			Timeouted
		}

		/// <summary>
		/// Report that the task is finished which unlocks <see cref="WaitAsync"/> method to continue.
		/// </summary>
		public void SetDone()
		{
			Cts.Cancel();
		}

		/// <summary>
		/// Waits until <see cref="Timeout"/> elapses or <see cref="SetDone"/> is called.
		///
		/// <para>This means that waiting time is at most <see cref="Timeout"/> and not greater.</para>
		/// </summary>
		public async Task<Result> WaitAsync()
		{
			try
			{
				await Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, Cts.Token);
				return Result.Ok;
			}
			catch (TaskCanceledException)
			{
				return Result.Timeouted;
			}
		}

		/// <summary>
		/// Dispose implementation.
		/// </summary>
		/// <param name="disposing">The disposing parameter should be false when called from a finalizer, and true when called from the IDisposable.Dispose method. In other words, it is true when deterministically called and false when non-deterministically called.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					// Dispose managed state (managed objects).
					Cts?.Dispose();
				}

				// Free unmanaged resources (unmanaged objects) and override finalizer if any.
				// Set large fields to null if any.
				_disposed = true;
			}
		}

		/// <summary>
		/// Standard Dispose implementation pattern.
		/// </summary>
		public void Dispose()
		{
			// Dispose of unmanaged resources.
			Dispose(disposing: true);
			// Suppress finalization.
			GC.SuppressFinalize(this);
		}
	}
}