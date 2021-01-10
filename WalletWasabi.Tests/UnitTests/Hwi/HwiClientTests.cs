using NBitcoin;
using System;
using WalletWasabi.Hwi;
using WalletWasabi.Hwi.Models;
using Xunit;

namespace WalletWasabi.Tests.UnitTests.Hwi
{
	/// <summary>
	/// Tests for <see cref="HwiClient"/>.
	/// </summary>
	public class HwiClientTests
	{
		[Fact]
		public void BuildOptionsTest()
		{
			// Fingerprint is defined, (deviceType, devicePath) pair is not => OK.
			HwiOption[] hwiOptions = HwiClient.BuildOptions(deviceType: null, devicePath: null, fingerprint: new HDFingerprint(0));
			Assert.NotNull(hwiOptions);

			// (deviceType, devicePath) pair is defined, fingerprint is not => OK.
			hwiOptions = HwiClient.BuildOptions(deviceType: HardwareWalletModels.Ledger_Nano_S, devicePath: "some-path", fingerprint: null);
			Assert.NotNull(hwiOptions);

			// (deviceType, devicePath) pair is defined, fingerprint is defined too => not allowed.
			Assert.Throws<NotSupportedException>(() =>
			{
				_ = HwiClient.BuildOptions(deviceType: HardwareWalletModels.Ledger_Nano_S, devicePath: "some-path", fingerprint: new HDFingerprint(0));
			});
		}
	}
}