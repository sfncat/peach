


using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using NLog;
using Peach.Core;
using Logger = NLog.Logger;

namespace Peach.Pro.Core.Publishers
{
	[Publisher("Udp")]
	[Parameter("Host", typeof(string), "Hostname or IP address of remote host")]
	[Parameter("Port", typeof(ushort), "Destination port number", "0")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data/connection (default 3000)", "3000")]
	[Parameter("Interface", typeof(IPAddress), "IP of interface to bind to", "")]
	[Parameter("SrcPort", typeof(ushort), "Source port number", "0")]
	[Parameter("MinMTU", typeof(uint), "Minimum allowable MTU property value", DefaultMinMTU)]
	[Parameter("MaxMTU", typeof(uint), "Maximum allowable MTU property value", DefaultMaxMTU)]
	public class UdpPublisher : DatagramPublisher
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();
		protected override Logger Logger { get { return logger; } }

		public UdpPublisher(Dictionary<string, Variant> args)
			: base("Udp", args)
		{
			Protocol = (byte)ProtocolType.Udp;
		}

		protected override SocketType SocketType
		{
			get { return SocketType.Dgram; }
		}

		protected override bool IsAddressFamilySupported(AddressFamily af)
		{
			return (af == AddressFamily.InterNetwork) || (af == AddressFamily.InterNetworkV6);
		}

		protected override void FilterOutput(byte[] buffer, int offset, int count)
		{
			if (_remoteEp.Port == 0)
				throw new PeachException("Error sending a Udp packet to {0}, the port was not specified.".Fmt(_remoteEp.Address));
		}

		protected override Variant OnGetProperty(string property)
		{
			switch(property)
			{
				case "Port":
					return new Variant(Port);
				case "SrcPort":
					return new Variant(SrcPort);
			}

			return base.OnGetProperty(property);
		}

		protected override void OnSetProperty(string property, Variant value)
		{
			ushort newPort;

			switch (property)
			{
				case "Port":
					newPort = UShortFromVariant(value);
					Logger.Debug("Changing Port from {0} to {1}.\n", Port, newPort);

					Port = newPort;
					OnStop();
					OnStart();
					return;
				case "SrcPort":
					newPort = UShortFromVariant(value);
					Logger.Debug("Changing SrcPort from {0} to {1}.\n", SrcPort, newPort);

					SrcPort = newPort;
					OnStop();
					OnStart();
					return;
			}

			base.OnSetProperty(property, value);
		}
	}
}
