using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Encoding = Peach.Core.Encoding;
using Monitor = Peach.Core.Agent.Monitor2;
using System.ComponentModel;

namespace MyExtensions
{
	[Monitor("PingExample")]
	[Description("Uses ICMP to verify whether a device is functional")]
	[Parameter("Host", typeof(string), "Host to ping")]
	[Parameter("Timeout", typeof(int), "Ping timeout in milliseconds", "1000")]
	[Parameter("RetryCount", typeof(int), "Number of times to retry before issuing a fault", "0")]
	[Parameter("Data", typeof(string), "Data to send", "")]
	[Parameter("FaultOnSuccess", typeof(bool), "Fault if ping is successful", "false")]
	public class PingMonitor : Monitor
	{
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
		private static readonly bool HasPermissions = CheckPermissions();

		public string Host { get; set; }
		public int Timeout { get; set; }
		public int RetryCount { get; set; }
		public string Data { get; set; }
		public bool FaultOnSuccess { get; set; }

		private MonitorData _data;


		private static bool CheckPermissions()
		{
			if (Platform.GetOS() == Platform.OS.Windows)
				return true;

			// Mono has two modes of operation for the Ping object, privileged and unprivileged.
			// In privileged mode, mono uses a raw icmp socket and things work well.
			// In unprivileged mode, mono tries to capture stdout from /bin/ping and things don't work well.
			// Therefore, ensure only privileged mode is used.

			try
			{
				using (new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp))
				{
					return true;
				}
			}
			catch
			{
				return false;
			}
		}

		public PingMonitor(string name)
			: base(name)
		{
		}

		public override void StartMonitor(Dictionary<string, string> args)
		{
			base.StartMonitor(args);

			if (!HasPermissions)
				throw new PeachException("Unable to open ICMP socket.  Ensure user has appropriate permissions.");

			if (Platform.GetOS() != Platform.OS.Windows)
			{
				// Mono only receives 100 bytes in its response processing.
				// This means the only payload we can expect to receive is 72 bytes
				// 100 bytes total - 20 byte IP - 8 byte ICMP
				const int maxLen = 100 - 20 - 8;
				var len = Encoding.ASCII.GetByteCount(Data ?? "");
				if (len > maxLen)
					throw new PeachException("Error, the value of parameter 'Data' is longer than the maximum length of " + maxLen + ".");
			}
		}

		public override bool DetectedFault()
		{
			_data = new MonitorData
			{
				Data = new Dictionary<string, Stream>()
			};

			try
			{
				using (var ping = new Ping())
				{
					PingReply reply;
					var count = 0;
					do
					{
						count++;
						Logger.Trace("DetectedFault(): Checking for fault, attempt #{0} to ping {1}", count, Host);
						reply = string.IsNullOrEmpty(Data)
							? ping.Send(Host, Timeout)
							: ping.Send(Host, Timeout, Encoding.UTF8.GetBytes(Data));
						Debug.Assert(reply != null);
					}
					while (RetryCount >= count && reply.Status != IPStatus.Success);

					Logger.Debug("DetectedFault(): {0} {1} {2}ms",
						Host,
						reply.Status == IPStatus.Success ? "replied" : "timed out",
						Timeout);

					if (reply.Status != IPStatus.Success ^ FaultOnSuccess)
					{
						_data.Fault = new MonitorData.Info
						{
							MajorHash = Hash(Host),
							MinorHash = Hash(reply.Status.ToString()),
						};
					}

					_data.Title = MakeDescription(reply);
				}
			}
			catch (Exception ex)
			{
				if (ex is PingException)
				{
					var se = ex.InnerException as SocketException;

					// An MX record is returned but no A record—indicating the host
					// itself exists, but is not directly reachable.
					if (se != null && se.SocketErrorCode == SocketError.NoData)
						ex = new SocketException((int)SocketError.HostNotFound);
					else
						ex = ex.InnerException;
				}

				_data.Title = ex.Message;

				if (!FaultOnSuccess)
				{
					_data.Fault = new MonitorData.Info
					{
						MajorHash = Hash(Host),
						MinorHash = Hash(ex.Message),
					};
				}
			}

			return _data.Fault != null;
		}

		public override MonitorData GetMonitorData()
		{
			return _data;
		}

		static string MakeDescription(PingReply reply)
		{
			switch (reply.Status)
			{
				case IPStatus.Success:
					return "Reply from {0}: bytes={1} time={2}ms TTL={3}".Fmt(
						reply.Address, reply.Buffer.Length, reply.RoundtripTime,reply.Options.Ttl);
				case IPStatus.Unknown:
					return "The ICMP echo request failed for an unknown reason.";
				case IPStatus.DestinationNetworkUnreachable:
					return "The ICMP echo request failed because the network that contains the destination computer is not reachable.";
				case IPStatus.DestinationHostUnreachable:
					return "The ICMP echo request failed because the destination computer is not reachable.";
				case IPStatus.DestinationProhibited:
					return "The ICMP echo request failed because contact with the destination computer is administratively prohibited.";
				case IPStatus.DestinationPortUnreachable:
					return "The ICMP echo request failed because the port on the destination computer is not available.";
				case IPStatus.NoResources:
					return "The ICMP echo request failed because of insufficient network resources.";
				case IPStatus.BadOption:
					return "The ICMP echo request failed because it contains an invalid option.";
				case IPStatus.HardwareError:
					return "The ICMP echo request failed because of a hardware error.";
				case IPStatus.PacketTooBig:
					return "The ICMP echo request failed because the packet containing the request is larger than the maximum transmission unit (MTU) of a node (router or gateway) located between the source and destination. The MTU defines the maximum size of a transmittable packet.";
				case IPStatus.TimedOut:
					return "The ICMP echo reply was not received within the allotted time.";
				case IPStatus.BadRoute:
					return "The ICMP echo request failed because there is no valid route between the source and destination computers.";
				case IPStatus.TtlExpired:
					return "The ICMP echo request failed because its Time to Live (TTL) value reached zero, causing the forwarding node (router or gateway) to discard the packet.";
				case IPStatus.TtlReassemblyTimeExceeded:
					return "The ICMP echo request failed because the packet was divided into fragments for transmission and all of the fragments were not received within the time allotted for reassembly.";
				case IPStatus.ParameterProblem:
					return "The ICMP echo request failed because a node (router or gateway) encountered problems while processing the packet header.";
				case IPStatus.SourceQuench:
					return "The ICMP echo request failed because the packet was discarded. This occurs when the source computer's output queue has insufficient storage space, or when packets arrive at the destination too quickly to be processed.";
				case IPStatus.BadDestination:
					return "The ICMP echo request failed because the destination IP address cannot receive ICMP echo requests or should never appear in the destination address field of any IP datagram.";
				case IPStatus.DestinationUnreachable:
					return "The ICMP echo request failed because the destination computer that is specified in an ICMP echo message is not reachable; the exact cause of problem is unknown.";
				case IPStatus.TimeExceeded:
					return "The ICMP echo request failed because its Time to Live (TTL) value reached zero, causing the forwarding node (router or gateway) to discard the packet.";
				case IPStatus.BadHeader:
					return "The ICMP echo request failed because the header is invalid.";
				case IPStatus.UnrecognizedNextHeader:
					return "The ICMP echo request failed because the Next Header field does not contain a recognized value. The Next Header field indicates the extension header type (if present) or the protocol above the IP layer, for example, TCP or UDP.";
				case IPStatus.IcmpError:
					return "The ICMP echo request failed because of an ICMP protocol error.";
				case IPStatus.DestinationScopeMismatch:
					return "The ICMP echo request failed because the source address and destination address that are specified in an ICMP echo message are not in the same scope. This is typically caused by a router forwarding a packet using an interface that is outside the scope of the source address. Address scopes (link-local, site-local, and global scope) determine where on the network an address is valid.";
				default:
					throw new ArgumentException();
			}
		}
	}
}