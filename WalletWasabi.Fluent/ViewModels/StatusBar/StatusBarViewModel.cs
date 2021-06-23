using System;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Input;
using NBitcoin.Protocol;
using ReactiveUI;
using WalletWasabi.BitcoinCore.Monitoring;
using WalletWasabi.BitcoinP2p;
using WalletWasabi.Fluent.Models;
using WalletWasabi.Helpers;
using WalletWasabi.Models;
using WalletWasabi.Services;
using WalletWasabi.Tor;

namespace WalletWasabi.Fluent.ViewModels.StatusBar
{
	public partial class StatusBarViewModel : IDisposable
	{
		[AutoNotify] private StatusBarState _currentState;
		[AutoNotify] private TorStatus _torStatus;
		[AutoNotify] private TorStatusReport _torStatusReport;
		[AutoNotify] private BackendStatus _backendStatus;
		[AutoNotify] private RpcStatus? _bitcoinCoreStatus;
		[AutoNotify] private int _peers;
		[AutoNotify] private bool _updateAvailable;
		[AutoNotify] private bool _criticalUpdateAvailable;
		[AutoNotify] private string? _versionText;

		public StatusBarViewModel()
		{
			UseTor = Services.Config.UseTor; // Do not make it dynamic, because if you change this config settings only next time will it activate.
			TorStatus = UseTor ? Services.Synchronizer.TorStatus : TorStatus.TurnedOff;
			UseBitcoinCore = Services.Config.StartLocalBitcoinCoreOnStartup;

			UpdateCommand = ReactiveCommand.CreateFromTask(async () => await IoHelpers.OpenBrowserAsync("https://wasabiwallet.io/#download"));
			AskMeLaterCommand = ReactiveCommand.Create(() => UpdateAvailable = false);

			this.WhenAnyValue(
					x => x.TorStatus,
					x => x.BackendStatus,
					x => x.Peers,
					x => x.BitcoinCoreStatus,
					x => x.UpdateAvailable,
					x => x.CriticalUpdateAvailable)
				.Throttle(TimeSpan.FromMilliseconds(100))
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(_ => SetStatusBarState());
		}

		public ICommand UpdateCommand { get; }

		public ICommand AskMeLaterCommand { get; }

		private CompositeDisposable Disposables { get; } = new ();

		private bool UseTor { get; }

		public bool UseBitcoinCore { get; }

		public string BitcoinCoreName => Constants.BuiltinBitcoinNodeName;

		private void SetStatusBarState()
		{
			if (CriticalUpdateAvailable)
			{
				CurrentState = StatusBarState.CriticalUpdateAvailable;
				return;
			}

			if (UpdateAvailable)
			{
				CurrentState = StatusBarState.UpdateAvailable;
				return;
			}

			// The source of the p2p connection comes from if we use Core for it or the network.
			var p2pConnected = UseBitcoinCore ? BitcoinCoreStatus?.Success is true : Peers >= 1;
			var torConnected = !UseTor || TorStatus == TorStatus.Running;

			if (torConnected && BackendStatus == BackendStatus.Connected && p2pConnected)
			{
				CurrentState = StatusBarState.Ready;
				return;
			}

			CurrentState = StatusBarState.Loading;
		}

		public void Initialize()
		{
			var nodes = Services.HostedServices.Get<P2pNetwork>().Nodes.ConnectedNodes;
			var synchronizer = Services.Synchronizer;
			var rpcMonitor = Services.HostedServices.GetOrDefault<RpcMonitor>();
			var updateChecker = Services.HostedServices.Get<UpdateChecker>();
			var torMonitor = Services.HostedServices.Get<TorMonitor>();

			BitcoinCoreStatus = rpcMonitor?.RpcStatus ?? RpcStatus.Unresponsive;

			synchronizer.WhenAnyValue(x => x.TorStatus)
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(status => TorStatus = UseTor ? status : TorStatus.TurnedOff)
				.DisposeWith(Disposables);

			Observable.FromEventPattern(torMonitor, nameof(torMonitor.OnTorStatusReport))
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(status => TorStatusReport = (TorStatusReport)status.EventArgs)
				.DisposeWith(Disposables);

			synchronizer.WhenAnyValue(x => x.BackendStatus)
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(status => BackendStatus = status)
				.DisposeWith(Disposables);

			Peers = TorStatus == TorStatus.NotRunning ? 0 : nodes.Count;
			Observable
				.Merge(Observable.FromEventPattern(nodes, nameof(nodes.Added)).Select(_ => Unit.Default)
				.Merge(Observable.FromEventPattern<NodeEventArgs>(nodes, nameof(nodes.Removed)).Select(_ => Unit.Default)
				.Merge(Services.Synchronizer.WhenAnyValue(x => x.TorStatus).Select(_ => Unit.Default))))
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(_ => Peers = synchronizer.TorStatus == TorStatus.NotRunning ? 0 : nodes.Count) // Set peers to 0 if Tor is not running, because we get Tor status from backend answer so it seems to the user that peers are connected over clearnet, while they are not.
				.DisposeWith(Disposables);

			if (rpcMonitor is { }) // TODO: Is it possible?
			{
				Observable.FromEventPattern<RpcStatus>(rpcMonitor, nameof(rpcMonitor.RpcStatusChanged))
					.ObserveOn(RxApp.MainThreadScheduler)
					.Subscribe(e => BitcoinCoreStatus = e.EventArgs)
					.DisposeWith(Disposables);
			}

			Observable.FromEventPattern<UpdateStatus>(updateChecker, nameof(updateChecker.UpdateStatusChanged))
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(async e =>
				{
					var updateStatus = e.EventArgs;
					var cancelToken = new CancellationTokenSource(TimeSpan.FromMinutes(1));
					var (clientVersion, _, _) = await updateChecker.WasabiClient.GetVersionsAsync(cancelToken.Token);

					UpdateAvailable = !updateStatus.ClientUpToDate;
					CriticalUpdateAvailable = !updateStatus.BackendCompatible;

					if (CriticalUpdateAvailable)
					{
						VersionText = $"Critical update required";
					}
					else if (UpdateAvailable)
					{
						VersionText = $"Version {clientVersion} is now available";
					}
				})
				.DisposeWith(Disposables);
		}

		public void Dispose()
		{
			Disposables.Dispose();
		}
	}
}
