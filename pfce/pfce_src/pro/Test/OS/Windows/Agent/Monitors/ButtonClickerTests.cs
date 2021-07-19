using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace Peach.Pro.Test.OS.Windows.Agent.Monitors
{
	[TestFixture]
	[Quick]
	[Peach]
	[Platform("Win")]
	[Ignore("This requires an interactive windows session")]
	class ButtonClickerTests
	{
		const string Monitor = "ButtonClicker";

		[Test]
		public void TestNoParams()
		{
			var runner = new MonitorRunner(Monitor, new Dictionary<string, string>());
			var ex = Assert.Catch(() => runner.Run());
			Assert.That(ex, Is.InstanceOf<PeachException>());
			// Note, param order is not guranteed so can't assert on parameter name
			var msg = "Could not start monitor \"ButtonClicker\".  Monitor 'ButtonClicker' is missing required parameter";
			StringAssert.StartsWith(msg, ex.Message);
		}

		[Test]
		public void TestBasic()
		{
			var form = new ButtonClickerForm();
			var thread = new Thread(() => Application.Run(form));

			try
			{
				var runner = new MonitorRunner(Monitor, new Dictionary<string, string>
				{
					{"WindowText", "ButtonClickerForm"},
					{"ButtonName", "Click Me"},
				})
				{
					IterationStarting = (m, args) =>
					{
						m.IterationStarting(args);

						thread.Start();

						Thread.Sleep(1000);
					}
				};
				var faults = runner.Run();
				Assert.AreEqual(0, faults.Length, "Faults mismatch");
				Assert.IsTrue(form.IsClicked);
			}
			finally
			{
				Application.Exit();
				thread.Join();
			}
		}
	}
}
