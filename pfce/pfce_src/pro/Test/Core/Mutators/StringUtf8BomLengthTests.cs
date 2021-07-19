using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Mutators
{
	[TestFixture]
	[Quick]
	[Peach]
	class StringUtf8BomLengthTests
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("StringUtf8BomLength");

			var str = new Peach.Core.Dom.String("String");
			Assert.True(runner.IsSupported(str));

			str.DefaultValue = new Variant("hello");
			Assert.True(runner.IsSupported(str));

			str.isMutable = false;
			Assert.False(runner.IsSupported(str));

			str.isMutable = true;
			Assert.True(runner.IsSupported(str));

			str.stringType = StringType.utf16;
			Assert.True(runner.IsSupported(str));

			str.stringType = StringType.utf16be;
			Assert.True(runner.IsSupported(str));

			str.stringType = StringType.utf32;
			Assert.True(runner.IsSupported(str));

			str.stringType = StringType.utf7;
			Assert.True(runner.IsSupported(str));

			str.stringType = StringType.utf8;
			Assert.True(runner.IsSupported(str));

			str.Hints.Add("Peach.TypeTransform", new Hint("Peach.TypeTransform", "false"));
			Assert.False(runner.IsSupported(str));
		}

		[Test]
		public void TestSequential()
		{
			var runner = new MutatorRunner("StringUtf8BomLength");

			var str = new Peach.Core.Dom.String("String");

			// Default length +/- 50 with a min of 0, not invluding default

			str.DefaultValue = new Variant("");
			var m1 = runner.Sequential(str);
			Assert.AreEqual(50, m1.Count());

			str.DefaultValue = new Variant("0");
			var m2 = runner.Sequential(str);
			Assert.AreEqual(51, m2.Count());

			str.DefaultValue = new Variant("01234");
			var m3 = runner.Sequential(str);
			Assert.AreEqual(55, m3.Count());

			str.DefaultValue = new Variant(new string('A', 300));
			var m4 = runner.Sequential(str);
			Assert.AreEqual(100, m4.Count());

			var token = new BitStream(Encoding.UTF8.ByteOrderMark);

			foreach (var item in m4)
			{
				var bs = item.Value;
				var i = bs.IndexOf(token, 0);

				// The utf8 bom should always be found
				Assert.AreNotEqual(-1, i);
			}
		}

		[Test]
		public void TestRandom()
		{
			var runner = new MutatorRunner("StringUtf8BomLength");

			var str = new Peach.Core.Dom.String("String") { DefaultValue = new Variant("Hello") };

			var m = runner.Random(500, str);

			Assert.AreEqual(500, m.Count());

			var token = new BitStream(Encoding.UTF8.ByteOrderMark);

			foreach (var item in m)
			{
				var bs = item.Value;

				var i = bs.IndexOf(token, 0);

				// The utf8 bom should always be found
				Assert.AreNotEqual(-1, i);
			}
		}
	}
}
