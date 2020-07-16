using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WalletWasabi.Helpers;
using Xunit;

namespace WalletWasabi.Tests.UnitTests.Helpers
{
	public class EnvironmentHelpersTests
	{
		[Fact]
		public void GetUserDataDirOrDefaultTest()
		{
			string[] args = new string[]
			{
				@"--datadir=E:\Portable\WasabiWallet"
			};

			Assert.Equal(@"E:\Portable\WasabiWallet", EnvironmentHelpers.GetUserDataDirOrDefault(args, Path.Combine("WalletWasabi", "Client")));
		}
	}
}
