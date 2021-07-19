using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Agent;
using Peach.Pro.OS.OSX.Agent.Monitors;
using Peach.Core.Test;
using Peach.Pro.Core.OS;

namespace Peach.Pro.Test.OS.OSX.Agent.Monitors
{
	[TestFixture]
	[Quick]
	[Peach]
	[Platform("MacOSX")]
	public class CrashReporterTest
	{
		ISingleInstance _si;

		[SetUp]
		public void SetUp()
		{
			// Ensure only 1 instance of the platform tests runs at a time
			_si = Pal.SingleInstance(Assembly.GetExecutingAssembly().FullName);
			_si.Lock();
		}

		[TearDown]
		public void TearDown()
		{
			if (_si != null)
			{
				_si.Dispose();
				_si = null;
			}
		}

		static string CrashingProcess
		{
			get { return Utilities.GetAppResourcePath("CrashingProgram"); }
		}

		[Test]
		public void NoProcessNoFault()
		{
			// ProcessName argument not provided to the monitor
			// When no crashing program is run, the monitor should not detect a fault

			var args = new Dictionary<string, string>();
			const string peach = "";
			const bool shouldFault = false;

			RunProcess(peach, null, shouldFault, args);
		}

		[Test]
		public void NoProcessFault()
		{
			// ProcessName argument not provided to the monitor
			// When crashing program is run, the monitor should detect a fault

			var args = new Dictionary<string, string>();
			const string peach = "qwertyuiopasdfghjklzxcvbnm";
			const bool shouldFault = true;

			var fault = RunProcess(peach, CrashingProcess, shouldFault, args);

			Assert.NotNull(fault);
			Assert.NotNull(fault.Fault);
			Assert.Greater(fault.Data.Count, 0);
			foreach (var item in fault.Data)
			{
				Assert.NotNull(item.Key);
				Assert.Greater(item.Value.Length, 0);
			}
		}

		[Test]
		public void ProcessFault()
		{
			// Correct ProcessName argument is provided to the monitor
			// When crashing program is run, the monitor should detect a fault

			var args = new Dictionary<string, string>
			{
				{ "ProcessName", "CrashingProgram" },
			};
			const string peach = "qwertyuiopasdfghjklzxcvbnm";
			const bool shouldFault = true;

			var fault = RunProcess(peach, CrashingProcess, shouldFault, args);

			Assert.NotNull(fault);
			Assert.NotNull(fault.Fault);
			Assert.Greater(fault.Data.Count, 0);
			foreach (var item in fault.Data)
			{
				Assert.NotNull(item.Key);
				Assert.Greater(item.Value.Length, 0);
			}
		}

		[Test]
		public void WrongProcessFault()
		{
			// Incorrect ProcessName argument is provided to the monitor
			// When crashing program is run, the monitor should not detect a fault

			var args = new Dictionary<string, string>
			{
				{ "ProcessName", "WrongCrashingProgram" },
			};
			const string peach = "qwertyuiopasdfghjklzxcvbnm";
			const bool shouldFault = false;

			RunProcess(peach, CrashingProcess, shouldFault, args);
		}

		private static MonitorData RunProcess(string peach, string process, bool shouldFault, Dictionary<string, string> args)
		{
			var reporter = new CrashReporter(null);
			reporter.StartMonitor(args);
			reporter.SessionStarting();
			reporter.IterationStarting(null);
			if (process != null)
			{
				using (var p = new System.Diagnostics.Process())
				{
					p.StartInfo = new System.Diagnostics.ProcessStartInfo();
					p.StartInfo.EnvironmentVariables["PEACH"] = peach;
					p.StartInfo.UseShellExecute = false;
					p.StartInfo.FileName = process;
					p.Start();
				}
			}
			Thread.Sleep(2000);
			reporter.IterationFinished();
			Assert.AreEqual(shouldFault, reporter.DetectedFault());
			var fault = reporter.GetMonitorData();
			reporter.StopMonitor();
			return fault;
		}
	}
}

