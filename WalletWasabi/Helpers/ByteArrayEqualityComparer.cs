#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace WalletWasabi.Helpers
{
	public class ByteArrayEqualityComparer : IEqualityComparer<byte[]>
	{
		public bool Equals(byte[]? x,  byte[]? y) => ByteHelpers.CompareFastUnsafe(x, y);

		public int GetHashCode(byte[] obj) => HashHelpers.ComputeHashCode(obj);
	}
}
