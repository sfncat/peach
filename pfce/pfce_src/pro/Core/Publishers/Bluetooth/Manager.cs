using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NDesk.DBus;

namespace Peach.Pro.Core.Publishers.Bluetooth
{
	public class Manager : IDisposable
	{
		private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		private readonly object _mutex = new object();

		private readonly Bus _bus;
		private readonly ObjectManager _mgr;

		private Adapter _adapter;
		private Device _device;

		public Manager()
		{
			_bus = Bus.Open(Address.System);

			_mgr = _bus.GetObject<ObjectManager>(ObjectPath.Root);

			_mgr.InterfacesAdded += InterfacesAdded;
			_mgr.IntefacesRemoved += IntefacesRemoved;
		}

		public string Adapter { get; set; }
		public string Device { get; set; }

		public int DiscoverTimeout { get; set; } = 10000;
		public int ConnectTimeout { get; set; } = 10000;
		public int PairTimeout { get; set; } = 10000;

		public ByUuid<RemoteService> RemoteServices
		{
			get { return _device.Services; }
		}

		private void IntefacesRemoved(ObjectPath path, string[] interfaces)
		{
			lock (_mutex)
			{
				Logger.Trace("InterfacesRemoved> {0}", path);
				Monitor.Pulse(_mutex);
			}
		}

		private void InterfacesAdded(ObjectPath path, IDictionary<string, IDictionary<string, object>> interfaces)
		{
			lock (_mutex)
			{
				Logger.Trace("InterfacesAdded> {0}", path);
				Monitor.Pulse(_mutex);
			}
		}

		public void Dump()
		{
			var objs = _mgr.GetManagedObjects();

			foreach (var kv in objs)
			{
				// kv.Key = Path
				// kv.Value = Interface Dictionary

				foreach (var item in kv.Value)
				{
					// item.Key = Interface
					// item.Value = Property Dictionary

					Logger.Debug("Dump> {0} ({1})", kv.Key, item.Key);

					foreach (var props in item.Value)
						Logger.Debug("Dump>    {0}={1}", props.Key, props.Value);
				}
			}
		}

		public void Open()
		{
			var adapterPath = FindAdapter();

			Logger.Debug("Open> Found Adapter: {0}", adapterPath);

			_adapter = new Adapter(_bus, adapterPath);

			Logger.Trace(_adapter.Introspect());

			foreach (var kv in _adapter.Properties)
			{
				Logger.Trace("  {0}={1}", kv.Key, kv.Value);
			}
		}

		public void Connect(bool pair, bool trust)
		{
			var devicePath = FindDevice(_adapter.Path);

			if (devicePath == null)
			{
				Logger.Debug("Connect> Discovering...");

				lock (_mutex)
				{
					_adapter.StartDiscovery();

					var sw = Stopwatch.StartNew();
					var remain = DiscoverTimeout;

					while (remain >= 0)
					{
						if (!Monitor.Wait(_mutex, remain))
							break;

						devicePath = FindDevice(_adapter.Path);
						if (devicePath != null)
							break;

						remain = DiscoverTimeout - (int)sw.ElapsedMilliseconds;
					}

					_adapter.StopDiscovery();
				}

				if (devicePath == null)
					throw new ApplicationException(string.Format("Couldn't locate device {0}", Device));
			}

			Logger.Debug("Connect> Found Device: {0}", devicePath);

			_device = new Device(_bus, devicePath);

			//Logger.Debug(_device.Introspect());

			{
				var sw = Stopwatch.StartNew();
				var remain = ConnectTimeout;

				if (!_device.Connected)
				{
					Logger.Debug("Connect> Discovering...");

					_device.Connect();

					while (remain >= 0)
					{
						Thread.Sleep(Math.Min(remain, 100));

						if (_device.Connected)
							break;

						remain = ConnectTimeout - (int)sw.ElapsedMilliseconds;
					}

					if (!_device.Connected)
						throw new ApplicationException(string.Format("Timed out connecting to device {0}", Device));
				}

				if (!_device.ServicesResolved)
				{
					while (remain >= 0)
					{
						Thread.Sleep(Math.Min(remain, 100));

						if (_device.ServicesResolved)
							break;

						remain = ConnectTimeout - (int)sw.ElapsedMilliseconds;
					}

					if (!_device.ServicesResolved)
						throw new ApplicationException(string.Format("Timed out resolving services for device {0}", Device));
				}
			}

			if (trust && !_device.Trusted)
			{
				Logger.Debug("Connect> Trusting...");

				_device.Trusted = true;
			}

			if (pair && !_device.Paired)
			{
				Logger.Debug("Connect> Pairing...");

				var sw = Stopwatch.StartNew();
				var remain = PairTimeout;

				_device.Pair();

				while (remain >= 0)
				{
					Thread.Sleep(Math.Min(remain, 100));

					if (_device.Paired)
						break;

					remain = PairTimeout - (int)sw.ElapsedMilliseconds;
				}

				if (!_device.Paired)
				{
					_device.CancelPairing();
					throw new ApplicationException(string.Format("Timed out pairing to device {0}", Device));
				}
			}

			foreach (var kv in _device.Properties)
			{
				Logger.Trace("  {0}={1}", kv.Key, kv.Value);
			}

			Logger.Debug("Connect> Discovering services...");

			foreach (var svc in EnumerateServices(_device.Path))
			{
				_device.Services.Add(svc);

				Logger.Trace("  Service: {0}, Primary: {1}", svc.UUID, svc.Primary);

				foreach (var c in svc.Characteristics)
				{
					var v = c.Flags.Contains("read")
						? ", Value: " + TryReadValue(c)
						: "";

					Logger.Trace("    Char: {0}, Flags: {1}{2}", c.UUID, string.Join("|", c.Flags), v);

					foreach (var d in c.Descriptors)
					{
						var v2 = c.Flags.Contains("read")
							? ", Value: " + string.Join("", d.ReadValue(new Dictionary<string, object>()).Select(x => x.ToString("X2")))
							: "";

						Logger.Trace("     Descriptor: {0}{1}", d.UUID, v2);
					}
				}
			}
		}

		string TryReadValue(ICharacteristic c)
		{
			try
			{
				return string.Join("", c.ReadValue(new Dictionary<string, object>()).Select(x => x.ToString("X2")));
			}
			catch (Exception ex)
			{
				return ex.Message;
			}
		}

		public void Iterate()
		{
			_bus.Iterate();
		}

		public void Serve(GattApplication app)
		{
			_adapter.Powered = false;
			_adapter.Powered = true;

			_bus.Register(app.Path, app);

			var svcNum = 0;
			foreach (var svc in app.Services)
			{
				svc.Path = new ObjectPath(string.Format("{0}/service{1}", app.Path, svcNum++));

				_bus.Register(svc.Path, svc);

				var chrNum = 0;
				foreach (var chr in svc.Characteristics)
				{
					chr.Service = svc.Path;
					chr.Path = new ObjectPath(string.Format("{0}/char{1}", svc.Path, chrNum++));

					_bus.Register(chr.Path, chr);

					var dscNum = 0;
					foreach (var dsc in chr.Descriptors)
					{
						dsc.Characteristic = chr.Path;
						dsc.Path = new ObjectPath(string.Format("{0}/descriptor{1}", chr.Path, dscNum++));
						_bus.Register(dsc.Path, dsc);
					}
				}
			}

			Logger.Debug("AdvertisingManager> Active: {0}, Supported: {1}, Included: {2}",
				_adapter.ActiveInstances, _adapter.SupportedInstances, string.Join(",", _adapter.SupportedIncludes));

			Logger.Debug("Serve> Registering application: {0}", app.Path);

			_adapter.RegisterApplication(app.Path, new Dictionary<string, object>());

			app.Advertisement.Path = new ObjectPath(string.Format("{0}/advertisement", app.Path));
			app.Advertisement.ServiceUUIDs = app.Services.Where(x => x.Advertise).Select(x => x.UUID).ToArray();
			app.Advertisement.LocalName = "peach";
			app.Advertisement.IncludeTxPower = true;
			app.Advertisement.Type = "peripheral";

			_bus.Register(app.Advertisement.Path, app.Advertisement);

			Logger.Debug("Serve> Registering advertisements: {0}", app.Advertisement.Path);

			_adapter.RegisterAdvertisement(app.Advertisement.Path, new Dictionary<string, object>());

			_adapter.DiscoverableTimeout = 0;
			_adapter.Discoverable = true;

		}

		private IEnumerable<RemoteService> EnumerateServices(ObjectPath devicePath)
		{
			var objs = _mgr.GetManagedObjects();

			foreach (var svcPath in objs.Iter<IService>(p => devicePath.Equals(p["Device"])))
			{
				var svc = new RemoteService(_bus, svcPath);

				foreach (var chrPath in objs.Iter<ICharacteristic>(p => svcPath.Equals(p["Service"])))
				{
					var chr = new RemoteCharacteristic(_bus, chrPath);

					foreach (var dscPath in objs.Iter<IDescriptor>(p => chrPath.Equals(p["Characteristic"])))
					{
						chr.Descriptors.Add(new RemoteDescriptor(_bus, dscPath));
					}

					svc.Characteristics.Add(chr);
				}

				yield return svc;
			}
		}

		private ObjectPath FindAdapter()
		{
			var iface = typeof(IAdapter).GetCustomAttributes(false).OfType<InterfaceAttribute>().First();
			var objs = _mgr.GetManagedObjects();

			ObjectPath ret = null;

			foreach (var kv in objs)
			{
				// kv.Key = Path
				// kv.Value = Interface Dictionary

				foreach (var item in kv.Value)
				{
					// item.Key = Interface
					// item.Value = Property Dictionary

					if (item.Key != iface.Name)
						continue;

					//Logger.Trace("FindAdapter: {0} {1}", kv.Key, item.Key);

					//foreach (var props in item.Value)
					//	Logger.Trace("  {0}={1}", props.Key, props.Value);

					if (AddressMatch(item.Value["Address"]) || IfaceMatch(kv.Key))
						ret = kv.Key;
				}
			}

			return ret;
		}

		private ObjectPath FindDevice(ObjectPath adapter)
		{
			var iface = typeof(IDevice).GetCustomAttributes(false).OfType<InterfaceAttribute>().First();
			var objs = _mgr.GetManagedObjects();

			ObjectPath ret = null;

			foreach (var kv in objs)
			{
				// kv.Key = Path
				// kv.Value = Interface Dictionary

				foreach (var item in kv.Value)
				{
					// item.Key = Interface
					// item.Value = Property Dictionary

					if (item.Key != iface.Name)
						continue;

					//Logger.Trace("FindDevice: {0} {1}", kv.Key, item.Key);

					//foreach (var props in item.Value)
					//	Logger.Trace("  {0}={1}", props.Key, props.Value);

					if (DeviceMatch(item.Value["Address"]) && item.Value["Adapter"].ToString() == adapter.ToString())
						ret = kv.Key;
				}
			}

			return ret;
		}

		private bool IfaceMatch(ObjectPath path)
		{
			var asStr = path.ToString();
			var lastSeg = asStr.Substring(asStr.LastIndexOf('/') + 1);
			return 0 == string.Compare(lastSeg, Adapter, StringComparison.OrdinalIgnoreCase);
		}

		private bool DeviceMatch(object addr)
		{
			return 0 == string.Compare(addr.ToString(), Device, StringComparison.OrdinalIgnoreCase);
		}

		private bool AddressMatch(object addr)
		{
			return 0 == string.Compare(addr.ToString(), Adapter, StringComparison.OrdinalIgnoreCase);
		}

		public void Dispose()
		{
			_bus.Close();
		}
	}
}
