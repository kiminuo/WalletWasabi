using Avalonia.Data.Converters;
using WalletWasabi.BitcoinCore.Monitoring;
using WalletWasabi.Models;

namespace WalletWasabi.Fluent.Converters
{
	public static class StatusConverters
	{
		//public static readonly IValueConverter TorStatusToString =
		//	new FuncValueConverter<TorStatus, string>(x => x switch
		//	{
		//		TorStatus.Running => "is running",
		//		TorStatus.NotRunning => "is not running",
		//		TorStatus.TurnedOff => "is turned off",
		//		{ } => x.ToString()
		//	});

		public static readonly IValueConverter TorStatusReportToString =
			new FuncValueConverter<TorStatusReport, string>(x =>
			{
				if (x is null)
				{
					return "is not running";
				}

				if (x.CircuitEstablished)
				{
					return " is running";
				}
				else
				{
					if (x.BootstrapProgress < 100)
					{
						return $"bootstraps ({x.BootstrapProgress}%)";
					} else
					{
						return " is creating a circuit";
					}					
				}
			});

		public static readonly IValueConverter BackendStatusToString =
			new FuncValueConverter<BackendStatus, string>(x => x switch
			{
				BackendStatus.Connected => "is connected",
				BackendStatus.NotConnected => "is not connected",
				{ } => x.ToString()
			});

		public static readonly IValueConverter RpcStatusStringConverter =
			new FuncValueConverter<RpcStatus?, string>(status => status is null ? RpcStatus.Unresponsive.ToString() : status.ToString());
	}
}
