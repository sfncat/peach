using System;
using System.Collections.Generic;
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
	class DataElementBitFlipperTests
	{
		class DummyTransformer : Transformer
		{
			public DummyTransformer(DataElement parent, Dictionary<string, Variant> args)
				: base(parent, args)
			{
			}

			protected override Peach.Core.IO.BitwiseStream internalEncode(Peach.Core.IO.BitwiseStream data)
			{
				return data;
			}

			protected override Peach.Core.IO.BitStream internalDecode(Peach.Core.IO.BitStream data)
			{
				throw new NotImplementedException();
			}
		}

		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("DataElementBitFlipper");

			var blob = new Peach.Core.Dom.Blob("Blob");
			Assert.False(runner.IsSupported(blob));

			blob.DefaultValue = new Variant(new byte[] { 0x01 });
			Assert.True(runner.IsSupported(blob));

			var str = new Peach.Core.Dom.String("String");
			Assert.False(runner.IsSupported(str));

			str.DefaultValue = new Variant("Hello");
			Assert.True(runner.IsSupported(str));

			str.Hints["Peach.TypeTransform"] = new Hint("Peach.TypeTransform", "false");
			Assert.False(runner.IsSupported(str));

			var num = new Peach.Core.Dom.Number("Number");
			Assert.True(runner.IsSupported(num));

			var flag = new Peach.Core.Dom.Flag("Flag");
			Assert.True(runner.IsSupported(flag));

			var blk = new Peach.Core.Dom.Block("Block");
			Assert.False(runner.IsSupported(blk));

			blk.transformer = new DummyTransformer(blk, null);
			Assert.False(runner.IsSupported(blk));

			blk.Add(str);
			Assert.True(runner.IsSupported(blk));

			var choice = new Choice("Choice");
			choice.choiceElements.Add(new Peach.Core.Dom.String("String")
			{
				DefaultValue = new Variant("Hello"),
				parent = choice
			});
			choice.SelectDefault();

			Assert.False(runner.IsSupported(choice));
			Assert.True(runner.IsSupported(choice.SelectedElement));
		}

		[Test]
		public void TestCounts()
		{
			var runner = new MutatorRunner("DataElementBitFlipper");

			var str = new Peach.Core.Dom.String("str") { DefaultValue = new Variant(new string('a', 100)) };
			Assert.AreEqual(800, runner.Sequential(str).Count());

			var num = new Peach.Core.Dom.Number("num") { length = 32 };
			Assert.AreEqual(32, runner.Sequential(num).Count());

			var blob = new Peach.Core.Dom.Blob("blob");
			Assert.AreEqual(0, runner.Sequential(blob).Count());
		}

		[Test]
		public void TestSequential()
		{
			var runner = new MutatorRunner("DataElementBitFlipper");

			var src = Encoding.ASCII.GetBytes("Hello");
			var blob = new Blob("Blob") { DefaultValue = new Variant(src) };

			var m = runner.Sequential(blob);

			foreach (var item in m)
			{
				var val = item.Value;
				var buf = val.ToArray();

				Assert.AreEqual(src.Length * 8, val.LengthBits);
				Assert.AreNotEqual(src, buf);
			}
		}

		[Test]
		public void TestRandom()
		{
			var runner = new MutatorRunner("DataElementBitFlipper");

			var src = Encoding.ASCII.GetBytes("Hello World");
			var blob = new Blob("Blob") { DefaultValue = new Variant(src) };

			var m = runner.Random(400, blob);

			var flipped = new byte[src.Length];

			foreach (var item in m)
			{
				var val = item.Value;
				var buf = val.ToArray();

				Assert.AreEqual(src.Length * 8, val.LengthBits);
				Assert.AreNotEqual(src, buf);

				for (int j = 0; j < src.Length; j++)
				{
					// Record a '1' for each flipped bit
					flipped[j] |= (byte)(src[j] ^ buf[j]);
				}
			}

			// Every bit should have been flipped
			foreach (var b in flipped)
				Assert.AreEqual(0xff, b);
		}

		void TestTransformer(string transformer, bool lengthSame)
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Block>
			<Block>
				<String name='str1' value='Hello' />
				<String name='str2' value='World' />
			</Block>

			<Number size='32' value='100' />
		</Block>
		<Transformer class='{0}' />
	</DataModel>
</Peach>
".Fmt(transformer);

			var dom = DataModelCollector.ParsePit(xml);

			var runner = new MutatorRunner("DataElementBitFlipper");

			Assert.True(runner.IsSupported(dom.dataModels[0]));

			var orig = dom.dataModels[0].Value.ToArray();
			var last = orig;

			runner.SeedOverride = 0;
			var m = runner.Random(500, dom.dataModels[0]);


			foreach (var item in m)
			{
				var val = item.Value;
				var buf = val.ToArray();

				// Length should always be the same as original
				// since we flip bits on post-transformed value
				Assert.AreEqual(orig.Length * 8, val.LengthBits);

				// Should not be the same as last mutation
				Assert.AreNotEqual(last, buf);

				// Should not be the same as the original value
				Assert.AreNotEqual(orig, buf);

				last = buf;
			}
		}

		[Test]
		public void TestNullTransformer()
		{
			TestTransformer("Null", true);
		}

		[Test]
		public void TestTransformer()
		{
			TestTransformer("Base64Encode", false);
		}
	}
}
