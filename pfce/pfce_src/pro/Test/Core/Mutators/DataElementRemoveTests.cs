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
	class DataElementRemoveTests
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("DataElementRemove");

			var blob = new Blob("Blob");
			Assert.False(runner.IsSupported(blob));

			var dm = new DataModel("DM");
			dm.Add(blob);

			Assert.False(runner.IsSupported(dm));
			Assert.True(runner.IsSupported(blob));

			var choice = new Choice("Choice");
			choice.choiceElements.Add(new String("String")
			{
				DefaultValue = new Variant("Hello"),
				parent = choice
			});
			choice.SelectDefault();
			dm.Add(choice);

			Assert.True(runner.IsSupported(choice));
			Assert.False(runner.IsSupported(choice.SelectedElement));
		}

		[Test]
		public void TestCounts()
		{
			var runner = new MutatorRunner("DataElementRemove");

			var blob = new Blob("Blob");
			var dm = new DataModel("DM");

			dm.Add(blob);

			var m1 = runner.Sequential(blob);
			Assert.AreEqual(1, m1.Count());
		}

		[Test]
		public void TestSequential()
		{
			var runner = new MutatorRunner("DataElementRemove");

			var blob = new Blob("Blob") { DefaultValue = new Variant(new byte[] { 0x01, 0x02, 0x03 }) };
			var dm = new DataModel("DM");

			dm.Add(blob);

			Assert.AreEqual(new byte[] { 0x01, 0x02, 0x03 }, dm.Value.ToArray());

			var m = runner.Sequential(blob);

			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreEqual(new byte[0], val);
			}
		}

		[Test]
		public void TestRandom()
		{
			var runner = new MutatorRunner("DataElementRemove");

			var blob = new Blob("Blob") { DefaultValue = new Variant(new byte[] { 0x01, 0x02, 0x03 }) };
			var dm = new DataModel("DM");

			dm.Add(blob);

			Assert.AreEqual(new byte[] { 0x01, 0x02, 0x03 }, dm.Value.ToArray());

			var m = runner.Random(10, blob);

			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreEqual(new byte[0], val);
			}
		}

		[Test]
		public void TestArrayVariance()
		{
			// Make sure this mutator doesn't cause issues with ArrayVariance mutations
			// that use the CountOverride functionality of array.

			var runner = new MutatorRunner("DataElementRemove");

			var array = new Peach.Core.Dom.Array("Array") { occurs = 1 };
			array.OriginalElement = new Peach.Core.Dom.String("Array") { DefaultValue = new Variant("Hello") };

			Assert.AreEqual(Encoding.ASCII.GetBytes("Hello"), array.Value.ToArray());
			Assert.AreEqual(1, array.Count);

			array[0].DefaultValue = new Variant("Foo");
			array.SetCountOverride(2, array[0].Value, 0);
			//array.CountOverride = 2;

			// Count override replicates the last element
			Assert.AreEqual(Encoding.ASCII.GetBytes("FooFoo"), array.Value.ToArray());

			var m = runner.Sequential(array[0]);
			Assert.AreEqual(1, m.Count());

			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				// Even though we mutated, CountOverride will stil produce 2 values
				// using the element Foo that was deleted
				Assert.AreEqual(Encoding.ASCII.GetBytes("FooFoo"), val);
			}
		}
	}
}
