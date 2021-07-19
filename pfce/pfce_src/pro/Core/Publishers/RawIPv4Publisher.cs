

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using NLog;
using Peach.Core;

namespace Peach.Pro.Core.Publishers
{
	internal static class RawHelpers
	{
		public const int IpHeaderLen = 20;

		public static void SetLength(byte[] buffer, int offset, int count)
		{
			if (count < IpHeaderLen)
				return;

			// Get in host order
			var ip_len = BitConverter.ToUInt16(buffer, offset + 2);
			ip_len += (ushort)(((ushort)(buffer[offset] & 0x0f)) << 2);
			// Set in network order
			buffer[offset + 2] = (byte)(ip_len >> 8);
			buffer[offset + 3] = (byte)(ip_len);
		}
	}

	/// <summary>
	/// Allows for input/output of raw IP packets.
	/// Protocol is the IP protocol number to send/receive.
	/// This publisher does not expect an IP header in the output buffer.
	/// The IP header is always included in the input buffer.
	/// </summary>
	/// <remarks>
	/// Mac raw sockets don't support TCP or UDP receptions.
	/// See the "b. FreeBSD" section at: http://sock-raw.org/papers/sock_raw
	/// </remarks>
	[Publisher("RawV4")]
	[Alias("Raw")]
	[Alias("raw.Raw")]
	[Parameter("Host", typeof(string), "Hostname or IP address of remote host")]
	[Parameter("Interface", typeof(IPAddress), "IP of interface to bind to", "")]
	[Parameter("Protocol", typeof(byte), "IP protocol to use")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data/connection (default 3000)", "3000")]
	[Parameter("MinMTU", typeof(uint), "Minimum allowable MTU property value", DefaultMinMTU)]
	[Parameter("MaxMTU", typeof(uint), "Maximum allowable MTU property value", DefaultMaxMTU)]
	public class RawV4Publisher : DatagramPublisher
	{
		private static readonly NLog.Logger _logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return _logger; } }

		public RawV4Publisher(Dictionary<string, Variant> args)
			: base("RawV4", args)
		{
		}

		protected override void FilterInput(byte[] buffer, int offset, int count)
		{
			if (Platform.GetOS() != Platform.OS.OSX)
				return;

			// On OSX, ip_len is in host order and does not include the ip header
			// http://cseweb.ucsd.edu/~braghava/notes/freebsd-sockets.txt
			RawHelpers.SetLength(buffer, offset, count);
		}

		protected override bool IsAddressFamilySupported(AddressFamily af)
		{
			return af == AddressFamily.InterNetwork;
		}

		protected override SocketType SocketType
		{
			get { return SocketType.Raw; }
		}
	}

	/// <summary>
	/// Allows for input/output of raw IP packets.
	/// Protocol is the IP protocol number to send/receive.
	/// This publisher expects an IP header in the output buffer.
	/// The IP header is always included in the input buffer.
	/// </summary>
	/// <remarks>
	/// Mac raw sockets don't support TCP or UDP receptions.
	/// See the "b. FreeBSD" section at: http://sock-raw.org/papers/sock_raw
	/// </remarks>
	[Publisher("RawIPv4")]
	[Alias("RawIp")]
	[Alias("raw.RawIp")]
	[Parameter("Host", typeof(string), "Hostname or IP address of remote host")]
	[Parameter("Interface", typeof(IPAddress), "IP of interface to bind to", "")]
	[Parameter("Protocol", typeof(byte), "IP protocol to use")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data/connection (default 3000)", "3000")]
	[Parameter("MinMTU", typeof(uint), "Minimum allowable MTU property value", DefaultMinMTU)]
	[Parameter("MaxMTU", typeof(uint), "Maximum allowable MTU property value", DefaultMaxMTU)]
	public class RawIPv4Publisher : DatagramPublisher
	{
		private static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		public RawIPv4Publisher(Dictionary<string, Variant> args)
			: base("RawIPv4", args)
		{
		}

		protected override bool IpHeaderInclude { get { return true; } }

		protected override bool IsAddressFamilySupported(AddressFamily af)
		{
			return af == AddressFamily.InterNetwork;
		}

		protected override SocketType SocketType
		{
			get { return SocketType.Raw; }
		}

		protected override void FilterInput(byte[] buffer, int offset, int count)
		{
			if (Platform.GetOS() != Platform.OS.OSX)
				return;

			// On OSX, ip_len is in host order and does not include the ip header
			// http://cseweb.ucsd.edu/~braghava/notes/freebsd-sockets.txt
			RawHelpers.SetLength(buffer, offset, count);
		}

		protected override void FilterOutput(byte[] buffer, int offset, int count)
		{
			if (Platform.GetOS() != Platform.OS.OSX)
				return;

			if (count < RawHelpers.IpHeaderLen)
				return;

			// On OSX, ip_len and ip_off need to be in host order
			// http://cseweb.ucsd.edu/~braghava/notes/freebsd-sockets.txt

			// Swap ip_len
			var tmp = buffer[offset + 2];
			buffer[offset + 2] = buffer[offset + 3];
			buffer[offset + 3] = tmp;

			// Swap ip_off
			tmp = buffer[offset + 6];
			buffer[offset + 6] = buffer[offset + 7];
			buffer[offset + 7] = tmp;
		}
	}
}
