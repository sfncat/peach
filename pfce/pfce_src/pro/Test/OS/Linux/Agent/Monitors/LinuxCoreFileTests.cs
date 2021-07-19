using System;
using NUnit.Framework;
using Peach.Core.Test;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Pro.Core.OS;

namespace Peach.Pro.Test.OS.Linux.Agent.Monitors
{
	[TestFixture]
	[Quick]
	[Peach]
	[Platform("Linux")]
	class LinuxCoreFileTests
	{
		ISingleInstance _si;
		TempDirectory _tmp;

		[SetUp]
		public void SetUp()
		{
			_tmp = new TempDirectory();

			// Ensure only 1 instance of the test runs at a time
			_si = Pal.SingleInstance(Assembly.GetExecutingAssembly().FullName);
			_si.Lock();
		}

		[TearDown]
		public void TearDown()
		{
			if (_tmp != null)
			{
				_tmp.Dispose();
				_tmp = null;
			}

			if (_si != null)
			{
				_si.Dispose();
				_si = null;
			}
		}

		[Test]
		public void TestNoFault()
		{
			// Verify no .info means no fault

			var runner = new MonitorRunner("LinuxCoreFile", new Dictionary<string, string>
			{
				{ "LogFolder", _tmp.Path }
			});

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length);
		}

		[Test]
		public void TestClearFolder()
		{
			// Verify pre-exiting .info files don't trigger faults

			var runner = new MonitorRunner("LinuxCoreFile", new Dictionary<string, string>
			{
				{ "LogFolder", _tmp.Path }
			});

			File.WriteAllText(Path.Combine(_tmp.Path, "aaa.info"), "Crash Description");
			File.WriteAllText(Path.Combine(_tmp.Path, "aaa.core"), "Core Dump");

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length);
		}

		[Test]
		public void TestSimulatedFault()
		{
			// Verify we fault when a .info files is detected, and save the .core file

			var it = 0;
			var runner = new MonitorRunner("LinuxCoreFile", new Dictionary<string, string>
			{
				{ "LogFolder", _tmp.Path }
			})
			{
				IterationFinished = m =>
				{
					if (++it == 1)
					{
						File.WriteAllText(Path.Combine(_tmp.Path, "aaa.info"), "Crash Description");
						File.WriteAllText(Path.Combine(_tmp.Path, "aaa.core"), "Core Dump");
					}
				}
			};

			var faults = runner.Run(10);

			Assert.AreEqual(1, faults.Length);

			Assert.NotNull(faults[0].Fault);
			Assert.AreEqual("aaa core dumped", faults[0].Title);
			Assert.AreEqual("Crash Description", faults[0].Fault.Description);

			Assert.AreEqual(2, faults[0].Data.Keys.Count);
			Assert.True(faults[0].Data.ContainsKey("aaa.info"));
			Assert.AreEqual(Encoding.ASCII.GetBytes("Crash Description"), ((MemoryStream)faults[0].Data["aaa.info"]).ToArray());
			Assert.True(faults[0].Data.ContainsKey("aaa.core"));
			Assert.AreEqual(Encoding.ASCII.GetBytes("Core Dump"), ((MemoryStream)faults[0].Data["aaa.core"]).ToArray());
		}

		[Test]
		public void TestSimulatedFaultNoCore()
		{
			// Verify we fault when a .info files is detected, and can handle no .core file

			var it = 0;
			var runner = new MonitorRunner("LinuxCoreFile", new Dictionary<string, string>
			{
				{ "LogFolder", _tmp.Path }
			})
			{
				IterationFinished = m =>
				{
					if (++it == 1)
					{
						File.WriteAllText(Path.Combine(_tmp.Path, "aaa.info"), "Crash Description");
					}
				}
			};

			var faults = runner.Run(10);

			Assert.AreEqual(1, faults.Length);

			Assert.NotNull(faults[0].Fault);
			Assert.AreEqual("aaa core dumped", faults[0].Title);
			Assert.AreEqual("Crash Description", faults[0].Fault.Description);

			Assert.AreEqual(1, faults[0].Data.Keys.Count);
			Assert.True(faults[0].Data.ContainsKey("aaa.info"));
			Assert.AreEqual(Encoding.ASCII.GetBytes("Crash Description"), ((MemoryStream)faults[0].Data["aaa.info"]).ToArray());
		}

		[Test]
		public void TestExecutableFault()
		{
			// Verify we fault when a .info files is detected that matches Executable param

			var it = 0;
			var runner = new MonitorRunner("LinuxCoreFile", new Dictionary<string, string>
			{
				{ "LogFolder", _tmp.Path },
				{ "Executable", "aaa" }
			})
			{
				IterationFinished = m =>
				{
					if (++it == 1)
					{
						File.WriteAllText(Path.Combine(_tmp.Path, "aaa.info"), "Crash Description");
						File.WriteAllText(Path.Combine(_tmp.Path, "aaa.core"), "Core Dump");
					}
				}
			};

			var faults = runner.Run(10);

			Assert.AreEqual(1, faults.Length);

			Assert.NotNull(faults[0].Fault);
			Assert.AreEqual("aaa core dumped", faults[0].Title);
			Assert.AreEqual("Crash Description", faults[0].Fault.Description);

			Assert.AreEqual(2, faults[0].Data.Keys.Count);
			Assert.True(faults[0].Data.ContainsKey("aaa.info"), "Should contain aaa.info");
			Assert.AreEqual(Encoding.ASCII.GetBytes("Crash Description"), ((MemoryStream)faults[0].Data["aaa.info"]).ToArray());
			Assert.True(faults[0].Data.ContainsKey("aaa.core"), "Should contain aaa.core");
			Assert.AreEqual(Encoding.ASCII.GetBytes("Core Dump"), ((MemoryStream)faults[0].Data["aaa.core"]).ToArray());
		}

		[Test]
		public void TestExecutableNoFault()
		{
			// Verify we don't fault when a .info files is detected that doesn't match Executable param

			var it = 0;
			var runner = new MonitorRunner("LinuxCoreFile", new Dictionary<string, string>
			{
				{ "LogFolder", _tmp.Path },
				{ "Executable", "bbb" }
			})
			{
				IterationFinished = m =>
				{
					if (++it == 1)
					{
						File.WriteAllText(Path.Combine(_tmp.Path, "aaa.info"), "Crash Description");
						File.WriteAllText(Path.Combine(_tmp.Path, "aaa.core"), "Core Dump");
					}
				}
			};

			var faults = runner.Run(10);

			Assert.AreEqual(0, faults.Length);
		}

		[Test]
		public void TestFault()
		{
			// Ensure we can catch an actual core dump

			var it = 0;
			var runner = new MonitorRunner("LinuxCoreFile", new Dictionary<string, string>
			{
				{ "LogFolder", _tmp.Path }
			})
			{
				IterationStarting = (m, i) =>
				{
					if (++it == 1)
					{
						// Crash!
						var tgt = Utilities.GetAppResourcePath("CrashingFileConsumer");
						ProcessHelper.Run(tgt, "/bin/ls", null, null, -1);

						// Ensure core shows up!
						Thread.Sleep(5000);
					}
				}
			};

			var faults = runner.Run(10);

			Assert.AreEqual(1, faults.Length);

			Assert.NotNull(faults[0].Fault);
			Assert.AreEqual("CrashingFileConsumer core dumped", faults[0].Title);
			StringAssert.Contains("Linux Crash Handler -- Crash Information", faults[0].Fault.Description);

			Console.WriteLine(faults[0].Fault.Description);

			var keys = faults[0].Data.Keys.ToList();

			keys.Sort();

			Assert.AreEqual(2, keys.Count);

			StringAssert.IsMatch("CrashingFileConsumer\\.\\d+\\.core", keys[0]);
			StringAssert.IsMatch("CrashingFileConsumer\\.\\d+\\.info", keys[1]);

			Assert.AreEqual(Monitor2.Hash("LinuxCoreFile.CrashingFileConsumer"), faults[0].Fault.MajorHash);
			Assert.AreEqual(Monitor2.Hash("CORE"), faults[0].Fault.MinorHash);
		}

		[Test]
		public void TestLongPath()
		{
			// Verify nice error messages if LogFolder value is too long

			var path = Path.Combine(_tmp.Path, _tmp.Path.Substring(1), _tmp.Path.Substring(1));

			Assert.Greater(path.Length, 60);

			var runner = new MonitorRunner("LinuxCoreFile", new Dictionary<string, string>
			{
				{ "LogFolder", path }
			});

			var ex = Assert.Throws<PeachException>(() => runner.Run());

			StringAssert.Contains("The specified log folder is too long, it must be less than 60 characters", ex.Message);
			StringAssert.Contains(path, ex.Message);
		}

		[Test]
		public void TestTwoMonitors()
		{
			// Verify only one instance of the monitor is allowed

			var runner = new MonitorRunner("LinuxCoreFile", new Dictionary<string, string>
			{
				{ "LogFolder", _tmp.Path }
			});

			using (var si = Pal.SingleInstance("LinuxCoreFile"))
			{
				Assert.True(si.TryLock(), "SingleInstance should have locked");

				var ex = Assert.Throws<PeachException>(() => runner.Run());
				Assert.AreEqual("Only a single running instance of the core file monitor is allowed on a host at any time.", ex.Message);
			}
		}
	}
}
