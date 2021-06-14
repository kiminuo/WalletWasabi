using Microsoft.Extensions.Caching.Memory;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WalletWasabi.Bases;
using WalletWasabi.Logging;
using WalletWasabi.Tor.Control;
using WalletWasabi.Tor.Control.Exceptions;
using WalletWasabi.Tor.Control.Messages;
using WalletWasabi.Tor.Control.Messages.CircuitStatus;
using WalletWasabi.Tor.Control.Messages.Events;
using WalletWasabi.Tor.Control.Messages.Events.StatusEvents;
using WalletWasabi.Tor.Control.Utils;
using WalletWasabi.Tor.Http;
using WalletWasabi.Tor.Socks5.Exceptions;
using WalletWasabi.Tor.Socks5.Models.Fields.OctetFields;
using WalletWasabi.Tor.Socks5.Pool;

namespace WalletWasabi.Tor
{
	/// <summary>
	/// Monitors Tor process.
	/// </summary>
	public class TorMonitor : PeriodicRunner
	{
		public static readonly TimeSpan CheckIfRunningAfterTorMisbehavedFor = TimeSpan.FromSeconds(7);

		public event EventHandler<string>? TorStatusChanged;

		/// <summary>
		/// Creates a new instance of the object.
		/// </summary>
		public TorMonitor(TimeSpan period, Uri fallbackBackendUri, TorHttpClient httpClient, TorProcessManager torProcessManager) : base(period)
		{
			FallbackBackendUri = fallbackBackendUri;
			HttpClient = httpClient;
			TorProcessManager = torProcessManager;
		}

		public static bool RequestFallbackAddressUsage { get; private set; }
		private Uri FallbackBackendUri { get; }
		private TorHttpClient HttpClient { get; }
		private TorProcessManager TorProcessManager { get; }

		public async Task InitializeAsync(CancellationToken cancellationToken)
		{
			TorControlClient client = TorProcessManager.TorControlClient!;

			await client.SubscribeEventsAsync(new string[] { "STATUS_GENERAL", "STATUS_CLIENT", "STATUS_SERVER", "CIRC" }, cancellationToken).ConfigureAwait(false);

			MemoryCacheEntryOptions cacheEntryOptions = new()
			{
				SlidingExpiration = TimeSpan.FromMinutes(10),
				Size = 1
			};

			using MemoryCache circuitCache = new(new MemoryCacheOptions()
			{
				SizeLimit = 500
			});

			string bootstrapInfo = "<Loading>";
			bool circuitEstablished = false;
			string circuitsInfo = "<Loading>";

			await foreach (TorControlReply reply in client.ReadEventsAsync(cancellationToken).ConfigureAwait(false))
			{
				IAsyncEvent asyncEvent;

				try
				{
					asyncEvent = AsyncEventParser.Parse(reply);
				}
				catch (Exception e)
				{
					Logger.LogError($"Exception thrown when parsing event: '{reply}'", e);
					continue;
				}

				if (asyncEvent is BootstrapStatusEvent bootstrapEvent)
				{
					bootstrapInfo = bootstrapEvent.Progress < 100
						? $"{bootstrapEvent.Progress}/100"
						: "DONE";
				}
				else if (asyncEvent is StatusEvent statusEvent)
				{
					if (statusEvent.Action == "CIRCUIT_ESTABLISHED")
					{
						circuitEstablished = true;
					}
				}
				else if (asyncEvent is CircEvent circEvent)
				{
					CircuitInfo info = circEvent.CircuitInfo;

					if (info.CircStatus is CircStatus.CLOSED or CircStatus.FAILED)
					{
						circuitCache.Remove(info.CircuitID);
					}
					else
					{
						_ = circuitCache.Set(info.CircuitID, info, cacheEntryOptions);
					}

					circuitsInfo = $"{circuitCache.Count} in total";
				}

				string torStatusXXX;

				// Allocates too much. Improve efficiency.
				if (!circuitEstablished)
				{
					torStatusXXX = $"[Tor monitor: Bootstrap: {bootstrapInfo}, Circuit established: No]";
				}
				else
				{
					torStatusXXX = $"[Tor monitor: Bootstrap: {bootstrapInfo}, Circuits: {circuitsInfo}]";
				}

				TorStatusChanged?.Invoke(this, torStatusXXX);
			}
		}

		/// <inheritdoc/>
		protected override async Task ActionAsync(CancellationToken token)
		{
			if (TorHttpPool.TorDoesntWorkSince is { }) // If Tor misbehaves.
			{
				TimeSpan torMisbehavedFor = DateTimeOffset.UtcNow - TorHttpPool.TorDoesntWorkSince ?? TimeSpan.Zero;

				if (torMisbehavedFor > CheckIfRunningAfterTorMisbehavedFor)
				{
					if (TorHttpPool.LatestTorException is TorConnectCommandFailedException torEx)
					{
						if (torEx.RepField == RepField.HostUnreachable)
						{
							Logger.LogInfo("Tor does not work properly. Test fallback URI.");
							using HttpRequestMessage request = new(HttpMethod.Get, FallbackBackendUri);
							using HttpResponseMessage _ = await HttpClient.SendAsync(request, token).ConfigureAwait(false);

							// Check if it changed in the meantime...
							if (TorHttpPool.LatestTorException is TorConnectCommandFailedException torEx2 && torEx2.RepField == RepField.HostUnreachable)
							{
								// Fallback here...
								RequestFallbackAddressUsage = true;
							}
						}
					}
					else
					{
						bool isRunning = await HttpClient.IsTorRunningAsync().ConfigureAwait(false);

						if (!isRunning)
						{
							Logger.LogInfo($"Tor did not work properly for {(int)torMisbehavedFor.TotalSeconds} seconds. Maybe it crashed. Attempting to start it...");

							// Try starting Tor, if it does not work it'll be another issue.
							bool started = await TorProcessManager.StartAsync(token).ConfigureAwait(false);

							Logger.LogInfo($"Tor re-starting attempt {(started ? "succeeded." : "FAILED. Will try again later.")}");
						}
						else
						{
							Logger.LogInfo("Tor is running. Waiting for a confirmation that HTTP requests can pass through.");
						}
					}
				}
			}
		}
	}
}
