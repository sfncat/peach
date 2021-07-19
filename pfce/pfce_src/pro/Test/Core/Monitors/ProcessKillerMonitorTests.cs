using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Monitors
{
	[TestFixture]
	[Quick]
	[Peach]
	class ProcessKillerMonitorTests
	{
		class Target : IDisposable
		{
			private readonly string _fileName;
			private Process _process;

			public string ProcName { get; private set; }

			public Target()
			{
				const string cs = "CrashableServer";
				var suffix = Platform.GetOS() == Platform.OS.Windows ? ".exe" : "";
				var tmp = Path.GetTempFileName();
				var dir = Path.GetDirectoryName(tmp);

				if (string.IsNullOrEmpty(dir))
					Assert.Fail("Temp directory should not be null or empty");

				ProcName = cs + "-" + Guid.NewGuid();
				_fileName = Path.Combine(dir, ProcName + suffix);

				File.Delete(tmp);
				File.Copy(Utilities.GetAppResourcePath(cs + suffix), _fileName);

				_process = ProcessHelper.Start(_fileName, "127.0.0.1 0", null, null);
			}

			public void Dispose()
			{
				if (_process != null)
				{
					// Disposing will internally kill process if needed
					_process.Dispose();
					_process = null;
				}

				try
				{
					File.Delete(_fileName);
				}
				catch
				{
					// Ignore errors
				}
			}
		}

		[Test]
		public void TestBadProcss()
		{
			// Nothing happens if process doesn't exist

			var runner = new MonitorRunner("ProcessKiller", new Dictionary<string, string>
			{
				{"ProcessNames", "some_invalid_process"},
			});

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length);
		}

		[Test]
		public void TestSingleProcess()
		{
			using (var t1 = new Target())
			{
				var procName = t1.ProcName;

				var runner = new MonitorRunner("ProcessKiller", new Dictionary<string, string>
				{
					{ "ProcessNames", procName }
				})
				{
					IterationFinished = m =>
					{
						Assert.True(ProcessExists(procName), "Process '{0}' should exist before IterationFinished".Fmt(procName));

						m.IterationFinished();

						Assert.False(ProcessExists(procName), "Process '{0}' should not exist after IterationFinished".Fmt(procName));
					}
				};

				var faults = runner.Run();

				// never faults!
				Assert.AreEqual(0, faults.Length);

				Assert.False(ProcessExists(procName), "Process '{0}' should not exist after test".Fmt(procName));
			}
		}

		[Test]
		public void TestMultiProcess()
		{
			using (var t1 = new Target())
			{
				using (var t2 = new Target())
				{
					var procName1 = t1.ProcName;
					var procName2 = t2.ProcName;

					var runner = new MonitorRunner("ProcessKiller", new Dictionary<string, string>
					{
						{ "ProcessNames", "{0},{1},some_invalid_process".Fmt(procName1, procName2) }
					})
					{
						IterationFinished = m =>
						{
							Assert.True(ProcessExists(procName1), "Process 1 '{0}' should exist before IterationFinished".Fmt(procName1));
							Assert.True(ProcessExists(procName2), "Process 2 '{0}' should exist before IterationFinished".Fmt(procName2));

							m.IterationFinished();

							Assert.False(ProcessExists(procName1), "Process '{0}' should not exist after IterationFinished".Fmt(procName1));
							Assert.False(ProcessExists(procName2), "Process '{0}' should not exist after IterationFinished".Fmt(procName2));
						}
					};

					var faults = runner.Run();

					// never faults!
					Assert.AreEqual(0, faults.Length);

					Assert.False(ProcessExists(procName1), "Process '{0}' should not exist after test".Fmt(procName1));
					Assert.False(ProcessExists(procName2), "Process '{0}' should not exist after test".Fmt(procName2));
				}
			}
		}

		static bool ProcessExists(string name)
		{
			var procs = ProcessHelper.GetProcessesByName(name);
			procs.ForEach(p => p.Dispose());
			return procs.Length > 0;
		}
	}
}