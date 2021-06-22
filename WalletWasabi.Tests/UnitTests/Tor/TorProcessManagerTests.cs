using Moq;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WalletWasabi.Logging;
using WalletWasabi.Microservices;
using WalletWasabi.Tests.Helpers;
using WalletWasabi.Tor;
using WalletWasabi.Tor.Control;
using WalletWasabi.Tor.Socks5;
using Xunit;

namespace WalletWasabi.Tests.UnitTests.Tor
{
	/// <summary>
	/// Tests for <see cref="TorProcessManager"/> class.
	/// </summary>
	public class TorProcessManagerTests
	{
		[Fact]
		public async Task StartProcessAsync()
		{
			Common.GetWorkDir();
			Logger.SetMinimumLevel(LogLevel.Trace);

			// Tor settings.
			string dataDir = Path.Combine("temp", "tempDataDir");
			string distributionFolder = "tempDistributionDir";
			TorSettings settings = new(dataDir, distributionFolder, terminateOnExit: true, owningProcessId: 7);

			// Dummy process.
			Mock<ProcessAsync> mockProcess = new(MockBehavior.Strict, new ProcessStartInfo());
			mockProcess.Setup(p => p.WaitForExitAsync(It.IsAny<CancellationToken>(), It.IsAny<bool>()))
				.Returns(async (CancellationToken cancellationToken, bool killOnCancel) => await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false));

			// Set up Tor control client.
			Pipe toServer = new();
			Pipe toClient = new();
			await using TorControlClient controlClient = new(pipeReader: toClient.Reader, pipeWriter: toServer.Writer);

			// Set up Tor process manager.
			Mock<TorTcpConnectionFactory> mockTcpConnectionFactory = new(MockBehavior.Strict, new IPEndPoint(IPAddress.Loopback, 80));
			mockTcpConnectionFactory.Setup(c => c.IsTorRunningAsync())
				.ReturnsAsync(false);

			Mock<TorProcessManager> mockTorProcessManager = new(MockBehavior.Loose, settings, mockTcpConnectionFactory.Object) { CallBase = true };
			mockTorProcessManager.Setup(c => c.StartProcess(It.IsAny<string>()))
				.Returns(mockProcess.Object);
			mockTorProcessManager.Setup(c => c.EnsureRunningAsync(It.IsAny<ProcessAsync>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(true);
			mockTorProcessManager.Setup(c => c.InitTorControlAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(controlClient);

			TorProcessManager manager = mockTorProcessManager.Object;

			using CancellationTokenSource cts = new();
			bool result = await manager.StartAsync(cts.Token);
			Assert.True(result);

			cts.Cancel();
		}
	}
}
