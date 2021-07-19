using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Mutators
{
	[TestFixture]
	[Quick]
	[Peach]
	class ArrayReverseOrderTests
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("ArrayReverseOrder");

			var array = new Peach.Core.Dom.Array("Array");
			array.OriginalElement = new Peach.Core.Dom.String("Str");
			array.ExpandTo(0);

			// Empty array can't be reversed
			Assert.False(runner.IsSupported(array));

			// Single element array can't be reversed
			array.ExpandTo(1);
			Assert.False(runner.IsSupported(array));

			// Anything > 1 element is reversable
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
			var runner = new MutatorRunner("ArrayReverseOrder");

			var array = new Peach.Core.Dom.Sequence("Sequence");

			// Empty array can be expanded
			Assert.False(runner.IsSupported(array));

			// Single element array can be expanded
			array.Add(new Peach.Core.Dom.String("Str"));
			Assert.False(runner.IsSupported(array));

			// Anything > 1 element is expandable
			array.Add(new Peach.Core.Dom.String("Str2"));
			Assert.True(runner.IsSupported(array));

			array.isMutable = false;
			Assert.False(runner.IsSupported(array));
		}

		[Test]
		public void TestSequential()
		{
			var runner = new MutatorRunner("ArrayReverseOrder");

			var array = new Peach.Core.Dom.Array("Array");
			array.OriginalElement = new Peach.Core.Dom.String("Str");
			array.ExpandTo(10);

			for (int i = 0; i < array.Count; ++i)
				array[i].DefaultValue = new Variant(i.ToString());

			var m = runner.Sequential(array);
			Assert.AreEqual(1, m.Count());

			var val = m.First().Value.ToArray();
			var exp = Encoding.ASCII.GetBytes("9876543210");

			Assert.AreEqual(exp, val);
		}

		[Test]
		public void SequenceTestSequential()
		{
			var runner = new MutatorRunner("ArrayReverseOrder");

			var seq = new Peach.Core.Dom.Sequence("Seq");

			//Add 10 strings to seq
			seq.Add(new Peach.Core.Dom.String());
			seq.Add(new Peach.Core.Dom.String());
			seq.Add(new Peach.Core.Dom.String());
			seq.Add(new Peach.Core.Dom.String());
			seq.Add(new Peach.Core.Dom.String());
			seq.Add(new Peach.Core.Dom.String());
			seq.Add(new Peach.Core.Dom.String());
			seq.Add(new Peach.Core.Dom.String());
			seq.Add(new Peach.Core.Dom.String());
			seq.Add(new Peach.Core.Dom.String());

			for (int i = 0; i < seq.Count; ++i)
				seq[i].DefaultValue = new Variant(i.ToString());

			var m = runner.Sequential(seq);
			Assert.AreEqual(1, m.Count());

			var val = m.First().Value.ToArray();
			var exp = Encoding.ASCII.GetBytes("9876543210");

			Assert.AreEqual(exp, val);
		}

		[Test]
		public void TestRandom()
		{
			var runner = new MutatorRunner("ArrayReverseOrder");

			var array = new Peach.Core.Dom.Array("Array");
			array.OriginalElement = new Peach.Core.Dom.String("Str");
			array.ExpandTo(10);

			for (int i = 0; i < array.Count; ++i)
				array[i].DefaultValue = new Variant(i.ToString());

			var m = runner.Random(10, array);
			Assert.AreEqual(10, m.Count());

			var exp = Encoding.ASCII.GetBytes("9876543210");

			foreach (var item in m)
			{
				var val = item.Value.ToArray();
				Assert.AreEqual(exp, val);
			}
		}

		[Test]
		public void SequenceTestRandom()
		{
			var runner = new MutatorRunner("ArrayReverseOrder");

			var seq = new Peach.Core.Dom.Sequence("Seq");

			//Add 10 strings to seq
			seq.Add(new Peach.Core.Dom.String());
			seq.Add(new Peach.Core.Dom.String());
			seq.Add(new Peach.Core.Dom.String());
			seq.Add(new Peach.Core.Dom.String());
			seq.Add(new Peach.Core.Dom.String());
			seq.Add(new Peach.Core.Dom.String());
			seq.Add(new Peach.Core.Dom.String());
			seq.Add(new Peach.Core.Dom.String());
			seq.Add(new Peach.Core.Dom.String());
			seq.Add(new Peach.Core.Dom.String());

			for (int i = 0; i < seq.Count; ++i)
				seq[i].DefaultValue = new Variant(i.ToString());

			var m = runner.Random(10, seq);
			Assert.AreEqual(10, m.Count());

			var exp = Encoding.ASCII.GetBytes("9876543210");

			foreach (var item in m)
			{
				var val = item.Value.ToArray();
				Assert.AreEqual(exp, val);
			}
		}
	}
}
