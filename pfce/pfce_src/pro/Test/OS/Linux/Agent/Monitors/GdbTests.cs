using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.OS.Linux.Agent.Monitors;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Peach.Pro.Test.OS.Linux.Agent.Monitors
{
	[TestFixture]
	[Quick]
	[Peach]
	[Platform("Linux")]
	public class GdbTests
	{
		private static readonly string CrashableServer = Utilities.GetAppResourcePath("CrashableServer");
		private static readonly string CrashingFileConsumer = Utilities.GetAppResourcePath("CrashingFileConsumer");
		private static readonly string CrashTest = Utilities.GetAppResourcePath("CrashTest");

		[Test]
		public void TestFault()
		{
			var self = Path.Combine(Utilities.ExecutionDirectory, "Peach.exe");

			var args = new Dictionary<string, string>() {
				{ "Executable", CrashingFileConsumer },
				{ "Arguments", self },
				{ "RestartOnEachTest", "true" },
			};

			var m = new GdbDebugger(null);
			m.StartMonitor(args);
			m.SessionStarting();
			m.IterationStarting(null);
			Thread.Sleep(5000);
			m.IterationFinished();
			Assert.IsTrue(m.DetectedFault(), "Should have detected fault");
			var fault = m.GetMonitorData();
			Assert.NotNull(fault, "Should have a fault");
			Assert.NotNull(fault.Fault, "Fault should have a Fault");
			Assert.AreEqual(3, fault.Data.Count);
			Assert.IsTrue(fault.Data.ContainsKey("StackTrace.txt"), "Fault should contain StackTrace.txt");
			Assert.IsTrue(fault.Data.ContainsKey("stdout.log"), "Fault should contain stdout.log");
			Assert.IsTrue(fault.Data.ContainsKey("stderr.log"), "Fault should contain stderr.log");
			Assert.Greater(fault.Data["StackTrace.txt"].Length, 0);
			StringAssert.Contains("PossibleStackCorruption", fault.Fault.Description);
			m.SessionFinished();
			m.StopMonitor();
		}

		[Test]
		public void TestNoFault()
		{
			var args = new Dictionary<string, string>() {
				{ "Executable", CrashableServer },
				{ "Arguments", "127.0.0.1 12346" },
			};

			var m = new GdbDebugger(null);
			m.StartMonitor(args);
			m.SessionStarting();
			m.IterationStarting(null);
			Thread.Sleep(5000);
			m.IterationFinished();
			Assert.IsFalse(m.DetectedFault());
			m.SessionFinished();
			m.StopMonitor();
		}

		[Test]
		public void TestMissingProgram()
		{
			var args = new Dictionary<string, string>() {
				{ "Executable",  "MissingProgram" },
			};

			var m = new GdbDebugger(null);
			m.StartMonitor(args);
			try
			{
				m.SessionStarting();
				Assert.Fail("should throw");
			}
			catch (PeachException ex)
			{
				Assert.AreEqual("GDB was unable to start 'MissingProgram'.", ex.Message);
			}

			m.SessionFinished();
		}

		[Test]
		public void TestMissingGdb()
		{
			var args = new Dictionary<string, string>() {
				{ "Executable",  "MissingProgram" },
				{ "GdbPath", "MissingGdb" },
			};

			var m = new GdbDebugger(null);
			m.StartMonitor(args);

			try
			{
				m.SessionStarting();
				Assert.Fail("should throw");
			}
			catch (PeachException ex)
			{
				const string exp = "Could not start debugger 'MissingGdb'.";
				var act = ex.Message.Substring(0, exp.Length);
				Assert.AreEqual(exp, act);
			}

			m.SessionFinished();
		}

		[Test]
		public void TestCpuKill()
		{
			var args = new Dictionary<string, string>() {
				{ "Executable", CrashableServer },
				{ "Arguments", "127.0.0.1 12346" },
				{ "StartOnCall", "Foo" },
			};

			var m = new GdbDebugger(null);
			m.StartMonitor(args);
			m.SessionStarting();
			m.IterationStarting(null);

			m.Message("Foo");
			Thread.Sleep(1000);

			var before = DateTime.Now;
			m.IterationFinished();
			var after = DateTime.Now;

			var span = (after - before);

			Thread.Sleep(1000);
			Assert.IsFalse(m.DetectedFault());
			m.SessionFinished();
			m.StopMonitor();

			Assert.GreaterOrEqual(span.TotalSeconds, 0.0);
			Assert.LessOrEqual(span.TotalSeconds, 0.5);
		}

		[Test]
		public void TestNoCpuKill()
		{
			var args = new Dictionary<string, string> {
				{ "Executable", CrashableServer },
				{ "Arguments", "127.0.0.1 0 5" },
				{ "StartOnCall", "Foo" },
				{ "NoCpuKill", "true" }
			};

			var m = new GdbDebugger(null);
			m.StartMonitor(args);
			m.SessionStarting();
			m.IterationStarting(null);

			m.Message("Foo");
			Thread.Sleep(1000);

			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();
			m.IterationFinished();
			sw.Stop();

			var span = sw.Elapsed;

			Assert.IsFalse(m.DetectedFault());
			m.SessionFinished();
			m.StopMonitor();

			Assert.GreaterOrEqual(span.TotalSeconds, 4);
			Assert.LessOrEqual(span.TotalSeconds, 6);
		}

		[Test]
		public void TestNoCpuKillWaitFail()
		{
			var args = new Dictionary<string, string>() {
				{ "Executable", CrashableServer },
				{ "Arguments", "127.0.0.1 0 5" },
				{ "StartOnCall", "Foo" },
				{ "NoCpuKill", "true" },
				{ "WaitForExitTimeout", "1000" },
			};

			var m = new GdbDebugger(null);
			m.StartMonitor(args);
			m.SessionStarting();
			m.IterationStarting(null);

			m.Message("Foo");
			Thread.Sleep(1000);

			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();
			m.IterationFinished();
			sw.Stop();

			var span = sw.Elapsed;

			Assert.IsFalse(m.DetectedFault());
			m.SessionFinished();
			m.StopMonitor();

			Assert.GreaterOrEqual(span.TotalSeconds, 0.5);
			Assert.LessOrEqual(span.TotalSeconds, 1.5);
		}

		[Test]
		public void TestSessionRestart()
		{
			var starts = 0;

			var runner = new MonitorRunner("Gdb", new Dictionary<string, string> {
				{ "Executable", CrashableServer },
				{ "Arguments", "127.0.0.1 0 1" },
			}) {
				StartMonitor = (m, args) =>
				{
					m.InternalEvent += (s, e) => ++starts;
					m.StartMonitor(args);
				},
				Message = m => Thread.Sleep(2000)
			};

			// Run for two iterations, expect no faults
			// but the process should be started twice
			var faults = runner.Run(2);

			Assert.AreEqual(0, faults.Length);
			Assert.AreEqual(2, starts);
		}

		[Test]
		public void TestRestartAfterFault()
		{
			var startCount = 0;
			var iteration = 0;

			var runner = new MonitorRunner("Gdb", new Dictionary<string, string> {
				{ "Executable", CrashableServer },
				{ "Arguments", "127.0.0.1 0" },
				{ "RestartAfterFault", "true" },
			}) {
				StartMonitor = (m, args) =>
				{
					m.InternalEvent += (s, e) => ++startCount;
					m.StartMonitor(args);
				},
				DetectedFault = m =>
				{
					Assert.False(m.DetectedFault(), "Should not have detected a fault");

					return ++iteration == 2;
				}
			};

			var faults = runner.Run(5);

			Assert.AreEqual(0, faults.Length);
			Assert.AreEqual(2, startCount);
		}

		[Test]
		public void TestForkingInferior()
		{
			var runner = new MonitorRunner("Gdb", new Dictionary<string, string> {
				{ "Executable", CrashTest },
				{ "Arguments", "fork" },
				{ "RestartOnEachTest", "true" },
			});

			var faults = runner.Run(2);

			Assert.AreEqual(0, faults.Length);
		}

		private static TcpClient Connect(int port, int timeout)
		{
			var tcp = (TcpClient)null;
			var sw = new Stopwatch();

			for (var i = 1; tcp == null; i *= 2)
			{
				try
				{
					// Must build a new client object after every failed attempt to connect.
					// For some reason, just calling BeginConnect again does not work on mono.
					tcp = new TcpClient();

					sw.Restart();

					var ar = tcp.BeginConnect(IPAddress.Loopback, port, null, null);
					if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeout)))
						throw new TimeoutException();
					tcp.EndConnect(ar);
				}
				catch
				{
					sw.Stop();

					if (tcp != null)
					{
						tcp.Close();
						tcp = null;
					}

					timeout -= (int)sw.ElapsedMilliseconds;

					if (timeout <= 0)
						throw;

					var waitTime = Math.Min(timeout, i);
					timeout -= waitTime;

					Thread.Sleep(waitTime);
				}
			}

			return tcp;
		}

		[Test]
		public void TestSessionLifetime()
		{
			var port = SetUpFixture.MakePort(45000, 46000);

			var startCount = 0;
			var iteration = 0;

			var runner = new MonitorRunner("Gdb", new Dictionary<string, string> {
				{ "Executable", CrashableServer },
				{ "Arguments", "127.0.0.1 " + port },
			}) {
				StartMonitor = (m, args) =>
				{
					m.InternalEvent += (s, e) => ++startCount;
					m.StartMonitor(args);
				},
				Message = m =>
				{
					using (var cli = Connect(port, 1000))
					{
						var ar = cli.BeginConnect(IPAddress.Loopback, port, null, null);
						if (!ar.AsyncWaitHandle.WaitOne(1000))
							Assert.Fail("Should have connected to CrashableServer in 1sec");
						cli.EndConnect(ar);

						if (++iteration == 2)
						{
							// Crash!
							cli.Client.Send(Encoding.ASCII.GetBytes(new string('A', 2000)));
						}
					}

					Thread.Sleep(500);
				},
			};

			var faults = runner.Run(10);

			Assert.AreEqual(1, faults.Length);
		}
	}
}
