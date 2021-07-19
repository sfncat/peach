using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Peach.Pro.Core.OS;
using SysProcess = System.Diagnostics.Process;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	[Quick]
	[Peach]
	class SingleInstanceTests
	{
		[Test]
		public void TestBasic()
		{
			var argsList = new List<string>();
			var path = Path.Combine(Utilities.ExecutionDirectory, "CrashTestDummy.exe");
			if (Platform.GetOS() != Platform.OS.Windows)
			{
				argsList.Add(path);
				path = "mono";
			}
			var args = string.Join(" ", argsList);
			var proc = new SysProcess
			{
				StartInfo = new ProcessStartInfo(path, args)
				{
					RedirectStandardOutput = true,
					CreateNoWindow = true,
					UseShellExecute = false
				}
			};

			proc.OutputDataReceived += (e, d) =>
			{
				if (!string.IsNullOrEmpty(d.Data))
					Console.WriteLine(d.Data);
			};

			using (var mutex = Pal.SingleInstance("CrashTestDummy"))
			{
				mutex.Lock();

				proc.Start();
				proc.BeginOutputReadLine();

				Assert.IsFalse(proc.WaitForExit(5000));
			}

			Assert.IsTrue(proc.WaitForExit(2000));
		}

		[Test]
		public void TestSameProc()
		{
			ISingleInstance m1 = null;
			try
			{
				m1 = Pal.SingleInstance("TestSameProc");
				Assert.True(m1.TryLock(), "First lock should pass");

				Task.Factory.StartNew(() =>
				{
					using (var m2 = Pal.SingleInstance("TestSameProc"))
					{
						Assert.False(m2.TryLock(), "Shouldn't grab lock when its held");
					}
				}).Wait();

				var tsk = Task.Factory.StartNew(() =>
				{
					using (var m2 = Pal.SingleInstance("TestSameProc"))
					{
						Assert.False(m2.TryLock(), "Still shouldn't grab lock when its held");

						m2.Lock();
					}
				});

				Assert.False(tsk.Wait(2000), "Task should be waiting on mutex");

				m1.Dispose();
				m1 = null;

				Assert.True(tsk.Wait(10000), "Task should be done");
			}
			finally
			{
				if (m1 != null)
					m1.Dispose();
			}
		}

		[Test]
		public void TestCanarySameProc()
		{
			var guid = Guid.NewGuid();

			// No one is holding canary, should get it
			using (var c1 = Pal.GetCanary(guid))
			{
				Assert.NotNull(c1, "1st get on canary should be non-null");

				for (var i = 0; i < 100; ++i)
				{
					// Someone is holding canary, should not get it
					using (var c2 = Pal.GetCanary(guid))
					{
						Assert.Null(c2, "2nd get on canary should be null");
					}
				}
			}

			// No one is holding canary, should get it
			using (var c3 = Pal.GetCanary(guid))
			{
				Assert.NotNull(c3, "3rd get on canary should be non-null");
			}
		}

		[Test]
		public void TestCanaryMultiProc()
		{
			var guid = Guid.NewGuid();
			var argsList = new List<string>();
			var path = Path.Combine(Utilities.ExecutionDirectory, "CrashTestDummy.exe");
			if (Platform.GetOS() != Platform.OS.Windows)
			{
				argsList.Add(path);
				path = "mono";
			}

			argsList.Add(guid.ToString());

			var args = string.Join(" ", argsList);
			var proc = new SysProcess
			{
				StartInfo = new ProcessStartInfo(path, args)
				{
					RedirectStandardOutput = true,
					CreateNoWindow = true,
					UseShellExecute = false
				}
			};

			proc.OutputDataReceived += (e, d) =>
			{
				if (!string.IsNullOrEmpty(d.Data))
					Console.WriteLine(d.Data);
			};

			using (var mutex = Pal.SingleInstance("CrashTestDummy"))
			{
				mutex.Lock();

				proc.Start();
				proc.BeginOutputReadLine();

				Assert.IsFalse(proc.WaitForExit(5000));

				using (var c = Pal.GetCanary(guid))
				{
					Assert.Null(c, "Canary should be held by other process");
				}

				proc.Kill();
			}

			Assert.IsTrue(proc.WaitForExit(1000));

			using (var c = Pal.GetCanary(guid))
			{
				Assert.NotNull(c, "Canary should no longer be held");
			}
		}
	}
}
