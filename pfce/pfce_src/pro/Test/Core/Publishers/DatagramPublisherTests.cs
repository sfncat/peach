using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Core.Test;
using Peach.Pro.Core.Publishers;
using Array = Peach.Core.Dom.Array;
using Encoding = Peach.Core.Encoding;
using Mono.Unix;

namespace Peach.Pro.Test.Core.Publishers
{
	[TestFixture]
	[Quick]
	[Peach]
	class DatagramPublisherTests
	{
		#region OSX Multicast IPV6 Declarations

		[DllImport("libc", SetLastError = true)]
		static extern uint if_nametoindex(string ifname);

		[DllImport("libc", SetLastError = true)]
		static extern int setsockopt(int socket, int level, int optname, ref ipv6_mreq opt, int optlen);

		[DllImport("libc", SetLastError = true)]
		static extern int setsockopt(int socket, int level, int optname, ref int opt, int optlen);

		// ReSharper disable InconsistentNaming

		const int IPPROTO_IPV6 = 41;
		const int IPV6_MULTICAST_IF = 9;
		const int IPV6_JOIN_GROUP = 12;

		[StructLayout(LayoutKind.Sequential)]
		struct ipv6_mreq
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			public byte[] ipv6mr_multiaddr;
			public int    ipv6mr_interface;
		}

		// ReSharper restore InconsistentNaming

		void JoinGroupV6(IPAddress group, int ifindex)
		{
			System.Diagnostics.Debug.Assert(_socket.Handle != IntPtr.Zero);
			System.Diagnostics.Debug.Assert(group.AddressFamily == AddressFamily.InterNetworkV6);
			System.Diagnostics.Debug.Assert(ifindex != 0);

			var mr = new ipv6_mreq
			{
				ipv6mr_multiaddr = group.GetAddressBytes(),
				ipv6mr_interface = ifindex,
			};

			var ret = setsockopt(_socket.Handle.ToInt32(), IPPROTO_IPV6, IPV6_JOIN_GROUP, ref mr, Marshal.SizeOf(mr));
			UnixMarshal.ThrowExceptionForLastErrorIf(ret);

			ret = setsockopt(_socket.Handle.ToInt32(), IPPROTO_IPV6, IPV6_MULTICAST_IF, ref ifindex, sizeof(int));
			UnixMarshal.ThrowExceptionForLastErrorIf(ret);
		}

		#endregion

		private const string Template = @"
<Peach>
	<DataModel name='IpPacket'>
		<Block name='Header'>
			<Number name='IHL' value='0x45' size='8' signed='false'/>
			<Number name='DSCP' size='8' signed='false'/>
			<Number name='TotalLength' endian='big' size='16' signed='false'>
				<Relation type='size' of='IpPacket' />
			</Number>
			<Number name='ID' size='16' signed='false'>
				<Fixup class='SequenceRandom' />
			</Number>
			<Number name='FragOff' size='16' signed='false'/>
			<Number name='TTL' value= '64' size='8' signed='false'/>
			<Number name='Protocol' value='12' size='8' signed='false'/>
			<Number name='Checksum' endian='big' size='16' signed='false'>
				<Fixup class='IcmpChecksum'>
					<Param name='ref' value='Header' />
				</Fixup>
			</Number>
			<Number name='Src' valueType='ipv4' value='{2}' size='32' signed='false' endian='big'/>
			<Number name='Dst' valueType='ipv4' value='{2}' size='32' signed='false' endian='big'/>
		</Block>
		<String name='Data' value='Hello World' />
	</DataModel>

	<DataModel name='UdpPacket'>
		<String name='Data' value='Hello World' />
	</DataModel>

	<DataModel name='LastRecvAddr'>
		<Number size='8' minOccurs='0' />
	</DataModel>

	<StateModel name='Udp' initialState='Initial'>
		<State name='Initial'>
			<Action name='Send' type='output'>
				<DataModel ref='UdpPacket'/>
			</Action>

			<Action name='Recv' type='input'>
				<DataModel ref='UdpPacket'/>
			</Action>

			<Action name='Addr' type='getProperty' property='LastRecvAddr'>
				<DataModel ref='LastRecvAddr' />
			</Action>
		</State>
	</StateModel>

	<StateModel name='RawIPv4' initialState='Initial'>
		<State name='Initial'>
			<!-- RawV4 needs IP header on output -->
			<Action name='Send' type='output'>
				<DataModel ref='IpPacket'/>
			</Action>

			<!-- RawV4 provides IP header on input -->
			<Action name='Recv1' type='input'>
				<DataModel ref='IpPacket'/>
			</Action>

			<!-- RawV4 provides IP header on input -->
			<Action name='Recv2' type='input'>
				<DataModel ref='IpPacket'/>
			</Action>
		</State>
	</StateModel>

	<StateModel name='RawV4' initialState='Initial'>
		<State name='Initial'>
			<!-- RawV4 does not need IP header on output -->
			<Action name='Send' type='output'>
				<DataModel ref='UdpPacket'/>
			</Action>

			<!-- RawV4 provides IP header on input -->
			<Action name='Recv1' type='input'>
				<DataModel ref='IpPacket'/>
			</Action>

			<!-- RawV4 provides IP header on input -->
			<Action name='Recv2' type='input'>
				<DataModel ref='IpPacket'/>
			</Action>
		</State>
	</StateModel>

	<StateModel name='RawV6' initialState='Initial'>
		<State name='Initial'>
			<Action type='open' />
			<!-- {3} -->
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='{0}' />

		<Publisher class='{0}'>
{1}		</Publisher>
	</Test>
</Peach>
";
		private Socket _socket;
		private IPAddress _groupIp;
		private IPEndPoint _localEp;
		private EndPoint _remoteEp;
		private byte[] _buffer;

		[SetUp]
		public void SetUp()
		{
			_socket = null;
			_groupIp = null;
			_localEp = null;
			_remoteEp = null;
			_buffer = new byte[100];
		}

		[TearDown]
		public void TearDown()
		{
			if (_socket != null)
			{
				_socket.Dispose();
				_socket = null;
			}
		}

		private IPAddress GetSelf(AddressFamily family)
		{
			var peachIface = Environment.GetEnvironmentVariable("PEACH_SELF");
			if (!string.IsNullOrEmpty(peachIface))
			{
				var adapter = NetworkInterface.GetAllNetworkInterfaces().Single(x => x.Name == peachIface);
				var addrs = adapter.GetIPProperties().UnicastAddresses;
				return addrs.First(a => a.Address.AddressFamily == family).Address;
			}
			return Helpers.GetPrimaryIface(family).Item2;
		}
		
		private static IEnumerable<Tuple<string, IPAddress>> GetAllLinkLocalIPv6()
		{
			// ReSharper disable once LoopCanBeConvertedToQuery
			foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (adapter.OperationalStatus != OperationalStatus.Up)
					continue;

				if (adapter.NetworkInterfaceType != NetworkInterfaceType.Ethernet)
					continue;

				foreach (var ip in adapter.GetIPProperties().UnicastAddresses)
				{
					if (ip.Address.AddressFamily != AddressFamily.InterNetworkV6)
						continue;

					if (!ip.Address.IsIPv6LinkLocal)
						continue;

					yield return new Tuple<string, IPAddress>(adapter.Name, ip.Address);
				}
			}
		}

		private static IPAddress GetLinkLocalIPv6()
		{
			return GetAllLinkLocalIPv6().Select(i => i.Item2).FirstOrDefault();
		}

		private void StartEcho(IPAddress localIp)
		{
			_socket = new Socket(localIp.AddressFamily, SocketType.Dgram, ProtocolType.IP);
			_socket.Bind(new IPEndPoint(localIp, 0));
			_localEp = (IPEndPoint)_socket.LocalEndPoint;
			_remoteEp = new IPEndPoint(localIp, 0);

			ScheduleRead(_socket);
		}

		private void StartRawEcho(IPAddress localIp, int protocol)
		{
			try
			{
				_socket = new Socket(localIp.AddressFamily, SocketType.Raw, (ProtocolType)protocol);
			}
			catch (SocketException ex)
			{
				if (ex.SocketErrorCode == SocketError.AccessDenied)
					Assert.Ignore("Test requires administrator access.");

				throw;
			}

			_socket.Bind(new IPEndPoint(localIp, 0));
			_localEp = (IPEndPoint)_socket.LocalEndPoint;
			_remoteEp = new IPEndPoint(localIp, 0);

			ScheduleRead(_socket);
		}

		private void StartMulticast(IPAddress localIp, IPAddress groupIp)
		{
			_socket = new Socket(localIp.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
			_groupIp = groupIp;

			if (groupIp.AddressFamily == AddressFamily.InterNetwork)
			{
				if (Platform.GetOS() == Platform.OS.Windows)
				{
					// Multicast needs to bind to INADDR_ANY on windows
					_socket.Bind(new IPEndPoint(IPAddress.Any, 0));
				}
				else if (Platform.GetOS() == Platform.OS.OSX)
				{
					// Multicast needs to bind to INADDR_ANY on osx 
					// the group address works for older versions of OSX
					_socket.Bind(new IPEndPoint(IPAddress.Any, 0));
				}
				else
				{
					// Multicast needs to bind to the group on linux
					_socket.Bind(new IPEndPoint(groupIp, 0));
				}

				var opt = new MulticastOption(groupIp, localIp);
				_socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, opt);
				_socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, localIp.GetAddressBytes());
			}
			else
			{
				// For IPv6 always bind to INADDR_ANY
				_socket.Bind(new IPEndPoint(IPAddress.IPv6Any, 0));

				if (Platform.GetOS() == Platform.OS.Windows)
				{
					var opt = new IPv6MulticastOption(groupIp, localIp.ScopeId);
					_socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, opt);
					_socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastInterface, (int)localIp.ScopeId);
				}
				else if (Platform.GetOS() == Platform.OS.OSX)
				{
					// ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
					if (localIp.Equals(IPAddress.IPv6Loopback))
						JoinGroupV6(groupIp, (int)if_nametoindex("lo0"));
					else
						JoinGroupV6(groupIp, (int)localIp.ScopeId);
				}
				else
				{
					var opt = new IPv6MulticastOption(groupIp, localIp.ScopeId);
					if (localIp.Equals(IPAddress.IPv6Loopback))
						opt.InterfaceIndex = if_nametoindex("lo");
					_socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, opt);
					_socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastInterface, (int)localIp.ScopeId);
				}
			}

			_localEp = (IPEndPoint)_socket.LocalEndPoint;
			_remoteEp = new IPEndPoint(localIp, 0);

			ScheduleRead(_socket);
		}

		private void OnReadComplete(IAsyncResult ar)
		{
			var s = (Socket)ar.AsyncState;

			try
			{
				var len = s.EndReceiveFrom(ar, ref _remoteEp);
				var response = Encoding.ASCII.GetBytes("Recv {0} bytes!".Fmt(len));

				if (_groupIp != null)
					((IPEndPoint) _remoteEp).Address = _groupIp;

				s.SendTo(response, SocketFlags.None, _remoteEp);
			}
			catch (ObjectDisposedException)
			{
				// If test fails and socket is closed prior to reading a packet
				// this exception wil be raised.
			}
		}

		private void ScheduleRead(Socket s)
		{
			s.BeginReceiveFrom(_buffer, 0, _buffer.Length, SocketFlags.None, ref _remoteEp, OnReadComplete, s);
		}

		private Peach.Core.Dom.Dom RunEngine(string publisher, Dictionary<string, string> parameters)
		{
			var sb = new StringBuilder();
			foreach (var kv in parameters)
			{
				sb.AppendFormat("			<Param name='{0}' value='{1}' />", kv.Key, kv.Value);
				sb.AppendLine();
			}

			string host4 = "127.0.0.1";
			string host6 = "::1";

			string host;
			if (parameters.TryGetValue("Host", out host))
			{
				var ip = IPAddress.Parse(host);
				if (ip.AddressFamily == AddressFamily.InterNetwork)
					host4 = host;
				else
					host6 = host;
			}

			var xml = Template.Fmt(publisher, sb, host4, host6);
			var dom = DataModelCollector.ParsePit(xml);

			var cfg = new RunConfiguration { singleIteration = true };
			var e = new Engine(null);

			e.startFuzzing(dom, cfg);

			return dom;
		}

		[Test]
		[TestCase("127.0.0.1")]
		[TestCase("::1")]
		public void UdpEcho(string host)
		{
			// Ensure basic send/recv functionality over loopback
			// for ipv4 and ipv6 hosts

			StartEcho(IPAddress.Parse(host));

			var dom = RunEngine("Udp", new Dictionary<string, string>
			{
				{ "Host", host },
				{ "Port", _localEp.Port.ToString(CultureInfo.InvariantCulture) },
			});

			Assert.NotNull(dom);

			var state = dom.tests[0].stateModel.states[0];

			Assert.AreEqual("Hello World", state.actions[0].dataModel.InternalValue.BitsToString());
			Assert.AreEqual("Recv 11 bytes!", state.actions[1].dataModel.InternalValue.BitsToString());
		}

		[Test]
		public void TestMissingLocalScopeId()
		{
			var ex = Assert.Throws<PeachException>(() => RunEngine("Udp", new Dictionary<string, string>
			{
				{ "Host", "fe80::%1" },
				{ "Interface", "fe80::" },
			}));
			Assert.AreEqual("Could not resolve scope id for interface with address 'fe80::'.", ex.Message);
		}

		[Test]
		public void TestMissingRemoteScopeId()
		{
			var ex = Assert.Throws<PeachException>(() => RunEngine("Udp", new Dictionary<string, string>
			{
				{ "Host", "fe80::" },
			}));
			Assert.AreEqual("IPv6 scope id required for resolving link local address: 'fe80::'.", ex.Message);
		}

		[Test]
		public void UdpNoPortSend()
		{
			var ex = Assert.Throws<PeachException>(() => RunEngine("Udp", new Dictionary<string, string>
			{
				{ "Host", "127.0.0.1" },
			}));
			Assert.AreEqual("Error sending a Udp packet to 127.0.0.1, the port was not specified.", ex.Message);
		}

		[Test]
		public void Udp6NoScope()
		{
			// If local is link-local ipv6 address w/o a scopeId, make sure
			// the publisher can resolve it.

			var withScope = GetLinkLocalIPv6();
			if (withScope == null)
				Assert.Ignore("No interface with a link-locak IPv6 address was found.");

			Assert.AreNotEqual(0, withScope.ScopeId);

			var withoutScope = new IPAddress(withScope.GetAddressBytes(), 0);

			Assert.AreEqual(0, withoutScope.ScopeId);

			StartEcho(withScope);

			var dom = RunEngine("Udp", new Dictionary<string, string>
			{
				{ "Interface", withoutScope.ToString() },
				{ "Host", withScope.ToString() },
				{ "Port", _localEp.Port.ToString(CultureInfo.InvariantCulture) },
			});

			Assert.NotNull(dom);

			var state = dom.tests[0].stateModel.states[0];

			Assert.AreEqual("Hello World", state.actions[0].dataModel.InternalValue.BitsToString());
			Assert.AreEqual("Recv 11 bytes!", state.actions[1].dataModel.InternalValue.BitsToString());
		}

		[Test]
		[TestCase("RawV6", "127.0.0.1")]
		[TestCase("RawV4", "::1")]
		[TestCase("RawIPv4", "::1")]
		public void BadAddressFamily(string pub, string host)
		{
			var ex = Assert.Throws<PeachException>(() => RunEngine(pub, new Dictionary<string, string>
			{
				{ "Host", host },
				{ "Protocol", "17" },
			}));

			var res = new IPEndPoint(IPAddress.Parse(host), 0).ToString();
			var exp = "The resolved IP '{0}' for host '{1}' is not compatible with the {2} publisher.".Fmt(res, host, pub);

			Assert.AreEqual(exp, ex.Message);
		}

		[Test]
		// PUP
		[TestCase("RawV4", AddressFamily.InterNetwork, ProtocolType.Pup)]
		[TestCase("RawIPv4", AddressFamily.InterNetwork, ProtocolType.Pup)]
		public void RawEcho(string pub, AddressFamily family, int protocol)
		{
			// Ensure basic send/recv functionality
			var self = GetSelf(family);

			StartRawEcho(self, protocol);

			var dom = RunEngine(pub, new Dictionary<string, string>
			{
				{ "Host", self.ToString() },
				{ "Protocol", protocol.ToString() },
			});

			Assert.NotNull(dom);

			var state = dom.tests[0].stateModel.states[0];

			var req = state.actions[0].dataModel.find("Data");
			Assert.NotNull(req);
			Assert.AreEqual("Hello World", req.InternalValue.ToString());

			// Because we are sending a test packet to ourselves, the first
			// input action is a copy of the "Hello World" packet that was
			// sent by the publisher.
			var resp1 = state.actions[1].dataModel.find("Data");
			Assert.NotNull(resp1);
			Assert.AreEqual("Hello World", resp1.InternalValue.ToString());

			// The second packet the publisher receives is the echo response
			// packet.  The value is 11 bytes of "Hello World" plus the 20
			// byte IP header for a total of 31 bytes.
			var resp2 = state.actions[2].dataModel.find("Data");
			Assert.NotNull(resp2);
			Assert.AreEqual("Recv 31 bytes!", resp2.InternalValue.ToString());
		}

		[Test]
		[TestCase("RawV4")]
		[TestCase("RawIPv4")]
		public void RawTcp(string pub)
		{
			var xml = @"
<Peach>
	<DataModel name='DM'>
		<Blob name='Data' valueType='hex' value='4500 003c 7f5c 4000 4006 bd5d 7f00 0001 7f00 0001 afb2 0050 00ea a292 0000 0000 a002 8018 fe30 0000 0204 400c 0402 080a 2f77 ddce 0000 0000 0103 0307' />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='{0}'>
			<Param name='Host' value='127.0.0.1' />
			<Param name='Protocol' value='6' />
		</Publisher>
	</Test>
</Peach>".Fmt(pub);

			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration { singleIteration = true };
			var e = new Engine(null);

			if (Platform.GetOS() == Platform.OS.Windows)
			{
				var ex = Assert.Throws<PeachException>(() => e.startFuzzing(dom, cfg));
				var msg = "The {0} publisher does not support the TCP protocol on windows.".Fmt(pub);
				Assert.AreEqual(msg, ex.Message);
			}
			else
			{
				e.startFuzzing(dom, cfg);
			}
		}

		[Test]
		public void UdpNoPortRecv()
		{
			// If no 'Port' parameter is specified, the publisher should
			// learn it from the remote endpoint of the first packet
			// that is received.  Packets that arrive at the publisher
			// that are not from the initial port are discarded.

			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='input'>
				<DataModel ref='DM'/>
			</Action>

			<Action type='input'>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Udp'>
			<Param name='Host' value='127.0.0.1'/>
			<Param name='SrcPort' value='0'/>
		</Publisher>
	</Test>
</Peach>
";

			var dom = DataModelCollector.ParsePit(xml);
			var pub = (UdpPublisher)dom.tests[0].publishers[0];

			pub.Opened += (sender, args) =>
			{
				var ep = pub.LocalEndPoint;

				using (var s1 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
				{
					s1.SendTo("Packet One", ep);

					using (var s2 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
					{
						s2.SendTo("Packet Two", ep);
					}

					s1.SendTo("Packet Three", ep);
				}
			};

			var cfg = new RunConfiguration { singleIteration = true };
			var e = new Engine(null);

			e.startFuzzing(dom, cfg);

			var state = dom.tests[0].stateModel.states[0];

			Assert.AreEqual("Packet One", state.actions[0].dataModel.InternalValue.BitsToString());
			Assert.AreEqual("Packet Three", state.actions[1].dataModel.InternalValue.BitsToString());
		}

		[Test]
		[TestCase("127.0.0.1", "234.5.6.7")]
		[TestCase("::1", "ff02::22")]
		[TestCase("", "234.5.6.7")]
		[TestCase("", "ff02::22")]
		public void MulticastUdp(string local, string group)
		{
			var groupIp = IPAddress.Parse(group);
			var localIp = string.IsNullOrEmpty(local) ? GetSelf(groupIp.AddressFamily) : IPAddress.Parse(local);

			// Can't do IPv6 multicast on loopback on linux
			if (Platform.GetOS() == Platform.OS.Linux && localIp.Equals(IPAddress.IPv6Loopback))
				Assert.Ignore();

			StartMulticast(localIp, groupIp);

			var dom = RunEngine("Udp", new Dictionary<string, string>
			{
				{ "Interface", localIp.ToString() },
				{ "Host", groupIp.ToString() },
				{ "Port", _localEp.Port.ToString(CultureInfo.InvariantCulture) },
			});

			Assert.NotNull(dom);

			var state = dom.tests[0].stateModel.states[0];

			Assert.AreEqual("Hello World", state.actions[0].dataModel.InternalValue.BitsToString());
			Assert.AreEqual("Recv 11 bytes!", state.actions[1].dataModel.InternalValue.BitsToString());
		}

		[Test]
		public void UdpMaxSize()
		{
			// If the data model is too large, the publisher should throw a SoftException

			var pub = new UdpPublisher(new Dictionary<string, Variant>
			{
				{ "Host", new Variant("127.0.0.1") },
				{ "Port", new Variant("1") },
			});

			var bs = new BitStream();
			bs.Seek(64999, SeekOrigin.Begin);
			bs.WriteByte(0);

			Assert.AreEqual(65000, bs.Length);

			try
			{
				pub.start();
				pub.open();

				bs.Seek(0, SeekOrigin.Begin);
				pub.output(bs);

				bs.Seek(0, SeekOrigin.End);
				bs.WriteByte(0);

				Assert.AreEqual(65001, bs.Length);

				bs.Seek(0, SeekOrigin.Begin);
				
				var se = Assert.Throws<SoftException>(() => pub.output(bs));

				StringAssert.Contains("Only sent 65000 of 65001 byte Udp packet.", se.Message);
			}
			finally
			{
				pub.close();
				pub.stop();
			}
		}

		[Test]
		public void TestMtuInterface()
		{
			var self = GetSelf(AddressFamily.InterNetwork);

			var pub = new UdpPublisher(new Dictionary<string, Variant>
			{
				{ "Interface", new Variant(self.ToString()) },
				{ "Host", new Variant(self.ToString()) },
			});

			try
			{
				pub.start();
				pub.open();

				var mtu1 = pub.getProperty("MTU");

				Assert.NotNull(mtu1, "Expected non-null MTU for interface " + self);

				var asInt = (int)mtu1;

				pub.setProperty("MTU", new Variant(Endian.Little.GetBytes(1280, 32)));

				var mtu2 = pub.getProperty("MTU");

				pub.setProperty("MTU", new Variant(Endian.Little.GetBytes(asInt, 32)));

				var mtu3 = pub.getProperty("MTU");

				Assert.NotNull(mtu2, "Expected non-null MTU for loopback after change");
				Assert.AreEqual(1280, (int)mtu2, "MTU should have changed to 1280");
				Assert.AreNotEqual(asInt, (int)mtu2, "MTU should be different from start");
				Assert.NotNull(mtu3, "Expected non-null MTU for interface " + self);
				Assert.AreEqual((int)mtu1, (int)mtu3, "MTU should have changed back to original");
			}
			finally
			{
				pub.close();
				pub.stop();
			}

		}

		[Test]
		public void TestMtuLoopback()
		{
			var pub = new UdpPublisher(new Dictionary<string, Variant>
			{
				{ "Interface", new Variant("127.0.0.1") },
				{ "Host", new Variant("127.0.0.1") },
			});

			try
			{
				pub.start();
				pub.open();

				var mtu1 = pub.getProperty("MTU");

				if (Platform.GetOS() == Platform.OS.Windows)
				{
					// Can't get mtu on loopback on windows
					Assert.Null(mtu1);

					var pe = Assert.Throws<SoftException>(() =>
						pub.setProperty("MTU", new Variant(Endian.Little.GetBytes(1280, 32))));

					StringAssert.Contains("MTU changes are not supported on interface", pe.Message);
				}
				else
				{
					Assert.NotNull(mtu1, "Expected non-null MTU for loopback before change");

					var asInt = (int)mtu1;

					pub.setProperty("MTU", new Variant(Endian.Little.GetBytes(1280, 32)));

					var mtu2 = pub.getProperty("MTU");

					pub.setProperty("MTU", new Variant(Endian.Little.GetBytes(asInt, 32)));

					Assert.NotNull(mtu2, "Expected non-null MTU for loopback after change");
					Assert.AreEqual(1280, (int)mtu2, "MTU should have changed to 128");
					Assert.AreNotEqual(asInt, (int)mtu2, "MTU should be different from start");
				}
			}
			finally
			{
				pub.close();
				pub.stop();
			}
		}

		[Test]
		public void TestLinkLocalTwoIface()
		{
			// Ensure we can send to a link local address
			// on two different interfaces at the same time

			var linkLocal = GetAllLinkLocalIPv6().ToList();

			if (linkLocal.Count < 2)
				Assert.Ignore("Test requires two interfaces with link-local ipv6 addresses");

			const string template = @"
<Peach>
	<DataModel name='DM'>
		<String name='Value'/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output' publisher='if0'>
				<DataModel ref='DM'/>
				<Data>
					<Field name='Value' value='Hello' />
				</Data>
			</Action>
			<Action type='output' publisher='if1'>
				<DataModel ref='DM'/>
				<Data>
					<Field name='Value' value='World' />
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher name='if0' class='Udp'>
			<Param name='Interface' value='{0}'/>
			<Param name='Host' value='{0}'/>
			<Param name='Port' value='{1}'/>
		</Publisher>
		<Publisher name='if1' class='Udp'>
			<Param name='Interface' value='{2}'/>
			<Param name='Host' value='{2}'/>
			<Param name='Port' value='{3}'/>
		</Publisher>
	</Test>
</Peach>
";

			using (var s1 = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp))
			{
				s1.Bind(new IPEndPoint(linkLocal[0].Item2, 0));

				using (var s2 = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp))
				{
					s2.Bind(new IPEndPoint(linkLocal[1].Item2, 0));

					var ep1 = (IPEndPoint)s1.LocalEndPoint;
					var ep2 = (IPEndPoint)s2.LocalEndPoint;

					// Don't include scope id's when generating pit parameters
					var src1 = new IPAddress(ep1.Address.GetAddressBytes());
					var src2 = new IPAddress(ep2.Address.GetAddressBytes());

					var xml = template.Fmt(
							src1,
							ep1.Port,
							src2,
							ep2.Port
						);

					var dom = DataModelCollector.ParsePit(xml);
					var cfg = new RunConfiguration { singleIteration = true };
					var e = new Engine(null);

					e.startFuzzing(dom, cfg);

					EndPoint ep = new IPEndPoint(IPAddress.IPv6Any, 0);
					var buf = new byte[1024];

					var ar = s1.BeginReceiveFrom(buf, 0, buf.Length, SocketFlags.None, ref ep, null, null);

					if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1)))
						Assert.Fail("Should have received message on first socket");

					var len = s1.EndReceiveFrom(ar, ref ep);
					var str = Encoding.ASCII.GetString(buf, 0, len);

					Assert.AreEqual("Hello", str);

					ar = s2.BeginReceiveFrom(buf, 0, buf.Length, SocketFlags.None, ref ep, null, null);

					if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1)))
						Assert.Fail("Should have received message on second socket");

					len = s2.EndReceiveFrom(ar, ref ep);
					str = Encoding.ASCII.GetString(buf, 0, len);

					Assert.AreEqual("World", str);
				}
			}
		}

		[Test]
		public void TestReceiveZeroTimeout()
		{
			const string xml = @"
<Peach>
	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action name='open' type='open' publisher='Rx' />

			<Action type='setProperty' property='Timeout' publisher='Rx'>
				<DataModel name='DM'>
					<Number size='32' value='0'/>
				</DataModel>
			</Action>

			<Action type='setProperty' property='NoReadException' publisher='Rx'>
				<DataModel name='DM'>
					<String value='true'/>
				</DataModel>
			</Action>

			<Action type='input' publisher='Rx'>
				<DataModel name='DM'>
					<Choice>
						<Block name='Yes'>
							<Blob length='1'/>
							<Blob />
						</Block>
						<Block name='No' />
					</Choice>
				</DataModel>
			</Action>

			<Action name='send' type='output' publisher='Tx'>
				<DataModel name='DM'>
					<Blob value='Hello World' />
				</DataModel>
			</Action>

			<Action type='input' publisher='Rx'>
				<DataModel name='DM'>
					<Blob />
				</DataModel>
			</Action>

			<!-- Ensure shutdown with pending IO works -->
			<Action type='input' publisher='Rx'>
				<DataModel name='DM'>
					<Choice>
						<Blob />
					</Choice>
				</DataModel>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='Udp' name='Rx'>
			<Param name='Host' value='127.0.0.1' />
			<Param name='SrcPort' value='0' />
		</Publisher>
		<Publisher class='Udp' name='Tx'>
			<Param name='Host' value='127.0.0.1' />
			<Param name='Port' value='0' />
		</Publisher>
	</Test>
</Peach>
";

			var dom = DataModelCollector.ParsePit(xml);
			var e = new Engine(null);
			var cfg = new RunConfiguration { singleIteration = true };
			e.IterationStarting += (ctx, it, tot) =>
			{
				ctx.ActionFinished += (c, a) =>
				{
					if (a.Name == "open")
					{
						((DatagramPublisher)c.test.publishers[1]).Port = ((DatagramPublisher)c.test.publishers[0]).SrcPort;
					}
					else if (a.Name == "send")
					{
						System.Threading.Thread.Sleep(1000);
					}
				};
			};

			e.startFuzzing(dom, cfg);

			var acts = dom.tests[0].stateModel.states[0].actions;

			var ch = acts[3].dataModel[0] as Choice;
			Assert.NotNull(ch, "Should have a choice element");

			Assert.AreEqual("No", ch.SelectedElement.Name);

			// Should have received hello world!
			Assert.AreEqual("Hello World", acts[5].dataModel.InternalValue.BitsToString());

			// Should have received nothing
			Assert.AreEqual("", acts[6].dataModel.InternalValue.BitsToString());
		}

		[Test, Ignore("Issue #493")]
		public void UdpGetLastRecvAddr()
		{
			// getProperty should crack the result to the data model
			// and not do dataModel.DefalueValue = XXX

			StartEcho(IPAddress.Loopback);

			var dom = RunEngine("Udp", new Dictionary<string, string>
			{
				{ "Host", "127.0.0.1" },
				{ "Port", _localEp.Port.ToString(CultureInfo.InvariantCulture) },
			});

			Assert.NotNull(dom);

			var state = dom.tests[0].stateModel.states[0];

			Assert.AreEqual("Hello World", state.actions[0].dataModel.InternalValue.BitsToString());
			Assert.AreEqual("Recv 11 bytes!", state.actions[1].dataModel.InternalValue.BitsToString());

			var dm = state.actions[2].dataModel;
			var array = (Array)dm[0];

			Assert.AreEqual(4, array.Count);
			Assert.AreEqual(127, (int)array[0].DefaultValue);
			Assert.AreEqual(0, (int)array[1].DefaultValue);
			Assert.AreEqual(0, (int)array[2].DefaultValue);
			Assert.AreEqual(1, (int)array[3].DefaultValue);
			Assert.NotNull(dm);
		}
	}
}
