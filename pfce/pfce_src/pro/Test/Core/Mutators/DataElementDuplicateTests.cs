using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Test;
using Peach.Pro.Core.Mutators.Utility;

namespace Peach.Pro.Test.Core.Mutators
{
	[TestFixture]
	[Quick]
	[Peach]
	class DataElementDuplicateTests : DataModelCollector
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("DataElementDuplicate");

			var blob = new Blob("Blob");
			Assert.False(runner.IsSupported(blob));

			var dm = new DataModel("DM");
			dm.Add(blob);

			Assert.False(runner.IsSupported(dm));
			Assert.False(runner.IsSupported(blob));

			blob.DefaultValue = new Variant(Encoding.ASCII.GetBytes("Hello"));
			Assert.True(runner.IsSupported(blob));

			var choice = new Choice("Choice");
			choice.choiceElements.Add(new String("String")
			{
				DefaultValue = new Variant("Hello"),
				parent = choice
			});
			choice.SelectDefault();
			dm.Add(choice);

			Assert.True(runner.IsSupported(choice));
			Assert.False(runner.IsSupported(choice.SelectedElement));
		}

		[Test]
		public void TestCounts()
		{
			var runner = new MutatorRunner("DataElementDuplicate");

			var blob = new Blob("Blob");
			var dm = new DataModel("DM");

			dm.Add(blob);

			var m1 = runner.Sequential(blob);
			Assert.AreEqual(50, m1.Count());
		}

		[Test]
		public void TestHints()
		{
			var runner = new MutatorRunner("DataElementDuplicate");

			var blob = new Blob("Blob");
			var dm = new DataModel("DM");

			blob.Hints.Add("DataElementDuplicate-N", new Hint("DataElementDuplicate-N", "100"));
			dm.Add(blob);

			var m1 = runner.Sequential(blob);
			Assert.AreEqual(100, m1.Count());
		}

		[Test]
		public void TestSequential()
		{
			var runner = new MutatorRunner("DataElementDuplicate");

			var blob = new Blob("Blob") { DefaultValue = new Variant(new byte[] { 0x01 }) };
			var dm = new DataModel("DM");

			dm.Add(blob);

			Assert.AreEqual(new byte[] { 0x01 }, dm.Value.ToArray());

			var m = runner.Sequential(blob);

			var len = 1;
			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreEqual(++len, val.Length);

				foreach (var b in val)
					Assert.AreEqual(0x01, b);
			}
		}

		[Test]
		public void TestRandom()
		{
			var runner = new MutatorRunner("DataElementDuplicate");

			var blob = new Blob("Blob") { DefaultValue = new Variant(new byte[] { 0x01 }) };
			var dm = new DataModel("DM");

			dm.Add(blob);

			Assert.AreEqual(new byte[] { 0x01 }, dm.Value.ToArray());

			runner.SeedOverride = 1;

			var m = runner.Random(5000, blob);

			var lengths = new Dictionary<int, int>();

			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				int cnt;
				lengths.TryGetValue(val.Length, out cnt);
				lengths[val.Length] = cnt + 1;
				
				foreach (var b in val)
					Assert.AreEqual(0x01, b);
			}

			Assert.True(!lengths.ContainsKey(1));
			Assert.True(lengths.ContainsKey(2));
			Assert.True(lengths.ContainsKey(50));

			Assert.Greater(lengths[2], lengths[50]);
		}

		[Test]
		public void TestTypeTransform()
		{
			var runner = new MutatorRunner("DataElementDuplicate");

			// Given a string with a size relation, the string can be grown
			// to make the size relation large.  When this happens, the string's
			// value will have the TypTransform mutation flag set.  Ensure the
			// mutator can handle elements with this flag.

			var num = new Peach.Core.Dom.Number("Num");
			var str = new Peach.Core.Dom.String("String") { DefaultValue = new Variant("Hello") };
			var dm = new DataModel("DM");

			num.relations.Add(new SizeRelation(num) { OfName = "String" });

			dm.Add(num);
			dm.Add(str);

			Assert.AreEqual(new byte[] { 0x05, 0x48, 0x65, 0x6c, 0x6c, 0x6f }, dm.Value.ToArray());
			
			// Expand string and override type transform using number relation
			SizedHelpers.ExpandTo(num, 128, false);

			var buf = dm.Value.ToArray();
			Assert.AreEqual(129, buf.Length);
			Assert.AreEqual(128, buf[0]);

			var val = Encoding.ASCII.GetString(buf, 1, buf.Length - 1);
			var exp = "HelloHelloHelloHelloHelloHelloHelloHelloHelloHelloHelloHelloHelloHelloHelloHelloHelloHelloHelloHelloHelloHelloHelloHelloHelloHel";

			Assert.AreEqual(exp, val);

			var m = runner.Random(10, str);

			foreach (var item in m)
			{
				buf = item.Value.ToArray();

				// Result should have grown by multiples of 128 bytes
				Assert.Greater(buf.Length, 129);
				var cnt = (buf.Length - 1) / 128;
				Assert.AreEqual(1 + (cnt * 128), buf.Length);

				// Each duplication should be a copy of the expansion
				for (int i = 0; i < cnt; ++i)
				{
					val = Encoding.ASCII.GetString(buf, (128 * i) + 1, 128);
					Assert.AreEqual(exp, val);
				}
			}
		}

		[Test]
		public void TestMaxOutputSize()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str' value='Hello World' />
	</DataModel>

	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action> 
		</State>
	</StateModel>

	<Test name='Default' maxOutputSize='100'>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
		<Strategy class='Sequential'/>
		<Mutators mode='include'>
			<Mutator class='DataElementDuplicate' />
		</Mutators>
	</Test>
</Peach>
";

			RunEngine(xml);

			// Size is 11 bytes, max is 100, default is 11 bytes
			// (100 - 11)/11 = 8
			Assert.AreEqual(8, mutatedDataModels.Count);
		}

		[Test]
		public void TestMaxOutputSizeOverflow()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str1' value='Hello!' />
		<String name='str2' value='World!' />
	</DataModel>

	<StateModel name='StateModel' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action> 
		</State>
	</StateModel>

	<Test name='Default' maxOutputSize='5'>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
		<Strategy class='Sequential'/>
		<Mutators mode='include'>
			<Mutator class='DataElementDuplicate' />
		</Mutators>
	</Test>
</Peach>
";

			RunEngine(xml);

			// Should be no mutations since duplication
			// would surpass maxOutputSize
			Assert.AreEqual(0, mutatedDataModels.Count);
		}
	}
}
