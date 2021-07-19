using System.Collections.Generic;
using System.Linq;
using NDesk.DBus;
using org.freedesktop.DBus;

namespace Peach.Pro.Core.Publishers.Bluetooth
{
	public class LocalAdvertisement : IAdvertisement, IGattProperties
	{
		private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		public static readonly InterfaceAttribute Attr =
			typeof(IAdvertisement).GetCustomAttributes(false).OfType<InterfaceAttribute>().First();

		public ObjectPath Path { get; set; }

		public LocalAdvertisement()
		{
			ServiceUUIDs = new string[0];
			ManufacturerData = new Dictionary<ushort, object>();
			SolicitUUIDs = new string[0];
			ServiceData = new Dictionary<string, object>();
		}

		public void Release()
		{
			Logger.Trace("Release>");
		}

		public string Type { get; set; }
		public string LocalName { get; set; }
		public string[] ServiceUUIDs { get; set; }
		public IDictionary<ushort, object> ManufacturerData { get; set; }
		public string[] SolicitUUIDs { get; set; }
		public IDictionary<string, object> ServiceData { get; set; }
		public bool IncludeTxPower { get; set; }

		public string InterfaceName
		{
			get { return Attr.Name; }
		}

		public object Get(string @interface, string propname)
		{
			Logger.Trace("Get> {0} {1}", @interface, propname);
			return null;
		}

		public void Set(string @interface, string propname, object value)
		{
			Logger.Trace(":Set> {0} {1}={2}", @interface, propname, value);
		}

		public IDictionary<string, object> GetAll(string @interface)
		{
			Logger.Trace("GetAll> {0}", @interface);

			if (@interface != InterfaceName)
				return new Dictionary<string, object>();

			return new Dictionary<string, object>
			{
				{"Type", Type},
				{"ServiceUUIDs", ServiceUUIDs},
				{"ManufacturerData", ManufacturerData},
				//{"SolicitUUIDs", SolicitUUIDs},
				//{"ServiceData", ServiceData},
				{"LocalName", LocalName},
				{"IncludeTxPower", IncludeTxPower},
			};
		}

		public event PropertiesChangedHandler PropertiesChanged
		{
			add { }
			remove { }
		}
	}
}
