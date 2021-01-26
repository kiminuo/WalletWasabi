using NBitcoin;
using System;
using System.Collections.Generic;
using System.Linq;
using WalletWasabi.Blockchain.TransactionBuilding;
using WalletWasabi.Blockchain.TransactionOutputs;

namespace WalletWasabi.Blockchain.Transactions.Services
{
	public class SmartCoinSelectionStrategy
	{
		public static List<SmartCoin> GetAllowedCoinsToSpend(ICoinsView coins, PaymentIntent payments, IEnumerable<OutPoint>? allowedInputs, bool allowUnconfirmed)
		{
			// Get allowed coins to spend.
			ICoinsView availableCoinsView = coins.Available();
			List<SmartCoin> allowedSmartCoinInputs = allowUnconfirmed // Inputs that can be used to build the transaction.
					? availableCoinsView.ToList()
					: availableCoinsView.Confirmed().ToList();
			if (allowedInputs is { }) // If allowedInputs are specified then select the coins from them.
			{
				if (!allowedInputs.Any())
				{
					throw new ArgumentException($"{nameof(allowedInputs)} is not null, but empty.");
				}

				allowedSmartCoinInputs = allowedSmartCoinInputs
					.Where(x => allowedInputs.Any(y => y.Hash == x.TransactionId && y.N == x.Index))
					.ToList();

				// Add those that have the same script, because common ownership is already exposed.
				// But only if the user didn't click the "max" button. In this case he'd send more money than what he'd think.
				if (payments.ChangeStrategy != ChangeStrategy.AllRemainingCustom)
				{
					var allScripts = allowedSmartCoinInputs.Select(x => x.ScriptPubKey).ToHashSet();
					foreach (var coin in availableCoinsView.Where(x => !allowedSmartCoinInputs.Any(y => x.TransactionId == y.TransactionId && x.Index == y.Index)))
					{
						if (!(allowUnconfirmed || coin.Confirmed))
						{
							continue;
						}

						if (allScripts.Contains(coin.ScriptPubKey))
						{
							allowedSmartCoinInputs.Add(coin);
						}
					}
				}
			}

			return allowedSmartCoinInputs;
		}
	}
}