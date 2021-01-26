using NBitcoin;
using System.Collections.Generic;
using WalletWasabi.Blockchain.Analysis.Clustering;
using WalletWasabi.Blockchain.Keys;
using WalletWasabi.Blockchain.TransactionBuilding;
using WalletWasabi.Blockchain.TransactionOutputs;
using WalletWasabi.Blockchain.Transactions;
using WalletWasabi.Blockchain.Transactions.Services;
using WalletWasabi.Models;
using Xunit;

namespace WalletWasabi.Tests.UnitTests.Transactions
{
	public class SmartCoinSelectionStrategyTests
	{
		[Fact]
		public void GetAllowedSmartCoinInputsTest()
		{
			SmartCoin c1 = MakeSmartCoin(label: "c1", amount: Money.Coins(1m), confirmed: true);
			SmartCoin c2 = MakeSmartCoin(label: "c2", amount: Money.Coins(2m), confirmed: true);

			List<SmartCoin> inputCoins = new List<SmartCoin>() { c1, c2 };
			CoinsView coinsView = new(inputCoins);

			using Key destination = new();
			PaymentIntent paymentIntent = new(destination, Money.Coins(3.0m), label: "spend-it-all");
			List<OutPoint> allowedInputs = new();

			List<SmartCoin> result = SmartCoinSelectionStrategy.GetAllowedCoinsToSpend(coinsView, paymentIntent, allowedInputs: null, allowUnconfirmed: false);

			Assert.Equal(inputCoins.Count, result.Count);

			Assert.Equal(inputCoins[0], result[0]);
			Assert.Equal(inputCoins[1], result[1]);
		}

		private static SmartCoin MakeSmartCoin(string label, Money amount, bool confirmed = true, int anonymitySet = 1)
		{
			KeyManager keyManager = KeyManager.CreateNew(out _, password: "123456");
			Transaction tx = Network.TestNet.Consensus.ConsensusFactory.CreateTransaction();

			HdPubKey randomKey = keyManager.GenerateNewKey(new SmartLabel(label), KeyState.Clean, isInternal: false);
			tx.Outputs.Add(new TxOut(amount, randomKey.P2wpkhScript));
			Height height = confirmed ? new Height(5) : Height.Mempool;
			SmartTransaction randomStx = new(tx, height);
			return new SmartCoin(randomStx, 0, randomKey);
		}
	}
}