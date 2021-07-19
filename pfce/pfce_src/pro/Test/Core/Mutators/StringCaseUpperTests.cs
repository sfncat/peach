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
	class StringCaseUpperTests
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("StringCaseUpper");

			var str = new Peach.Core.Dom.String("String");

			// Empty string, not supported
			str.DefaultValue = new Variant("");
			Assert.False(runner.IsSupported(str));

			// Uppercase string, not supported
			str.DefaultValue = new Variant("HELLO");
			Assert.False(runner.IsSupported(str));

			// Numeric string, not supported
			str.DefaultValue = new Variant("100");
			Assert.False(runner.IsSupported(str));

			// At least 1 uppercase letter, supported
			str.DefaultValue = new Variant("hELLO");
			Assert.True(runner.IsSupported(str));

			// Not mutable, not supported
			str.isMutable = false;
			Assert.False(runner.IsSupported(str));
		}

		[Test]
		public void TestSequential()
		{
			var runner = new MutatorRunner("StringCaseUpper");

			var str = new Peach.Core.Dom.String("String") { DefaultValue = new Variant("Hello") };

			var m = runner.Sequential(str);

			// only 1 mutation
			Assert.AreEqual(1, m.Count());

			var asStr = (string)m.First().InternalValue;

			Assert.AreEqual("HELLO", asStr);
		}

		[Test]
		public void TestRandom()
		{
			var runner = new MutatorRunner("StringCaseUpper");

			var str = new Peach.Core.Dom.String("String") { DefaultValue = new Variant("Hello") };

			var m = runner.Random(100, str);

			Assert.AreEqual(100, m.Count());

			foreach (var item in m)
			{
				var asStr = (string)item.InternalValue;
				Assert.AreEqual("HELLO", asStr);
			}
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

			var runner = new MutatorRunner("StringCaseUpper");

			// Both elements should be supported
			Assert.True(runner.IsSupported(str1));
			Assert.True(runner.IsSupported(str2));

			// Simulate a mutation type override on str1
			str1.MutatedValue = new Variant(new byte[] { 0xff, 0xff });
			str1.mutationFlags = MutateOverride.Default | MutateOverride.TypeTransform;

			var val = dm.dataModels[0].Value.ToArray();
			var exp = new byte[] { 0xff, 0xff, 0xff, 0xff };

			Assert.AreEqual(exp, val);

			// Run string mutator on str1, it should do nothing because of the type transform
			var m = runner.Sequential(str2);
			Assert.AreEqual(1, m.Count());

			var val2 = m.First().InternalValue;

			Assert.AreEqual(Variant.VariantType.BitStream, val2.GetVariantType());
			Assert.AreEqual(exp, ((BitwiseStream)val2).ToArray());
		}
	}
}
