using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	[Quick]
	[Peach]
	public class ProcessTests
	{
		static string GetTarget(string name)
		{
			var suffix = (Platform.GetOS() == Platform.OS.Windows) ? ".exe" : "";
			return Utilities.GetAppResourcePath(name + suffix);
		}

		public static readonly string CrashingFileConsumer = GetTarget("CrashingFileConsumer");
		public static readonly string CrashableServer = GetTarget("CrashableServer");
		public static readonly string CrashTest = GetTarget("CrashTest");
		public static readonly string CrashTestDummy = GetTarget("CrashTestDummy");

		[Test]
		public void OwnedDispose()
		{
			// Non-attached (owned) processes should be stopped during Dispose()

			int pid;

			var sw = Stopwatch.StartNew();

			using (var p = ProcessHelper.Start(CrashableServer, "127.0.0.1 0 5000", null, null))
			{
				pid = p.Id;
			}

			var elapsed = sw.Elapsed;

			// Program should not have run to completion
			Assert.Less(elapsed, TimeSpan.FromSeconds(1000));

			// Process should not be running
			Assert.Throws<ArgumentException>(() =>
			{
				for (var i = 0; i < 10; ++i)
				{
					ProcessHelper.GetProcessById(pid);
					Thread.Sleep(10);
				}
			});
		}

		[Test]
		public void AttachedDispose()
		{
			// Attached processes (non-owned) should not be stopped during Dispose()

			int pid;

			var sw = Stopwatch.StartNew();

			using (var p1 = ProcessHelper.Start(CrashableServer, "127.0.0.1 0 5000", null, null))
			{
				pid = p1.Id;

				using (var p2 = ProcessHelper.GetProcessById(p1.Id))
				{
					Assert.True(p2.IsRunning, "Process p2 sohuld be running");
				}
				var elapsed1 = sw.Elapsed;

				// Program should not have run to completion
				Assert.Less(elapsed1, TimeSpan.FromSeconds(5000));

				// Disposing the attached process should not cause a stop to occur
				Assert.True(p1.IsRunning, "Process p1 sohuld be running");
			}

			var elapsed2 = sw.Elapsed;

			// Program should not have run to completion
			Assert.Less(elapsed2, TimeSpan.FromSeconds(1000));

			// Process should not be running
			Assert.Throws<ArgumentException>(() => ProcessHelper.GetProcessById(pid));
		}

		[Test]
		public void TestRunCompleteZero()
		{
			var result = ProcessHelper.Run(CrashTest, "exit 0", null, null, 10000);

			Assert.False(result.Timeout, "Process should not have timed out");
			Assert.AreEqual(0, result.ExitCode, "Exit code should be 0");

			Assert.Throws<ArgumentException>(() => ProcessHelper.GetProcessById(result.Pid));
		}

		[Test]
		public void TestRunCompleteNonZero()
		{
			var result = ProcessHelper.Run(CrashTest, "exit 1", null, null, 10000);

			Assert.False(result.Timeout, "Process should not have timed out");
			Assert.AreEqual(1, result.ExitCode, "Exit code should be 1");

			Assert.Throws<ArgumentException>(() => ProcessHelper.GetProcessById(result.Pid));
		}

		[Test]
		public void TestRunTimeout()
		{
			var result = ProcessHelper.Run(CrashableServer, "127.0.0.1 0", null, null, 500);

			Assert.True(result.Timeout, "Process should have timed out");
			Assert.AreEqual(-1, result.ExitCode, "Exit code should be -1");

			Assert.Throws<ArgumentException>(() => ProcessHelper.GetProcessById(result.Pid));
		}

		[Test]
		public void TestAttachStopSuccess()
		{
			// 1) Attach to process that stops on SIGTERM
			// 2) Call Stop(1000)
			// 3) Verify it stops before the timeout occurs

			int pid;

			using (var p1 = ProcessHelper.Start(CrashableServer, "127.0.0.1 0 1000", null, null))
			{
				pid = p1.Id;

				using (var p2 = ProcessHelper.GetProcessById(p1.Id))
				{
					var sw = Stopwatch.StartNew();
					p2.Stop(10000);
					var elapsed = sw.Elapsed;

					// Stop should have returned before 10 seconds
					Assert.Less(elapsed, TimeSpan.FromSeconds(10));

					// Process gets killed by stop success
					Assert.False(p2.IsRunning, "Process p2 should not be running");
				}

				Assert.False(p1.IsRunning, "Process p1 sould not be running");
			}

			Assert.Throws<ArgumentException>(() => ProcessHelper.GetProcessById(pid));
		}

		[Test]
		public void TestAttachStopFail()
		{
			// 1) Attach to process that ignores SIGTERM
			// 2) Call Stop(1000)
			// 3) Verify it stops after the timeout occurs

			var exe = CrashTest;
			var args = "nosigterm";

			if (Platform.GetOS() == Platform.OS.Windows)
			{
				exe = CrashTestDummy;
				args = "--gui --noclose";
			}

			int pid;

			using (var p1 = ProcessHelper.Start(exe, args, null, null))
			{
				pid = p1.Id;

				Thread.Sleep(1000);

				using (var p2 = ProcessHelper.GetProcessById(p1.Id))
				{
					var sw = Stopwatch.StartNew();
					p2.Stop(1000);
					var elapsed = sw.Elapsed;

					// Stop should have returned after 1 second
					Assert.Greater(elapsed, TimeSpan.FromSeconds(1));

					// Process gets killed regardless
					Assert.False(p2.IsRunning, "Process p2 should not be running");
				}

				Assert.False(p1.IsRunning, "Process p1 should not be running");
			}

			Assert.Throws<ArgumentException>(() => ProcessHelper.GetProcessById(pid));
		}

		[Test]
		public void TestAttachWaitForExitSuccess()
		{
			// 1) Attach to process that stops on SIGTERM
			// 2) Call Shutdown() and WaitForExit(1000)
			// 3) Verify WaitForExit returns successfully

			var exe = CrashableServer;
			var args = "127.0.0.1 0 1000";

			if (Platform.GetOS() == Platform.OS.Windows)
			{
				exe = CrashTestDummy;
				args = "--gui";
			}

			int pid;

			using (var p1 = ProcessHelper.Start(exe, args, null, null))
			{
				pid = p1.Id;

				using (var p2 = ProcessHelper.GetProcessById(p1.Id))
				{
					p2.Shutdown();

					var sw = Stopwatch.StartNew();
					var success = p2.WaitForExit(10000);
					var elapsed = sw.Elapsed;

					// Wait should have succeeded
					Assert.True(success, "WaitForExit should have succeeded");

					// WaitForExit should have returned before 10 seconds
					Assert.Less(elapsed, TimeSpan.FromSeconds(5));
				}

				Assert.False(p1.IsRunning, "Process sould not be running");
			}

			Assert.Throws<ArgumentException>(() => ProcessHelper.GetProcessById(pid));
		}

		[Test]
		public void TestAttachWaitForExitFail()
		{
			// 1) Attach to process that ignores SIGTERM
			// 2) Call Shutdown() and WaitForExit(1000)
			// 3) Verify WaitForExit fails

			int pid;

			using (var p1 = ProcessHelper.Start(CrashableServer, "127.0.0.1 0 1000", null, null))
			{
				pid = p1.Id;

				using (var p2 = ProcessHelper.GetProcessById(p1.Id))
				{
					// Simulate ignore by not sending sigterm p2.Shutdown();

					var sw = Stopwatch.StartNew();
					var success = p2.WaitForExit(100);
					var elapsed = sw.Elapsed;

					// Wait should have succeeded
					Assert.False(success, "WaitForExit should have failed");

					// WaitForExit should have returned before 10 seconds
					Assert.Greater(elapsed, TimeSpan.FromMilliseconds(100));

					// Process gets killed regardless of wait  for exit success
					Assert.False(p2.IsRunning, "Process p2 should not be running");
				}

				Assert.False(p1.IsRunning, "Process should still be running");
			}

			Assert.Throws<ArgumentException>(() => ProcessHelper.GetProcessById(pid));
		}
	}
}
