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
	class StringUtf32BomStaticTests
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("StringUtf32BomStatic");

			var str = new Peach.Core.Dom.String("String");
			str.stringType = StringType.utf16;

			Assert.True(runner.IsSupported(str));

			str.DefaultValue = new Variant("hello");
			Assert.True(runner.IsSupported(str));

			str.isMutable = false;
			Assert.False(runner.IsSupported(str));

			str.isMutable = true;
			Assert.True(runner.IsSupported(str));

			str.stringType = StringType.ascii;
			Assert.False(runner.IsSupported(str));

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
			var runner = new MutatorRunner("StringUtf32BomStatic");

			var str = new Peach.Core.Dom.String("String") { DefaultValue = new Variant("Hello") };
			str.stringType = StringType.utf16;

			var m = runner.Sequential(str).ToList();

			// Count is same as StringStatic
			Assert.AreEqual(1660, m.Count);

			var tokenBE = new BitStream(Encoding.BigEndianUTF32.ByteOrderMark);
			var tokenLE = new BitStream(Encoding.UTF32.ByteOrderMark);

			var cntBE = 0;
			var cntLE = 0;
			var cntBoth = 0;

			foreach (var item in m)
			{
				var bs = item.Value;

				bool hasBE = bs.IndexOf(tokenBE, 0) != -1;
				bool hasLE = bs.IndexOf(tokenLE, 0) != -1;

				if (hasBE && hasLE)
					++cntBoth;
				else if (hasBE)
					++cntBE;
				else if (hasLE)
					++cntLE;
				else
					Assert.Fail("Missing BOM in mutated string");
			}

			Assert.Greater(cntBE, 0);
			Assert.Greater(cntLE, 0);
			Assert.Greater(cntBoth, 0);
		}

		[Test]
		public void TestRandom()
		{
			var runner = new MutatorRunner("StringUtf32BomStatic");

			var str = new Peach.Core.Dom.String("String") { DefaultValue = new Variant("Hello") };
			str.stringType = StringType.utf32be;

			var m = runner.Random(500, str);

			Assert.AreEqual(500, m.Count());

			var tokenBE = new BitStream(Encoding.BigEndianUTF32.ByteOrderMark);
			var tokenLE = new BitStream(Encoding.UTF32.ByteOrderMark);

			var cntBE = 0;
			var cntLE = 0;
			var cntBoth = 0;

			foreach (var item in m)
			{
				var bs = item.Value;

				bool hasBE = bs.IndexOf(tokenBE, 0) != -1;
				bool hasLE = bs.IndexOf(tokenLE, 0) != -1;

				if (hasBE && hasLE)
					++cntBoth;
				else if (hasBE)
					++cntBE;
				else if (hasLE)
					++cntLE;
				else
					Assert.Fail("Missing BOM in mutated string");
			}

			Assert.Greater(cntBE, 0);
			Assert.Greater(cntLE, 0);
			Assert.Greater(cntBoth, 0);
		}
	}
}
