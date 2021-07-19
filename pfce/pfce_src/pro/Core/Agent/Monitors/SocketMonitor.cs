using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Peach.Core;
using Peach.Core.Agent;
using Monitor = Peach.Core.Agent.Monitor2;

namespace Peach.Pro.Core.Agent.Monitors
{
	[Monitor("Socket")]
	[Description("Waits for an incoming TCP or UDP connection")]
	[Parameter("Host", typeof(IPAddress), "IP address of remote host", "")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data/connection (default 3000)", "3000")]
	[Parameter("Interface", typeof(IPAddress), "IP of interface to listen on", "")]
	[Parameter("Protocol", typeof(Proto), "Protocol type to listen for", "tcp")]
	[Parameter("Port", typeof(ushort), "Port to listen on", "8080")]
	[Parameter("FaultOnSuccess", typeof(bool), "Fault if no connection is recorded", "false")]
	public class SocketMonitor : Monitor
	{
		public enum Proto { Udp = ProtocolType.Udp, Tcp = ProtocolType.Tcp }

		public IPAddress    Host           { get; set; }
		public int          Timeout        { get; set; }
		public IPAddress    Interface      { get; set; }
		public Proto        Protocol       { get; set; }
		public ushort       Port           { get; set; }
		public int          Backlog        { get; set; }
		public bool         FaultOnSuccess { get; set; }

		private const int TcpBlockSize = 1024;
		private const int MaxDgramSize = 65000;

		private readonly MemoryStream _recvBuffer = new MemoryStream();

		private Socket _socket;
		private MonitorData _data;
		private bool _multicast;

		public SocketMonitor(string name)
			: base(name)
		{
		}

		public override void StartMonitor(Dictionary<string, string> args)
		{
			base.StartMonitor(args);

			if (Host != null)
			{
				_multicast = Host.IsMulticast();

				if (Interface != null && Interface.AddressFamily != Host.AddressFamily)
					throw new PeachException("Interface '" + Interface + "' is not compatible with the address family for Host '" + Host + "'.");

				if (_multicast && Protocol != Proto.Udp)
					throw new PeachException("Multicast hosts are not supported with the tcp protocol.");
			}

			OpenSocket();
		}

		#region Monitor Interface

		public override void IterationFinished()
		{
			_data = new MonitorData
			{
				Data = new Dictionary<string, Stream>()
			};

			var data = ReadSocket();
			if (data != null)
			{
				_data.Title = "Received {0} bytes from '{1}'.".Fmt(data.Item2.Length, data.Item1);
				_data.Data.Add("Response.bin", new MemoryStream(data.Item2));

				if (!FaultOnSuccess)
				{
					_data.Fault = new MonitorData.Info
					{
						MajorHash = Hash("{0}{1}{2}{3}".Fmt(Host, Interface, Protocol, Port)),
						MinorHash = Hash(data.Item1.ToString()),
					};
				}
			}
			else
			{
				var ep = _multicast ? "{0}:{1}".Fmt(Host, Port) : _socket.LocalEndPoint.ToString();
				_data.Title = "Monitoring {0}, no connections recorded.".Fmt(ep);

				if (FaultOnSuccess)
				{
					_data.Fault = new MonitorData.Info
					{
						MajorHash = Hash("{0}{1}{2}{3}".Fmt(Host, Interface, Protocol, Port)),
						MinorHash = Hash("NoConnection"),
					};
				}
			}
		}

		public override void StopMonitor()
		{
			CloseSocket();
		}

		public override bool DetectedFault()
		{
			return _data.Fault != null;
		}

		public override MonitorData GetMonitorData()
		{
			return _data;
		}

		#endregion

		#region Socket Implementation

		private void OpenSocket()
		{
			System.Diagnostics.Debug.Assert(_socket == null);

			var local = GetLocalIp();
			_socket = new Socket(local.AddressFamily, Protocol == Proto.Tcp ? SocketType.Stream : SocketType.Dgram, (ProtocolType)Protocol);

			if (_multicast)
			{
				if (Platform.GetOS() == Platform.OS.OSX)
				{
					if (local.Equals(IPAddress.Any) || local.Equals(IPAddress.IPv6Any))
						throw new PeachException("Error, the value for parameter 'Interface' can not be '" + local + "' when the 'Host' parameter is multicast.");
				}

				if (Platform.GetOS() == Platform.OS.Windows)
				{
					// Multicast needs to bind to INADDR_ANY on windows
					// ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
					if (Host.AddressFamily == AddressFamily.InterNetwork)
						_socket.Bind(new IPEndPoint(IPAddress.Any, Port));
					else
						_socket.Bind(new IPEndPoint(IPAddress.IPv6Any, Port));
				}
				else
				{
					// Multicast needs to bind to the group on *nix
					_socket.Bind(new IPEndPoint(Host, Port));
				}

				var level = local.AddressFamily == AddressFamily.InterNetwork ? SocketOptionLevel.IP : SocketOptionLevel.IPv6;
				var opt = new MulticastOption(Host, local);
				_socket.SetSocketOption(level, SocketOptionName.AddMembership, opt);
			}
			else
			{
				_socket.Bind(new IPEndPoint(local, Port));
			}

			// Allow for ephemeral ports
			Port = (ushort)((IPEndPoint)_socket.LocalEndPoint).Port;

			if (Protocol == Proto.Tcp)
				_socket.Listen(Backlog);
		}

		private void CloseSocket()
		{
			if (_socket != null)
			{
				_socket.Close();
				_socket = null;
			}
		}

		private Tuple<IPEndPoint, byte[]> ReadSocket()
		{
			IPEndPoint ep;

			_recvBuffer.Seek(0, SeekOrigin.Begin);
			_recvBuffer.SetLength(0);

			// ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
			if (Protocol == Proto.Udp)
				ep = WaitForData(_socket, 1, MaxDgramSize, Recv);
			else
				ep = WaitForData(_socket, 1, MaxDgramSize, Accept);

			_recvBuffer.Seek(0, SeekOrigin.Begin);

			return ep == null ? null : new Tuple<IPEndPoint, byte[]>(ep, _recvBuffer.ToArray());
		}

		private delegate IPEndPoint IoFunc(Socket s, int blockSize);

		private IPEndPoint WaitForData(Socket s, int maxReads, int blockSize, IoFunc read)
		{
			IPEndPoint ret = null;
			var now = Environment.TickCount;
			var expire = now + Timeout;
			var cnt = 0;

			while ((maxReads < 0 || cnt < maxReads) && now <= expire)
			{
				var remain = expire - now;

				var fds = new List<Socket> { s };
				Socket.Select(fds, null, null, (remain) * 1000);

				if (fds.Count == 0)
					return null;

				var len = _recvBuffer.Length;

				var ep = read(s, blockSize);

				now = Environment.TickCount;

				if (ep != null)
				{
					ret = ep;
					++cnt;
					expire = now + Timeout;

					// EOF
					if (_recvBuffer.Length == len)
						break;
				}
			}

			return ret;
		}

		private IPEndPoint Accept(Socket s, int blockSize)
		{
			using (var client = _socket.Accept())
			{
				var remoteEp = (IPEndPoint)client.RemoteEndPoint;

				if (Host != null && !Host.Equals(remoteEp.Address))
				{
					try
					{
						client.Shutdown(SocketShutdown.Both);
					}
					// ReSharper disable once EmptyGeneralCatchClause
					catch
					{
					}

					return null;
				}

				try
				{
					// Indicate we have nothing to send
					client.Shutdown(SocketShutdown.Send);
				}
				// ReSharper disable once EmptyGeneralCatchClause
				catch
				{
				}

				// Read client data
				WaitForData(client, -1, TcpBlockSize, Recv);

				return remoteEp;
			}
		}

		private IPEndPoint Recv(Socket s, int blockSize)
		{
			_recvBuffer.Seek(blockSize - 1, SeekOrigin.Current);
			_recvBuffer.WriteByte(0);
			_recvBuffer.Seek(-blockSize, SeekOrigin.Current);

			var pos = (int)_recvBuffer.Position;
			var buf = _recvBuffer.GetBuffer();

			EndPoint remoteEp;
			int len;

			if (s.SocketType == SocketType.Stream)
			{
				remoteEp = new IPEndPoint(Host ?? IPAddress.Any, 0);
				len = s.Receive(buf, pos, blockSize, SocketFlags.None);
			}
			else
			{
				remoteEp = new IPEndPoint(_socket.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, 0);
				len = s.ReceiveFrom(buf, pos, blockSize, SocketFlags.None, ref remoteEp);
			}

			if (!_multicast && Host != null && !Host.Equals(((IPEndPoint)remoteEp).Address))
				return null;

			_recvBuffer.SetLength(_recvBuffer.Position + len);
			_recvBuffer.Seek(0, SeekOrigin.End);

			return (IPEndPoint)remoteEp;
		}

		private IPAddress GetLocalIp()
		{
			var local = Interface;

			if (local == null)
			{
				if (Host == null)
				{
					local = IPAddress.Any;
				}
				else if (Host.IsMulticast())
				{
					// Use INADDR_ANY for the local interface which causes the OS to find
					// the interface with the "best" multicast route
					if (Host.AddressFamily == AddressFamily.InterNetwork)
						return IPAddress.Any;

					return IPAddress.IPv6Any;
				}
				else
				{
					using (var s = new Socket(Host.AddressFamily, SocketType.Dgram, ProtocolType.Udp))
					{
						s.Connect(Host, 1);
						local = ((IPEndPoint)s.LocalEndPoint).Address;
					}
				}
			}

			return local;
		}

		#endregion
	}
}
