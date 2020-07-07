#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace WalletWasabi.Tests.Helpers
{
	/// <summary>
	/// Tests for <see cref="ByteHelpers"/> class.
	/// </summary>
	public class ByteHelpersTest
	{
		/// <summary>
		/// Basic sanity test.
		/// </summary>
		[Fact]
		public void ToHexTest()
		{
			// Assert that null byte is converted correctly.
			var nullByte = new byte[] { 0x00 };
			Assert.Equal("00", ByteHelpers.ToHex(nullByte));

			// Assert that result contains digits and upper-case letters.
			var helloBytes = Encoding.UTF8.GetBytes("Hello!");
			Assert.Equal("48656C6C6F21", ByteHelpers.ToHex(helloBytes));
		}
	}
}
