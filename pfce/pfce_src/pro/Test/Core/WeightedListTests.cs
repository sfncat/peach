using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Pro.Core;
using Random = Peach.Core.Random;
using Peach.Core.Test;
using Peach.Pro.Core.Mutators.Utility;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	[Quick]
	[Peach]
	class WeightedListTests
	{
		[DebuggerDisplay("{Name}")]
		class Item : IWeighted
		{
			public Item(string name, int weight)
			{
				Name = name;
				SelectionWeight = weight;
			}

			public string Name { get; private set; }
			public int SelectionWeight { get; private set; }
			public int TransformWeight(Func<int, int> how)
			{
				return how(SelectionWeight);
			}
		}

		[Test]
		public void TestSimple()
		{
			var lst = new WeightedList<Item>();

			Assert.AreEqual(0, lst.Count);
			Assert.AreEqual(0, lst.Max);

			lst.Add(new Item("Item 1", 1));
			Assert.AreEqual(1, lst.Max);

			var f = lst.ToArray();
			Assert.AreEqual(1, f.Length);

			lst.Add(new Item("Item 2", 1));
			Assert.AreEqual(2, lst.Max);

			lst.Add(new Item("Item 3", 10));
			Assert.AreEqual(12, lst.Max);

			lst.Add(new Item("Item 4", 100));
			Assert.AreEqual(112, lst.Max);
		}

		[Test]
		public void TestRandom()
		{
			var lst = new WeightedList<Item>();

			Assert.AreEqual(0, lst.Count);
			Assert.AreEqual(0, lst.Max);

			lst.Add(new Item("1", 1));
			Assert.AreEqual(1, lst.Max);

			lst.Add(new Item("2", 1));
			Assert.AreEqual(2, lst.Max);

			lst.Add(new Item("3", 10));
			Assert.AreEqual(12, lst.Max);

			lst.Add(new Item("4", 100));
			Assert.AreEqual(112, lst.Max);

			var rng = new Random(0);

			int num1 = 0, num2 = 0, num3 = 0, num4 = 0;

			for (int i = 0; i < 10000; ++i)
			{
				var elem = rng.WeightedChoice(lst);

				if (elem.Name == "1")
					++num1;
				else if (elem.Name == "2")
					++num2;
				else if (elem.Name == "3")
					++num3;
				else if (elem.Name == "4")
					++num4;
				else
					Assert.Fail("Unexpected element chosen");
			}

			// picked 10000 times.

			// num1 should be 1/112
			var pct1 = (1.0 * lst[0].SelectionWeight) / lst.Max;
			var exp1 = (1.0 / 112);
			Assert.AreEqual(Math.Round(pct1, 4), Math.Round(exp1, 4));

			// num2 should be 1/112
			var pct2 = (1.0 * lst[1].SelectionWeight) / lst.Max;
			var exp2 = (1.0 / 112);
			Assert.AreEqual(Math.Round(pct2, 4), Math.Round(exp2, 4));

			// num3 should be 10/112
			var pct3 = (1.0 * lst[2].SelectionWeight) / lst.Max;
			var exp3 = (10.0 / 112);
			Assert.AreEqual(Math.Round(pct3, 4), Math.Round(exp3, 4));

			// num4 should be 100/112
			var pct4 = (1.0 * lst[3].SelectionWeight) / lst.Max;
			var exp4 = (100.0 / 112);
			Assert.AreEqual(Math.Round(pct4, 4), Math.Round(exp4, 4));
		}

		[Test]
		public void TestRandomSample()
		{
			var lst = new WeightedList<Item>();

			lst.Add(new Item("1", 1));
			lst.Add(new Item("2", 10));
			lst.Add(new Item("3", 100));
			lst.Add(new Item("4", 1));

			var rng = new Random(0);
			var samples = rng.WeightedSample(lst, 4);

			Assert.AreEqual(4, samples.Length);
			Assert.AreEqual("3", samples[0].Name);
			Assert.AreEqual("2", samples[1].Name);
			Assert.AreEqual("1", samples[2].Name);
			Assert.AreEqual("4", samples[3].Name);
		}

		[Test]
		public void TestRandomSample2()
		{
			var lst = new WeightedList<Item>();

			lst.Add(new Item("1", 200));
			lst.Add(new Item("2", 100));
			lst.Add(new Item("3", 10));
			lst.Add(new Item("4", 1));

			var rng = new Random(0);
			var samples = rng.WeightedSample(lst, 4);

			Assert.AreEqual(4, samples.Length);
			Assert.AreEqual("1", samples[0].Name);
			Assert.AreEqual("2", samples[1].Name);
			Assert.AreEqual("3", samples[2].Name);
			Assert.AreEqual("4", samples[3].Name);
		}
	}
}
