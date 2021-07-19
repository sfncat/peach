

//using NUnit.Framework.Constraints;

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.PitParserTests
{
	[TestFixture]
	[Quick]
	[Peach]
	class ArrayTests
	{
		class Resetter : DataElement
		{
			public static void Reset()
			{
				DataElement._uniqueName = 0;
			}

			public override void Crack(Peach.Core.Cracker.DataCracker context, Peach.Core.IO.BitStream data, long? size)
			{
				throw new NotImplementedException();
			}

			public override void WritePit(System.Xml.XmlWriter pit)
			{
				throw new NotImplementedException();
			}
		}

		[Test]
		public void ArrayHintsTest()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob value=\"Hello World\" minOccurs=\"100\">" +
				"			<Hint name=\"Hello\" value=\"World\"/>"+
				"		</Blob>"+
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Peach.Core.Dom.Array array = dom.dataModels[0][0] as Peach.Core.Dom.Array;

			Assert.NotNull(array);
			Assert.AreEqual(0, array.Count);
			Assert.NotNull(array.OriginalElement);

			Assert.NotNull(array.Hints);
			Assert.AreEqual(1, array.Hints.Count);
			Assert.AreEqual("World", array.Hints["Hello"].Value);

			Assert.NotNull(array.OriginalElement.Hints);
			Assert.AreEqual(1, array.OriginalElement.Hints.Count);
			Assert.AreEqual("World", array.OriginalElement.Hints["Hello"].Value);

			// Array expansion doesn't happen until .Value is called
			var val = dom.dataModels[0].Value;
			Assert.NotNull(val);
			Assert.AreEqual(100, array.Count);

			// Hint gets applied to every array item
			foreach (var item in array)
			{
				Assert.NotNull(item.Hints);
				Assert.AreEqual(1, item.Hints.Count);
				Assert.AreEqual("World", item.Hints["Hello"].Value);
			}
		}

		[Test]
		public void ArrayNameTest()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob name=\"stuff\" value=\"Hello World\" minOccurs=\"100\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Peach.Core.Dom.Array array = dom.dataModels[0][0] as Peach.Core.Dom.Array;

			Assert.NotNull(array);
			Assert.AreEqual(0, array.Count);
			Assert.AreEqual("TheDataModel.stuff", array.fullName);
			Assert.NotNull(array.OriginalElement);
			Assert.AreEqual("TheDataModel.stuff.stuff", array.OriginalElement.fullName);
			Assert.AreEqual(array, array.OriginalElement.parent);

			// Array expansion doesn't happen until .Value is called
			var val = dom.dataModels[0].Value;
			Assert.NotNull(val);
			Assert.AreEqual(100, array.Count);

			Assert.AreEqual("TheDataModel.stuff.stuff_0", array[0].fullName);
			Assert.AreEqual("TheDataModel.stuff.stuff_1", array[1].fullName);
		}

		[Test]
		public void ArrayNoNameTest()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob value=\"Hello World\" minOccurs=\"100\"/>" +
				"	</DataModel>" +
				"</Peach>";

			Resetter.Reset();

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Peach.Core.Dom.Array array = dom.dataModels[0][0] as Peach.Core.Dom.Array;

			Assert.NotNull(array);
			Assert.AreEqual(0, array.Count);
			Assert.AreEqual("TheDataModel.DataElement_0", array.fullName);
			Assert.NotNull(array.OriginalElement);
			Assert.AreEqual("TheDataModel.DataElement_0.DataElement_0", array.OriginalElement.fullName);
			Assert.AreEqual(array, array.OriginalElement.parent);

			// Array expansion doesn't happen until .Value is called
			var val = dom.dataModels[0].Value;
			Assert.NotNull(val);
			Assert.AreEqual(100, array.Count);

			Assert.AreEqual("TheDataModel.DataElement_0.DataElement_0_0", array[0].fullName);
			Assert.AreEqual("TheDataModel.DataElement_0.DataElement_0_1", array[1].fullName);
		}

		[Test]
		public void ArrayOfRelationTest()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number name=\"Length\" size=\"32\">" +
				"			<Relation type=\"size\" of=\"Data\" />" +
				"		</Number>" +
				"		<Blob name=\"Data\" value=\"Hello World\" minOccurs=\"100\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Peach.Core.Dom.Array array = dom.dataModels[0][1] as Peach.Core.Dom.Array;

			Assert.NotNull(array);
			Assert.AreEqual(0, array.Count);
			Assert.AreEqual(1, array.relations.Count);
			Assert.NotNull(array.OriginalElement);
			Assert.AreEqual(0, array.OriginalElement.relations.Count);

			// Array expansion doesn't happen until .Value is called
			var val = dom.dataModels[0].Value;
			Assert.NotNull(val);
			Assert.AreEqual(100, array.Count);
		}

		[Test]
		public void ArrayFromRelationTest()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob name=\"Data\" value=\"Hello World\"/>" +
				"		<Number name=\"Length\" size=\"32\"  minOccurs=\"100\">" +
				"			<Relation type=\"size\" of=\"Data\" />" +
				"		</Number>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Peach.Core.Dom.Array array = dom.dataModels[0][1] as Peach.Core.Dom.Array;

			Assert.NotNull(array);
			Assert.AreEqual(0, array.Count);
			Assert.AreEqual(0, array.relations.Count);
			Assert.NotNull(array.OriginalElement);
			Assert.AreEqual(1, array.OriginalElement.relations.Count);

			// Array expansion doesn't happen until .Value is called
			var val = dom.dataModels[0].Value;
			Assert.NotNull(val);
			Assert.AreEqual(100, array.Count);
		}

		[Test]
		public void TestArrayClone()
		{
			// If an array is cloned with a new name, the 1st element in the array needs
			// to have its name updated as well

			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob name=\"Data\" value=\"Hello World\" minOccurs=\"100\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			Peach.Core.Dom.Array array = dom.dataModels[0][0] as Peach.Core.Dom.Array;

			Assert.NotNull(array);
			Assert.AreEqual(0, array.Count);
			Assert.AreEqual("Data", array.Name);
			Assert.NotNull(array.OriginalElement);
			Assert.AreEqual("Data", array.OriginalElement.Name);

			var clone = array.Clone("NewData") as Peach.Core.Dom.Array;

			Assert.NotNull(clone);
			Assert.AreEqual(0, clone.Count);
			Assert.AreEqual("NewData", clone.Name);
			Assert.NotNull(clone.OriginalElement);
			Assert.AreEqual("NewData", clone.OriginalElement.Name);

			// Array expansion doesn't happen until .Value is called
			var val = clone.Value;
			Assert.NotNull(val);

			Assert.AreEqual(100, clone.Count);
			for (int i = 0; i < clone.Count; ++i)
			{
				Assert.AreEqual("NewData_" + i.ToString(), clone[i].Name);
			}
		}

		private void DoOccurs(string occurs, byte[] expected)
		{
			string template =
@"<Peach>
	<DataModel name=""DM"">
		<String value=""XYZ"" {0}/>
	</DataModel>
</Peach>";

			string xml = string.Format(template, occurs);

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			var value = dom.dataModels[0].Value.ToArray();
			Assert.AreEqual(expected, value);
		}

		[Test]
		public void TestOccurs()
		{
			DoOccurs("minOccurs=\"5\"", Encoding.ASCII.GetBytes("XYZXYZXYZXYZXYZ"));
			DoOccurs("minOccurs=\"1\"", Encoding.ASCII.GetBytes("XYZ"));
			DoOccurs("minOccurs=\"0\"", Encoding.ASCII.GetBytes(""));

			DoOccurs("occurs=\"5\"", Encoding.ASCII.GetBytes("XYZXYZXYZXYZXYZ"));
			DoOccurs("occurs=\"1\"", Encoding.ASCII.GetBytes("XYZ"));
			DoOccurs("occurs=\"0\"", Encoding.ASCII.GetBytes(""));

			DoOccurs("maxOccurs=\"5\"", Encoding.ASCII.GetBytes("XYZ"));
			DoOccurs("maxOccurs=\"1\"", Encoding.ASCII.GetBytes("XYZ"));
			DoOccurs("maxOccurs=\"0\"", Encoding.ASCII.GetBytes("XYZ"));
		}

		[Test]
		public void TestArrayFields()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Block name='Items' minOccurs='1'>
			<String name='Value' value='***' />
		</Block>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
				<Data>
					<Field name='Items[0].Value' value='xxx'/>
					<Field name='Items[2].Value' value='zzz'/>
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='Null' />
	</Test>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var e = new Engine(null);
			var c = new RunConfiguration() { singleIteration = true };

			e.startFuzzing(dom, c);

			var model = dom.tests[0].stateModel.states[0].actions[0].dataModel;

			var final = model.Value.ToArray();
			var asStr = Encoding.ASCII.GetString(final);

			Assert.AreEqual("xxx***zzz", asStr);

			var names = model.PreOrderTraverse().Select(x => x.fullName).ToArray();
			var exp = new string[] {
				"DM",
				"DM.Items",
				"DM.Items.Items_0",
				"DM.Items.Items_0.Value",
				"DM.Items.Items_1",
				"DM.Items.Items_1.Value",
				"DM.Items.Items_2",
				"DM.Items.Items_2.Value",
			};

			Assert.AreEqual(names, exp);
		}

		[Test]
		public void TestArrayArrayFields()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Block name='Items' minOccurs='1'>
			<Block name='SubItems' minOccurs='0'>
				<String name='Value' value='Value' />
			</Block>
		</Block>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
				<Data>
					<Field name='Items[1].SubItems[1].Value' value='zzz'/>
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='Null' />
	</Test>
</Peach>";


			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var e = new Engine(null);
			var c = new RunConfiguration() { singleIteration = true };

			e.startFuzzing(dom, c);

			var model = dom.tests[0].stateModel.states[0].actions[0].dataModel;

			var final = model.Value.ToArray();
			var asStr = Encoding.ASCII.GetString(final);

			Assert.AreEqual("Valuezzz", asStr);

			var names = model.PreOrderTraverse().Select(x => x.fullName).ToArray();
			var exp = new string[] {
				"DM",
				"DM.Items",
				"DM.Items.Items_0",
				"DM.Items.Items_0.SubItems",
				"DM.Items.Items_1",
				"DM.Items.Items_1.SubItems",
				"DM.Items.Items_1.SubItems.SubItems_0",
				"DM.Items.Items_1.SubItems.SubItems_0.Value",
				"DM.Items.Items_1.SubItems.SubItems_1",
				"DM.Items.Items_1.SubItems.SubItems_1.Value",
			};

			Assert.AreEqual(exp, names);
		}

		[Test]
		public void TestArrayChoiceFields()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Choice name='Items' minOccurs='1'>
			<Block name='One'>
				<String name='Value' value='Value One' />
			</Block>
			<Block name='Two'>
				<String name='Value' value='Value One' />
			</Block>
			<Block name='Three'>
				<String name='Value' value='Value One' />
			</Block>
		</Choice>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
				<Data>
					<Field name='Items[0].Two.Value' value='xxx'/>
					<Field name='Items[1].Three.Value' value='yyy'/>
					<Field name='Items[2].One.Value' value='zzz'/>
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='Null' />
	</Test>
</Peach>";


			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var e = new Engine(null);
			var c = new RunConfiguration() { singleIteration = true };

			e.startFuzzing(dom, c);

			var model = dom.tests[0].stateModel.states[0].actions[0].dataModel;

			var final = model.Value.ToArray();
			var asStr = Encoding.ASCII.GetString(final);

			Assert.AreEqual("xxxyyyzzz", asStr);

			var names = model.PreOrderTraverse().Select(x => x.fullName).ToArray();
			var exp = new string[] {
				"DM",
				"DM.Items",
				"DM.Items.Items_0",
				"DM.Items.Items_0.Two",
				"DM.Items.Items_0.Two.Value",
				"DM.Items.Items_1",
				"DM.Items.Items_1.Three",
				"DM.Items.Items_1.Three.Value",
				"DM.Items.Items_2",
				"DM.Items.Items_2.One",
				"DM.Items.Items_2.One.Value",
			};

			Assert.AreEqual(names, exp);
		}

		[Test]
		public void TestArrayRefFields()
		{
			string xml = @"
<Peach>
	<DataModel name='Content'>
		<Block name='Items' minOccurs='1'>
			<String name='Value' />
		</Block>
	</DataModel>

	<DataModel name='DM'>
		<Block name='Content' ref='Content' />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
				<Data>
					<Field name='Content.Items[0].Value' value='xxx'/>
					<Field name='Content.Items[1].Value' value='yyy'/>
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='Null' />
	</Test>
</Peach>";


			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var e = new Engine(null);
			var c = new RunConfiguration() { singleIteration = true };

			e.startFuzzing(dom, c);

			var model = dom.tests[0].stateModel.states[0].actions[0].dataModel;

			var final = model.Value.ToArray();
			var asStr = Encoding.ASCII.GetString(final);

			Assert.AreEqual("xxxyyy", asStr);

			var names = model.PreOrderTraverse().Select(x => x.fullName).ToArray();
			var exp = new string[] {
				"DM",
				"DM.Content",
				"DM.Content.Items",
				"DM.Content.Items.Items_0",
				"DM.Content.Items.Items_0.Value",
				"DM.Content.Items.Items_1",
				"DM.Content.Items.Items_1.Value",
			};

			Assert.AreEqual(names, exp);
		}

		[Test]
		public void TestArrayOverrideTemplate()
		{
			string xml = @"
<Peach>
	<DataModel name='Content'>
		<Block name='Items' minOccurs='2'>
			<String name='str1' value='Value1' />
			<String name='str2' value='Value2' />
		</Block>
	</DataModel>

	<DataModel name='DM'>
		<Block name='Content' ref='Content'>
			<String name='Items.str1' value='New1'/>
			<String name='Items.str3' value='Value3'/>
		</Block>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
				<Data>
					<Field name='Content.Items[2].str1' value='xxx'/>
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='Null' />
	</Test>
</Peach>";


			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var e = new Engine(null);
			var c = new RunConfiguration() { singleIteration = true };

			e.startFuzzing(dom, c);

			var model = dom.tests[0].stateModel.states[0].actions[0].dataModel;

			var final = model.Value.ToArray();
			var asStr = Encoding.ASCII.GetString(final);

			Assert.AreEqual("New1Value2Value3New1Value2Value3xxxValue2Value3", asStr);

			var names = model.PreOrderTraverse().Select(x => x.fullName).ToArray();
			var exp = new string[] {
				"DM",
				"DM.Content",
				"DM.Content.Items",
				"DM.Content.Items.Items_0",
				"DM.Content.Items.Items_0.str1",
				"DM.Content.Items.Items_0.str2",
				"DM.Content.Items.Items_0.str3",
				"DM.Content.Items.Items_1",
				"DM.Content.Items.Items_1.str1",
				"DM.Content.Items.Items_1.str2",
				"DM.Content.Items.Items_1.str3",
				"DM.Content.Items.Items_2",
				"DM.Content.Items.Items_2.str1",
				"DM.Content.Items.Items_2.str2",
				"DM.Content.Items.Items_2.str3",
			};

			Assert.AreEqual(names, exp);
		}

		[Test]
		public void TestArrayRefOverrideOne()
		{
			const string xml = @"
<Peach>
	<DataModel name='Item'>
			<String name='str1' value='str1' />
			<String name='str2' value='str2' />
	</DataModel>

	<DataModel name='ArrayModel'>
		<Block name='Items' ref='Item' minOccurs='1' />
	</DataModel>

	<DataModel name='DerivedArrayModel' ref='ArrayModel'>
		<Block name='Items.str2'>
			<String name='str2a' value='str2a' />
			<String name='str2b' value='str2b' />
		</Block>
	</DataModel>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			var val = dom.dataModels[2].InternalValue.BitsToString();
			Assert.AreEqual("str1str2astr2b", val);
		}

		[Test]
		public void TestArrayRefOverrideTwo()
		{
			const string xml = @"
<Peach>
	<DataModel name='Item'>
			<String name='str1' value='str1' />
			<Block name='str2'>
				<String name='str2a' value='str2a' />
			</Block>
	</DataModel>

	<DataModel name='ArrayModel'>
		<Block name='Outter'>
			<Block name='Items' ref='Item' minOccurs='1' />
		</Block>
	</DataModel>

	<DataModel name='DerivedArrayModel' ref='ArrayModel'>
		<String name='Outter.Items.str2.str2b' value='str2b' />
	</DataModel>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			var val = dom.dataModels[2].InternalValue.BitsToString();
			Assert.AreEqual("str1str2astr2b", val);
		}

		[Test]
		public void TestArrayRefOverrideThree()
		{
			const string xml = @"
<Peach>
	<DataModel name='Item'>
			<String name='str1' value='str1' />
			<String name='str2' value='str2' />
	</DataModel>

	<DataModel name='ArrayModel'>
		<Block name='Items' ref='Item' minOccurs='0' />
	</DataModel>

	<DataModel name='DerivedArrayModel' ref='ArrayModel'>
		<Block name='Items'>
			<String name='str2a' value='str2a' />
			<String name='str2b' value='str2b' />
		</Block>
	</DataModel>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			var val = dom.dataModels[2].InternalValue.BitsToString();
			Assert.AreEqual("str2astr2b", val);
		}

		[Test]
		public void TestArrayRefOverrideNotFound()
		{
			const string xml = @"
<Peach>
	<DataModel name='Item'>
			<String name='str1' value='str1' />
			<Block name='str2'>
				<String name='str2a' value='str2a' />
			</Block>
	</DataModel>

	<DataModel name='ArrayModel'>
		<Block name='Outter'>
			<Block name='Items' ref='Item' minOccurs='1' />
		</Block>
	</DataModel>

	<DataModel name='DerivedArrayModel' ref='ArrayModel'>
		<String name='Items.str2.str2b' value='str2b' />
	</DataModel>
</Peach>";

			var ex = Assert.Throws<PeachException>(() => DataModelCollector.ParsePit(xml));
			Assert.AreEqual("Error parsing String named 'Items.str2.str2b', DataModel 'DerivedArrayModel' has no child element named 'Items'.", ex.Message);
		}

		[Test]
		public void TestArrayRefOverrideNotContainer()
		{
			const string xml = @"
<Peach>
	<DataModel name='Item'>
			<String name='str1' value='str1' />
			<Block name='str2'>
				<String name='str2a' value='str2a' />
			</Block>
	</DataModel>

	<DataModel name='ArrayModel'>
		<Block name='Items' ref='Item' minOccurs='1' />
	</DataModel>

	<DataModel name='DerivedArrayModel' ref='ArrayModel'>
		<String name='Items.str2.str2a.str2b' value='str2b' />
	</DataModel>
</Peach>";

			var ex = Assert.Throws<PeachException>(() => DataModelCollector.ParsePit(xml));
			Assert.AreEqual("Error parsing String named 'Items.str2.str2a.str2b', String 'DerivedArrayModel.Items.Items.str2.str2a' is not a container element.", ex.Message);
		}
	}
}
