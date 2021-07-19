using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NDesk.DBus;

namespace Peach.Pro.Core.Publishers.Bluetooth
{
	public class LocalCharacteristic : ICharacteristic, IGattProperties
	{
		private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		public static readonly InterfaceAttribute Attr =
			typeof(ICharacteristic).GetCustomAttributes(false).OfType<InterfaceAttribute>().First();

		public LocalCharacteristic()
		{
			Descriptors = new ByUuid<LocalDescriptor>();
			_mutex = new object();
			_inputs = new Queue<byte[]>();
			_notify = false;
			_value = new byte[0];
		}

		public ObjectPath Path { get; set; }
		public ByUuid<LocalDescriptor> Descriptors { get; private set; }
		public Action<ICharacteristic, byte[], IDictionary<string, object>> OnWrite { get; set; }

		private readonly object _mutex;
		private readonly Queue<byte[]> _inputs;
		private bool _notify;
		private byte[] _value;

		public byte[] Value
		{
			get
			{
				lock (_mutex)
				{
					return _value;
				}
			}
			set
			{
				// Save the value so when the client requests it we have it
				lock (_mutex)
				{
					_value = value;

					if (!_notify)
						return;
				}

				// If a client has requested notifications, tell them of the new value
				if (PropertiesChanged != null)
					PropertiesChanged(Attr.Name, new Dictionary<string, object> { { "Value", value } }, new string[0]);
			}
		}

		public byte[] Read(int timeout)
		{
			lock (_mutex)
			{
				if (_inputs.Count == 0 && !Monitor.Wait(_mutex, timeout))
					return null;

				return _inputs.Dequeue();
			}
		}

		public void Write(byte[] value)
		{
			Value = value;
		}

		#region ICharacteristic

		public string UUID { get; set; }
		public ObjectPath Service { get; set; }
		public string[] Flags { get; set; }

		public byte[] ReadValue(IDictionary<string, object> options)
		{
			byte[] value;

			lock (_mutex)
			{
				value = _value;
			}

			Logger.Debug("ReadValue> Length: {0}", value.Length);
			foreach (var kv in options)
				Logger.Debug("  {0}={1}", kv.Key, kv.Value);

			return value;
		}

		public void WriteValue(byte[] value, IDictionary<string, object> options)
		{
			Logger.Debug("WriteValue> Length: {0}", value.Length);
			foreach (var kv in options)
				Logger.Debug("  {0}={1}", kv.Key, kv.Value);

			if (OnWrite != null)
				OnWrite(this, value, options);

			lock (_mutex)
			{
				_inputs.Enqueue(value);
				Monitor.Pulse(_mutex);
			}
		}

		public void StartNotify()
		{
			Logger.Debug("StartNotify>");

			lock (_mutex)
			{
				_notify = true;
			}
		}

		public void StopNotify()
		{
			Logger.Debug("StopNotify>");

			lock (_mutex)
			{
				_notify = false;
			}
		}

		public void AcquireNotify(IDictionary<string, object> options, out int fd, out ushort mtu)
		{
			Logger.Debug("AcquireNotify>");
			fd = 0;
			mtu = 0;
		}

		#endregion

		#region IGattProperties

		public string InterfaceName { get { return Attr.Name; } }

		public object Get(string @interface, string propname)
		{
			Logger.Trace("Get> {0} {1}", @interface, propname);
			return null;
		}

		public void Set(string @interface, string propname, object value)
		{
			Logger.Trace("Set> {0} {1}={2}", @interface, propname, value);
		}

		public IDictionary<string, object> GetAll(string @interface)
		{
			Logger.Trace("GetAll> {0}", @interface);

			if (@interface != InterfaceName)
				return new Dictionary<string, object>();

			return new Dictionary<string, object>
			{
				{"Service", Service },
				{"UUID", UUID},
				{"Flags", Flags},
			};
		}

		public event PropertiesChangedHandler PropertiesChanged;

		#endregion
	}
}
