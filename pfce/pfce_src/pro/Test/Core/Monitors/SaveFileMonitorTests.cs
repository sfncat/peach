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
	class SaveFileMonitorTests
	{
		private string _file;
		private byte[] _fileContents;

		[SetUp]
		public void SetUp()
		{
			_fileContents = Encoding.ASCII.GetBytes("Hello World");
			_file = Path.GetTempFileName();

			File.WriteAllBytes(_file, _fileContents);
		}

		[TearDown]
		public void TearDown()
		{
			File.Delete(_file);

			_file = null;
			_fileContents = null;
		}

		[Test]
		public void TestNoParams()
		{
			var runner = new MonitorRunner("SaveFile", new Dictionary<string, string>());
			var ex = Assert.Throws<PeachException>(() => runner.Run());

			const string msg = "Could not start monitor \"SaveFile\".  Monitor 'SaveFile' is missing required parameter 'Filename'.";

			Assert.AreEqual(msg, ex.Message);
		}

		[Test]
		public void TestNoFaultValidFile()
		{
			var runner = new MonitorRunner("SaveFile", new Dictionary<string, string>
			{
				{ "Filename", _file },
			});

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length);
		}

		[Test]
		public void TestNoFaultInvalidFile()
		{
			var runner = new MonitorRunner("SaveFile", new Dictionary<string, string>
			{
				{ "Filename", "some_invalid_file" },
			});

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length);
		}

		[Test]
		public void TestFaultValidFile()
		{
			var runner = new MonitorRunner("SaveFile", new Dictionary<string, string>
			{
				{ "Filename", _file },
			})
			{
				DetectedFault = m =>
				{
					Assert.False(m.DetectedFault(), "Monitor should not have detected a fault.");

					// Trigger GetMonitorData
					return true;
				}
			};

			var fileName = Path.GetFileName(_file);
			Assert.NotNull(fileName);

			var faults = runner.Run();

			Assert.AreEqual(1, faults.Length);
			Assert.Null(faults[0].Fault);
			Assert.AreEqual("SaveFile", faults[0].DetectionSource);
			Assert.AreEqual("Save File \"{0}\".".Fmt(_file), faults[0].Title);
			Assert.NotNull(faults[0].Data);
			Assert.AreEqual(1, faults[0].Data.Count);
			Assert.True(faults[0].Data.ContainsKey(fileName));
			Assert.AreEqual("Hello World", faults[0].Data[fileName].AsString());
		}

		[Test]
		public void TestFaultInvalidFile()
		{
			var runner = new MonitorRunner("SaveFile", new Dictionary<string, string>
			{
				{ "Filename", "some_invalid_file" },
			})
			{
				DetectedFault = m =>
				{
					Assert.False(m.DetectedFault(), "Monitor should not have detected a fault.");

					// Trigger GetMonitorData
					return true;
				}
			};

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length);
		}
	}
}
