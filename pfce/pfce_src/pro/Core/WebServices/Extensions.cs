using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Peach.Pro.Core.WebServices.Models;

namespace Peach.Pro.Core.WebServices
{
	internal static class Extensions
	{
		private class OrderedContractResolver : DefaultContractResolver
		{
			protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
			{
				return base.CreateProperties(type, memberSerialization).OrderBy(p => p.PropertyName).ToList();
			}
		}

		internal static string ToJson(this List<ParamDetail> details)
		{
			var json = JsonConvert.SerializeObject(details, Formatting.Indented, new JsonSerializerSettings
			{
				Converters = new List<JsonConverter> { new StringEnumConverter() },
				NullValueHandling = NullValueHandling.Ignore,
				DefaultValueHandling = DefaultValueHandling.Ignore,
				ContractResolver = new OrderedContractResolver()
			});

			return json;
		}

		public static List<Models.Agent> ToWeb(this IEnumerable<PeachElement.AgentElement> agents)
		{
			return agents.Select(a =>
				new Models.Agent
			{
				AgentUrl = a.Location,
				Name = EnsureNotEmpty(a.Name, "Name", "agent"),
				Monitors = a.Monitors.Select(m =>
					new Monitor
				{
					Name = m.Name,
					MonitorClass = m.Class,
					Map = m.Params.Select(p =>
						new Param
					{
						Key = p.Name,
						Value = p.Value
					}).ToList()
				}).ToList()
			}).ToList();
		}
		
		public static List<Models.Agent> FromWeb(this List<Models.Agent> agents)
		{
			return agents.Select(a =>
				new Models.Agent
				{
					Name = a.Name,
					AgentUrl = a.AgentUrl,
					Monitors = a.Monitors.Select(m => new Monitor
					{
						Name = m.Name,
						MonitorClass = EnsureNotEmpty(m.MonitorClass, "MonitorClass", "monitor"),
						Map = m.Map.SelectMany(ToMonitorParam).ToList()
					}).ToList()
				}).ToList();
		}

		static Param[] ToMonitorParam(Param p)
		{
			// For backwards compatibility:
			// Peach 3.7 posts name/value, Peach 3.8 posts key/value
			var key = EnsureNotEmpty(p.Key ?? p.Name, "Key", "monitor parameter");

			if (String.IsNullOrEmpty(p.Value))
				return new Param[0];

			return new[] { new Param { Key = key, Value = p.Value } };
		}
		
		static string EnsureNotEmpty(string value, string name, string type)
		{
			if (String.IsNullOrEmpty(value))
				throw new ArgumentException("Required parameter '" + name + "' was not specified for entry in "+ type + " list.");

			return value;
		}

		public static List<ParamDetail> ToWeb(this PitDefines defines, List<Param> config)
		{
			var set = new SortedSet<string>(config.Select(x => x.Key));
			set.ExceptWith(defines.Walk().Select(x => x.Key));

			var userConfigs = new List<Param>();
			userConfigs.AddRange(set.Select(key => config.Single(x => x.Key == key)));
			var userDefines = (userConfigs.Count) == 0 ? null : 
				userConfigs.Select(cfg => new ParamDetail
				{
					Key = cfg.Key,
					Name = cfg.Name,
					Description = cfg.Description,
					Type = ParameterType.User,
				}).ToList();

			var ifaces = new IfaceOptions();
			var reserved = defines.SystemDefines.Select(d => d.Key).ToList();

			var ret = DefineToParamDetail(defines.Children, reserved, ifaces) ?? new List<ParamDetail>();

			reserved = new List<string>();

			ret.Add(new ParamDetail
			{
				Key = "UserDefines",
				Name = "User Defines",
				Description = "",
				Type = ParameterType.Group,
				Collapsed = false,
				OS = "",
				Items = userDefines,
			});

			ret.Add(new ParamDetail
			{
				Key = "SystemDefines",
				Name = "System Defines",
				Description = "These values are controlled by Peach.",
				Type = ParameterType.Group,
				Collapsed = true,
				OS = "",
				Items = defines.SystemDefines.Select(d => DefineToParamDetail(d, reserved, ifaces)).ToList()
			});

			return ret.Where(d => d.Type != ParameterType.Group || d.Items != null).ToList();
		}

		private class IfaceOptions
		{
			private List<NetworkInterface> interfaces;

			public IEnumerable<NetworkInterface> Interfaces
			{
				get
				{
					// ReSharper disable once ConvertIfStatementToNullCoalescingExpression
					if (interfaces == null)
						interfaces = NetworkInterface.GetAllNetworkInterfaces()
							.Where(i => i.OperationalStatus == OperationalStatus.Up)
							.Where(i => i.NetworkInterfaceType == NetworkInterfaceType.Ethernet
								|| i.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
								|| i.NetworkInterfaceType == NetworkInterfaceType.Loopback)
							.ToList();

					return interfaces;
				}
			}
		}

		private static List<ParamDetail> DefineToParamDetail(
			IEnumerable<PitDefines.Define> defines, 
			List<string> reserved, 
			IfaceOptions ifaces)
		{
			if (defines == null)
				return null;

			var ret = defines
				.Where(d => !reserved.Contains(d.Key))
				.Select(d => DefineToParamDetail(d, reserved, ifaces))
				.ToList();

			return ret.Count > 0 ? ret : null;
		}

		private static ParamDetail DefineToParamDetail(
			PitDefines.Define define, 
			List<string> reserved, 
			IfaceOptions ifaces)
		{
			var grp = define as PitDefines.Collection;

			var ret = new ParamDetail
			{
				Key = define.Key,
				Name = define.Name,
				Value = define.Value,
				Optional = define.Optional,
				Options = define.Defaults != null ? define.Defaults.ToList() : null,
				OS = grp != null ? grp.Platform.ToString() : null,
				Collapsed = grp != null && grp.Collapsed,
				Type = define.ConfigType,
				Min = define.Min,
				Max = define.Max,
				Description = define.Description,
				Items = DefineToParamDetail(define.Defines, reserved, ifaces)
			};

			switch (ret.Type)
			{
				case ParameterType.Hwaddr:
					ret.Options.AddRange(
						ifaces.Interfaces
							.Select(i => i.GetPhysicalAddress().GetAddressBytes())
							.Select(a => string.Join(":", a.Select(b => b.ToString("x2"))))
							.Where(s => !string.IsNullOrEmpty(s)));
					break;
				case ParameterType.Iface:
					ret.Options.AddRange(ifaces.Interfaces.Select(i => i.Name));
					break;
				case ParameterType.Ipv4:
					ret.Options.AddRange(
						ifaces.Interfaces
							.SelectMany(i => i.GetIPProperties().UnicastAddresses)
							.Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
							.Select(a => a.Address.ToString()));
					break;
				case ParameterType.Ipv6:
					ret.Options.AddRange(
						ifaces.Interfaces
							.SelectMany(i => i.GetIPProperties().UnicastAddresses)
							.Where(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6)
							.Select(a => a.Address.ToString()));
					break;
			}

			return ret;
		}
	}
}
