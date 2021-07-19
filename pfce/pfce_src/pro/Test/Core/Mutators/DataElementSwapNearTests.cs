using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Mutators
{
	[TestFixture]
	[Quick]
	[Peach]
	class DataElementSwapNearTests
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("DataElementSwapNear");

			var blob1 = new Blob("Blob1");
			Assert.False(runner.IsSupported(blob1));

			var blob2 = new Blob("Blob2");
			Assert.False(runner.IsSupported(blob2));

			var dm = new DataModel("DM");
			dm.Add(blob1);
			dm.Add(blob2);

			var flags = new Flags("Flags") { lengthType = LengthType.Bits, length = 32 };
			var flag = new Flag("Flag") { length = 8, position = 0 };
			flags.Add(flag);

			Assert.False(runner.IsSupported(dm));
			Assert.True(runner.IsSupported(blob1));
			Assert.False(runner.IsSupported(blob2));

			dm.Add(flags);
			dm.Add(new Blob("Blob3"));

			Assert.True(runner.IsSupported(blob2));
			Assert.True(runner.IsSupported(flags));
			Assert.False(runner.IsSupported(flag));
		}

		[Test]
		public void TestCounts()
		{
			var runner = new MutatorRunner("DataElementSwapNear");

			var blob1 = new Blob("Blob1");
			var blob2 = new Blob("Blob2");
			var dm = new DataModel("DM");

			dm.Add(blob1);
			dm.Add(blob2);

			var m1 = runner.Sequential(blob1);
			Assert.AreEqual(1, m1.Count());
		}

		[Test]
		public void TestSequential()
		{
			var runner = new MutatorRunner("DataElementSwapNear");

			var blob1 = new Blob("Blob1") { DefaultValue = new Variant(new byte[] { 0x01 }) };
			var blob2 = new Blob("Blob2") { DefaultValue = new Variant(new byte[] { 0x02 }) };
			var dm = new DataModel("DM");

			dm.Add(blob1);
			dm.Add(blob2);

			Assert.AreEqual(new byte[] { 0x01, 0x02 }, dm.Value.ToArray());

			var m = runner.Sequential(blob1);

			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreEqual(new byte[] { 0x02, 0x01 }, val);
			}
		}

		[Test]
		public void TestRandom()
		{
			var runner = new MutatorRunner("DataElementSwapNear");

			var blob1 = new Blob("Blob1") { DefaultValue = new Variant(new byte[] { 0x01 }) };
			var blob2 = new Blob("Blob2") { DefaultValue = new Variant(new byte[] { 0x02 }) };
			var dm = new DataModel("DM");

			dm.Add(blob1);
			dm.Add(blob2);

			Assert.AreEqual(new byte[] { 0x01, 0x02 }, dm.Value.ToArray());

			var m = runner.Random(10, blob1);

			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreEqual(new byte[] { 0x02, 0x01 }, val);
			}
		}
	}
}
