using System.Collections.Generic;
using System.Linq;
using NDesk.DBus;
using org.freedesktop.DBus;

namespace Peach.Pro.Core.Publishers.Bluetooth
{
	public class RemoteDescriptor : IDescriptor
	{
		private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		private static readonly InterfaceAttribute Attr =
			typeof(IDescriptor).GetCustomAttributes(false).OfType<InterfaceAttribute>().First();

		private readonly IDescriptor _desc;
		private readonly Properties _props;
		private readonly Introspectable _info;

		public RemoteDescriptor(Bus bus, ObjectPath path)
		{
			_desc = bus.GetObject<IDescriptor>(path);
			_props = bus.GetObject<Properties>(path);
			_props.PropertiesChanged += OnPropertiesChanged;
			_info = bus.GetObject<Introspectable>(path);

			Path = path;
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

		public byte[] ReadValue(IDictionary<string, object> options)
		{
			return _desc.ReadValue(options);
		}

		public void WriteValue(byte[] value, IDictionary<string, object> options)
		{
			_desc.WriteValue(value, options);
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
		public ObjectPath Characteristic { get { return Get<ObjectPath>("Characteristic"); } }
		public byte[] Value { get { return Get<byte[]>("Vaue"); } }
		public string[] Flags { get { return Get<string[]>("Flags"); } }
	}
}
