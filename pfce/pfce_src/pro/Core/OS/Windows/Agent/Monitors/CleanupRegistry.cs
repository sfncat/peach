

using System.Collections.Generic;
using Microsoft.Win32;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Monitor = Peach.Core.Agent.Monitor2;
using System.ComponentModel;

namespace Peach.Pro.OS.Windows.Agent.Monitors
{
	[Monitor("CleanupRegistry")]
	[Description("Remove a registry key or a key's children")]
	[Parameter("Key", typeof(string), "Registry key to remove.")]
	[Parameter("ChildrenOnly", typeof(bool), "Only cleanup sub-keys. (defaults to false)", "false")]
	public class CleanupRegistry : Monitor
	{
		static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		public string Key { get; set; }
		public bool ChildrenOnly { get; set; }
		public RegistryKey Root { get; set; }

		public CleanupRegistry(string name)
			: base(name)
		{
		}

		public override void StartMonitor(Dictionary<string, string> args)
		{
			base.StartMonitor(args);

			if (Key.StartsWith("HKCU\\"))
				Root = Registry.CurrentUser;
			else if (Key.StartsWith("HKCC\\"))
				Root = Registry.CurrentConfig;
			else if (Key.StartsWith("HKLM\\"))
				Root = Registry.LocalMachine;
			else if (Key.StartsWith("HKPD\\"))
				Root = Registry.PerformanceData;
			else if (Key.StartsWith("HKU\\"))
				Root = Registry.Users;
			else
				throw new PeachException("Error, CleanupRegistry monitor Key parameter must be prefixed with HKCU, HKCC, HKLM, HKPD, or HKU.");

			Key = Key.Substring(Key.IndexOf("\\", System.StringComparison.Ordinal) + 1);
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			if (!ChildrenOnly)
			{
				Logger.Debug("Removing key: " + Key);
				Root.DeleteSubKeyTree(Key, false);
				return;
			}

			var key = Root.OpenSubKey(Key, true);
			if (key == null)
				return;

			foreach (var subkey in key.GetSubKeyNames())
			{
				Logger.Debug("Removing subkey: " + subkey);
				key.DeleteSubKeyTree(subkey, false);
			}
		}
	}
}
