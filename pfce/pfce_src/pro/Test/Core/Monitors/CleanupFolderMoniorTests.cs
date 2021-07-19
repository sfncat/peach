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
	class CleanupFolderMonitorTests
	{
		string _tmp;
		string _dir1;
		string _file1;
		string _dir1File;

		[SetUp]
		public void SetUp()
		{
			_tmp = Path.GetTempFileName();
			_dir1 = Path.Combine(_tmp, "sub");
			_file1 = Path.Combine(_tmp, "file");
			_dir1File = Path.Combine(_dir1, "file");

			File.Delete(_tmp);
			Directory.CreateDirectory(_tmp);
			Directory.CreateDirectory(_dir1);
			File.Create(_file1).Close();
			File.Create(_dir1File).Close();
		}

		[TearDown]
		public void TearDown()
		{
			Assert.True(Directory.Exists(_tmp), "Temp directory '{0}' should exist".Fmt(_tmp));
			Assert.True(Directory.Exists(_dir1), "Directory '{0}' should exist".Fmt(_dir1));
			Assert.True(File.Exists(_file1), "File '{0}' should exist".Fmt(_file1));
			Assert.True(File.Exists(_dir1File), "File '{0}' should exist".Fmt(_dir1File));

			Directory.Delete(_tmp, true);
		}

		[Test]
		public void TestBadFolder()
		{
			// Should run even if the folder does not exist
			var runner = new MonitorRunner("CleanupFolder", new Dictionary<string, string>
			{
				{ "Folder", "some_unknown_filder" },
			});

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length, "Monitor should produce no faults");
		}

		[Test]
		public void TestNoNewFiles()
		{
			// Should not delete the folder being monotired or any files/directories that already exist

			var runner = new MonitorRunner("CleanupFolder", new Dictionary<string, string>
			{
				{ "Folder", _tmp },
			});

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length, "Monitor should produce no faults");
		}

		[Test]
		public void TestPreserveExisting()
		{
			var dir2 = Path.Combine(_tmp, "newsub");
			var file2 = Path.Combine(_tmp, "newfile");
			var dir2File = Path.Combine(dir2, "newfile");

			var runner = new MonitorRunner("CleanupFolder", new Dictionary<string, string>
			{
				{ "Folder", _tmp },
			});

			runner.IterationStarting += (m, args) =>
			{
				Directory.CreateDirectory(dir2);
				File.Create(file2).Close();
				File.Create(dir2File).Close();

				m.IterationStarting(args);

				Assert.False(Directory.Exists(dir2), "Directory '{0}' should not exist".Fmt(dir2));
				Assert.False(File.Exists(file2), "File '{0}' should not exist".Fmt(file2));
				Assert.False(File.Exists(dir2File), "File '{0}' should not exist".Fmt(dir2File));
			};

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length, "Monitor should produce no faults");
		}
	}
}
