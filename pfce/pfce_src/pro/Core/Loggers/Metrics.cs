using System;
using System.Collections.Generic;
using Peach.Core;

namespace Peach.Pro.Core.Loggers
{

	[Logger("Metrics", true)]
	[Obsolete]
	[Parameter("Path", typeof(string), "Log folder")]
	public class MetricsLogger : Logger
	{
		public MetricsLogger(Dictionary<string, Variant> args)
		{
			NLog.LogManager.GetCurrentClassLogger().Warn("The Metrics logger is obsolete.");
		}
	}
}
