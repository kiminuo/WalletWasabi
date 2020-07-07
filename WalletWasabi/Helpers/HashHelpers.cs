#nullable enable

using System;
using System.Security.Cryptography;
using System.Text;

namespace WalletWasabi.Helpers
{
	/// <summary>
	/// Computes SHA-256 hashes.
	/// </summary>
	public static class HashHelpers
	{
		/// <summary>
		/// Computes SHA-256 hash for UTF-8 encoded string.
		/// </summary>
		/// <param name="input">Input string.</param>
		/// <returns>Hexadecimal string with upper-case letters.</returns>
		public static string GenerateSha256Hash(string input)
		{
			using var sha256 = SHA256.Create();
			var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

			return ByteHelpers.ToHex(hash);
		}

		/// <summary>
		/// Computes SHA-256 hash.
		/// </summary>
		/// <param name="input">Input byte array.</param>
		/// <returns>Hash bytes.</returns>
		public static byte[] GenerateSha256Hash(byte[] input)
		{
			using var sha256 = SHA256.Create();
			var hash = sha256.ComputeHash(input);

			return hash;
		}

		/// <summary>
		/// Computes hash code from a byte array.
		///
		/// <para>Implementation based on the StackOverflow answer.</para>
		/// </summary>
		/// <see href="https://stackoverflow.com/a/468084/2061103"/>
		public static int ComputeHashCode(params byte[] data)
		{
			unchecked
			{
				const int P = 16777619;
				int hash = (int)2166136261;

				for (int i = 0; i < data.Length; i++)
				{
					hash = (hash ^ data[i]) * P;
				}

				hash += hash << 13;
				hash ^= hash >> 7;
				hash += hash << 3;
				hash ^= hash >> 17;
				hash += hash << 5;
				return hash;
			}
		}
	}
}
