using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Xml;
using System.Linq;
using Peach.Core;

// This assembly contains Peach plugins
[assembly: PluginAssembly]

namespace Peach.Core
{

	public static class NameValueCollectionExtensions
	{
		public static string PrettyPrint(this NameValueCollection collection, string separator = ",")
		{
			var items = collection.AllKeys.Select(x => "{0}={1}".Fmt(x, collection[x]));
			return string.Join(separator, items);
		}
	}

	public static class KeyValueConfigurationCollectionExtensions
	{
		public static string Get(this KeyValueConfigurationCollection settings, string key)
		{
			var item = settings[key];
			return item != null ? item.Value : null;
		}

		public static void Set(this KeyValueConfigurationCollection settings, string key, string value)
		{
			if (settings.Get(key) == null)
				settings.Add(key, value);
			else
				settings[key].Value = value;
		}
	}

	public static class SocketExtensions
	{
		public static void SendTo(this Socket socket, string msg, IPEndPoint remote)
		{
			socket.SendTo(Encoding.ASCII.GetBytes(msg), remote);
		}

		public static void Send(this Socket socket, string msg)
		{
			socket.Send(Encoding.ASCII.GetBytes(msg), SocketFlags.None);
		}
	}

	public static class EnumerableExtensions
	{
		public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
		{
			foreach (var item in sequence)
				action(item);
		}

		public static IEnumerable<T> LastEnumerable<T>(this IEnumerable<T> source)
		{
			return new[] { source.Last() };
		}
	}

	public static class ListExtensions
	{
		public static T First<T>(this List<T> list)
		{
			return list[0];
		}

		public static T Last<T>(this List<T> list)
		{
			return list[list.Count - 1];
		}
	}

	public static class StringExtensions
	{
		public static string Fmt(this string format, params object[] args)
		{
			return string.Format(format, args);
		}
	}

	public static class TypeExtensions
	{
		public static string GetPluginName(this Type type)
		{
			return type.GetAttributes<PluginAttribute>()
				.Select(a => a.Name).FirstOrDefault() ?? type.Name;
		}
	}

	public static class IpAddressExtensions
	{
		public static bool IsMulticast(this System.Net.IPAddress ip)
		{
			// IPv6 -> 1st byte is 0xff
			// IPv4 -> 1st byte is 0xE0 -> 0xEF

			byte[] buf = ip.GetAddressBytes();

			if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
				return (buf[0] & 0xf0) == 0xe0;
			else
				return (buf[0] == 0xff);
		}
	}

	public static class AssemblyExtensions
	{
		public static string LoadStringResource(this Assembly asm, string name)
		{
			var fullName = asm.GetName().Name + ".Resources." + name;
			return Utilities.LoadStringResource(asm, fullName);
		}

		public static MemoryStream LoadBinaryResource(this Assembly asm, string name)
		{
			var fullName = asm.GetName().Name + ".Resources." + name;
			return Utilities.LoadBinaryResource(asm, fullName);
		}

		public static string GetCopyright(this Assembly asm)
		{
			return asm.GetCustomAttributes(false)
				.OfType<AssemblyCopyrightAttribute>()
				.Select(a => a.Copyright)
				.FirstOrDefault();
		}
	}

	public static class ByteArrayExtensions
	{
		public static bool IsSame(this byte[] buffer, byte[] other)
		{
			return buffer.IsSame(other, 0, other.Length);
		}

		public static bool IsSame(this byte[] buffer, byte[] other, int offset, int count)
		{
			if (offset < 0)
				throw new ArgumentOutOfRangeException("offset");
			if (count < 0 || (offset + count) > other.Length)
				throw new ArgumentOutOfRangeException("count");

			if (buffer.Length != count)
				return false;

			for (int i = 0; i < buffer.Length; ++i)
			{
				if (buffer[i] != other[offset + i])
					return false;
			}

			return true;
		}
	}

	public static class XmlExtensions
	{
		/// <summary>
		/// Presents an xml node list as an IEnumerable&lt;XmlNode&gt;
		/// </summary>
		/// <param name="list">Xml node list</param>
		/// <returns></returns>
		public static IEnumerable<XmlNode> AsEnumerable(this XmlNodeList list)
		{
			foreach (XmlNode item in list)
			{
				yield return item;
			}
		}

		/// <summary>
		/// Tests if an attribute exists on an XmlNode
		/// </summary>
		/// <param name="node">Node to test</param>
		/// <param name="name">Attribute name to check for</param>
		/// <returns>True if atribute exists, false otherwise</returns>
		public static bool hasAttr(this XmlNode node, string name)
		{
			string value = node.getAttr(name, null);
			return value != null;
		}

		/// <summary>
		/// Gets the value of an xml attribute as a string.
		/// Throws an error if the attribute does not exist
		/// </summary>
		/// <param name="node">Xml node</param>
		/// <param name="name">Name of the attribute</param>
		/// <returns>Attribute value as a string</returns>
		public static string getAttrString(this XmlNode node, string name)
		{
			string value = node.getAttr(name, null);
			if (value == null)
				throw new PeachException("Error, '" + node.Name + "' element is missing required attribute '" + name + "'.");
			return value;
		}

		/// <summary>
		/// Gets the value of an xml attribute as an int.
		/// Throws an error if the attribute does not exist or
		/// if the value can not be converted to an int.
		/// </summary>
		/// <param name="node">Xml node</param>
		/// <param name="name">Name of the attribute</param>
		/// <returns>Attribute value as an int</returns>
		public static int getAttrInt(this XmlNode node, string name)
		{
			return StringToInt(node, name, node.getAttrString(name));
		}

		/// <summary>
		/// Gets the value of an xml attribute as an unsigned int.
		/// Throws an error if the attribute does not exist or
		/// if the value can not be converted to an unsigned int.
		/// </summary>
		/// <param name="node">Xml node</param>
		/// <param name="name">Name of the attribute</param>
		/// <returns>Attribute value as an int</returns>
		public static uint getAttrUInt32(this XmlNode node, string name)
		{
			return StringToUInt32(node, name, node.getAttrString(name));
		}

		/// <summary>
		/// Gets the value of an xml attribute as a ulong.
		/// Throws an error if the attribute does not exist or
		/// if the value can not be converted to a ulong.
		/// </summary>
		/// <param name="node">Xml node</param>
		/// <param name="name">Name of the attribute</param>
		/// <returns>Attribute value as an int</returns>
		public static ulong getAttrUInt64(this XmlNode node, string name)
		{
			return StringToUInt64(node, name, node.getAttrString(name));
		}

		/// <summary>
		/// Gets the value of an xml attribute as a bool.
		/// Throws an error if the attribute does not exist or
		/// if the value can not be converted to a bool.
		/// </summary>
		/// <param name="node">Xml node</param>
		/// <param name="name">Name of the attribute</param>
		/// <returns>Attribute value as a bool</returns>
		public static bool getAttrBool(this XmlNode node, string name)
		{
			return StringToBool(node, name, node.getAttrString(name));
		}

		/// <summary>
		/// Gets the value of an xml attribute as a char.
		/// Throws an error if the attribute does not exist or
		/// if the value can not be converted to a char.
		/// </summary>
		/// <param name="node">Xml node</param>
		/// <param name="name">Name of the attribute</param>
		/// <returns>Attribute value as a char</returns>
		public static char getAttrChar(this XmlNode node, string name)
		{
			return StringToChar(node, name, node.getAttrString(name));
		}

		/// <summary>
		/// Gets the value of an xml attribute as a string.
		/// Throws an error if the attribute value can not be converted to a string.
		/// Returns the defaultValue if the attribute is not set.
		/// </summary>
		/// <param name="node">Xml node</param>
		/// <param name="name">Name of the attribute</param>
		/// <param name="defaultValue">Value to use when the attribute is not set</param>
		/// <returns>Attribute value as a string</returns>
		public static string getAttr(this XmlNode node, string name, string defaultValue)
		{
			XmlAttribute attr = node.Attributes.GetNamedItem(name) as XmlAttribute;

			if (attr != null)
				return attr.InnerText;
			else
				return defaultValue;
		}

		/// <summary>
		/// Gets the value of an xml attribute as a bool.
		/// Throws an error if the attribute value can not be converted to a bool.
		/// Returns the defaultValue if the attribute is not set.
		/// </summary>
		/// <param name="node">Xml node</param>
		/// <param name="name">Name of the attribute</param>
		/// <param name="defaultValue">Value to use when the attribute is not set</param>
		/// <returns>Attribute value as a bool</returns>
		public static bool getAttr(this XmlNode node, string name, bool defaultValue)
		{
			string value = node.getAttr(name, null);
			if (value == null)
				return defaultValue;
			return StringToBool(node, name, value);
		}

		/// <summary>
		/// Gets the value of an xml attribute as an int.
		/// Throws an error if the attribute value can not be converted to an int.
		/// Returns the defaultValue if the attribute is not set.
		/// </summary>
		/// <param name="node">Xml node</param>
		/// <param name="name">Name of the attribute</param>
		/// <param name="defaultValue">Value to use when the attribute is not set</param>
		/// <returns>Attribute value as an int</returns>
		public static int getAttr(this XmlNode node, string name, int defaultValue)
		{
			string value = node.getAttr(name, null);
			if (value == null)
				return defaultValue;
			return StringToInt(node, name, value);

		}

		/// <summary>
		/// Gets the value of an xml attribute as a ulong.
		/// Throws an error if the attribute value can not be converted to a ulong.
		/// Returns the defaultValue if the attribute is not set.
		/// </summary>
		/// <param name="node">Xml node</param>
		/// <param name="name">Name of the attribute</param>
		/// <param name="defaultValue">Value to use when the attribute is not set</param>
		/// <returns>Attribute value as an int</returns>
		public static ulong getAttr(this XmlNode node, string name, ulong defaultValue)
		{
			string value = node.getAttr(name, null);
			if (value == null)
				return defaultValue;
			return StringToUInt64(node, name, value);

		}

		/// <summary>
		/// Gets the value of an xml attribute as a char.
		/// Throws an error if the attribute value can not be converted to a char.
		/// Returns the defaultValue if the attribute is not set.
		/// </summary>
		/// <param name="node">Xml node</param>
		/// <param name="name">Name of the attribute</param>
		/// <param name="defaultValue">Value to use when the attribute is not set</param>
		/// <returns>Attribute value as a char</returns>
		public static int getAttr(this XmlNode node, string name, char defaultValue)
		{
			string value = node.getAttr(name, null);
			if (value == null)
				return defaultValue;
			return StringToChar(node, name, value);
		}

		private static string getError(XmlNode node, string key)
		{
			return string.Format("Error, element '{0}' has an invalid value for attribute '{1}'.", node.Name, key);
		}

		private static bool StringToBool(XmlNode node, string name, string value)
		{
			switch (value)
			{
				case "1":
				case "true":
					return true;
				case "0":
				case "false":
					return false;
				default:
					throw new PeachException(getError(node, name) + "  Could not convert value '" + value + "' to a boolean.");
			}
		}

		private static char StringToChar(XmlNode node, string name, string value)
		{
			if (value.Length != 1)
				throw new PeachException(getError(node, name) + "  Could not convert value '" + value + "' to a character.");
			return value[0];
		}

		private static int StringToInt(XmlNode node, string name, string value)
		{
			int ret;
			if (!int.TryParse(value, out ret))
				throw new PeachException(getError(node, name) + "  Could not convert value '" + value + "' to an integer.");
			return ret;
		}

		private static uint StringToUInt32(XmlNode node, string name, string value)
		{
			uint ret;
			if (!uint.TryParse(value, out ret))
				throw new PeachException(getError(node, name) + "  Could not convert value '" + value + "' to a 32-bit unsigned integer.");
			return ret;
		}

		private static ulong StringToUInt64(XmlNode node, string name, string value)
		{
			ulong ret;
			if (!ulong.TryParse(value, out ret))
				throw new PeachException(getError(node, name) + "  Could not convert value '" + value + "' to a 64-bit unsigned integer.");
			return ret;
		}

		/// <summary>
		/// Set attribute on XmlNode object.
		/// </summary>
		/// <param name="node">XmlNode to set attribute on</param>
		/// <param name="name">Name of attribute</param>
		/// <param name="value">Value of attribute</param>
		public static void AppendAttribute(this XmlNode node, string name, string value)
		{
			if (!String.IsNullOrEmpty(value))
				((XmlElement)node).SetAttribute(name, value);
		}
	}
}
