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
	class BlobChangeToNullTests
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("BlobChangeToNull");

			Assert.False(runner.IsSupported(new Blob()));

			Assert.True(runner.IsSupported(new Blob()
			{
				DefaultValue = new Variant(Encoding.ASCII.GetBytes("Hello")),
			}));

			Assert.False(runner.IsSupported(new Blob()
			{
				DefaultValue = new Variant(new byte[0]),
			}));

			Assert.False(runner.IsSupported(new Blob()
			{
				DefaultValue = new Variant(Encoding.ASCII.GetBytes("Hello")),
				isMutable = false,
			}));
		}

		[Test]
		public void TestCounts()
		{
			var runner = new MutatorRunner("BlobChangeToNull");

			var m1 = runner.Sequential(new Blob() { DefaultValue = new Variant(new byte[1]) });
			Assert.AreEqual(1, m1.Count());

			var m2 = runner.Sequential(new Blob() { DefaultValue = new Variant(new byte[50]) });
			Assert.AreEqual(50, m2.Count());

			var m3 = runner.Sequential(new Blob() { DefaultValue = new Variant(new byte[500]) });
			Assert.AreEqual(100, m3.Count());

			var m4 = runner.Sequential(new Blob() { DefaultValue = new Variant(new byte[0]) });
			Assert.AreEqual(0, m4.Count());
		}

		[Test]
		public void TestSequential()
		{
			var runner = new MutatorRunner("BlobChangeToNull");
			var src = new byte[10];

			for (int i = 0; i < src.Length; ++i)
				src[i] = 0xff;

			var m = runner.Sequential(new Blob() { DefaultValue = new Variant(src) });

			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreEqual(src.Length, val.Length);
				Assert.AreNotEqual(src, val);
			}
		}

		[Test]
		public void TestSequentialOne()
		{
			var runner = new MutatorRunner("BlobChangeToNull");
			var src = new byte[1] { 0xff };
			var m = runner.Sequential(new Blob() { DefaultValue = new Variant(src) });

			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreEqual(src.Length, val.Length);
				Assert.AreNotEqual(src, val);
			}
		}

		[Test]
		public void TestRandom()
		{
			var runner = new MutatorRunner("BlobChangeToNull");
			var src = new byte[10];

			for (int i = 0; i < src.Length; ++i)
				src[i] = 0xff;

			var m = runner.Random(100, new Blob() { DefaultValue = new Variant(src) });

			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreEqual(src.Length, val.Length);
				Assert.AreNotEqual(src, val);
			}
		}

		[Test]
		public void TestRandomOne()
		{
			var runner = new MutatorRunner("BlobChangeToNull");
			var src = new byte[1] { 0xff };
			var m = runner.Random(100, new Blob() { DefaultValue = new Variant(src) });

			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreEqual(src.Length, val.Length);
				Assert.AreNotEqual(src, val);
			}
		}

		[Test]
		public void TestRandomZero()
		{
			var runner = new MutatorRunner("BlobChangeToNull");
			var src = new byte[0];
			var m = runner.Random(100, new Blob() { DefaultValue = new Variant(src) });

			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreEqual(src, val);
			}
		}
	}
}
