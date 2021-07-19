using System.Collections.Generic;
using System.Linq;
using NDesk.DBus;
using org.freedesktop.DBus;

namespace Peach.Pro.Core.Publishers.Bluetooth
{
	public class RemoteService : IService
	{
		private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		private static readonly InterfaceAttribute Attr =
			typeof(IService).GetCustomAttributes(false).OfType<InterfaceAttribute>().First();

		private readonly Properties _props;
		private readonly Introspectable _info;

		public RemoteService(Bus bus, ObjectPath path)
		{
			_props = bus.GetObject<Properties>(path);
			_props.PropertiesChanged += OnPropertiesChanged;
			_info = bus.GetObject<Introspectable>(path);

			Path = path;
			Characteristics = new ByUuid<RemoteCharacteristic>();
		}

		public ObjectPath Path
		{
			get;
			private set;
		}

		public ByUuid<RemoteCharacteristic> Characteristics
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

		public string UUID { get { return Get<string>("UUID"); } }
		public ObjectPath Device { get { return Get<ObjectPath>("Device"); } }
		public bool Primary { get { return Get<bool>("Primary"); } }
		public ObjectPath[] Includes { get { return Get<ObjectPath[]>("Includes"); } }
	}

}
