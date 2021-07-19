using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using NDesk.DBus;
using org.freedesktop.DBus;

namespace Peach.Pro.Core.Publishers.Bluetooth
{
	public static class DBusExtensions
	{
		private const string BusName = "org.bluez";

		public static ObjectPath Parent(this ObjectPath item)
		{
			var value = item.ToString();
			if (value == ObjectPath.Root.ToString())
				return null;
			var str = value.Substring(0, value.LastIndexOf('/'));
			if (str == string.Empty)
				str = "/";
			return new ObjectPath(str);
		}

		public static IEnumerable<ObjectPath> Iter<T>(
			this IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>> objs)
		{
			var iface = typeof(T).GetCustomAttributes(false).OfType<InterfaceAttribute>().First();

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

					yield return kv.Key;
				}
			}
		}

		public static IEnumerable<ObjectPath> Iter<T>(
			this IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>> objs, Func<IDictionary<string, object>, bool> pred)
		{
			var iface = typeof(T).GetCustomAttributes(false).OfType<InterfaceAttribute>().First();

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

					if (pred(item.Value))
						yield return kv.Key;
				}
			}
		}

		public static string IntrospectPretty(this Introspectable obj)
		{
			var xml = obj.Introspect();
			var stream = new StringWriter();
			var writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true });
			var doc = new XmlDocument();

			doc.LoadXml(xml);
			doc.Save(writer);

			return stream.ToString();
		}

		public static T GetObject<T>(this Bus bus, ObjectPath path)
		{
			return bus.GetObject<T>(BusName, path);
		}
	}
}
