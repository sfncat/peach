using System.Collections.Generic;
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
	class StringCaseRandomTests
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("StringCaseRandom");

			var str = new Peach.Core.Dom.String("String");

			// Empty string, not supported
			str.DefaultValue = new Variant("");
			Assert.False(runner.IsSupported(str));

			// Uppercase string, supported
			str.DefaultValue = new Variant("HELLO");
			Assert.True(runner.IsSupported(str));

			// Lowercase string, supported
			str.DefaultValue = new Variant("hello");
			Assert.True(runner.IsSupported(str));

			// Numeric string, not supported
			str.DefaultValue = new Variant("100");
			Assert.False(runner.IsSupported(str));

			// Mixed case, supported
			str.DefaultValue = new Variant("Hello");
			Assert.True(runner.IsSupported(str));

			// Not mutable, not supported
			str.isMutable = false;
			Assert.False(runner.IsSupported(str));
		}

		[Test]
		public void TestSequential()
		{
			var runner = new MutatorRunner("StringCaseRandom");

			var str = new Peach.Core.Dom.String("String") { DefaultValue = new Variant("Hello") };

			// String length is mutation count
			var m1 = runner.Sequential(str);
			Assert.AreEqual(5, m1.Count());

			str.DefaultValue = new Variant("HelloWorld");

			var m2 = runner.Sequential(str);
			Assert.AreEqual(10, m2.Count());

			// Should to a case toggle, so every mutation will be different from original
			foreach (var item in m2)
			{
				var asStr = (string)item.InternalValue;
				Assert.AreEqual(10, asStr.Length);
				Assert.AreNotEqual("HelloWorld", asStr);
			}
		}

		[Test]
		public void TestRandom()
		{
			var runner = new MutatorRunner("StringCaseRandom");

			var exp = "HelloWorld";
			var str = new Peach.Core.Dom.String("String") { DefaultValue = new Variant(exp) };

			var m = runner.Random(100, str);
			Assert.AreEqual(100, m.Count());

			var dict = new Dictionary<int, bool>();

			foreach (var item in m)
			{
				var asStr = (string)item.InternalValue;

				Assert.AreEqual(exp.Length, asStr.Length);
				Assert.AreNotEqual(exp, asStr);

				for (int i = 0; i < exp.Length; ++i)
					if (exp[i] != asStr[i])
						dict[i] = true;
			}

			// Every character should have been flipped
			Assert.AreEqual(exp.Length, dict.Count);

			for (int i = 0; i < exp.Length; ++i)
				Assert.True(dict.ContainsKey(i));
		}

		[Test]
		public void TestCopyValue()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str1' value='Hello' />
		<String name='str2'>
			<Fixup class='CopyValue'>
				<Param name='ref' value='str1' />
			</Fixup>
		</String>
	</DataModel>
</Peach>";

			var dm = DataModelCollector.ParsePit(xml);
			var str1 = dm.dataModels[0][0];
			var str2 = dm.dataModels[0][1];

			var runner = new MutatorRunner("StringCaseRandom");

			// Both elements should be supported
			Assert.True(runner.IsSupported(str1));
			Assert.True(runner.IsSupported(str2));

			var exp = new byte[] { 0xff, 0xff, 0xff, 0xff };

			// Run string mutator on str1, it should do nothing because of the type transform
			var m = runner.Sequential(str2, () =>
			{
				// Simulate a mutation type override on str1
				str1.MutatedValue = new Variant(new byte[] { 0xff, 0xff });
				str1.mutationFlags = MutateOverride.Default | MutateOverride.TypeTransform;
			});

			Assert.AreEqual(5, m.Count());

			foreach (var item in m)
			{
				var val2 = m.First().InternalValue;

				Assert.AreEqual(Variant.VariantType.BitStream, val2.GetVariantType());
				Assert.AreEqual(exp, ((BitwiseStream)val2).ToArray());
			}
		}
	}
}
