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
	class StringUtf8ExtraBytesTests
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("StringUtf8ExtraBytes");

			var str = new Peach.Core.Dom.String("String");
			Assert.False(runner.IsSupported(str));

			str.DefaultValue = new Variant("hello");
			Assert.True(runner.IsSupported(str));

			str.isMutable = false;
			Assert.False(runner.IsSupported(str));

			str.isMutable = true;
			Assert.True(runner.IsSupported(str));

			str.stringType = StringType.utf16;
			Assert.False(runner.IsSupported(str));

			str.stringType = StringType.utf16be;
			Assert.False(runner.IsSupported(str));

			str.stringType = StringType.utf32;
			Assert.False(runner.IsSupported(str));

			str.stringType = StringType.utf7;
			Assert.False(runner.IsSupported(str));

			str.stringType = StringType.utf8;
			Assert.True(runner.IsSupported(str));

			str.Hints.Add("Peach.TypeTransform", new Hint("Peach.TypeTransform", "false"));
			Assert.False(runner.IsSupported(str));
		}

		[Test]
		public void TestSequential()
		{
			var runner = new MutatorRunner("StringUtf8ExtraBytes");

			var str = new Peach.Core.Dom.String("String") { DefaultValue = new Variant("Hello") };

			var m = runner.Sequential(str);

			// Count is proportional to the string length
			Assert.AreEqual(5, m.Count());

			foreach (var item in m)
			{
				var buf = item.Value.ToArray();

				// Should have expanded
				Assert.Greater(buf.Length, 5);
			}
		}

		[Test]
		public void TestRandom()
		{
			var runner = new MutatorRunner("StringUtf8ExtraBytes");

			var str = new Peach.Core.Dom.String("String") { DefaultValue = new Variant("Hello") };

			var m = runner.Random(500, str);

			// Count is proportional to the string length
			Assert.AreEqual(500, m.Count());

			foreach (var item in m)
			{
				var buf = item.Value.ToArray();

				// Should have expanded
				Assert.Greater(buf.Length, 5);
			}
		}
	}
}
