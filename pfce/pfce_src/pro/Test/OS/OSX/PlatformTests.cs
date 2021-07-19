using System;
using System.Diagnostics;
using NUnit.Framework;
using Peach.Core;
using System.Linq;
using System.Threading;
using Peach.Core.Test;
using SysProcess = System.Diagnostics.Process;

namespace Peach.Pro.Test.OS.OSX
{
	[TestFixture]
	[Quick]
	[Peach]
	[Platform("MacOSX")]
	public class PlatformTests
	{
		[Test]
		public void TestCpuUsage()
		{
			using (var p = ProcessHelper.GetProcessById(1))
			{
				var pi = p.Snapshot();
				Assert.NotNull(pi);
				Assert.AreEqual(1, pi.Id);
				Assert.AreEqual("launchd", pi.ProcessName);
				Assert.Greater(pi.PrivilegedProcessorTicks, 0);
				Assert.Greater(pi.UserProcessorTicks, 0);
			}

			using (var p = ProcessHelper.Start("/bin/ls", "", null, null))
			{
				p.WaitForExit(Timeout.Infinite);

				Assert.False(p.IsRunning);

				Assert.Throws<InvalidOperationException>(() => p.Snapshot());
			}
		}

		[Test]
		public void GetProcByName()
		{
			string procName;
			int procId;

			using (var self = ProcessHelper.GetCurrentProcess())
			{
				// Use process snapshot so we are sure to get the correct name on osx
				var pi = self.Snapshot();

				procName = pi.ProcessName;
				procId = pi.Id;
			}

			var p = ProcessHelper.GetProcessesByName(procName);

			try
			{
				Assert.NotNull(p);
				Assert.Greater(p.Length, 0);

				var match = p.Select(i => i.Snapshot()).Where(i => i.Id == procId);

				Assert.AreEqual(1, match.Count());
			}
			finally
			{
				foreach (var i in p)
					i.Dispose();
			}
		}
	}
}
