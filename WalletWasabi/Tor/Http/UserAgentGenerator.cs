using System;
using WalletWasabi.Crypto.Randomness;

namespace WalletWasabi.Tor.Http
{
	/// <summary>Generator of HTTP <c>User-Agent</c> headers.</summary>
	public class UserAgentGenerator
	{
		/// <summary>Release date of Chrome browser v91.</summary>
		/// <seealso href="https://en.wikipedia.org/wiki/Google_Chrome_version_history"/>
		private static readonly DateTime Chrome91ReleaseDate = new(2021, 5, 25, 0, 0, 0, DateTimeKind.Utc);

		/// <summary>Release date of Firefox browser v89.</summary>
		/// <seealso href="https://en.wikipedia.org/wiki/Firefox_version_history#Firefox_91_through_100"/>
		private static readonly DateTime Firefox89ReleaseDate = new(2021, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		/// <summary>Release schedule for Chrome and Firefox (every 6 weeks).</summary>
		private const int ReleaseCadenceInDays = 6 * 7;

		private WasabiRandom Random { get; }

		public UserAgentGenerator(WasabiRandom? random = null)
		{
			Random = random ?? new InsecureRandom();
		}

		public virtual string GetRandomUserAgent()
		{
			return GetRandomUserAgent(DateTime.UtcNow);
		}

		internal string GetRandomUserAgent(DateTime currentDateTime)
		{
			int value = Random.GetInt(0, 100);
			int majorVersion;

			// Chrome.
			if (value <= 65)
			{
				majorVersion = ComputeExpectedVersion(Chrome91ReleaseDate, version: 91, currentDateTime);

				// * "AppleWebKit/537.36" and "Safari/537.36" parts seem to be fixed for a very long time
				//   (see https://groups.google.com/a/chromium.org/g/blink-dev/c/HzpCutO4248/m/FLzLThCDzSkJ)
				// * After the user-agent freeze, we will have minor, bugfix and build versions equal to zero (i.e. "<majorVersion>.0.0.0").
				return $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{majorVersion}.0.0.0 Safari/537.36";
			}

			value -= 65;

			// Safari.
			if (value <= 18)
			{
				// * User agent values for macOS 11 represent a source of many bugs (https://www.otsukare.info/2021/02/15/capping-macos-user-agent)
				// * Since Safari 11.1, the freezed value "AppleWebKit/605.1.15" is used in Safari
				//   (see https://twitter.com/__jakub_g/status/1398324307814752256).
				// * "Intel" is returned even for ARM64 (https://bugzilla.mozilla.org/show_bug.cgi?id=1655285)
				return $"Mozilla/5.0 (Macintosh; Intel Mac OS X 11_4) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.1.1 Safari/605.1.15";
			}

			// Firefox.
			int increment = (int)((currentDateTime.Subtract(Firefox89ReleaseDate).TotalDays - 1) / ReleaseCadenceInDays);
			majorVersion = 89 + increment;

			return $"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:{majorVersion}.0) Gecko/20100101 Firefox/{majorVersion}.0";
		}

		private static int ComputeExpectedVersion(DateTime releaseDate, int version, DateTime currentDateTime)
		{
			// -1 to account for that it's not typical to upgrade a browser on a release date.
			// Also, Browser updates are not offered immediately but gradually to general public.
			int increment = (int)((currentDateTime.Subtract(releaseDate).TotalDays - 1) / ReleaseCadenceInDays);
			return version + increment;
		}
	}
}
