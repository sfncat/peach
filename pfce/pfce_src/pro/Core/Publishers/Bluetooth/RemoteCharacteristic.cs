using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NDesk.DBus;
using org.freedesktop.DBus;

namespace Peach.Pro.Core.Publishers.Bluetooth
{
	public class RemoteCharacteristic : ICharacteristic
	{
		private class NotifyLock
		{
			public bool Notifying;
		}

		private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		private static readonly InterfaceAttribute Attr =
			typeof(ICharacteristic).GetCustomAttributes(false).OfType<InterfaceAttribute>().First();

		private readonly ICharacteristic _char;
		private readonly Properties _props;
		private readonly Introspectable _info;
		private readonly NotifyLock _mutex;
		private readonly Queue<byte[]> _notifications;

		public RemoteCharacteristic(Bus bus, ObjectPath path)
		{
			_char = bus.GetObject<ICharacteristic>(path);
			_props = bus.GetObject<Properties>(path);
			_props.PropertiesChanged += OnPropertiesChanged;
			_info = bus.GetObject<Introspectable>(path);
			_mutex = new NotifyLock {Notifying = false};
			_notifications = new Queue<byte[]>();

			Path = path;
			Descriptors = new ByUuid<RemoteDescriptor>();
		}

		public ObjectPath Path
		{
			get;
			private set;
		}

		public ByUuid<RemoteDescriptor> Descriptors
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
			return _char.ReadValue(options);
		}

		public void WriteValue(byte[] value, IDictionary<string, object> options)
		{
			_char.WriteValue(value, options);
		}

		public void StartNotify()
		{
			lock (_mutex)
			{
				if (!_mutex.Notifying)
				{
					_mutex.Notifying = true;
					_char.StartNotify();
				}
			}
		}

		public void StopNotify()
		{
			lock (_mutex)
			{
				if (_mutex.Notifying)
				{
					_mutex.Notifying = false;
					_notifications.Clear();
					_char.StopNotify();
				}
			}
		}

		public void AcquireNotify(IDictionary<string, object> options, out int fd, out ushort mtu)
		{
			_char.AcquireNotify(options, out fd, out mtu);
		}

		public byte[] GetNotification(int millisecondsTimeout)
		{
			lock (_mutex)
			{
				if (!_mutex.Notifying)
				{
					_mutex.Notifying = true;
					_char.StartNotify();
				}

				if (_notifications.Count == 0 && !Monitor.Wait(_mutex, millisecondsTimeout))
					return null;

				return _notifications.Dequeue();
			}
		}

		private void OnPropertiesChanged(string s, IDictionary<string, object> d, string[] a)
		{
			Logger.Trace("OnPropertiesChanged> {0} ({1})", s, string.Join(",", a));

			foreach (var kv in d)
				Logger.Trace("OnPropertiesChanged>  {0}={1}", kv.Key, kv.Value);

			object value;
			if (!d.TryGetValue("Value", out value))
				return;

			lock (_mutex)
			{
				if (_mutex.Notifying)
				{
					var asBytes = (byte[])value;
					Logger.Debug("OnPropertiesChanged> Notify: {0}", string.Join("", asBytes.Select(x => x.ToString("X2"))));
					_notifications.Enqueue(asBytes);
					Monitor.Pulse(_mutex);
				}
			}
		}

		private T Get<T>(string name)
		{
			return (T)_props.Get(Attr.Name, name);
		}

		public string UUID { get { return Get<string>("UUID"); } }
		public ObjectPath Service { get { return Get<ObjectPath>("Service"); } }
		public byte[] Value { get { return Get<byte[]>("Value"); } }
		public bool Notifying { get { return Get<bool>("Notifying"); } }
		public string[] Flags { get { return Get<string[]>("Flags"); } }
	}
}
