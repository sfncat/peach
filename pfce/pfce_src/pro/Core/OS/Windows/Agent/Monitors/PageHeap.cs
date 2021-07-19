

using System;
using System.Collections.Generic;
using System.IO;
using Peach.Core;
using Peach.Core.Agent;
using Monitor = Peach.Core.Agent.Monitor2;
using System.ComponentModel;

namespace Peach.Pro.OS.Windows.Agent.Monitors
{
	[Monitor("PageHeap")]
	[Description("Enables page heap debugging options for an executable")]
	[Parameter("Executable", typeof(string), "Name of executable to enable")]
	[Parameter("WinDbgPath", typeof(string), "Path to WinDbg install.  If not provided we will try and locate it.", "")]
	public class PageHeap : Monitor
	{
		public string Executable { get; set; }
		public string WinDbgPath { get; set; }

		private const string Gflags = "gflags.exe";
		private const string GflagsArgsEnable = "/p /enable \"{0}\" /full";
		private const string GflagsArgsDisable = "/p /disable \"{0}\"";

		public PageHeap(string name)
			: base(name)
		{
		}

		public override void StartMonitor(Dictionary<string, string> args)
		{
			base.StartMonitor(args);

			WinDbgPath = WindowsKernelDebugger.FindWinDbg(WinDbgPath);
		}

		public override void SessionStarting()
		{
			Enable();
		}

		public override void SessionFinished()
		{
			Disable();
		}

		protected void Enable()
		{
			try
			{
				Run(string.Format(GflagsArgsEnable, Executable));
			}
			catch (Exception ex)
			{
				throw new PeachException("Error, Enable PageHeap: " + ex.Message, ex);
			}
		}

		protected void Disable()
		{
			try
			{
				Run(string.Format(GflagsArgsDisable, Executable));
			}
			catch (Exception ex)
			{
				throw new PeachException("Error, Disable PageHeap: " + ex.Message, ex);
			}
		}

		private void Run(string args)
		{
			ProcessHelper.Run(Path.Combine(WinDbgPath, Gflags), args, null, null, -1);
		}
	}
}
