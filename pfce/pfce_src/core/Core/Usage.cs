

// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.ComponentModel;
using System.Linq;
using System.Collections.Generic;

namespace Peach.Core
{
	public class UsageAttribute : Attribute
	{
		public string Message { get; private set; }

		public UsageAttribute(string message)
		{
			Message = message;
		}
	}

	public class LongDescriptionAttribute : Attribute
	{
		public string Text { get; private set; }

		public LongDescriptionAttribute(string text)
		{
			Text = text;
		}
	}

	public static class Usage
	{
		private class PluginAlias : PluginAttribute
		{
			public PluginAlias(Type type, string name)
				: base(type, name, false)
			{
			}
		}

		private class TypeComparer : IComparer<Type>
		{
			public int Compare(Type x, Type y)
			{
				return x.Name.CompareTo(y.Name);
			}
		}

		private class PluginComparer : IComparer<PluginAttribute>
		{
			public int Compare(PluginAttribute x, PluginAttribute y)
			{
				if (x.IsDefault == y.IsDefault)
					return x.Name.CompareTo(y.Name);

				if (x.IsDefault)
					return -1;

				return 1;
			}
		}

		private class ParamComparer : IComparer<ParameterAttribute>
		{
			public int Compare(ParameterAttribute x, ParameterAttribute y)
			{
				if (x.required == y.required)
					return x.name.CompareTo(y.name);

				if (x.required)
					return -1;

				return 1;
			}
		}

		public static void Print()
		{
			var color = Console.ForegroundColor;

			var dupes = new List<string>();

			var pluginsByName = new SortedDictionary<string, Type>();
			var plugins = new SortedDictionary<Type, SortedDictionary<Type, SortedSet<PluginAttribute>>>(new TypeComparer());

			foreach (var type in ClassLoader.GetAllByAttribute<PluginAttribute>())
			{
				if (type.Key.Scope == PluginScope.Internal)
					continue;

				var pluginType = type.Key.Type;

				var fullName = type.Key.Type.Name + ": " + type.Key.Name;
				if (pluginsByName.ContainsKey(fullName))
				{
					AddDuplicate(dupes, type.Key.Type.Name, type.Key.Name, pluginsByName[fullName], type.Value);
					continue;
				}

				pluginsByName.Add(fullName, type.Value);

				if (!plugins.ContainsKey(pluginType))
					plugins.Add(pluginType, new SortedDictionary<Type, SortedSet<PluginAttribute>>(new TypeComparer()));

				var plugin = plugins[pluginType];

				if (!plugin.ContainsKey(type.Value))
					plugin.Add(type.Value, new SortedSet<PluginAttribute>(new PluginComparer()));

				var attrs = plugin[type.Value];

				var added = attrs.Add(type.Key);
				System.Diagnostics.Debug.Assert(added);

				foreach (var a in type.Value.GetAttributes<AliasAttribute>())
					attrs.Add(new PluginAlias(type.Value, a.Name));
			}

			foreach (var kv in plugins)
			{
				var name = kv.Key.Name;

				if (kv.Key.IsInterface && name[0] == 'I')
					name = name.Substring(1);

				var isLower = false;

				for (var i = 0; i < name.Length; ++i)
				{
					var isUpper = char.IsUpper(name[i]);

					if (isUpper && isLower)
					{
						name = name.Insert(i, " ");
						++i;
					}

					isLower = !isUpper;
				}

				Console.WriteLine();
				Console.WriteLine();
				Console.WriteLine("-----{0}", name.PadRight(74, '-'));

				foreach (var plugin in kv.Value.OrderBy(x => x.Value.Single(y => y.IsDefault).Name))
				{
					var obsolete = plugin.Key.GetAttributes<ObsoleteAttribute>().SingleOrDefault();

					Console.WriteLine();
					Console.Write(" ");

					foreach (var attr in plugin.Value)
					{
						Console.Write(" ");
						if (attr.IsDefault)
							Console.ForegroundColor = ConsoleColor.White;
						Console.Write(attr.Name);
						Console.ForegroundColor = color;

						if (attr.IsDefault && attr.Scope == PluginScope.Beta)
						{
							Console.Write(" ");
							Console.ForegroundColor = ConsoleColor.Yellow;
							Console.Write("(beta)");
							Console.ForegroundColor = color;
						}

						if (obsolete != null)
						{
							Console.Write(" ");
							Console.ForegroundColor = ConsoleColor.Red;
							Console.Write("(deprecated)");
							Console.ForegroundColor = color;
						}
					}

					Console.WriteLine();

					var desc = plugin.Key.GetAttributes<DescriptionAttribute>().SingleOrDefault();
					if (desc != null)
						Console.WriteLine("    {0}", desc.Description);

					if (obsolete != null && !string.IsNullOrEmpty(obsolete.Message))
					{
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine("    {0}", obsolete.Message);
						Console.ForegroundColor = color;
					}

					PrintParams(plugin.Key);
				}
			}

			foreach (var dupe in dupes)
			{
				Console.WriteLine();
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(dupe);
				Console.ForegroundColor = color;
			}
		}

		private static void AddDuplicate(List<string> dupes, string category, string name, Type type1, Type type2)
		{
			if (type1 == type2)
			{
				// duplicate name on same type
				dupes.Add("{0} '{1}' declared more than once in assembly '{2}' class '{3}'.".Fmt(
					category, name, type1.Assembly.Location, type1.FullName));
			}
			else
			{
				// duplicate name on different types
				dupes.Add("{0} '{1}' declared in assembly '{2}' class '{3}' and in assembly {4} and class '{5}'.".Fmt(
					category, name, type1.Assembly.Location, type1.FullName, type2.Assembly.Location, type2.FullName));
			}

		}

		private static void PrintParams(Type elem)
		{
			var properties = new SortedSet<ParameterAttribute>(elem.GetAttributes<ParameterAttribute>(null), new ParamComparer());

			foreach (var prop in properties)
			{
				string value = "";
				if (!prop.required)
					value = string.Format(" default=\"{0}\"", prop.defaultValue.Replace("\r", "\\r").Replace("\n", "\\n"));

				string type;
				if (prop.type.IsGenericType && prop.type.GetGenericTypeDefinition() == typeof(Nullable<>))
					type = string.Format("({0}?)", prop.type.GetGenericArguments()[0].Name);
				else
					type = string.Format("({0})", prop.type.Name);

				Console.WriteLine("    {0} {1} {2} {3}.{4}", prop.required ? "*" : "-",
					prop.name.PadRight(24), type.PadRight(14), prop.description, value);
			}
		}
	}
}
