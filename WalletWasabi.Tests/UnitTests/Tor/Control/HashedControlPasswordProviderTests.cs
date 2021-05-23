using System;
using WalletWasabi.Tor.Control;
using Xunit;

namespace WalletWasabi.Tests.UnitTests.Tor.Control
{
	/// <summary>
	/// Tests for <see cref="HashedControlPasswordProvider"/>.
	/// </summary>
	public class HashedControlPasswordProviderTests
	{
		[Fact]
		public void HashingTest()
		{
			string actual = HashedControlPasswordProvider.Compute(salt: ByteHelpers.FromHex("660537E3E1CD4999"), passphrase: "foo", indicator: 0x60);
			Assert.Equal("16:660537E3E1CD49996044A3BF558097A981F539FEA2F9DA662B4626C1C2", actual);
		}
	}
}
