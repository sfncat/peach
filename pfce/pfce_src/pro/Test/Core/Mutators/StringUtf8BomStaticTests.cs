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
	class StringUtf8BomStaticTests
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("StringUtf8BomStatic");

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
			var runner = new MutatorRunner("StringUtf8BomStatic");

			var str = new Peach.Core.Dom.String("String") { DefaultValue = new Variant("Hello") };

			var m = runner.Sequential(str).ToList();

			// Count is same as StringStatic
			Assert.AreEqual(1660, m.Count);

			var token = new BitStream(Encoding.UTF8.ByteOrderMark);

			foreach (var item in m)
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
			var runner = new MutatorRunner("StringUtf8BomStatic");

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
