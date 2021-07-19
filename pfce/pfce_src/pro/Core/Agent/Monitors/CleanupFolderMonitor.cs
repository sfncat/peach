using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Monitor = Peach.Core.Agent.Monitor2;

namespace Peach.Pro.Core.Agent.Monitors
{
	[Monitor("CleanupFolder")]
	[Description("Remove folder contents created by a target during runtime")]
	[Parameter("Folder", typeof(string), "The folder to cleanup.")]
	public class CleanupFolderMonitor : Monitor
	{
		static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		List<string> _folderListing;

		public string Folder { get; set; }

		public CleanupFolderMonitor(string name)
			: base(name)
		{
		}

		public override void SessionStarting()
		{
			_folderListing = GetListing();
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			var toDel = GetListing().Except(_folderListing);

			foreach (var item in toDel)
			{
				try
				{
					var attr = File.GetAttributes(item);
					if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
						Directory.Delete(item, true);
					else
						File.Delete(item);
				}
				catch (Exception ex)
				{
					Logger.Debug("Could not delete '{0}'. {1}", item, ex.Message);
				}
			}
		}

		List<string> GetListing()
		{
			try
			{
				return Directory.EnumerateFileSystemEntries(Folder, "*", SearchOption.TopDirectoryOnly).ToList();
			}
			catch (Exception ex)
			{
				Logger.Debug("Could not list contents of folder '{0}'. {1}", Folder, ex.Message);
				return new List<string>();
			}
		}
	}
}
