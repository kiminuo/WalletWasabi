using NBitcoin;
using System;
using WalletWasabi.Helpers;
using WalletWasabi.Hwi.Parsers;

namespace WalletWasabi.Hwi.Models
{
	/// <seealso href="https://github.com/bitcoin-core/HWI/blob/1b1596ac6f4fb1ce47a0d1ca7feb1fc553d08e09/hwilib/cli.py#L118">Available arguments.</seealso>
	public class HwiOption : IEquatable<HwiOption>
	{
		public static readonly HwiOption Debug = new HwiOption(HwiOptions.Debug);
		public static readonly HwiOption Help = new HwiOption(HwiOptions.Help);
		public static readonly HwiOption Interactive = new HwiOption(HwiOptions.Interactive);
		public static readonly HwiOption TestNet = new HwiOption(HwiOptions.TestNet);
		public static readonly HwiOption Version = new HwiOption(HwiOptions.Version);
		public static readonly HwiOption StdIn = new HwiOption(HwiOptions.StdIn);

		private HwiOption(HwiOptions type, string? argument = null)
		{
			Type = type;
			Arguments = argument;
		}

		public HwiOptions Type { get; }
		public string? Arguments { get; }

		public static HwiOption DevicePath(string devicePath)
		{
			devicePath = Guard.NotNullOrEmptyOrWhitespace(nameof(devicePath), devicePath, trim: true);
			return new HwiOption(HwiOptions.DevicePath, devicePath);
		}

		public static HwiOption DeviceType(HardwareWalletModels deviceType) => new HwiOption(HwiOptions.DeviceType, deviceType.ToHwiFriendlyString());

		public static HwiOption Fingerprint(HDFingerprint fingerprint) => new HwiOption(HwiOptions.Fingerprint, fingerprint.ToString());

		public static HwiOption Password(string password) => new HwiOption(HwiOptions.Password, password);

		public override bool Equals(object? obj) => Equals(obj as HwiOption);

		public bool Equals(HwiOption? other) => this == other;

		public override int GetHashCode() => (Type, Arguments).GetHashCode();

		public static bool operator ==(HwiOption? x, HwiOption? y) => (x?.Type, x?.Arguments) == (y?.Type, y?.Arguments);

		public static bool operator !=(HwiOption? x, HwiOption? y) => !(x == y);
	}
}
