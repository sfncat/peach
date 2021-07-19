using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using Peach.Core;
using Peach.Core.IO;
using Peach.Pro.Core.OS;

namespace Peach.Pro.Core.Publishers
{
	public abstract class DatagramPublisher : Publisher
	{
		#region MTU Related Declarations

		// Max IP len is 65535, ensure we can fit that plus ip header plus ethernet header.
		// In order to account for Jumbograms which are > 65535, max MTU is double 65535
		// MinMTU is 1280 so that IPv6 info isn't lost if MTU is fuzzed

		public const string DefaultMinMTU = "1280";
		public const string DefaultMaxMTU = "131070"; // 65535 * 2

		#endregion

		public byte Protocol { get; set; }
		public IPAddress Interface { get; set; }
		public string Host { get; set; }
		public ushort Port { get; set; }
		public ushort SrcPort { get; set; }
		public int Timeout { get; set; }
		public uint MinMTU { get; set; }
		public uint MaxMTU { get; set; }

		/// <summary>
		/// Do not throw exceptions when reading from socket. This includes timeout
		/// exceptions.
		/// </summary>
		/// <remarks>
		/// This property can be set through publisher parameters, or via
		/// SetProperty and GetProperty.
		/// </remarks>
		public bool NoReadException { get; set; }

		const int MaxBufSize = 65000;

		private IPEndPoint _localEp;
		protected IPEndPoint _remoteEp;
		private IPEndPoint _lastRxEp;
		private IPEndPoint _bindEp;
		private readonly string _type;
		private NetworkInterface _iface;
		private string _ifaceName;
		private uint? _origMtu;
		private uint? _mtu;
		private readonly byte[] _rxBuf = new byte[MaxBufSize];
		private MemoryStream _stream = new MemoryStream();
		protected IDatagramImpl _impl;

		protected abstract bool IsAddressFamilySupported(AddressFamily af);
		protected abstract SocketType SocketType { get; }

		protected virtual bool IpHeaderInclude { get { return false; } }
		protected virtual void FilterInput(byte[] buffer, int offset, int count) { }
		protected virtual void FilterOutput(byte[] buffer, int offset, int count) { }

		// Needed for DtlsPublisher. Perhaps it will go away after we are done integrating.
		protected MemoryStream RecvBuffer { get { return _stream; } }

		// Exposed internal for unit testing
		internal event EventHandler Opened;
		internal IPEndPoint LocalEndPoint { get { return _localEp;  } }
		internal IPEndPoint RemoteEndPoint { get { return _remoteEp; } }

		protected DatagramPublisher(string type, Dictionary<string, Variant> args)
			: base(args)
		{
			_type = type;
			_impl = PlatformFactory<IDatagramImpl>.CreateInstance(_type);
		}

		private IPEndPoint ResolveHost()
		{
			var entries = Dns.GetHostAddresses(Host);
			foreach (var ip in entries)
			{
				if (ip.ToString() != Host)
					Logger.Debug("Resolved host \"{0}\" to \"{1}\".", Host, ip);

				if (Interface == null && ip.IsIPv6LinkLocal && ip.ScopeId == 0)
					throw new PeachException("IPv6 scope id required for resolving link local address: '{0}'.".Fmt(Host));

				return new IPEndPoint(ip, Port);
			}

			throw new PeachException("Could not resolve the IP address of host \"" + Host + "\".");
		}

		/// <summary>
		/// Resolves the ScopeId for a Link-Local IPv6 address
		/// </summary>
		/// <param name="ip"></param>
		/// <returns></returns>
		private static IPAddress GetScopeId(IPAddress ip)
		{
			if (!ip.IsIPv6LinkLocal || ip.ScopeId != 0)
				throw new ArgumentException("ip");

			var results = new List<Tuple<string, IPAddress>>();
			var nics = NetworkInterface.GetAllNetworkInterfaces();
			foreach (var adapter in nics)
			{
				foreach (var addr in adapter.GetIPProperties().UnicastAddresses)
				{
					if (!addr.Address.IsIPv6LinkLocal)
						continue;

					var candidate = new IPAddress(addr.Address.GetAddressBytes(), 0);
					if (Equals(candidate, ip))
					{
						results.Add(new Tuple<string, IPAddress>(adapter.Name, addr.Address));
					}
				}
			}

			if (results.Count == 0)
				throw new PeachException("Could not resolve scope id for interface with address '" + ip + "'.");

			if (results.Count != 1)
				throw new PeachException(string.Format("Found multiple interfaces with address '{0}'.{1}\t{2}",
					ip, Environment.NewLine,
					string.Join(Environment.NewLine + "\t", results.Select(a => a.Item1.ToString() + " -> " + a.Item2.ToString()))));

			return results[0].Item2;
		}

		/// <summary>
		/// Returns the local ip that should be used to talk to 'remote'
		/// </summary>
		/// <param name="remote"></param>
		/// <returns></returns>
		protected static IPAddress GetLocalIp(IPEndPoint remote)
		{
			using (var s = new Socket(remote.AddressFamily, SocketType.Dgram, ProtocolType.Udp))
			{
				try
				{
					s.Connect(remote.Address, 22);
				}
				catch (SocketException)
				{
					if (remote.Address.IsMulticast())
						return remote.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any;

					throw;
				}
				var local = s.LocalEndPoint as IPEndPoint;
				return local.Address;
			}
		}

		protected NetworkInterface GetInterface(IPAddress Ip)
		{
			if (Ip == null)
				throw new ArgumentNullException("Ip");

			foreach (var iface in NetworkInterface.GetAllNetworkInterfaces())
			{
				foreach (var ifaceIp in iface.GetIPProperties().UnicastAddresses)
				{
					if (Equals(ifaceIp.Address, Ip))
					{
						return iface;
					}
				}
			}

			throw new Exception("Unable to locate interface for IP: {0}".Fmt(Ip));
		}

		protected override void OnStart()
		{
			IPEndPoint ep;
			IPAddress local;
			string ifaceName;
			NetworkInterface iface = null;
			uint? mtu;

			try
			{
				ep = ResolveHost();

				if (!IsAddressFamilySupported(ep.AddressFamily))
					throw new PeachException(string.Format("The resolved IP '{0}' for host '{1}' is not compatible with the {2} publisher.", ep, Host, _type));

				local = Interface;
				if (Interface == null)
					local = GetLocalIp(ep);

				if (local.IsIPv6LinkLocal && local.ScopeId == 0)
				{
					local = GetScopeId(local);
					Logger.Trace("Resolved link-local interface IP for {0} socket to {1}.", _type, local);
				}

				try
				{
					if (IPAddress.Any.Equals(local) || IPAddress.IPv6Any.Equals(local))
					{
						ifaceName = local.ToString();
						mtu = null;
					}
					else
					{
						iface = GetInterface(local);
						if (iface == null)
							throw new PeachException("Could not resolve interface name for local IP '{0}'.".Fmt(local));

						ifaceName = iface.Name;

						try
						{
							using (var cfg = NetworkAdapter.CreateInstance(ifaceName))
							{
								mtu = cfg.MTU;
							}
						}
						catch (Exception ex)
						{
							var msg = ex.Message;
							if (ex is TypeInitializationException || ex is TargetInvocationException)
								msg = ex.InnerException.Message;

							mtu = null;
							Logger.Debug("Could not query the MTU of '{0}'. {1}", ifaceName, msg);
						}
					}
				}
				catch (Exception ex)
				{
					var msg = ex.Message;
					if (ex is TypeInitializationException || ex is TargetInvocationException)
						msg = ex.InnerException.Message;

					ifaceName = local.ToString();
					mtu = null;
					Logger.Debug("Could not resolve the interface name for address '{0}'. {1}", ifaceName, msg);
				}
			}
			catch (Exception ex)
			{
				Logger.Error("Unable to start {0} publisher for {1}:{2}. {3}.", _type, Host, Port, ex.Message);
				throw new SoftException(ex);
			}

			_remoteEp = ep;
			_bindEp = new IPEndPoint(local, SrcPort);
			_iface = iface;
			_ifaceName = ifaceName;
			_mtu = mtu;
			_origMtu = mtu;
		}

		protected override void OnStop()
		{
			if (_mtu != _origMtu)
			{
				using (var cfg = NetworkAdapter.CreateInstance(_ifaceName))
				{
					Logger.Debug("Restoring the MTU of '{0}' to {1}.", _ifaceName, _origMtu.HasValue ? _origMtu.ToString() : "<null>");
					cfg.MTU = _origMtu;
				}
			}

			_remoteEp = null;
			_bindEp = null;
			_ifaceName = null;
			_mtu = null;
			_origMtu = null;
		}

		protected override void OnOpen()
		{
			System.Diagnostics.Debug.Assert(_remoteEp != null);
			System.Diagnostics.Debug.Assert(_bindEp != null);
			System.Diagnostics.Debug.Assert(_ifaceName != null);

			try
			{
				// reset the port so that the next OnReceive in a new iteration will accept packets from a new port
				_remoteEp.Port = Port;

				_localEp = _impl.Open(
					SocketType, 
					Protocol, 
					IpHeaderInclude, 
					_bindEp, 
					_remoteEp, 
					_iface, 
					_ifaceName,
					MaxBufSize
				);
			}
			catch (Exception ex)
			{
				var se = ex as SocketException;
				if (se != null && se.SocketErrorCode == SocketError.AccessDenied)
					throw new PeachException(string.Format("Access denied when trying open a {0} socket.  Ensure the user has the appropriate permissions.", _type), ex);

				Logger.Error("Unable to open {0} socket to {1}:{2}. {3}.", _type, Host, Port, ex.Message);

				throw new SoftException(ex);
			}

			// Update port if port 0 was passed
			SrcPort = (ushort)_localEp.Port;

			Logger.Trace("Opened {0} socket, Local: {1}, Remote: {2}", _type, _localEp, _remoteEp);

			if (Opened != null)
				Opened(this, EventArgs.Empty);
		}

		protected override void OnClose()
		{
			_impl.Close();
		}

		protected override void OnInput()
		{
			var len = 0;
			try
			{
				_lastRxEp = _impl.Receive(_remoteEp, _rxBuf, out len, Timeout);
			}
			catch (Exception ex)
			{
				if (ex is TimeoutException)
				{
					Logger.Debug("{0} packet not received from {1}:{2} in {3}ms, timing out.",
						_type, Host, Port, Timeout);
				}
				else
				{
					Logger.Error("Unable to receive {0} packet from {1}:{2}. {3}",
						_type, Host, Port, ex.Message);
				}

				if (!NoReadException)
					throw new SoftException(ex);
			}
			finally 
			{
				// Ensure user always sees an empty stream in the case of an error!
				_stream = new MemoryStream(_rxBuf, 0, len, false);
			}

			FilterInput(_rxBuf, 0, len);

			if (Logger.IsDebugEnabled)
				Logger.Debug("\n\n" + Utilities.HexDump(_rxBuf, 0, len));
		}

		protected override void OnOutput(BitwiseStream data)
		{
			if (Logger.IsDebugEnabled)
				Logger.Debug("\n\n" + Utilities.HexDump(data));

			var buf = new byte[MaxBufSize];
			var len = data.Read(buf, 0, buf.Length);

			FilterOutput(buf, 0, len);

			try
			{
				var sent = _impl.Send(_remoteEp, buf, len, Timeout);
				if (sent != data.Length)
					throw new Exception(string.Format("Only sent {0} of {1} byte {2} packet.", sent, data.Length, _type));
			}
			catch (Exception ex)
			{
				if (ex is TimeoutException)
				{
					Logger.Debug("{0} packet not sent to {1}:{2} in {3}ms, timing out.",
						_type, Host, Port, Timeout);
				}
				else
				{
					Logger.Error("Unable to send {0} packet to {1}:{2}. {3}",
						_type, Host, Port, ex.Message);
				}

				throw new SoftException(ex);
			}
		}

		protected override Variant OnGetProperty(string property)
		{
			if (property == "MTU")
			{
				if (_mtu == null)
				{
					Logger.Debug("MTU of '{0}' is unknown.", _ifaceName);
					return null;
				}

				Logger.Debug("MTU of '{0}' is {1}.", _ifaceName, _mtu);
				return new Variant(_mtu.Value);
			}

			if (property == "LastRecvAddr")
			{
				if (_lastRxEp == null)
					return new Variant(new BitStream());
				return new Variant(new BitStream(_lastRxEp.Address.GetAddressBytes()));
			}

			if (property == "Timeout")
				return new Variant(Timeout);

			if (property == "NoReadException")
				return new Variant(NoReadException ? "True" : "False");

			return null;
		}

		protected override void OnSetProperty(string property, Variant value)
		{
			if (property == "MTU")
			{
				uint mtu;

				switch (value.GetVariantType())
				{
					case Variant.VariantType.BitStream:
						{
							var bs = (BitwiseStream)value;
							bs.SeekBits(0, SeekOrigin.Begin);
							ulong bits;
							var len = bs.ReadBits(out bits, 32);
							mtu = Endian.Little.GetUInt32(bits, len);
						}
						break;
					case Variant.VariantType.ByteString:
						{
							var buf = (byte[])value;
							var len = Math.Min(buf.Length * 8, 32);
							mtu = Endian.Little.GetUInt32(buf, len);
						}
						break;
					default:
						throw new SoftException("Can't set MTU, 'value' is an unsupported type.");
				}

				if (MaxMTU >= mtu && mtu >= MinMTU)
				{
					using (var cfg = NetworkAdapter.CreateInstance(_ifaceName))
					{
						try
						{
							cfg.MTU = mtu;
						}
						catch (Exception ex)
						{
							var msg = ex.Message;
							if (ex is TypeInitializationException || ex is TargetInvocationException)
								msg = ex.InnerException.Message;

							var err = "Failed to change MTU of '{0}' to {1}. {2}".Fmt(_ifaceName, mtu, msg);
							Logger.Error(err);
							var se = new SoftException(err, ex);
							throw new SoftException(se);
						}

						_mtu = cfg.MTU;

						if (!_mtu.HasValue || _mtu.Value != mtu)
						{
							var err = "Failed to change MTU of '{0}' to {1}. The change did not take effect.".Fmt(_ifaceName, mtu);
							Logger.Error(err);
							throw new SoftException(err);
						}
						Logger.Debug("Changed MTU of '{0}' to {1}.", _ifaceName, mtu);
					}
				}
				else
				{
					Logger.Debug("Not setting MTU of '{0}', value is out of range.", _ifaceName);
				}
			}
			else if (property == "Timeout")
			{
				switch (value.GetVariantType())
				{
					case Variant.VariantType.BitStream:
						{
							var bs = (BitwiseStream)value;
							bs.SeekBits(0, SeekOrigin.Begin);
							ulong bits;
							var len = bs.ReadBits(out bits, 32);
							Timeout = Endian.Little.GetInt32(bits, len);
						}
						break;
					case Variant.VariantType.ByteString:
						{
							var buf = (byte[])value;
							var len = Math.Min(buf.Length * 8, 32);
							Timeout = Endian.Little.GetInt32(buf, len);
						}
						break;
					default:
						try
						{
							Timeout = (int)value;
						}
						catch
						{
							throw new SoftException("Can't set Timeout, 'value' is an unsupported type.");
						}
						break;
				}
			}
			else if (property == "NoReadException")
			{
				string val;
				switch (value.GetVariantType())
				{
					case Variant.VariantType.BitStream:
						var stream = (BitwiseStream)value;
						stream.Position = 0;
						var buff = new byte[stream.Length];
						for (var cnt = 0; cnt < stream.Length; cnt++)
							buff[cnt] = (byte)stream.ReadByte();

						val = Encoding.UTF8.GetString(buff);
						break;
					case Variant.VariantType.ByteString:
						val = Encoding.UTF8.GetString((byte[])value);
						break;
					default:
						try
						{
							val = (string)value;
						}
						catch
						{
							throw new SoftException("Can't set NoReadException, 'value' is an unsupported type.");
						}
						break;
				}

				NoReadException = (val.ToLower() == "true");
			}
			else
			{
				throw new SoftException("Unknown property '" + property + "'.");
			}
		}

		#region Read Stream

		public override bool CanRead
		{
			get { return _stream.CanRead; }
		}

		public override bool CanSeek
		{
			get { return _stream.CanSeek; }
		}

		public override long Length
		{
			get { return _stream.Length; }
		}

		public override long Position
		{
			get { return _stream.Position; }
			set { _stream.Position = value; }
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return _stream.Seek(offset, origin);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _stream.Read(buffer, offset, count);
		}

		#endregion
	}
}
