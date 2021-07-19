using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.OS.Windows.Agent.Monitors;
using System;
using System.Collections.Generic;
using System.IO;
using Peach.Pro.Core.OS.Windows;

namespace Peach.Pro.Test.OS.Windows.Agent.Monitors
{
	[TestFixture]
	[Quick]
	[Peach]
	[Platform("Win")]
	class PageHeapTests
	{
		const string Monitor = "PageHeap";

		private bool Check(string exe)
		{
			var dbgPath = WindowsKernelDebugger.FindWinDbg(null);
			var p = ProcessHelper.Run(Path.Combine(dbgPath, "gflags.exe"), "/p", null, null, -1);
			var stdout = p.StdOut.ToString();
			var lines = stdout.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

			bool isFirst = true;
			foreach (var line in lines)
			{
				if (isFirst)
				{
					isFirst = false;
					continue;
				}

				if (line.Contains("{0}: page heap enabled".Fmt(exe)))
					return true;
			}

			return false;
		}

		[SetUp]
		public void SetUp()
		{
			if (Platform.GetOS() == Platform.OS.Windows)
			{
				if (!Environment.Is64BitProcess && Environment.Is64BitOperatingSystem)
					Assert.Ignore("32bit builds are not aupported on 64bit operating systems");

				if (Environment.Is64BitProcess && !Environment.Is64BitOperatingSystem)
					Assert.Ignore("64bit builds are not aupported on 32bit operating systems");
			}
		}

		[Test]
		public void TestNoParams()
		{
			var runner = new MonitorRunner(Monitor, new Dictionary<string, string>());
			var ex = Assert.Catch(() => runner.Run());
			Assert.That(ex, Is.InstanceOf<PeachException>());
			var msg = "Could not start monitor \"PageHeap\".  Monitor 'PageHeap' is missing required parameter 'Executable'.";
			StringAssert.StartsWith(msg, ex.Message);
		}

		[Test]
		public void TestBasic()
		{
			if (!Privilege.IsUserAdministrator())
				Assert.Ignore("User is not an administrator.");

			var exe = "foobar";
			var runner = new MonitorRunner(Monitor, new Dictionary<string, string>
			{
				{"Executable", exe},
			})
			{
				SessionStarting = m =>
				{
					m.SessionStarting();
					Assert.IsTrue(Check(exe), "PageHeap should be enabled");
				},
				SessionFinished = m =>
				{
					m.SessionFinished();
					Assert.IsFalse(Check(exe), "PageHeap should be disabled");
				},
			};
			runner.Run();
		}
	}
}
