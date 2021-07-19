using System;
using System.Collections.Generic;
using System.Linq;
using NDesk.DBus;
using org.freedesktop.DBus;

namespace Peach.Pro.Core.Publishers.Bluetooth
{
	public class LocalService : IService, IGattProperties
	{
		private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		public static readonly InterfaceAttribute Attr =
			typeof(IService).GetCustomAttributes(false).OfType<InterfaceAttribute>().First();

		public LocalService()
		{
			Characteristics = new ByUuid<LocalCharacteristic>();
		}

		public bool Advertise { get; set; }
		public ObjectPath Path { get; set; }
		public ByUuid<LocalCharacteristic> Characteristics { get; private set; }

		#region IService

		public string UUID { get; set; }
		public bool Primary { get; set; }

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
				{"UUID", UUID},
				{"Primary", Primary},
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
