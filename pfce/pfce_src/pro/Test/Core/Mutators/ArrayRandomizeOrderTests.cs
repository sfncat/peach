using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Mutators
{
	[TestFixture]
	[Quick]
	[Peach]
	class ArrayRandomizeOrderTests
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("ArrayRandomizeOrder");

			var array = new Peach.Core.Dom.Array("Array")
			{
				OriginalElement = new Peach.Core.Dom.String("Str")
			};
			array.ExpandTo(0);

			// Empty array can't be randomized
			Assert.False(runner.IsSupported(array));

			// Single element array can't be randomized
			array.ExpandTo(1);
			Assert.False(runner.IsSupported(array));

			// Anything > 1 element is randomizable
			array.ExpandTo(2);
			Assert.True(runner.IsSupported(array));

			array.ExpandTo(10);
			Assert.True(runner.IsSupported(array));

			array.isMutable = false;
			Assert.False(runner.IsSupported(array));
		}

		[Test]
		public void SequenceSupportedTest()
		{
			var runner = new MutatorRunner("ArrayRandomizeOrder");

			var array = new Peach.Core.Dom.Sequence("Seq");

			// Empty array can't be randomized
			Assert.False(runner.IsSupported(array));

			// Single element array can't be randomized
			array.Add(new Peach.Core.Dom.String());
			Assert.False(runner.IsSupported(array));

			// Anything > 1 element is randomizable
			array.Add(new Peach.Core.Dom.String());
			Assert.True(runner.IsSupported(array));

			array.Add(new Peach.Core.Dom.String());
			Assert.True(runner.IsSupported(array));

			array.isMutable = false;
			Assert.False(runner.IsSupported(array));
		}

		[Test]
		public void TestSmallSequential()
		{
			var runner = new MutatorRunner("ArrayRandomizeOrder")
			{
				SeedOverride = 1
			};


			var array = new Peach.Core.Dom.Array("Array")
			{
				OriginalElement = new Peach.Core.Dom.String("Str")
			};
			array.ExpandTo(3);

			for (var i = 0; i < array.Count; ++i)
				array[i].DefaultValue = new Variant(i.ToString());

			// 3 elements, has 6 permutations
			var m = runner.Sequential(array);
			Assert.AreEqual(6, m.Count());

			var totals = m.Select(i => Encoding.ASCII.GetString(i.Value.ToArray())).Total();

			// For 6 mutations, expect at least 3 unique
			Assert.GreaterOrEqual(totals.Count, 3);
		}

		[Test]
		public void SequenceSmallSequentialTest()
		{
			var runner = new MutatorRunner("ArrayRandomizeOrder");

			runner.SeedOverride = 1;

			var seq = new Peach.Core.Dom.Sequence("Seq");
			seq.Add(new Peach.Core.Dom.String());
			seq.Add(new Peach.Core.Dom.String());
			seq.Add(new Peach.Core.Dom.String());

			for (int i = 0; i < seq.Count; ++i)
				seq[i].DefaultValue = new Variant(i.ToString());

			// 3 elements, has 6 permutations
			var m = runner.Sequential(seq);
			Assert.AreEqual(6, m.Count());

			var totals = m.Select(i => Encoding.ASCII.GetString(i.Value.ToArray())).Total();

			// For 6 mutations, expect at least 3 unique
			Assert.GreaterOrEqual(totals.Count, 3);
		}

		[Test]
		public void TestSmallRandom()
		{
			var runner = new MutatorRunner("ArrayRandomizeOrder");

			var array = new Peach.Core.Dom.Array("Array");
			array.OriginalElement = new Peach.Core.Dom.String("Str");
			array.ExpandTo(3);

			for (int i = 0; i < array.Count; ++i)
				array[i].DefaultValue = new Variant(i.ToString());

			// 3 elements, has 6 permutations
			var m = runner.Random(100, array);
			Assert.AreEqual(100, m.Count());

			var totals = m.Select(i => Encoding.ASCII.GetString(i.Value.ToArray())).Total();

			// Should have hit every permutation
			Assert.AreEqual(6, totals.Count);
		}

		[Test]
		public void SequenceSmallRandomTest()
		{
			var runner = new MutatorRunner("ArrayRandomizeOrder");

			var seq = new Peach.Core.Dom.Sequence("Seq");
			seq.Add(new Peach.Core.Dom.String());
			seq.Add(new Peach.Core.Dom.String());
			seq.Add(new Peach.Core.Dom.String());

			for (int i = 0; i < seq.Count; ++i)
				seq[i].DefaultValue = new Variant(i.ToString());

			// 3 elements, has 6 permutations
			var m = runner.Random(100, seq);
			Assert.AreEqual(100, m.Count());

			var totals = m.Select(i => Encoding.ASCII.GetString(i.Value.ToArray())).Total();

			// Should have hit every permutation
			Assert.AreEqual(6, totals.Count);
		}

		[Test]
		public void TestLargeSequential()
		{
			var runner = new MutatorRunner("ArrayRandomizeOrder");

			var array = new Peach.Core.Dom.Array("Array");
			array.OriginalElement = new Peach.Core.Dom.String("Str");
			array.ExpandTo(200);

			for (int i = 0; i < array.Count; ++i)
				array[i].DefaultValue = new Variant(" 0x{0:X2}".Fmt(i));

			// 3 elements, has 6 permutations
			var m = runner.Sequential(array);
			Assert.AreEqual(100, m.Count());

			var totals = m.Select(i => Encoding.ASCII.GetString(i.Value.ToArray())).Total();

			// Expect 100 different mutations
			Assert.AreEqual(100, totals.Count);
		}

		[Test]
		public void SequenceLargeSequentialTest()
		{
			var runner = new MutatorRunner("ArrayRandomizeOrder");

			var seq = new Peach.Core.Dom.Sequence("Seq");

			for (int i = 0; i < 200; ++i)
			{
				seq.Add(new Peach.Core.Dom.String());
			}

			for (int i = 0; i < seq.Count; ++i)
				seq[i].DefaultValue = new Variant(" 0x{0:X2}".Fmt(i));

			// 3 elements, has 6 permutations
			var m = runner.Sequential(seq);
			Assert.AreEqual(100, m.Count());

			var totals = m.Select(i => Encoding.ASCII.GetString(i.Value.ToArray())).Total();

			// Expect 100 different mutations
			Assert.AreEqual(100, totals.Count);
		}

		[Test]
		public void TestLargeRandom()
		{
			var runner = new MutatorRunner("ArrayRandomizeOrder");

			var array = new Peach.Core.Dom.Array("Array");
			array.OriginalElement = new Peach.Core.Dom.String("Str");
			array.ExpandTo(200);

			for (int i = 0; i < array.Count; ++i)
				array[i].DefaultValue = new Variant(" 0x{0:X2}".Fmt(i));

			const int count = 5000;
			var m = runner.Random(count, array);
			Assert.AreEqual(count, m.Count());

			var totals = m.Select(i => Encoding.ASCII.GetString(i.Value.ToArray())).Total();

			// Expect all mutations to be different
			Assert.AreEqual(count, totals.Count);
		}

		[Test]
		public void SequenceLargeRandomTest()
		{
			var runner = new MutatorRunner("ArrayRandomizeOrder");

			var seq = new Peach.Core.Dom.Sequence("Seq");

			for (int i = 0; i < 200; ++i)
			{
				seq.Add(new Peach.Core.Dom.String());
			}

			for (int i = 0; i < seq.Count; ++i)
				seq[i].DefaultValue = new Variant(" 0x{0:X2}".Fmt(i));

			const int count = 5000;
			var m = runner.Random(count, seq);
			Assert.AreEqual(count, m.Count());

			var totals = m.Select(i => Encoding.ASCII.GetString(i.Value.ToArray())).Total();

			// Expect all mutations to be different
			Assert.AreEqual(count, totals.Count);
		}
	}
}
