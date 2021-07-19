using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NDesk.DBus;

namespace Peach.Pro.Core.Publishers.Bluetooth
{
	public class LocalDescriptor : IDescriptor, IGattProperties
	{
		private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		public static readonly InterfaceAttribute Attr =
			typeof(IDescriptor).GetCustomAttributes(false).OfType<InterfaceAttribute>().First();

		public LocalDescriptor()
		{
			_mutex = new object();
			_inputs = new Queue<byte[]>();
			_value = new byte[0];
		}

		public ObjectPath Path { get; set; }
		public Action<IDescriptor, byte[], IDictionary<string, object>> OnWrite { get; set; }

		private readonly object _mutex;
		private readonly Queue<byte[]> _inputs;
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
				lock (_mutex)
				{
					_value = value;
				}
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

		#region IDescriptor

		public string UUID { get; set; }
		public ObjectPath Characteristic { get; set; }
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
				{"Characteristic", Characteristic },
				{"UUID", UUID},
				{"Flags", Flags},
			};
		}

		public event PropertiesChangedHandler PropertiesChanged
		{
			add { }
			remove { }
		}

		#endregion
	}
}
