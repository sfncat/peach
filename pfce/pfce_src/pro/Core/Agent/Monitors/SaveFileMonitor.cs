using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Peach.Core;
using Peach.Core.Agent;
using Monitor = Peach.Core.Agent.Monitor2;

namespace Peach.Pro.Core.Agent.Monitors
{
	/// <summary>
	/// Save a file when a fault occurs.
	/// </summary>
	[Monitor("SaveFile")]
	[Description("Saves the specified file as part of the logged data when a fault occurs")]
	[Parameter("Filename", typeof(string), "File to save on fault")]
	public class SaveFileMonitor : Monitor
	{
		public string Filename { get; set; }

		public SaveFileMonitor(string name)
			: base(name)
		{
		}

		public override MonitorData GetMonitorData()
		{
			if (!File.Exists(Filename))
				return null;

			var ret = new MonitorData
			{
				Title = "Save File \"{0}\".".Fmt(Filename),
				Data = new Dictionary<string, Stream>
				{
					{ Path.GetFileName(Filename), new MemoryStream(File.ReadAllBytes(Filename)) }
				}
			};

			return ret;
		}
	}
}
