
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using NLog;
using Peach.Core;

namespace Peach.Pro.Core.Publishers
{
	/// <summary>
	/// Allows for input/output of raw IP packets.
	/// Protocol is the IP protocol number to send/receive.
	/// This publisher does not expect an IP header in the output buffer.
	/// The IP header is always included in the input buffer.
	/// </summary>
	[Publisher("RawV6")]
	[Alias("Raw6")]
	[Alias("raw.Raw6")]
	[Parameter("Host", typeof(string), "Hostname or IP address of remote host")]
	[Parameter("Interface", typeof(IPAddress), "IP of interface to bind to", "")]
	[Parameter("Protocol", typeof(byte), "IP protocol to use")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data/connection (default 3000)", "3000")]
	[Parameter("MinMTU", typeof(uint), "Minimum allowable MTU property value", DefaultMinMTU)]
	[Parameter("MaxMTU", typeof(uint), "Maximum allowable MTU property value", DefaultMaxMTU)]
	public class RawV6Publisher : DatagramPublisher
	{
		private static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		public RawV6Publisher(Dictionary<string, Variant> args)
			: base("RawV6", args)
		{
		}

		protected override bool IsAddressFamilySupported(AddressFamily af)
		{
			return af == AddressFamily.InterNetworkV6;
		}

		protected override SocketType SocketType
		{
			get { return SocketType.Raw; }
		}
	}
}
