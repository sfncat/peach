using System.Collections.Generic;
using System.Linq;
using Peach.Core;
using DomMonitor = Peach.Core.Dom.Monitor;
using DomObject = Peach.Core.Dom.Dom;
using WebAgent = Peach.Pro.Core.WebServices.Models.Agent;
using Peach.Pro.Core.WebServices.Models;
using System.Text;

namespace Peach.Pro.Core
{
	public static class PitInjector
	{
		public static void InjectDefines(PitConfig cfg, PitDefines defs, List<KeyValuePair<string, string>> ret)
		{
			foreach (var item in cfg.Config)
			{
				if (item.Key == "PitLibraryPath" || 
					defs.SystemDefines.Any(d => d.Key == item.Key))
					continue;

				var i = ret.FindIndex(x => x.Key == item.Key);
				if (i < 0)
					ret.Add(new KeyValuePair<string, string>(item.Key, item.Value));
				else
					ret[i] = new KeyValuePair<string, string>(item.Key, item.Value);
			}
		}

		public static void InjectAgents(PitConfig cfg, IEnumerable<KeyValuePair<string, string>> defs, DomObject dom)
		{
			dom.agents.Clear();
			foreach (var test in dom.tests)
			{
				test.agents.Clear();
			}

			foreach (var agent in cfg.Agents)
			{
				var domAgent = new Peach.Core.Dom.Agent
				{
					Name = Expand(defs, agent.Name) ?? dom.agents.UniqueName(),
					location = Expand(defs, agent.AgentUrl) ?? "local://",
					monitors = ConvertMonitors(agent, defs),
				};

				dom.agents.Add(domAgent);

				foreach (var test in dom.tests)
				{
					test.agents.Add(domAgent);
				}
			}
		}

		private static string Expand(IEnumerable<KeyValuePair<string, string>> defs, string value)
		{
			if (string.IsNullOrEmpty(value))
				return null;

			var sb = new StringBuilder(value);
			foreach (var kv in defs)
			{
				sb.Replace("##" + kv.Key + "##", kv.Value);
			}
			return sb.ToString();
		}

		private static NamedCollection<DomMonitor> ConvertMonitors(WebAgent agent, IEnumerable<KeyValuePair<string, string>> defs)
		{
			var monitors = new NamedCollection<DomMonitor>();
			foreach (var monitor in agent.Monitors)
			{
				monitors.Add(new DomMonitor
				{
					cls = Expand(defs, monitor.MonitorClass),
					Name = Expand(defs, monitor.Name) ?? monitors.UniqueName(),
					parameters = ConvertParameters(monitor, defs),
				});
			}
			return monitors;
		}

		private static Dictionary<string, Variant> ConvertParameters(Monitor monitor, IEnumerable<KeyValuePair<string, string>> defs)
		{
			var ret = new Dictionary<string, Variant>();
			foreach (var x in monitor.Map)
			{
				if (!string.IsNullOrEmpty(x.Value))
					ret.Add(x.Key ?? x.Name, new Variant(Expand(defs, x.Value)));
			}
			return ret;
		}
	}
}

