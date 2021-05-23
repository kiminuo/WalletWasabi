using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WalletWasabi.Tor.Control
{
	public class HashedControlPasswordProvider
	{
		/// <summary>
		/// Computes <c>HashedControlPassword</c> command line parameter of Tor program.
		/// <para>Computation is based on Iteration and Salted S2K algorithm in RFC 2440 [https://tools.ietf.org/html/rfc2440#section-3.6.1.3].</para>
		/// </summary>
		/// <param name="salt">Salt bytes that helps mitigate possible dictionary attacks on resulting SHA-1 hashes.</param>
		/// <param name="passphrase">Password that must be used to authenticate via Tor control.</param>
		/// <param name="indicator">Count parameter in the S2K algorithm. Any </param>
		/// <remarks>
		/// See relevant spec section: https://gitweb.torproject.org/torspec.git/tree/control-spec.txt [5.1]
		/// <c>
		/// If the 'HashedControlPassword' option is set, it must contain the salted
		/// hash of a secret password.The salted hash is computed according to the
		/// S2K algorithm in RFC 2440 (OpenPGP), and prefixed with the s2k specifier.
		/// This is then encoded in hexadecimal, prefixed by the indicator sequence
		/// "16:".  Thus, for example, the password 'foo' could encode to:
		///
		///   16:660537E3E1CD49996044A3BF558097A981F539FEA2F9DA662B4626C1C2
		///   ++++++++++++++++**^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
		///         salt                      hashed value
		///			      indicator
		///
		/// You can generate the salt of a password by calling: tor --hash-password PASSWORD
		/// </c>
		/// </remarks>
		/// <returns></returns>
		public static string Compute(byte[] salt, string passphrase, byte indicator)
		{
			int count = (16 + (indicator & 15)) << ((indicator >> 4) + 6);
			string stuffHex = ByteHelpers.ToHex(salt.Concat(Encoding.ASCII.GetBytes(passphrase)).ToArray());
			int repetitions = count / (stuffHex.Length / 2) + 1;

			string repeatedInputBytesHex = new StringBuilder(stuffHex.Length * repetitions)
				.Insert(0, stuffHex, repetitions)
				.ToString()[0..(2 * count)];

			byte[] hash = SHA1.Create().ComputeHash(ByteHelpers.FromHex(repeatedInputBytesHex));

			return $"16:{ByteHelpers.ToHex(salt)}{ByteHelpers.ToHex(indicator)}{ByteHelpers.ToHex(hash)}";
		}
	}
}
