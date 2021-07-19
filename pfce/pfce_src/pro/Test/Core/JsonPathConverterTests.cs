using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Core.WebServices;
using Peach.Pro.Core.WebServices.Models;
using MAgent = Peach.Pro.Core.WebServices.Models.Agent;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	[Quick]
	[Peach]
	class JsonPathConverterTests
	{
		private readonly TempFile _tmpFile = new TempFile();

		[SetUp]
		public void SetUp()
		{
		}

		[TearDown]
		public void TearDown()
		{
			_tmpFile.Dispose();
		}

		[Test]
		public void TestBasic()
		{
			var expected = new[]
			{
				"{",
				"  \"originalPit\": \"Category/Test.xml\",",
				"  \"config\": [],",
				"  \"agents\": [],",
				"  \"weights\": []",
				"}"
			};

			var cfg = new PitConfig
			{
				OriginalPit = "Category\\Test.xml",
				Config = new List<Param>(),
				Agents = new List<MAgent>(),
				Weights = new List<PitWeight>(),
			};
			PitDatabase.SavePitConfig(_tmpFile.Path, cfg);

			var actual = File.ReadAllLines(_tmpFile.Path);
			CollectionAssert.AreEqual(expected, actual);

			var loaded = PitDatabase.LoadPitConfig(_tmpFile.Path);
			Assert.AreEqual("Category{0}Test.xml".Fmt(Path.DirectorySeparatorChar), loaded.OriginalPit);
		}
	}
}
