using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Encoding = Peach.Core.Encoding;
using Monitor = Peach.Core.Agent.Monitor2;

namespace Peach.Pro.Core.Agent.Monitors
{
	[Monitor("Ping")]
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
		private IPAddress _address;

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

				IPAddress addr;
				if (IPAddress.TryParse(Host, out addr))
				{
					if (addr.AddressFamily == AddressFamily.InterNetworkV6)
						throw new PeachException("Error, the Ping monitor only supports IPv6 addresses on Windows.");
				}
			}

			if (!IPAddress.TryParse(Host, out _address))
			{
				_address = Dns.GetHostAddresses(Host)[0];
			}

			Logger.Trace("Resolved '{0}' as '{1}'", Host, _address);
		}

		protected MonoPing.PingReply Ping(IPAddress address, int timeout, string data)
		{
			if (Platform.IsRunningOnMono())
			{
				using (var ping = new MonoPing())
				{
					return string.IsNullOrEmpty(Data)
						? ping.Send(address, timeout)
						: ping.Send(address, timeout, Encoding.UTF8.GetBytes(data));
				}
			}

			using (var ping = new Ping())
			{
				var reply = string.IsNullOrEmpty(Data)
					? ping.Send(address, timeout)
					: ping.Send(address, timeout, Encoding.UTF8.GetBytes(data));

				MonoPing.PingOptions options = null;
				if (reply.Options != null)
					options = new MonoPing.PingOptions(reply.Options.Ttl, reply.Options.DontFragment);

				return new MonoPing.PingReply(
					reply.Address, 
					reply.Buffer, 
					options, 
					reply.RoundtripTime, 
					reply.Status);
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
				MonoPing.PingReply reply;
				var count = 0;
				do
				{
					count++;
					Logger.Trace("DetectedFault(): Checking for fault, attempt #{0} to ping '{1}' at '{2}'", count, Host, _address);

					reply = Ping(_address, Timeout, Data);
				}
				while (RetryCount >= count && reply.Status != IPStatus.Success);

				Logger.Error("DetectedFault(): {0} {1} {2}ms",
					Host,
					reply.Status == IPStatus.Success ? "replied" : "timed out",
					reply.Status == IPStatus.Success ? reply.RoundtripTime : Timeout);

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

		static string MakeDescription(MonoPing.PingReply reply)
		{
			switch (reply.Status)
			{
				case IPStatus.Success:
					if (reply.Options == null) // Happens with ipv6 pings
						return "Reply from {0}: bytes={1} time={2}ms".Fmt(
							reply.Address, reply.Buffer.Length, reply.RoundtripTime);
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

	/// <summary>
	/// Port of the Mono 5 Ping class to work around several bugs in the Mono 4.8.1 Ping.
	/// This would also allow us to add IPv6 support if wanted.
	/// </summary>
	public class MonoPing : Component, IDisposable
	{
		[StructLayout(LayoutKind.Sequential)]
		struct cap_user_header_t
		{
			public UInt32 version;
			public Int32 pid;
		};

		[StructLayout(LayoutKind.Sequential)]
		struct cap_user_data_t
		{
			public UInt32 effective;
			public UInt32 permitted;
			public UInt32 inheritable;
		}

		const int DefaultCount = 1;
		const int default_timeout = 4000; // 4 sec.
		ushort identifier;

		// Request 32-bit capabilities by using version 1
		const UInt32 _LINUX_CAPABILITY_VERSION_1 = 0x19980330;

		static readonly byte[] default_buffer = new byte[0];


		public MonoPing()
		{
			// Generate a new random 16 bit identifier for every ping
			RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
			byte[] randomIdentifier = new byte[2];
			rng.GetBytes(randomIdentifier);
			identifier = (ushort)(randomIdentifier[0] + (randomIdentifier[1] << 8));
		}

		void IDisposable.Dispose()
		{
		}

		// Sync

		public PingReply Send(IPAddress address)
		{
			return Send(address, default_timeout);
		}

		public PingReply Send(IPAddress address, int timeout)
		{
			return Send(address, timeout, default_buffer);
		}

		public PingReply Send(IPAddress address, int timeout, byte[] buffer)
		{
			return Send(address, timeout, buffer, new PingOptions());
		}

		public PingReply Send(string hostNameOrAddress)
		{
			return Send(hostNameOrAddress, default_timeout);
		}

		public PingReply Send(string hostNameOrAddress, int timeout)
		{
			return Send(hostNameOrAddress, timeout, default_buffer);
		}

		public PingReply Send(string hostNameOrAddress, int timeout, byte[] buffer)
		{
			return Send(hostNameOrAddress, timeout, buffer, new PingOptions());
		}

		public PingReply Send(string hostNameOrAddress, int timeout, byte[] buffer, PingOptions options)
		{
			IPAddress[] addresses = Dns.GetHostAddresses(hostNameOrAddress);
			return Send(addresses[0], timeout, buffer, options);
		}

		public PingReply Send(IPAddress address, int timeout, byte[] buffer, PingOptions options)
		{
			if (address == null)
				throw new ArgumentNullException("address");
			if (timeout < 0)
				throw new ArgumentOutOfRangeException("timeout", "timeout must be non-negative integer");
			if (buffer == null)
				throw new ArgumentNullException("buffer");
			if (buffer.Length > 65500)
				throw new ArgumentException("buffer");
			// options can be null.

			return SendPrivileged(address, timeout, buffer, options);
		}

		private PingReply SendPrivileged(IPAddress address, int timeout, byte[] buffer, PingOptions options)
		{
			IPEndPoint target = new IPEndPoint(address, 0);

			// FIXME: support IPv6
			using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp))
			{
				if (options != null)
				{
					s.DontFragment = options.DontFragment;
					s.Ttl = (short)options.Ttl;
				}
				s.SendTimeout = timeout;
				s.ReceiveTimeout = timeout;
				// not sure why Identifier = 0 is unacceptable ...
				IcmpMessage send = new IcmpMessage(8, 0, identifier, 0, buffer);
				byte[] bytes = send.GetBytes();
				s.SendBufferSize = bytes.Length;
				s.SendTo(bytes, bytes.Length, SocketFlags.None, target);

				var sw = Stopwatch.StartNew();

				// receive
				bytes = new byte[100];
				do
				{
					EndPoint endpoint = target;
					SocketError error = 0;
					var rc = 0;

					try
					{
						rc = s.ReceiveFrom(bytes, 0, 100, SocketFlags.None, ref endpoint);
					}
					catch (SocketException e)
					{
						error = (SocketError) e.ErrorCode;

						if (error == SocketError.WouldBlock)
							error = SocketError.TimedOut;
					}


					if (error != SocketError.Success)
					{
						if (error == SocketError.TimedOut)
						{
							return new PingReply(null, new byte[0], options, 0, IPStatus.TimedOut);
						}
						throw new NotSupportedException(String.Format("Unexpected socket error during ping request: {0}", error));
					}

					long rtt = (long)sw.ElapsedMilliseconds;
					int headerLength = (bytes[0] & 0xF) << 2;
					int bodyLength = rc - headerLength;

					// Ping reply to different request. discard it.
					if (!((IPEndPoint)endpoint).Address.Equals(target.Address))
					{
						long t = timeout - rtt;
						if (t <= 0)
							return new PingReply(null, new byte[0], options, 0, IPStatus.TimedOut);
						s.ReceiveTimeout = (int)t;
						continue;
					}

					IcmpMessage recv = new IcmpMessage(bytes, headerLength, bodyLength);

					/* discard ping reply to different request or echo requests if running on same host. */
					if (recv.Identifier != identifier || recv.Type == 8)
					{
						long t = timeout - rtt;
						if (t <= 0)
							return new PingReply(null, new byte[0], options, 0, IPStatus.TimedOut);
						s.ReceiveTimeout = (int)t;
						continue;
					}

					return new PingReply(address, recv.Data, options, rtt, recv.IPStatus);
				} while (true);
			}
		}
			
		// ICMP message

		class IcmpMessage
		{
			byte[] bytes;

			// received
			public IcmpMessage(byte[] bytes, int offset, int size)
			{
				this.bytes = new byte[size];
				Buffer.BlockCopy(bytes, offset, this.bytes, 0, size);
			}

			// to be sent
			public IcmpMessage(byte type, byte code, ushort identifier, ushort sequence, byte[] data)
			{
				bytes = new byte[data.Length + 8];
				bytes[0] = type;
				bytes[1] = code;
				bytes[4] = (byte)(identifier & 0xFF);
				bytes[5] = (byte)((int)identifier >> 8);
				bytes[6] = (byte)(sequence & 0xFF);
				bytes[7] = (byte)((int)sequence >> 8);
				Buffer.BlockCopy(data, 0, bytes, 8, data.Length);

				ushort checksum = ComputeChecksum(bytes);
				bytes[2] = (byte)(checksum & 0xFF);
				bytes[3] = (byte)((int)checksum >> 8);
			}

			public byte Type
			{
				get { return bytes[0]; }
			}

			public byte Code
			{
				get { return bytes[1]; }
			}

			public ushort Identifier
			{
				get { return (ushort)(bytes[4] + (bytes[5] << 8)); }
			}

			public ushort Sequence
			{
				get { return (ushort)(bytes[6] + (bytes[7] << 8)); }
			}

			public byte[] Data
			{
				get
				{
					byte[] data = new byte[bytes.Length - 8];
					Buffer.BlockCopy(bytes, 8, data, 0, data.Length);
					return data;
				}
			}

			public byte[] GetBytes()
			{
				return bytes;
			}

			static ushort ComputeChecksum(byte[] data)
			{
				uint ret = 0;
				for (int i = 0; i < data.Length; i += 2)
				{
					ushort us = i + 1 < data.Length ? data[i + 1] : (byte)0;
					us <<= 8;
					us += data[i];
					ret += us;
				}
				ret = (ret >> 16) + (ret & 0xFFFF);
				return (ushort)~ret;
			}

			public IPStatus IPStatus
			{
				get
				{
					switch (Type)
					{
						case 0:
							return IPStatus.Success;
						case 3: // destination unreacheable
							switch (Code)
							{
								case 0:
									return IPStatus.DestinationNetworkUnreachable;
								case 1:
									return IPStatus.DestinationHostUnreachable;
								case 2:
									return IPStatus.DestinationProtocolUnreachable;
								case 3:
									return IPStatus.DestinationPortUnreachable;
								case 4:
									return IPStatus.BadOption; // FIXME: likely wrong
								case 5:
									return IPStatus.BadRoute; // not sure if it is correct
							}
							break;
						case 11:
							switch (Code)
							{
								case 0:
									return IPStatus.TimeExceeded;
								case 1:
									return IPStatus.TtlReassemblyTimeExceeded;
							}
							break;
						case 12:
							return IPStatus.ParameterProblem;
						case 4:
							return IPStatus.SourceQuench;
						case 8:
							return IPStatus.Success;
					}
					return IPStatus.Unknown;
					//throw new NotSupportedException (String.Format ("Unexpected pair of ICMP message type and code: type is {0} and code is {1}", Type, Code));
				}
			}
		}

		public class PingReply
		{
			IPAddress address;
			PingOptions options;
			IPStatus ipStatus;  // the status code returned by icmpsendecho, or the icmp status field on the raw socket
			long rtt;  // the round trip time.
			byte[] buffer; //buffer of the data


			internal PingReply()
			{
			}

			internal PingReply(IPStatus ipStatus)
			{
				this.ipStatus = ipStatus;
				buffer = new byte[0];
			}

			// The downlevel constructor. 
			internal PingReply(byte[] data, int dataLength, IPAddress address, int time)
			{
				this.address = address;
				rtt = time;


				ipStatus = GetIPStatus((IcmpV4Type)data[20], (IcmpV4Code)data[21]);

				if (ipStatus == IPStatus.Success)
				{
					buffer = new byte[dataLength - 28];
					Array.Copy(data, 28, buffer, 0, dataLength - 28);
				}
				else
					buffer = new byte[0];
			}

			internal PingReply (IPAddress address, byte [] buffer, PingOptions options, long roundtripTime, IPStatus status)
			{
				this.address = address;
				this.buffer = buffer;
				this.options = options;
				this.rtt = roundtripTime;
				this.ipStatus = status;
			}

			// the main constructor for the icmpsendecho apis
			internal PingReply(IcmpEchoReply reply)
			{
				address = new IPAddress(reply.address);
				ipStatus = (IPStatus)reply.status; //the icmpsendecho ip status codes

				//only copy the data if we succeed w/ the ping operation
				if (ipStatus == IPStatus.Success)
				{
					rtt = (long)reply.roundTripTime;
					buffer = new byte[reply.dataSize];
					Marshal.Copy(reply.data, buffer, 0, reply.dataSize);
					options = new PingOptions(reply.options);
				}
				else
					buffer = new byte[0];

			}

			// the main constructor for the icmpsendecho apis
			internal PingReply(Icmp6EchoReply reply, IntPtr dataPtr, int sendSize)
			{

				address = new IPAddress(reply.Address.Address, reply.Address.ScopeID);
				ipStatus = (IPStatus)reply.Status; //the icmpsendecho ip status codes

				//only copy the data if we succeed w/ the ping operation
				if (ipStatus == IPStatus.Success)
				{
					rtt = (long)reply.RoundTripTime;
					buffer = new byte[sendSize];
					Marshal.Copy(IntPtrHelper.Add(dataPtr, 36), buffer, 0, sendSize);
					//options = new PingOptions (reply.options);
				}
				else
					buffer = new byte[0];

			}

			//translates the relevant icmpsendecho codes to a ipstatus code
			private IPStatus GetIPStatus(IcmpV4Type type, IcmpV4Code code)
			{
				switch (type)
				{
					case IcmpV4Type.ICMP4_ECHO_REPLY:
						return IPStatus.Success;
					case IcmpV4Type.ICMP4_SOURCE_QUENCH:
						return IPStatus.SourceQuench;
					case IcmpV4Type.ICMP4_PARAM_PROB:
						return IPStatus.ParameterProblem;
					case IcmpV4Type.ICMP4_TIME_EXCEEDED:
						return IPStatus.TtlExpired;

					case IcmpV4Type.ICMP4_DST_UNREACH:
					{
						switch (code)
						{
							case IcmpV4Code.ICMP4_UNREACH_NET:
								return IPStatus.DestinationNetworkUnreachable;
							case IcmpV4Code.ICMP4_UNREACH_HOST:
								return IPStatus.DestinationHostUnreachable;
							case IcmpV4Code.ICMP4_UNREACH_PROTOCOL:
								return IPStatus.DestinationProtocolUnreachable;
							case IcmpV4Code.ICMP4_UNREACH_PORT:
								return IPStatus.DestinationPortUnreachable;
							case IcmpV4Code.ICMP4_UNREACH_FRAG_NEEDED:
								return IPStatus.PacketTooBig;
							default:
								return IPStatus.DestinationUnreachable;
						}
					}
				}
				return IPStatus.Unknown;
			}

			//the basic properties
			public IPStatus Status { get { return ipStatus; } }
			public IPAddress Address { get { return address; } }
			public long RoundtripTime { get { return rtt; } }
			public PingOptions Options
			{
				get
				{
					return options;
				}
			}
			public byte[] Buffer { get { return buffer; } }
		}

		public class PingOptions
		{
			const int DontFragmentFlag = 2;
			int ttl = 128;
			bool dontFragment;

			internal PingOptions(IPOptions options)
			{
				this.ttl = options.ttl;
				this.dontFragment = ((options.flags & DontFragmentFlag) > 0 ? true : false);
			}

			public PingOptions(int ttl, bool dontFragment)
			{
				if (ttl <= 0)
				{
					throw new ArgumentOutOfRangeException("ttl");
				}

				this.ttl = ttl;
				this.dontFragment = dontFragment;
			}

			public PingOptions()
			{
			}

			public int Ttl
			{
				get
				{
					return ttl;
				}
				set
				{
					if (value <= 0)
					{
						throw new ArgumentOutOfRangeException("value");
					}
					ttl = value; //useful to discover routes
				}
			}

			public bool DontFragment
			{
				get
				{
					return dontFragment;
				}
				set
				{
					dontFragment = value;  //useful for discovering mtu
				}
			}
		}

		static class IntPtrHelper
		{
			private const string KERNEL32 = "kernel32.dll";

			//internal static bool IsZero(IntPtr a) 
			//{
			//    return ((long) a)==0;
			//}

			//internal static IntPtr Add(IntPtr a, IntPtr b) 
			//{
			//    return (IntPtr) ((long) a + (long) b);
			//}

			//internal static IntPtr Add(IntPtr a, long b) 
			//{
			//    return (IntPtr) ((long) a + b);
			//}

			//internal static IntPtr Add(long a, IntPtr b) 
			//{
			//    return (IntPtr) (a + (long) b);
			//}

			internal static IntPtr Add(IntPtr a, int b)
			{
				return (IntPtr)((long)a + (long)b);
			}

			//internal static IntPtr Add(int a, IntPtr b) 
			//{
			//    return (IntPtr) ((long) a + (long) b);
			//}
		}

		internal enum IcmpV4Type
		{
			//can map these
			ICMP4_ECHO_REPLY = 0, // Echo Reply.
			ICMP4_DST_UNREACH = 3, // Destination Unreachable.
			ICMP4_SOURCE_QUENCH = 4, // Source Quench.
			ICMP4_TIME_EXCEEDED = 11, // Time Exceeded.
			ICMP4_PARAM_PROB = 12, // Parameter Problem.

			//unmappable
			ICMP4_REDIRECT = 5, // Redirect.
			ICMP4_ECHO_REQUEST = 8, // Echo Request.
			ICMP4_ROUTER_ADVERT = 9, // Router Advertisement.
			ICMP4_ROUTER_SOLICIT = 10, // Router Solicitation.
			ICMP4_TIMESTAMP_REQUEST = 13, // Timestamp Request.
			ICMP4_TIMESTAMP_REPLY = 14, // Timestamp Reply.
			ICMP4_MASK_REQUEST = 17, // Address Mask Request.
			ICMP4_MASK_REPLY = 18, // Address Mask Reply.
		}

		internal enum IcmpV4Code
		{
			ICMP4_UNREACH_NET = 0,
			ICMP4_UNREACH_HOST = 1,
			ICMP4_UNREACH_PROTOCOL = 2,
			ICMP4_UNREACH_PORT = 3,
			ICMP4_UNREACH_FRAG_NEEDED = 4,
			ICMP4_UNREACH_SOURCEROUTE_FAILED = 5,
			ICMP4_UNREACH_NET_UNKNOWN = 6,
			ICMP4_UNREACH_HOST_UNKNOWN = 7,
			ICMP4_UNREACH_ISOLATED = 8,
			ICMP4_UNREACH_NET_ADMIN = 9,
			ICMP4_UNREACH_HOST_ADMIN = 10,
			ICMP4_UNREACH_NET_TOS = 11,
			ICMP4_UNREACH_HOST_TOS = 12,
			ICMP4_UNREACH_ADMIN = 13,
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct IPOptions
		{
			internal byte ttl;
			internal byte tos;
			internal byte flags;
			internal byte optionsSize;
			internal IntPtr optionsData;

			internal IPOptions(PingOptions options)
			{
				ttl = 128;
				tos = 0;
				flags = 0;
				optionsSize = 0;
				optionsData = IntPtr.Zero;

				if (options != null)
				{
					this.ttl = (byte)options.Ttl;

					if (options.DontFragment)
					{
						flags = 2;
					}
				}
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct IcmpEchoReply
		{
			internal uint address;
			internal uint status;
			internal uint roundTripTime;
			internal ushort dataSize;
			internal ushort reserved;
			internal IntPtr data;
			internal IPOptions options;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		internal struct Ipv6Address
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
			internal byte[] Goo;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			internal byte[] Address;    // Replying address.
			internal uint ScopeID;
		}


		[StructLayout(LayoutKind.Sequential)]
		internal struct Icmp6EchoReply
		{
			internal Ipv6Address Address;
			internal uint Status;               // Reply IP_STATUS.
			internal uint RoundTripTime; // RTT in milliseconds.
			internal IntPtr data;
			// internal IPOptions options;
			// internal IntPtr data; data os after tjos
		}
	}
}
