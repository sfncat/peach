using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Pro.Core.WebServices.Models;

namespace Peach.Pro.Core.WebServices
{
	public class MonitorMetadata
	{
		public event ErrorEventHandler ErrorEventHandler;

		public static List<ParamDetail> Generate(List<string> calls)
		{
			var m = new MonitorMetadata();
			var ret = m.Load(calls);
			return ret;
		}

		#region JSON Metatata DTOs

		private enum ItemType { Group, Space, Monitor, Param }

		[Serializable]
		private class TypedItem
		{
			// ReSharper disable UnusedAutoPropertyAccessor.Local
			public string Name { get; set; }
			public bool Collapsed { get; set; }
			public ItemType Type { get; set; }
			public List<TypedItem> Items { get; set; }
			// ReSharper restore UnusedAutoPropertyAccessor.Local
		}

		[Serializable]
		private class NamedItem
		{
			// ReSharper disable UnusedAutoPropertyAccessor.Local
			public string Name { get; set; }
			public bool Collapsed { get; set; }
			public List<string> Items { get; set; }
			// ReSharper restore UnusedAutoPropertyAccessor.Local
		}


		[Serializable]
		private class GroupInfo
		{
			// ReSharper disable UnusedAutoPropertyAccessor.Local
			public List<NamedItem> Groups { get; set; }
			public Dictionary<string, List<NamedItem>> Parameters { get; set; }
			// ReSharper restore UnusedAutoPropertyAccessor.Local
		}

		#endregion

		private class MonitorInfo : INamed
		{
			[Obsolete]
			string INamed.name { get { return Key; } }
			string INamed.Name { get { return Key; } }

			public string Key { get; set; }
			public string Name { get; set; }
			public string Description { get; set; }
			public string OS { get; set; }
			public Type Type { get; set; }
			public bool Visited { get; set; }
			public PluginScope Scope { get; set; }
		}

		private class ParamInfo : INamed
		{
			[Obsolete]
			string INamed.name { get { return Attr.name; } }
			string INamed.Name { get { return Attr.name; } }

			public string Monitor { get; set; }
			public ParameterAttribute Attr { get; set; }
			public int Usage { get; set; }
		}

		private List<string> _calls;
		private NamedCollection<MonitorInfo> _monitors;

		internal MonitorMetadata()
		{
		}

		protected virtual TextReader OpenMetadataStream()
		{
			var asm = Assembly.GetExecutingAssembly();
			var strm = asm.GetManifestResourceStream("Peach.Pro.Core.Resources.MonitorMetadata.json");

			if (strm == null)
				return null;

			return new StreamReader(strm, System.Text.Encoding.UTF8);
		}

		protected virtual IEnumerable<KeyValuePair<MonitorAttribute, Type>> GetAllMonitors()
		{
			return ClassLoader.GetAllByAttribute<MonitorAttribute>();
		}

		public List<ParamDetail> Load(List<string> calls)
		{
			var groupings = GetGroupings();

			try
			{
				_calls = calls;
				_monitors = GetMonitorInfo();

				var ret = AsParameter(groupings, null, null);

				var missing = _monitors.Where(m => !m.Visited).ToList();

				if (missing.Count > 0)
				{
					missing.Sort(MonitorSorter);

					var missingNames = string.Join("', '",
						missing
							.Where(m => m.Scope != PluginScope.Internal)
							.Select(m => m.Key)
					);

					// If GetGroupings() failed we already raised ErrorEventHandler
					if (ret == null)
						ret = new List<ParamDetail>();
					else if (ErrorEventHandler != null && !string.IsNullOrEmpty(missingNames))
					{
						var ex = new ApplicationException(
							"Missing metadata entries for the following monitors: '{0}'.".Fmt(missingNames)
						);
						var args = new ErrorEventArgs(ex);

						ErrorEventHandler(this, args);
					}

					var grp = new ParamDetail
					{
						Name = "Other",
						Type = ParameterType.Group,
						Items = new List<ParamDetail>()
					};

					foreach (var monitor in missing)
					{
						var parameters = GetParamInfo(monitor.Type, monitor.Key);
						var items = parameters.Select(ParameterAttrToModel).ToList();

						items.Sort(ParameterSorter);

						grp.Items.Add(MakeMonitor(monitor, items));
					}

					ret.Add(grp);
				}

				return ret;
			}
			finally
			{
				_calls = null;
				_monitors = null;
			}
		}

		private GroupInfo GetGroupInfo()
		{
			using (var strm = OpenMetadataStream())
			{
				if (strm == null)
				{
					if (ErrorEventHandler != null)
						ErrorEventHandler(this, new ErrorEventArgs(new ApplicationException("Unable to locate monitor metadata resource.")));

					return null;
				}

				var rdr = new JsonTextReader(strm);
				var s = new JsonSerializer();

				try
				{
					var result = s.Deserialize<GroupInfo>(rdr);
					if (result != null)
						return result;
					else
						return new GroupInfo { Groups = new List<NamedItem>(), Parameters = new Dictionary<string, List<NamedItem>>() };
				}
				catch (JsonReaderException ex)
				{
					if (ErrorEventHandler != null)
						ErrorEventHandler(this, new ErrorEventArgs(new ApplicationException("Unable to parse monitor metadata resource. See line {0}, position {1}".Fmt(ex.LineNumber, ex.LinePosition), ex)));

					return null;
				}
				catch (Exception ex)
				{
					if (ErrorEventHandler != null)
						ErrorEventHandler(this, new ErrorEventArgs(new ApplicationException("Unable to parse monitor metadata resource.", ex)));

					return null;
				}
			}
		}

		private List<TypedItem> GetGroupings()
		{
			var info = GetGroupInfo();

			if (info == null)
				return null;

			var ret = new List<TypedItem>();

			if (info.Groups == null || info.Parameters == null)
				return ret;

			foreach (var grp in info.Groups)
			{
				var g = new TypedItem
				{
					Name = grp.Name,
					Type = ItemType.Group,
					Items = new List<TypedItem>()
				};

				foreach (var name in grp.Items)
				{
					List<NamedItem> paramGroups;
					if (!info.Parameters.TryGetValue(name, out paramGroups))
					{
						if (ErrorEventHandler != null)
							ErrorEventHandler(this, new ErrorEventArgs(new ApplicationException("No parameter entry for monitor '{0}' referenced in group '{1}'.".Fmt(name, grp.Name))));

						continue;
					}

					var m = new TypedItem
					{
						Name = name,
						Type = ItemType.Monitor,
						Items = paramGroups.Select(paramGroup => new TypedItem
						{
							Name = paramGroup.Name,
							Type = ItemType.Group,
							Collapsed = paramGroup.Collapsed,
							Items = paramGroup.Items.Select(i => new TypedItem
							{
								Name = string.IsNullOrEmpty(i) ? null : i,
								Type = string.IsNullOrEmpty(i) ? ItemType.Space : ItemType.Param
							}).ToList()
						}).Where(i => i.Items.Count > 0).ToList()
					};

					g.Items.Add(m);
				}

				if (g.Items.Count > 0)
					ret.Add(g);
			}

			return ret;
		}

		private NamedCollection<MonitorInfo> GetMonitorInfo()
		{
			var ret = new NamedCollection<MonitorInfo>();

			foreach (var kv in GetAllMonitors().Where(FilterInternal))
			{
				try
				{
					ret.Add(new MonitorInfo
					{
						Key = kv.Key.Name,
						Name = KeyToName(kv.Key.Name),
						Description = GetDescription(kv),
						OS = GetOS(kv.Key),
						Type = kv.Value,
						Scope = kv.Key.Scope,
						Visited = false
					});
				}
				catch (ArgumentException ex)
				{
					if (ErrorEventHandler != null)
					{
						var key = kv.Key.Name;
						var dupes = GetAllMonitors()
							.Where(i => i.Key.Name == key)
							.Select(i => "{0} @ {1}".Fmt(i.Value.FullName, i.Value.Assembly.Location));
						var error = "Duplicate entries detected for monitor '{0}' on types '{1}'".Fmt(kv.Key.Name, string.Join("', '", dupes));

						ErrorEventHandler(this, new ErrorEventArgs(new ArgumentException(error, ex)));
					}
				}
			}

			return ret;
		}

		private NamedCollection<ParamInfo> GetParamInfo(Type type, string name)
		{
			return new NamedCollection<ParamInfo>(
				type
					.GetAttributes<ParameterAttribute>()
					.Select(a => new ParamInfo
					{
						Attr = a,
						Monitor = name,
						Usage = 0
					}));
		}

		private static bool FilterInternal(KeyValuePair<MonitorAttribute, Type> kv)
		{
#if !DEBUG
			return kv.Key.Scope != PluginScope.Internal;
#else
			return true;
#endif
		}

		private static string KeyToName(string key)
		{
			return Regex.Replace(key, "[A-Z]+", " $&").Trim();
		}

		private static int MonitorSorter(MonitorInfo lhs, MonitorInfo rhs)
		{
			return string.CompareOrdinal(lhs.Key, rhs.Key);
		}

		private static int ParameterSorter(ParamDetail lhs, ParamDetail rhs)
		{
			if (lhs.Optional == rhs.Optional)
				return string.CompareOrdinal(lhs.Key, rhs.Key);

			return lhs.Optional ? 1 : -1;
		}

		private string GetDescription(KeyValuePair<MonitorAttribute, Type> kv)
		{
			var desc = kv.Value
				.GetAttributes<System.ComponentModel.DescriptionAttribute>()
				.Select(a => a.Description)
				.FirstOrDefault();

			if (!string.IsNullOrEmpty(desc))
				return desc;

			if (ErrorEventHandler != null)
				ErrorEventHandler(this, new ErrorEventArgs(new ApplicationException("Monitor {0} does not have a description.".Fmt(kv.Key.Name))));

			return "";
		}

		private string GetOS(MonitorAttribute attr)
		{
			if (attr.OS == Platform.OS.Unix)
			{
				if (ErrorEventHandler != null)
					ErrorEventHandler(this, new ErrorEventArgs(new NotSupportedException("Monitor {0} specifies unsupported OS '{1}'.".Fmt(attr.Name, attr.OS))));
			}
			else if (attr.OS != Platform.OS.All)
			{
				return attr.OS.ToString();
			}
			return "";
		}

		private List<ParamDetail> AsParameter(List<TypedItem> items, string monitorName, NamedCollection<ParamInfo> parameters)
		{
			if (items == null)
				return null;

			var ret = new List<ParamDetail>();

			foreach (var item in items)
			{
				switch (item.Type)
				{
					case ItemType.Group:
						var detail = new ParamDetail
						{
							Name = item.Name,
							Collapsed = item.Collapsed,
							Type = ParameterType.Group,
							Items = AsParameter(item.Items, monitorName, parameters)
						};

						if (detail.Items.Any(i => i.Type != ParameterType.Space && !i.Optional) && detail.Collapsed)
						{
							if (ErrorEventHandler != null)
								ErrorEventHandler(this, new ErrorEventArgs(new NotSupportedException("Group '" + detail.Name + "' for monitor '" + monitorName + "' is collapsed but contains required parameters.")));

							detail.Collapsed = false;
						}

						ret.Add(detail);
						break;
					case ItemType.Monitor:
						MonitorInfo monitor;
						if (!_monitors.TryGetValue(item.Name, out monitor))
						{
							if (ErrorEventHandler != null)
								ErrorEventHandler(this, new ErrorEventArgs(new NotSupportedException("Ignoring metadata entry for monitor '" + item.Name + "', no plugin exists with that name.")));

							break;
						}

						monitor.Visited = true;

						parameters = GetParamInfo(monitor.Type, monitor.Key);

						ret.Add(MakeMonitor(monitor, AsParameter(item.Items, monitor.Key, parameters)));

						if (ErrorEventHandler != null)
						{
							var omitted = parameters.Where(p => p.Usage == 0).Select(p => p.Attr.name).ToList();
							if (omitted.Count != 0)
							{
								omitted.Sort();
								ErrorEventHandler(this, new ErrorEventArgs(new NotSupportedException("Monitor {0} had the following parameters omitted from the metadata: '{1}'.".Fmt(monitor.Key, string.Join("', '", omitted)))));
							}

							var duped = parameters.Where(p => p.Usage > 1).Select(p => p.Attr.name).ToList();
							if (duped.Count != 0)
							{
								duped.Sort();
								ErrorEventHandler(this, new ErrorEventArgs(new NotSupportedException("Monitor {0} had the following parameters duplicated in the metadata: '{1}'.".Fmt(monitor.Key, string.Join("', '", duped)))));
							}
						}

						break;

					case ItemType.Space:
						ret.Add(new ParamDetail { Type = ParameterType.Space });
						break;

					case ItemType.Param:
						ParamInfo param;
						if (string.IsNullOrEmpty(item.Name) || !parameters.TryGetValue(item.Name, out param))
						{
							if (ErrorEventHandler != null)
								ErrorEventHandler(this, new ErrorEventArgs(new NotSupportedException("Ignoring metadata entry for parameter '" + item.Name + "' on monitor '" + monitorName + "', no parameter exists with that name.")));
						}
						else
						{
							ret.Add(ParameterAttrToModel(param));
						}

						break;
				}
			}

			return ret;
		}

		private static ParamDetail MakeMonitor(MonitorInfo monitor, List<ParamDetail> items)
		{
			return new ParamDetail
			{
				Key = monitor.Key,
				Name = monitor.Name,
				Type = ParameterType.Monitor,
				Description = monitor.Description,
				OS = monitor.OS,
				Items = items
			};
		}

		private ParamDetail ParameterAttrToModel(ParamInfo info)
		{
			var attr = info.Attr;

			var p = new ParamDetail
			{
				Key = attr.name,
				Name = KeyToName(attr.name),
				DefaultValue = attr.required ? null : attr.defaultValue,
				Optional = !attr.required,
				Description = attr.description
			};

			var key = attr.type.Name;
			if (attr.type.IsGenericType)
			{
				key = attr.type.GetGenericArguments().First().Name;
			}
			else if (attr.type.IsEnum)
			{
				key = "Enum";
			}

			switch (key)
			{
				case "String":
				case "String[]":
				case "Int32[]":
					p.Type = ParameterType.String;
					break;
				case "UInt16":
					p.Type = ParameterType.Range;
					p.Max = ushort.MaxValue;
					p.Min = ushort.MinValue;
					break;
				case "UInt32":
					p.Type = ParameterType.Range;
					p.Max = uint.MaxValue;
					p.Min = uint.MinValue;
					break;
				case "Int32":
					p.Type = ParameterType.Range;
					p.Max = int.MaxValue;
					p.Min = int.MinValue;
					break;
				case "Boolean":
					p.Type = ParameterType.Bool;
					p.Options = new List<string> { "true", "false" };
					break;
				case "Enum":
					p.Type = ParameterType.Enum;
					p.Options = Enum.GetNames(attr.type).ToList();
					break;
				case "IPAddress":
					p.Type = ParameterType.Ipv4;
					break;
				default:
					p.Type = ParameterType.String;

					if (ErrorEventHandler != null)
						ErrorEventHandler(this, new ErrorEventArgs(new NotSupportedException("Monitor {0} has invalid parameter type '{1}'.".Fmt(info.Monitor, attr.type.FullName))));

					break;
			}

			if (attr.name.Contains("OnCall"))
			{
				p.Type = ParameterType.Call;
				p.Options = _calls;
			}

			info.Usage++;

			return p;
		}
	}
}
