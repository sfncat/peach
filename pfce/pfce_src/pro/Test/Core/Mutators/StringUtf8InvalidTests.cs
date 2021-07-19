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
	class StringUtf8InvalidTests
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("StringUtf8Invalid");

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
			var runner = new MutatorRunner("StringUtf8Invalid");

			var val = "Hello";
			var str = new Peach.Core.Dom.String("String") { DefaultValue = new Variant(val) };
			var exp = Encoding.UTF8.GetBytes(val);

			var m = runner.Sequential(str);

			// Count is proportional to the string length
			Assert.AreEqual(exp.Length, m.Count());

			foreach (var item in m)
			{
				var buf = item.Value.ToArray();

				// Should have the same length
				Assert.AreEqual(exp.Length, buf.Length);

				// Should have different contents
				Assert.AreNotEqual(exp, buf);
			}
		}

		[Test]
		public void TestRandom()
		{
			var runner = new MutatorRunner("StringUtf8Invalid");

			var val = "\u7ffffff0\u0088\u4201\u7ffff0\u7ff0";
			var str = new Peach.Core.Dom.String("String") { stringType = StringType.utf8, DefaultValue = new Variant(val) };
			var exp = Encoding.UTF8.GetBytes(val);

			var m = runner.Random(500, str);

			// Count is proportional to the string length
			Assert.AreEqual(500, m.Count());

			foreach (var item in m)
			{
				var buf = item.Value.ToArray();

				// Should have the same length
				Assert.AreEqual(exp.Length, buf.Length);

				// Should have different contents
				Assert.AreNotEqual(exp, buf);
			}
		}
	}
}
