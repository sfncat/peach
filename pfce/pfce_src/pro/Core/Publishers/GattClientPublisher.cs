using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NLog;
using Peach.Core;
using Peach.Core.IO;
using Peach.Pro.Core.Publishers.Bluetooth;

namespace Peach.Pro.Core.Publishers
{
	[Publisher("GattClient")]
	[Parameter("Adapter", typeof(string), "Local adapter name (eg: hci0)")]
	[Parameter("Device", typeof(string), "Remote device address (eg: 01:02:03:04:05:06)")]
	[Parameter("DiscoverTimeout", typeof(int), "How long to wait when discovering", "10000")]
	[Parameter("ConnectTimeout", typeof(int), "How long to wait when connecting", "10000")]
	[Parameter("PairTimeout", typeof(int), "How long to wait when pairing", "10000")]
	[Parameter("NotifyTimeout", typeof(int), "How long to wait for incoming notification", "1000")]
	[Parameter("Pair", typeof(bool), "Pair with device", "false")]
	[Parameter("Trust", typeof(bool), "Device is trusted", "false")]
	public class GattClientPublisher : Publisher
	{
		// BLE max payload is 33 bytes, so attribute data has a max of 20 bytes
		private const int Mtu = 20;

		private static readonly NLog.Logger ClassLogger = LogManager.GetCurrentClassLogger();
		private readonly object _mutex;
		private Thread _thread;
		private Manager _mgr;
		private bool _lastWasError;

		protected override NLog.Logger Logger
		{
			get { return ClassLogger; }
		}

		public string Adapter { get; set; }
		public string Device { get; set; }
		public int DiscoverTimeout { get; set; }
		public int ConnectTimeout { get; set; }
		public int PairTimeout { get; set; }
		public int NotifyTimeout { get; set; }
		public bool Pair { get; set; }
		public bool Trust { get; set; }

		public GattClientPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
			_mutex = new object();
		}

		protected override void OnOpen()
		{
			if (_mgr != null)
				return;

			var mgr = new Manager
			{
				Adapter = Adapter,
				Device = Device,
				ConnectTimeout = ConnectTimeout,
				DiscoverTimeout = DiscoverTimeout,
				PairTimeout = PairTimeout
			};

			try
			{
				mgr.Open();
			}
			catch (Exception ex)
			{
				mgr.Dispose();
				throw new SoftException(ex);
			}

			try
			{
				mgr.Connect(Pair, Trust);
			}
			catch (Exception ex)
			{
				mgr.Dispose();
				throw new SoftException(ex);
			}

			_mgr = mgr;

			_thread = new Thread(IterateThread);
			_thread.Start();
		}

		protected override Variant OnCall(string method, List<BitwiseStream> args)
		{
			// The state model doesn't require publishers to be open when
			// performing call actions, but we want to track errors on a
			// per iteration basis so ensure we are opened here

			open();

			switch (method)
			{
				case "writeCharacteristic":
					return WriteCharacteristic(args);
				case "readCharacteristic":
					return ReadCharacteristic(args);
				case "writeDescriptor":
					return WriteDescriptor(args);
				case "readDescriptor":
					return ReadDescriptor(args);
				case "getNotification":
					return GetNotification(args);
				default:
					throw new PeachException("Error, method '{0}' not supported by GattClient publisher".Fmt(method));
			}
		}

		protected override void OnClose()
		{
			// If there was an error on the iteration
			// close everything up and try again

			if (_lastWasError)
			{
				lock (_mutex)
				{
					_mgr.Dispose();
					_mgr = null;
				}

				_thread.Join();
				_thread = null;
				_lastWasError = false;
			}
		}

		protected override void OnStop()
		{
			if (_mgr != null)
			{
				lock (_mutex)
				{
					_mgr.Dispose();
					_mgr = null;
				}

				_thread.Join();
				_thread = null;
			}
		}

		private Variant WriteCharacteristic(List<BitwiseStream> args)
		{
			var svcUuid = GetUuid(args, 0, "service");
			var chrUuid = GetUuid(args, 1, "characteristic");
			var value = GetBuf(args, 2);

			RemoteService svc;
			if (!_mgr.RemoteServices.TryGetValue(svcUuid, out svc))
				throw new PeachException("Couldn't resolve service {0}".Fmt(svcUuid));

			RemoteCharacteristic chr;
			if (!svc.Characteristics.TryGetValue(chrUuid, out chr))
				throw new PeachException("Couldn't resolve characteristic {0}".Fmt(chrUuid));

			try
			{
				Logger.Debug("WriteCharacteristic> Svc: {0}, Chr: {1}, Data: {2}",
					svcUuid, chrUuid, string.Join("", value.Select(x => x.ToString("X2"))));

				chr.WriteValue(value, new Dictionary<string, object>());
			}
			catch (Exception ex)
			{
				_lastWasError = true;
				throw new SoftException(ex);
			}

			return new Variant(new byte[0]);
		}

		private Variant ReadCharacteristic(List<BitwiseStream> args)
		{
			var svcUuid = GetUuid(args, 0, "service");
			var chrUuid = GetUuid(args, 1, "characteristic");

			RemoteService svc;
			if (!_mgr.RemoteServices.TryGetValue(svcUuid, out svc))
				throw new PeachException("Couldn't resolve service {0}".Fmt(svcUuid));

			RemoteCharacteristic chr;
			if (!svc.Characteristics.TryGetValue(chrUuid, out chr))
				throw new PeachException("Couldn't resolve characteristic {0}".Fmt(chrUuid));

			try
			{
				Logger.Debug("ReadCharacteristic> Svc: {0}, Chr: {1}", svcUuid, chrUuid);

				return new Variant(chr.ReadValue(new Dictionary<string, object>()));
			}
			catch (Exception ex)
			{
				_lastWasError = true;
				throw new SoftException(ex);
			}
		}

		private Variant GetNotification(List<BitwiseStream> args)
		{
			var svcUuid = GetUuid(args, 0, "service");
			var chrUuid = GetUuid(args, 1, "characteristic");

			RemoteService svc;
			if (!_mgr.RemoteServices.TryGetValue(svcUuid, out svc))
				throw new PeachException("Couldn't resolve service {0}".Fmt(svcUuid));

			RemoteCharacteristic chr;
			if (!svc.Characteristics.TryGetValue(chrUuid, out chr))
				throw new PeachException("Couldn't resolve characteristic {0}".Fmt(chrUuid));

			byte[] ret;

			try
			{
				Logger.Debug("GetNotification> Svc: {0}, Chr: {1}", svcUuid, chrUuid);
				ret = chr.GetNotification(NotifyTimeout);
			}
			catch (Exception ex)
			{
				_lastWasError = true;
				throw new SoftException(ex);
			}

			if (ret == null)
				throw new SoftException("Timed out waiting for incoming notification");

			return new Variant(ret);
		}

		private Variant WriteDescriptor(List<BitwiseStream> args)
		{
			var svcUuid = GetUuid(args, 0, "service");
			var chrUuid = GetUuid(args, 1, "characteristic");
			var dscUuid = GetUuid(args, 2, "descriptor");
			var value = GetBuf(args, 3);

			RemoteService svc;
			if (!_mgr.RemoteServices.TryGetValue(svcUuid, out svc))
				throw new PeachException("Couldn't resolve service {0}".Fmt(svcUuid));

			RemoteCharacteristic chr;
			if (!svc.Characteristics.TryGetValue(chrUuid, out chr))
				throw new PeachException("Couldn't resolve characteristic {0}".Fmt(chrUuid));

			RemoteDescriptor dsc;
			if (!chr.Descriptors.TryGetValue(dscUuid, out dsc))
				throw new PeachException("Couldn't resolve descriptor {0}".Fmt(dscUuid));

			try
			{
				Logger.Debug("WriteDescriptor> Svc: {0}, Chr: {1}, Dsc: {2}, Data: {3}",
					svcUuid, chrUuid, dscUuid, string.Join("", value.Select(x => x.ToString("X2"))));

				dsc.WriteValue(value, new Dictionary<string, object>());
			}
			catch (Exception ex)
			{
				_lastWasError = true;
				throw new SoftException(ex);
			}

			return new Variant(new byte[0]);
		}

		private Variant ReadDescriptor(List<BitwiseStream> args)
		{
			var svcUuid = GetUuid(args, 0, "service");
			var chrUuid = GetUuid(args, 1, "characteristic");
			var dscUuid = GetUuid(args, 2, "descriptor");

			RemoteService svc;
			if (!_mgr.RemoteServices.TryGetValue(svcUuid, out svc))
				throw new PeachException("Couldn't resolve service {0}".Fmt(svcUuid));

			RemoteCharacteristic chr;
			if (!svc.Characteristics.TryGetValue(chrUuid, out chr))
				throw new PeachException("Couldn't resolve characteristic {0}".Fmt(chrUuid));

			RemoteDescriptor dsc;
			if (!chr.Descriptors.TryGetValue(dscUuid, out dsc))
				throw new PeachException("Couldn't resolve descriptor {0}".Fmt(dscUuid));

			try
			{
				Logger.Debug("ReadDescriptor> Svc: {0}, Chr: {1}, Dsc: {2}", svcUuid, chrUuid, dscUuid);
				return new Variant(dsc.ReadValue(new Dictionary<string, object>()));
			}
			catch
			{
				_lastWasError = true;
				throw;
			}
		}

		private byte[] GetBuf(List<BitwiseStream> args, int index)
		{
			var bs = args[index];
			bs.Seek(0, SeekOrigin.Begin);
			var len = Math.Min(Mtu, bs.Length);
			var buf = new byte[len];
			bs.Read(buf, 0, buf.Length);
			return buf;
		}

		private Guid GetUuid(List<BitwiseStream> args, int index, string type)
		{
			if (args.Count <= index)
				throw new PeachException("Missing {0} UUID at parameter {1}".Fmt(type, index));

			var bs = args[index];
			bs.Seek(0, SeekOrigin.Begin);
			var asStr = new BitReader(bs).ReadString();

			Guid guid;
			if (!Guid.TryParse(asStr, out guid))
				throw new PeachException("Invalid {0} UUID '{1}' at parameter {2}".Fmt(type, asStr, index));

			return guid;
		}

		private void IterateThread()
		{
			Logger.Trace("IterateThread> Begin");

			try
			{
				while (true)
				{
					Manager mgr;

					lock (_mutex)
					{
						mgr = _mgr;
					}

					if (mgr == null)
						break;

					mgr.Iterate();
				}
			}
			catch (Exception ex)
			{
				Logger.Trace(ex);
			}

			Logger.Trace("IterateThread> End");
		}
	}
}
 