using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using Peach.Pro.Core.OS.Windows;

namespace Peach.Pro.Test.OS.Windows.Agent.Monitors
{
	[TestFixture]
	[Quick]
	[Peach]
	[Platform("Win")]
	class WindowsServiceTests
	{
		const string Monitor = "WindowsService";
		const string Service = "pla"; // Performance Logs & Alerts
		private ServiceController _ctrl;
		private int _counter;

		private void SafeStop()
		{
			try { _ctrl.Stop(); }
			catch { }
		}

		private ServiceControllerStatus Status
		{
			get
			{
				_ctrl.Refresh();
				return _ctrl.Status;
			}
		}

		private MonitorRunner CreateRunner(Dictionary<string, string> args)
		{
			_counter = 0;
			args.Add("Service", Service);
			var runner = new MonitorRunner(Monitor, args)
			{
				StartMonitor = (m, _args) =>
				{
					m.InternalEvent += (s, e) =>
					{
						var tea = (ToggleEventArgs)e;
						var expected = tea.Toggle ?
							ServiceControllerStatus.Running : ServiceControllerStatus.Stopped;
						Assert.AreEqual(expected, Status);
						_counter++;
					};
					m.StartMonitor(_args);
				}
			};
			return runner;
		}

		[SetUp]
		public void SetUp()
		{
			if (!Privilege.IsUserAdministrator())
				Assert.Ignore("User is not an administrator.");

			_ctrl = new ServiceController(Service);
			SafeStop();
		}

		[TearDown]
		public void TearDown()
		{
			if (_ctrl != null)
			{
				SafeStop();
				_ctrl.Dispose();
				_ctrl = null;
			}
		}

		[Test]
		public void TestNoParams()
		{
			var runner = new MonitorRunner(Monitor, new Dictionary<string, string>());
			var ex = Assert.Catch(() => runner.Run());
			Assert.That(ex, Is.InstanceOf<PeachException>());
			var msg = "Could not start monitor \"WindowsService\".  Monitor 'WindowsService' is missing required parameter 'Service'.";
			StringAssert.StartsWith(msg, ex.Message);
		}

		[Test]
		public void TestBasic()
		{
			var runner = CreateRunner(new Dictionary<string, string>());
			var faults = runner.Run(2);
			Assert.AreEqual(0, faults.Length, "No faults expected");
			Assert.AreEqual(ServiceControllerStatus.Running, Status);
			Assert.AreEqual(1, _counter, "Counter mismatch");
		}

		[Test]
		public void TestRestart()
		{
			var runner = CreateRunner(new Dictionary<string, string>
			{
				{"Restart", "true"},
			});
			var faults = runner.Run(2);
			Assert.AreEqual(0, faults.Length, "No faults expected");
			Assert.AreEqual(ServiceControllerStatus.Running, Status);
			Assert.AreEqual(3, _counter, "Counter mismatch");
		}

		[Test]
		public void TestFaultOnEarlyExit()
		{
			var runner = CreateRunner(new Dictionary<string, string>
			{
				{"FaultOnEarlyExit", "true"},
			});
			runner.IterationStarting = (m, args) =>
			{
				m.IterationStarting(args);
				Assert.AreEqual(ServiceControllerStatus.Running, Status);

				_ctrl.Stop();
				_ctrl.WaitForStatus(ServiceControllerStatus.Stopped);
			};

			var faults = runner.Run();
			Assert.AreEqual(1, faults.Length, "Faults mismatch");
			Assert.AreEqual(ServiceControllerStatus.Stopped, Status);
			Assert.AreEqual(1, _counter, "Counter mismatch");
		}

		[Test]
		public void TestNoFaultOnEarlyExit()
		{
			var runner = CreateRunner(new Dictionary<string, string>());
			runner.IterationStarting = (m, args) =>
			{
				m.IterationStarting(args);
				Assert.AreEqual(ServiceControllerStatus.Running, Status);

				_ctrl.Stop();
				_ctrl.WaitForStatus(ServiceControllerStatus.Stopped);
			};

			var faults = runner.Run(2);
			Assert.AreEqual(0, faults.Length, "No faults expected");
			Assert.AreEqual(ServiceControllerStatus.Stopped, Status);
			Assert.AreEqual(2, _counter, "Counter mismatch");
		}
	}
}
