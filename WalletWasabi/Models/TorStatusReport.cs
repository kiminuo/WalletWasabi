namespace WalletWasabi.Models
{
	public record TorStatusReport
	{
		public int BootstrapProgress { get; }
		public bool CircuitEstablished { get; }

		public TorStatusReport(int bootstrapProgress, bool circuitEstablished)
		{
			BootstrapProgress = bootstrapProgress;
			CircuitEstablished = circuitEstablished;
		}
	}
}
