using System.Linq;
using NUnit.Framework;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Mutators
{
	[TestFixture]
	[Quick]
	[Peach]
	class StringStaticTests
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("StringStatic");

			var str = new Peach.Core.Dom.String("String")
			{
				stringType = Peach.Core.Dom.StringType.ascii
			};

			Assert.True(runner.IsSupported(str));
		}

		[Test]
		public void TestSequential()
		{
			var runner = new MutatorRunner("StringStatic");

			var str = new Peach.Core.Dom.String("String")
			{
				stringType = Peach.Core.Dom.StringType.ascii
			};

			var m = runner.Sequential(str);

			var vals = m.ToArray();

			// verify first two values, last two values, and count
			var val1 = "Peach";
			var val2 = "abcdefghijklmnopqrstuvwxyz";
			var val3 = "18446744073709551664";
			var val4 = "10";

			Assert.AreEqual(1660, vals.Length);

			// Ensure all items are strings
			foreach (var item in vals)
			{
				var asStr = (string)item.InternalValue;
				Assert.NotNull(asStr);

				var val = item.Value.ToArray();
				Assert.NotNull(val);

				// Are all ascii strings
				Assert.AreEqual(asStr.Length, val.Length);
			}

			Assert.AreEqual(val1, (string)vals[0].InternalValue);
			Assert.AreEqual(val2, (string)vals[1].InternalValue);
			Assert.AreEqual(val3, (string)vals[vals.Length - 2].InternalValue);
			Assert.AreEqual(val4, (string)vals[vals.Length - 1].InternalValue);
		}

		[Test]
		public void TestRandom()
		{
			var runner = new MutatorRunner("StringStatic");

			var str = new Peach.Core.Dom.String("String")
			{
				stringType = Peach.Core.Dom.StringType.ascii
			};

			var m = runner.Random(5000, str);
			Assert.AreEqual(5000, m.Count());

			// Ensure all items are strings
			foreach (var item in m)
			{
				var asStr = (string)item.InternalValue;
				Assert.NotNull(asStr);

				var val = item.Value.ToArray();
				Assert.NotNull(val);

				// Are all ascii strings
				Assert.AreEqual(asStr.Length, val.Length);
			}
		}
	}
}
