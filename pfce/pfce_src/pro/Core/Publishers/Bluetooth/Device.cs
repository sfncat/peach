using System;
using System.Collections.Generic;
using System.Linq;
using NDesk.DBus;
using org.freedesktop.DBus;

namespace Peach.Pro.Core.Publishers.Bluetooth
{
	public class Device : IDevice
	{
		private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		private static readonly InterfaceAttribute Attr =
			typeof(IDevice).GetCustomAttributes(false).OfType<InterfaceAttribute>().First();

		private readonly IDevice _dev;
		private readonly Properties _props;
		private readonly Introspectable _info;

		public Device(Bus bus, ObjectPath path)
		{
			_dev = bus.GetObject<IDevice>(path);
			_props = bus.GetObject<Properties>(path);
			_props.PropertiesChanged += OnPropertiesChanged;
			_info = bus.GetObject<Introspectable>(path);

			Path = path;
			Services = new ByUuid<RemoteService>();
		}

		public ByUuid<RemoteService> Services
		{
			get;
			private set;
		}

		public ObjectPath Path
		{
			get;
			private set;
		}

		public IDictionary<string, object> Properties
		{
			get { return _props.GetAll(Attr.Name); }
		}

		public string Introspect()
		{
			return _info.IntrospectPretty();
		}

		public void CancelPairing()
		{
			_dev.CancelPairing();
		}

		public void Connect()
		{
			_dev.Connect();
		}

		public void Disconnect()
		{
			_dev.Disconnect();
		}

		public void Pair()
		{
			_dev.Pair();
		}

		private void OnPropertiesChanged(string s, IDictionary<string, object> d, string[] a)
		{
			Logger.Debug("OnPropertiesChanged> {0} ({1})", s, string.Join(",", a));

			foreach (var kv in d)
				Logger.Debug("OnPropertiesChanged>  {0}={1}", kv.Key, kv.Value);

		}

		private T Get<T>(string name)
		{
			return (T)_props.Get(Attr.Name, name);
		}

		private void Set<T>(string name, T value)
		{
			_props.Set(Attr.Name, name, value);
		}

		public string Address { get { return Get<string>("Address"); } }
		public string Name { get { return Get<string>("Name"); } }
		public string Alias
		{
			get { return Get<string>("Alias"); }
			set { Set("Alias", value); }
		}
		public uint Class { get { return Get<uint>("Class"); } }
		public ushort Appearance { get { return Get<ushort>("Appearance"); } }
		public string Icon { get { return Get<string>("Icon"); } }
		public bool Paired { get { return Get<bool>("Paired"); } }
		public bool Trusted
		{
			get { return Get<bool>("Trusted"); }
			set { Set("Trusted", value); }
		}
		public bool Blocked
		{
			get { return Get<bool>("Blocked"); }
			set { Set("Blocked", value); }
		}
		public bool LegacyPairing { get { return Get<bool>("LegacyPairing"); } }
		public short RSSI { get { return Get<short>("RSSI"); } }
		public bool Connected { get { return Get<bool>("Connected"); } }
		public string[] UUIDs { get { return Get<string[]>("UUIDs"); } }
		public string Modalias { get { return Get<string>("Modalias"); } }
		public ObjectPath Adapter { get { return Get<ObjectPath>("Adapter"); } }
		public IDictionary<ushort, object> ManufacturerData { get { return Get<IDictionary<ushort, object>>("ManufacturerData"); } }
		public IDictionary<string, object> ServiceData { get { return Get<IDictionary<string, object>>("ServiceData"); } }
		public short TxPower { get { return Get<short>("TxPower"); } }
		public bool ServicesResolved { get { return Get<bool>("ServicesResolved"); } }
	}

}
