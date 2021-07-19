using System.Collections.Generic;
using NUnit.Framework;
using Peach.Core;
using Peach.Pro.Core;
using Peach.Core.Test;
using Peach.Pro.Core.Mutators.Utility;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	[Quick]
	[Peach]
	class SuperbestRandomTests
	{
		[Test]
		public void TestPermutation()
		{
			var rng = new Random(0);

			var dict = new Dictionary<int, int>();

			for (int i = 0; i < 500; ++i)
			{
				// 20 numbers from [1,100]
				var seq = rng.Permutation(100, 20);

				Assert.AreEqual(20, seq.Length);

				dict.Clear();

				foreach (var x in seq)
				{
					Assert.LessOrEqual(x, 100);
					Assert.GreaterOrEqual(x, 0);
					Assert.False(dict.ContainsKey(x));
					dict[x] = 1;
				}
			}

			// 20 numbers from [1,20]
			for (int i = 0; i < 500; ++i)
			{
				var seq = rng.Permutation(20, 20);

				Assert.AreEqual(20, seq.Length);

				dict.Clear();

				foreach (var x in seq)
				{
					Assert.LessOrEqual(x, 20);
					Assert.GreaterOrEqual(x, 0);
					Assert.False(dict.ContainsKey(x));
					dict[x] = 1;
				}
			}

			// 20 numbers from [1,5] yields 5
			for (int i = 0; i < 500; ++i)
			{
				var seq = rng.Permutation(5, 20);

				Assert.AreEqual(5, seq.Length);

				dict.Clear();

				foreach (var x in seq)
				{
					Assert.LessOrEqual(x, 5);
					Assert.GreaterOrEqual(x, 0);
					Assert.False(dict.ContainsKey(x));
					dict[x] = 1;
				}
			}
		}
	}
}
