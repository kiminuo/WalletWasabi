using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace WalletWasabi.Tests.UnitTests.Extensions
{
	public class TaskExtensionsTests
	{
		[Fact]
		public async void WithAwaitCancellationAsyncTestAsync()
		{
			{
				var stopwatch = new Stopwatch();
				stopwatch.Start();

				try
				{
					await Task.Delay(TimeSpan.FromSeconds(5)).WithAwaitCancellationAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
				}
				catch (OperationCanceledException e)
				{
					Debug.WriteLine($"Exception: {e}");
				}

				stopwatch.Stop();
				long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
				Debug.WriteLine($"first: {elapsedMilliseconds}");
			}

			{
				var stopwatch = new Stopwatch();
				stopwatch.Start();

				try
				{
					await Task.Delay(TimeSpan.FromSeconds(5)).WithAwaitCancellationAsync(TimeSpan.FromSeconds(7)).ConfigureAwait(false);
				}
				catch (OperationCanceledException e)
				{
					Debug.WriteLine($"Exception: {e}");
				}

				stopwatch.Stop();
				long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
				Debug.WriteLine($"second: {elapsedMilliseconds}");
			}
		}
	}
}
