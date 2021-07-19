using System.Linq;
using NUnit.Framework;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Mutators
{
	[TestFixture]
	[Quick]
	[Peach]
	class StringSqlInjectionTests
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("StringSqlInjection");

			var str = new Peach.Core.Dom.String("String")
			{
				stringType = Peach.Core.Dom.StringType.ascii
			};

			Assert.True(runner.IsSupported(str));
		}

		[Test]
		public void TestSequential()
		{
			var runner = new MutatorRunner("StringSqlInjection");

			var str = new Peach.Core.Dom.String("String")
			{
				stringType = Peach.Core.Dom.StringType.ascii
			};

			var m = runner.Sequential(str);

			var vals = m.ToArray();

			Assert.AreEqual(5, vals.Length);

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

			Assert.AreEqual("'", (string)vals[0].InternalValue);
			Assert.AreEqual("\"", (string)vals[1].InternalValue);
			Assert.AreEqual(" -- ", (string)vals[2].InternalValue);
			Assert.AreEqual(" /* ", (string)vals[3].InternalValue);
			Assert.AreEqual("%%", (string)vals[4].InternalValue);
		}

		[Test]
		public void TestRandom()
		{
			var runner = new MutatorRunner("StringSqlInjection");

			var str = new Peach.Core.Dom.String("String")
			{
				stringType = Peach.Core.Dom.StringType.ascii
			};

			var m = runner.Random(5000, str).ToList();
			Assert.AreEqual(5000, m.Count);

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
