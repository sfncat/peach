using System;
using Peach.Core;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;
using System.Diagnostics;
using NLog;

namespace Peach.Pro.Core.OS.Windows
{
	[PlatformImpl(Platform.OS.Windows)]
	public class DatagramImpl : IDatagramImpl
	{
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		private readonly string _publisher;
		private EndPoint _pendingEp;
		private IAsyncResult _pendingRx;
		private Socket _socket;

		public DatagramImpl(string publisher)
		{
			_publisher = publisher;
		}

		#region IDatagramImpl implementation

		public IPEndPoint Open(
			SocketType socketType,
			byte protocol,
			bool ipHeaderInclude,
			IPEndPoint localEp, 
			IPEndPoint remoteEp, 
			NetworkInterface iface, 
			string ifaceName,
			int bufSize)
		{
			Debug.Assert(_socket == null);

			var protocolType = GetProtocolType(protocol);
			// https://msdn.microsoft.com/en-us/library/windows/desktop/ms740548%28v=vs.85%29.aspx
			if (protocolType == ProtocolType.Tcp)
				throw new PeachException("The {0} publisher does not support the TCP protocol on windows.".Fmt(_publisher));

			_socket = new Socket(remoteEp.AddressFamily, socketType, protocolType);

			if (ipHeaderInclude)
				_socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);
			
			if (remoteEp.Address.IsMulticast())
			{
				// Multicast needs to bind to INADDR_ANY on windows
				if (remoteEp.AddressFamily == AddressFamily.InterNetwork)
				{
					_socket.Bind(new IPEndPoint(IPAddress.Any, localEp.Port));

					var opt = new MulticastOption(remoteEp.Address, localEp.Address);
					_socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, opt);

					if (!Equals(localEp.Address, IPAddress.Any))
					{
						Logger.Trace("Setting multicast interface for {0} socket to {1}.", _publisher, localEp.Address);
						_socket.SetSocketOption(
							SocketOptionLevel.IP, 
							SocketOptionName.MulticastInterface, 
							localEp.Address.GetAddressBytes()
						);
					}
				}
				else
				{
					_socket.Bind(new IPEndPoint(IPAddress.IPv6Any, localEp.Port));

					if (!Equals(localEp.Address, IPAddress.IPv6Any))
					{
						var opt = new IPv6MulticastOption(remoteEp.Address, localEp.Address.ScopeId);
						_socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, opt);
					}
					else
					{
						var opt = new IPv6MulticastOption(remoteEp.Address);
						_socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, opt);
					}
				}
			}
			else
			{
				Logger.Debug("Binding to {0}", localEp);
				_socket.Bind(localEp);
			}

			_socket.ReceiveBufferSize = bufSize;
			_socket.SendBufferSize = bufSize;

			return _socket.LocalEndPoint as IPEndPoint;
		}

		public void Close()
		{
			Debug.Assert(_socket != null);
			_socket.Close();

			if (_pendingRx != null)
			{
				try
				{
					_socket.EndReceiveFrom(_pendingRx, ref _pendingEp);
				}
				catch (Exception ex)
				{
					// ignored
					Logger.Trace("OnClose> Exception: {0}", ex.Message);
				}
			}

			_pendingRx = null;
			_pendingEp = null;
			_socket = null;
		}

		public int Send(IPEndPoint remoteEp, byte[] buf, int len, int timeout)
		{
			Debug.Assert(_socket != null);

			var ar = _socket.BeginSendTo(buf, 0, len, SocketFlags.None, remoteEp, null, null);
			if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeout)))
				throw new TimeoutException();

			return _socket.EndSendTo(ar);
		}

		public IPEndPoint Receive(IPEndPoint expected, byte[] buf, out int len, int timeout)
		{
			Debug.Assert(_socket != null);

			int expires = Environment.TickCount + timeout;

			for (;;)
			{
				var wait = Math.Max(0, expires - Environment.TickCount);

				try
				{
					if (_pendingRx == null)
					{
						var addr = _socket.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any;
						_pendingEp = new IPEndPoint(addr, 0);

						_pendingRx = _socket.BeginReceiveFrom(
							buf,
							0,
							buf.Length,
							SocketFlags.None,
							ref _pendingEp,
							null,
							null);
					}

					if (!_pendingRx.AsyncWaitHandle.WaitOne(wait))
						throw new TimeoutException();

					try
					{
						len = _socket.EndReceiveFrom(_pendingRx, ref _pendingEp);
					}
					finally
					{
						_pendingRx = null;
					}

					var actual = (IPEndPoint)_pendingEp;

					if (!expected.Address.IsMulticast())
					{
						if (expected.Port == 0)
						{
							if (!Equals(expected.Address, actual.Address))
							{
								Logger.Debug("Ignoring received packet from {0}, want packets from {1}.", actual, expected);
								continue;
							}

							if (actual.Port != 0)
							{
								Logger.Debug("Updating expected remote address from {0} to {1}.", expected, actual);
								expected.Port = actual.Port;
							}
						}
						else if (!Equals(actual, expected))
						{
							Logger.Debug("Ignoring received packet from {0}, want packets from {1}.", actual, expected);
							continue;
						}
					}

					// Got a valid packet
					return actual;
				}
				catch (SocketException ex)
				{
					if (ex.SocketErrorCode == SocketError.ConnectionRefused ||
					    ex.SocketErrorCode == SocketError.ConnectionReset)
					{
						// Eat Connection reset by peer errors
						Logger.Debug("Connection reset by peer.  Ignoring...");
						continue;
					}

					throw;
				}
			}
		}

		#endregion

		private ProtocolType GetProtocolType(byte protocol)
		{
			switch (protocol)
			{
			case 0:   // Dummy Protocol
			case 1:   // Internet Control Message Protocol
			case 2:   // Internet Group Management Protocol
			case 4:   // IPIP tunnels
			case 6:   // Transmission Control Protocol
			case 12:  // PUP protocol
			case 17:  // User Datagram Protocol
			case 22:  // XNS IDP Protocol
			case 41:  // IPv6-in-IPv4 tunneling
			case 43:  // IPv6 routing header
			case 44:  // IPv6 fragmentation header
			case 50:  // Encapsulation Security PAyload protocol
			case 51:  // Authentication Header protocol
			case 58:  // ICMPv6
			case 59:  // IPv6 no next header
			case 60:  // IPv6 destination options
			case 255: // Raw IP packets
				return (ProtocolType)protocol;
			default:
				throw new PeachException("Error, the {0} publisher does not support protocol type 0x{1:X2}.".Fmt(_publisher, protocol));
			}
		}
	}
}
