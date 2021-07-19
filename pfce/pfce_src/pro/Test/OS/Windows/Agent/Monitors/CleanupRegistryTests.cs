using Microsoft.Win32;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Peach.Pro.Test.OS.Windows.Agent.Monitors
{
	[TestFixture]
	[Quick]
	[Peach]
	[Platform("Win")]
	class CleanupRegistryTests
	{
		const string Monitor = "CleanupRegistry";

		[Test]
		public void TestNoParams()
		{
			var runner = new MonitorRunner(Monitor, new Dictionary<string, string>());
			var ex = Assert.Catch(() => runner.Run());
			Assert.That(ex, Is.InstanceOf<PeachException>());
			var msg = "Could not start monitor \"CleanupRegistry\".  Monitor 'CleanupRegistry' is missing required parameter 'Key'.";
			StringAssert.StartsWith(msg, ex.Message);
		}

		[Test]
		public void TestBasic()
		{
			using (var peachTest = Registry.CurrentUser.CreateSubKey("PeachTest"))
			{
				peachTest.CreateSubKey("Child1").SetValue("Name", "Value");
				peachTest.CreateSubKey("Child2");
			}

			var runner = new MonitorRunner(Monitor, new Dictionary<string, string>
			{
				{"Key", @"HKCU\PeachTest"},
			});
			runner.Run();

			Assert.IsNull(Registry.CurrentUser.OpenSubKey("PeachTest"));
		}

		[Test]
		public void TestChildrenOnly()
		{
			using (var peachTest = Registry.CurrentUser.CreateSubKey("PeachTest"))
			{
				peachTest.CreateSubKey("Child1").SetValue("Name", "Value");
				peachTest.CreateSubKey("Child2");
			}

			try
			{
				var runner = new MonitorRunner(Monitor, new Dictionary<string, string>
			{
				{"Key", @"HKCU\PeachTest"},
				{"ChildrenOnly", "True"},
			});
				runner.Run();

				using (var peachTest = Registry.CurrentUser.OpenSubKey("PeachTest"))
				{
					Assert.IsNotNull(peachTest);
					Assert.IsNull(peachTest.OpenSubKey("Child1"));
					Assert.IsNull(peachTest.OpenSubKey("Child2"));
				}
			}
			finally
			{
				try { Registry.CurrentUser.DeleteSubKey("PeachTest"); }
				catch { }
			}
		}
	}
}
