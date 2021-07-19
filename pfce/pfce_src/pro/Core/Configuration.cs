using System.IO;
using NLog;
using Peach.Core;
using SysConfig = System.Configuration.Configuration;

namespace Peach.Pro.Core
{
	internal static class Configuration
	{
		public static string ScriptsPath { get; set; }

		public static string PluginsPath { get; set; }

		public static string LogRoot { get; set; }

		public static LogLevel LogLevel { get; set; }

		public static bool UseAsyncLogging { get; set; }

		static Configuration()
		{
			var config = Utilities.GetUserConfig();

			LogRoot = config.GetPath("LogRoot", "Logs");
			PluginsPath = config.GetPath("Plugins", "Plugins");
			ScriptsPath = config.GetPath("Scripts", "Scripts");
			UseAsyncLogging = true;
		}

		static string GetPath(this SysConfig config, string setting, string defPath)
		{
			var path =
				config.AppSettings.Settings.Get(setting) ??
				Utilities.GetAppResourcePath(defPath);
			return Path.GetFullPath(path);
			
		}
	}
}
