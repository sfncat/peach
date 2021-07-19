

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Core.Test;
using Array = Peach.Core.Dom.Array;

namespace Peach.Pro.Test.Core.CrackingTests
{
	[TestFixture]
	[Quick]
	[Peach]
	public class ChoiceTests
	{
		[Test]
		public void CrackChoice1()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Choice>"  +
				"			<Blob name=\"Blob10\" length=\"10\" />" +
				"			<Blob name=\"Blob5\" length=\"5\" />"   +
				"		</Choice>" +
				"	</DataModel>"  +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 1, 2, 3, 4, 5 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.IsTrue(dom.dataModels[0][0] is Choice);
			Assert.AreEqual("Blob5", ((Choice)dom.dataModels[0][0])[0].Name);
			Assert.AreEqual(new byte[] { 1, 2, 3, 4, 5 }, ((DataElementContainer)dom.dataModels[0][0])[0].DefaultValue.BitsToArray());
		}

		[Test]
		public void MinOccurs0()
		{
			string xml = @"
<Peach>
	<DataModel name=""DM_choice1"">
		<Blob name=""smallData"" length=""2""/>
		<Number size=""32"" token=""true"" value=""1""/>
	</DataModel>

	<DataModel name=""DM_choice2"">
		<Blob name=""BigData"" length=""10""/>
		<Number size=""32"" token=""true"" value=""2""/>
	</DataModel>

	<DataModel name=""DM"">
		<Blob name=""Header"" length=""5""/>
		<Choice name=""options"" minOccurs=""0"">
			<Block ref=""DM_choice1""/>
			<Block ref=""DM_choice2""/>
		</Choice>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 11, 22, 33, 44, 55 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[2], data);

			Assert.AreEqual(2, dom.dataModels[2].Count);
			Assert.IsTrue(dom.dataModels[2][0] is Blob);
			Assert.IsTrue(dom.dataModels[2][1] is Peach.Core.Dom.Array);
			Assert.AreEqual(0, ((Peach.Core.Dom.Array)dom.dataModels[2][1]).Count);
		}

		[Test]
		public void ArrayOfChoice()
		{
			string xml = @"
<Peach>
	<DataModel name=""DM_choice1"">
		<Block>
		<Number size=""8"" token=""true"" value=""1""/>
		<Blob name=""smallData"" length=""2""/>
		</Block>
	</DataModel>

	<DataModel name=""DM_choice2"">
		<Block>
		<Number size=""8"" token=""true"" value=""2""/>
		<Blob name=""BigData"" length=""4""/>
		</Block>
	</DataModel>

	<DataModel name=""DM"">
		<Blob name=""Header"" length=""5""/>
		<Choice name=""options"" minOccurs=""0"">
			<Block ref=""DM_choice1""/>
			<Block ref=""DM_choice2""/>
		</Choice>
		<Blob name=""extra"" length=""1""/>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 11, 22, 33, 44, 55, 1, 9, 9, 2, 8, 8, 8, 8, 1, 7, 7, 0 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[2], data);

			Assert.AreEqual(3, dom.dataModels[2].Count);
			Assert.IsTrue(dom.dataModels[2][0] is Blob);
			Assert.IsTrue(dom.dataModels[2][1] is Peach.Core.Dom.Array);
			var array = dom.dataModels[2][1] as Peach.Core.Dom.Array;
			Assert.NotNull(array);
			Assert.AreEqual(3, array.Count);
		}

		[Test]
		public void PickChoice()
		{
			string temp = Path.GetTempFileName();

			string xml = @"
<Peach>
	<DataModel name='DM1'>
		<String name='str' value='token1' token='true'/>
	</DataModel>

	<DataModel name='DM2'>
		<String name='str' value='token2' token='true'/>
	</DataModel>

	<DataModel name='DM'>
		<Choice name='choice'>
			<Block name='token1' ref='DM1'/>
			<Block name='token2' ref='DM2'/>
		</Choice>
	</DataModel>

	<StateModel name='SM_In' initialState='Initial'>
		<State name='Initial'>
			<Action type='input'>
				<DataModel ref='DM' />
			</Action>
		</State>
	</StateModel>

	<StateModel name='SM_Out' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
				<Data>
					<Field name='choice.token2' value='' />
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Input'>
		<StateModel ref='SM_In' />
		<Publisher class='File'>
			<Param name='FileName' value='{0}'/>
			<Param name='Overwrite' value='false'/>
		</Publisher>
	</Test>

	<Test name='Output'>
		<StateModel ref='SM_Out' />
		<Publisher class='File'>
			<Param name='FileName' value='{0}'/>
			<Param name='Overwrite' value='true'/>
		</Publisher>
	</Test>

</Peach>".Fmt(temp);

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;
			config.runName = "Output";

			Engine e = new Engine(null);
			e.startFuzzing(dom, config);

			var file = File.ReadAllText(temp);
			Assert.AreEqual("token2", file);

			config.runName = "Input";

			dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			e = new Engine(null);
			e.startFuzzing(dom, config);
		}


		[Test]
		public void ChoiceSizeRelations()
		{
			string xml = @"
<Peach>
	<DataModel name=""DM"">
		<Choice>
			<Block name=""C1"">
				<Number name=""version"" size=""8"" value=""1"" token=""true""/>
				<Number name=""LengthBig"" size=""16"">
					<Relation type=""size"" of=""data""/>
				</Number>
			</Block>
			<Block name=""C2"">
				<Number name=""version"" size=""8"" value=""2"" token=""true""/>
				<Number name=""LengthSmall"" size=""8"">
					<Relation type=""size"" of=""data""/>
				</Number>
			</Block>
		</Choice>
		<Blob name=""data""/>
		<Blob/>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 0x02, 0x03, 0x33, 0x44, 0x55 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(3, dom.dataModels[0].Count);
			Assert.IsTrue(dom.dataModels[0][0] is Peach.Core.Dom.Choice);
			Assert.IsTrue(dom.dataModels[0][1] is Blob);
			Assert.IsTrue(dom.dataModels[0][2] is Blob);
			Assert.AreEqual(3, dom.dataModels[0][1].Value.Length);
			Assert.AreEqual(0, dom.dataModels[0][2].Value.Length);

		}

		[Test]
		public void ChoiceFieldSelection()
		{
			const string xml = @"
<Peach>
	<DataModel name='Template'>
		<Choice name='c'>
			<Blob name='empty' />
			<Blob name='blob' value='blob' />
			<String name='str' value='string' />
			<Number name='num' value='48' size='8' />
			<Block name='block'>
				<String name='str' value='block' />
			</Block>
		</Choice>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel name='DM'>
					<Block name='b1' ref='Template' />
					<Block name='b2' ref='Template' />
					<Block name='b3' ref='Template' />
					<Block name='b4' ref='Template' />
					<Block name='b5' ref='Template' />
				</DataModel>
				<Data>
					<Field name='b1.c.empty' value='' />
					<Field name='b2.c.blob' value='' />
					<Field name='b3.c.str' value='' />
					<Field name='b4.c.num' value='' />
					<Field name='b5.c.block' value='' />
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='Null'/>
	</Test>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration { singleIteration = true };
			var e = new Engine(null);

			e.startFuzzing(dom, cfg);

			var final = dom.tests[0].stateModel.states[0].actions[0].dataModel.Value.ToArray();
			var asStr = Encoding.ASCII.GetString(final);

			Assert.AreEqual("blobstring0block", asStr);
		}

		[Test]
		public void ChoiceSizeRelationsParent()
		{
			string xml = @"
<Peach>
	<DataModel name=""DM"">
		<Block name=""blk"">
			<Choice>
				<Block name=""C1"">
					<Number name=""version"" size=""8"" value=""1"" token=""true""/>
					<Number name=""LengthBig"" size=""16"">
						<Relation type=""size"" of=""blk""/>
					</Number>
				</Block>
				<Block name=""C2"">
					<Number name=""version"" size=""8"" value=""2"" token=""true""/>
					<Number name=""LengthSmall"" size=""8"">
						<Relation type=""size"" of=""blk""/>
					</Number>
				</Block>
			</Choice>
			<Blob name=""blb""/>
		</Block>
		<Blob/>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 0x01, 0x06, 0x00, 0x33, 0x44, 0x55 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(2, dom.dataModels[0].Count);
			Assert.IsTrue(dom.dataModels[0][0] is Peach.Core.Dom.Block);
			Assert.IsTrue(dom.dataModels[0][1] is Blob);
			Assert.AreEqual(3, dom.dataModels[0].find("blk.blb").Value.Length);
			Assert.AreEqual(0, dom.dataModels[0][1].Value.Length);
		}

		[Test]
		public void ChoiceSizeRelationsParentTwice()
		{
			string xml = @"
<Peach>
	<DataModel name=""DM"">
		<Block name=""blk"">
			<Choice>
				<Block name=""C1"">
					<Number name=""LengthBig"" size=""16"">
						<Relation type=""size"" of=""blk"" expressionGet=""size + 3"" expressionSet=""size - 3""/>
					</Number>
					<Number name=""version"" size=""8"" value=""0"" token=""true""/>
				</Block>
				<Block name=""C2"">
					<Number name=""LengthSmall"" size=""8"">
						<Relation type=""size"" of=""blk"" expressionGet=""size + 2"" expressionSet=""size - 2""/>
					</Number>
					<Number name=""version"" size=""8"" value=""0"" token=""true""/>
				</Block>
			</Choice>
			<Blob name=""blb""/>
		</Block>
		<Blob/>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 0x03, 0x00, 0x33, 0x44, 0x55 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(2, dom.dataModels[0].Count);
			Assert.IsTrue(dom.dataModels[0][0] is Peach.Core.Dom.Block);
			Assert.IsTrue(dom.dataModels[0][1] is Blob);
			Assert.AreEqual(3, dom.dataModels[0].find("blk.blb").Value.Length);
			Assert.AreEqual(0, dom.dataModels[0][1].Value.Length);
		}

		[Test]
		public void ChoiceCountRelations()
		{
			string xml = @"
<Peach>
	<DataModel name=""DM"">
		<Choice>
			<Block name=""C1"">
				<Number name=""version"" size=""8"" value=""1"" token=""true""/>
				<Number name=""LengthBig"" size=""16"">
					<Relation type=""count"" of=""data""/>
				</Number>
			</Block>
			<Block name=""C2"">
				<Number name=""version"" size=""8"" value=""2"" token=""true""/>
				<Number name=""LengthSmall"" size=""8"">
					<Relation type=""count"" of=""data""/>
				</Number>
			</Block>
		</Choice>
		<Number size=""8"" minOccurs=""0"" name=""data""/>
		<Blob />
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 0x02, 0x03, 0x33, 0x44, 0x55 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(3, dom.dataModels[0].Count);
			Assert.IsTrue(dom.dataModels[0][0] is Peach.Core.Dom.Choice);
			Assert.IsTrue(dom.dataModels[0][1] is Peach.Core.Dom.Array);
			Assert.IsTrue(dom.dataModels[0][2] is Peach.Core.Dom.Blob);

			Peach.Core.Dom.Array array = dom.dataModels[0][1] as Peach.Core.Dom.Array;
			Assert.AreEqual(3, array.Count);

			Assert.AreEqual(0, dom.dataModels[0][2].Value.Length);
		}

		[Test]
		public void ChoiceOffsetRelations()
		{
			string xml = @"
<Peach>
	<DataModel name=""DM"">
		<Choice>
			<Block name=""C1"">
				<Number name=""version"" size=""8"" value=""1"" token=""true""/>
				<Number name=""LengthBig"" size=""16"">
					<Relation type=""offset"" of=""data""/>
				</Number>
			</Block>
			<Block name=""C2"">
				<Number name=""version"" size=""8"" value=""2"" token=""true""/>
				<Number name=""LengthSmall"" size=""8"">
					<Relation type=""offset"" of=""data""/>
				</Number>
			</Block>
		</Choice>
		<Blob name=""data""/>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 0x02, 0x03, 0x33, 0x44, 0x55 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(2, dom.dataModels[0].Count);
			Assert.IsTrue(dom.dataModels[0][0] is Peach.Core.Dom.Choice);
			Assert.IsTrue(dom.dataModels[0][1] is Peach.Core.Dom.Blob);

			var expected = new byte[] { 0x44, 0x55 };
			var actual = dom.dataModels[0][1].Value.ToArray();

			Assert.AreEqual(expected, actual);
		}


		[Test]
		public void ChoiceSizeRelations2()
		{
			string xml = @"
<Peach>
	<DataModel name=""DM"">
		<Block name=""TheBlock"">
			<Choice>
				<Block name=""C1"">
					<Number name=""version"" size=""8"" value=""1"" token=""true""/>
					<Number name=""LengthBig"" size=""16"">
						<Relation type=""size"" of=""TheBlock""/>
					</Number>
				</Block>
				<Block name=""C2"">
					<Number name=""version"" size=""8"" value=""2"" token=""true""/>
					<Number name=""LengthSmall"" size=""8"">
						<Relation type=""size"" of=""TheBlock""/>
					</Number>
				</Block>
			</Choice>
			<Blob name=""data""/>
		</Block>
		<Blob/>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", new byte[] { 0x02, 0x05, 0x33, 0x44, 0x55 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);


			Peach.Core.Dom.Block TheBlock = (Peach.Core.Dom.Block)dom.dataModels[0][0];
			Assert.AreEqual(2, TheBlock.Count);
			Assert.IsTrue(TheBlock[0] is Peach.Core.Dom.Choice);
			Assert.IsTrue(TheBlock[1] is Blob);
			Assert.IsTrue(dom.dataModels[0][1] is Blob);
			Assert.AreEqual(3, TheBlock[1].Value.Length);
			Assert.AreEqual(0, ((Peach.Core.Dom.Blob)dom.dataModels[0][1]).Value.Length);

		}

		[Test]
		public void ChoiceArrayField()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Block name='Root' minOccurs='0'>
			<Choice name='Choice'>
				<Block name='C1'>
					<String name='str1' value='Choice 1='/>
					<String name='str2'/>
				</Block>
				<Block name='C2'>
					<String name='str1' value='Choice 2='/>
					<String name='str2'/>
				</Block>
			</Choice>
			<Blob name='data'/>
		</Block>
		<Blob/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM' />
				<Data>
					<Field name='Root[0].Choice.C1.str2' value='foo,' />
					<Field name='Root[1].Choice.C2.str2' value='bar' />
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='Null'/>
	</Test>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(null);
			e.startFuzzing(dom, config);

			var dm = dom.tests[0].stateModel.states["Initial"].actions[0].dataModel;

			var bytes = dm.Value.ToArray();
			string str = Encoding.ASCII.GetString(bytes);
			Assert.AreEqual("Choice 1=foo,Choice 2=bar", str);
		}

		[Test]
		public void ChoiceUnsizedLookahead()
		{
			string xml = @"
<Peach>
	<DataModel name=""DM"">
		<Choice name=""c"">
			<Block name=""C1"">
				<Number size=""8"" value=""0xff"" token=""true""/>
				<Blob/>
			</Block>
			<Block name=""C2"">
				<Blob length=""1""/>
				<Blob/>
			</Block>
		</Choice>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			// Generate Value prior to cracking, to simulate state model running then an input action
			var model = dom.dataModels[0].Value;
			Assert.NotNull(model);

			var data = Bits.Fmt("{0}", new byte[] { 0x02, 0x05, 0x33, 0x44, 0x55 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);


			Peach.Core.Dom.Choice c = (Peach.Core.Dom.Choice)dom.dataModels[0][0];
			var selected = c.SelectedElement as Peach.Core.Dom.Block;
			Assert.AreEqual("C2", selected.Name);
			Assert.AreEqual(1, selected[0].DefaultValue.BitsToArray().Length);
			Assert.AreEqual(4, selected[1].DefaultValue.BitsToArray().Length);
		}

		[Test]
		public void ChoiceUnsizedLookahead2()
		{
			string xml = @"
<Peach>
	<DataModel name=""DM"">
		<Choice name=""c"">
			<Block name=""C1"">
				<Blob name=""blb"" length=""1"" valueType=""hex"" value=""0x08"" token=""true""/>
				<Block name=""array"" minOccurs=""0"">
					<Blob name=""inner"" length=""1"" valueType=""hex"" value=""0x05""/>
					<Blob name=""unsized""/>
				</Block>
			</Block>
		</Choice>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			// Generate Value prior to cracking, to simulate state model running then an input action
			var model = dom.dataModels[0].Value;
			Assert.NotNull(model);

			var data = Bits.Fmt("{0}", new byte[] { 0x08, 0x05, 0x33, 0x44, 0x55 });

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);


			Peach.Core.Dom.Choice c = dom.dataModels[0][0] as Peach.Core.Dom.Choice;
			Assert.NotNull(c);
			var selected = c.SelectedElement as Peach.Core.Dom.Block;
			Assert.NotNull(selected);
			Assert.AreEqual("C1", selected.Name);
			Assert.AreEqual(2, selected.Count);
			var array = selected[1] as Peach.Core.Dom.Array;
			Assert.NotNull(array);
			Assert.AreEqual(1, array.Count);
			var innerBlock = array[0] as Peach.Core.Dom.Block;
			Assert.NotNull(innerBlock);
			Assert.AreEqual(2, innerBlock.Count);
			Assert.AreEqual(1, selected[0].DefaultValue.BitsToArray().Length);
			Assert.AreEqual(1, innerBlock[0].DefaultValue.BitsToArray().Length);
			Assert.AreEqual(3, innerBlock[1].DefaultValue.BitsToArray().Length);
		}

		void TryCrackChoice(DataModel model, string data, bool shouldFail)
		{
			var bs = Bits.Fmt("{0}", data);

			var cracker = new DataCracker();

			try
			{
				cracker.CrackData(model.Clone(), bs);
				Assert.False(shouldFail);
			}
			catch (CrackingFailure)
			{
				Assert.True(shouldFail);
			}
		}

		[Test]
		public void CrackDerivedChoice()
		{
			string xml = @"
<Peach>
	<DataModel name='Parent'>
		<Choice name='Prefixes'>
			<String value='A' token='true' />
			<String value='B' token='true' />
		</Choice>
		<String value='-' token='true' />
		<String value='Hello' token='true' />
	</DataModel>

	<DataModel name='Child' ref='Parent'>
		<String name='Prefixes.X' value='X' token='true' />
		<String name='Prefixes.Y' value='Y' token='true' />
	</DataModel>

	<DataModel name='Grandchild' ref='Child'>
		<String name='Prefixes.Z' value='Z' token='true' />
	</DataModel>
</Peach>
";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			TryCrackChoice(dom.dataModels[0], "A-Hello", false);
			TryCrackChoice(dom.dataModels[0], "B-Hello", false);
			TryCrackChoice(dom.dataModels[0], "X-Hello", true);
			TryCrackChoice(dom.dataModels[0], "Y-Hello", true);
			TryCrackChoice(dom.dataModels[0], "Z-Hello", true);
			TryCrackChoice(dom.dataModels[0], "Q-Hello", true);

			TryCrackChoice(dom.dataModels[1], "A-Hello", false);
			TryCrackChoice(dom.dataModels[1], "B-Hello", false);
			TryCrackChoice(dom.dataModels[1], "X-Hello", false);
			TryCrackChoice(dom.dataModels[1], "Y-Hello", false);
			TryCrackChoice(dom.dataModels[1], "Z-Hello", true);
			TryCrackChoice(dom.dataModels[1], "Q-Hello", true);

			TryCrackChoice(dom.dataModels[2], "A-Hello", false);
			TryCrackChoice(dom.dataModels[2], "B-Hello", false);
			TryCrackChoice(dom.dataModels[2], "X-Hello", false);
			TryCrackChoice(dom.dataModels[2], "Y-Hello", false);
			TryCrackChoice(dom.dataModels[2], "Z-Hello", false);
			TryCrackChoice(dom.dataModels[2], "Q-Hello", true);
		}

		[Test]
		public void FindChosenElement()
		{
			string xml = @"
<Peach>
	<DataModel name=""DM"">
		<Choice name=""Choice"">
			<String name=""Foo"" value=""Foo"" token=""true""/>
			<String name=""Bar"" value=""Bar"" token=""true""/>
		</Choice>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			// Prior to selecting a choice, we should be able to find Foo and Bar
			var foo = dom.dataModels[0].find("DM.Choice.Foo");
			Assert.NotNull(foo);
			var bar = dom.dataModels[0].find("DM.Choice.Bar");
			Assert.NotNull(bar);

			var data = Bits.Fmt("{0}", "Bar");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			// After selecting a choice, we should only be able to find the chosen element
			foo = dom.dataModels[0].find("DM.Choice.Foo");
			Assert.Null(foo);
			bar = dom.dataModels[0].find("DM.Choice.Bar");
			Assert.NotNull(bar);
		}

		[Test]
		public void ScriptingThrowsPeach()
		{
			string xml = @"
<Peach>
	<DataModel name=""DM"">
		<Choice name=""Choice"">
			<Block>
				<Number size=""16"" name=""Foo"">
					<Relation type=""size"" of=""Value"" expressionGet=""a bad scripting expression""/>
				</Number>
				<String name=""Value""/>
			</Block>
			<String name=""Bar"" value=""Bar""/>
		</Choice>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "Bar");

			DataCracker cracker = new DataCracker();

			// Invalid syntax in a scripting expression should propigate all the way up
			// as a PeachException and not result in us matching on choice 'Bar'
			Assert.Throws<PeachException>(() => cracker.CrackData(dom.dataModels[0], data));
		}

		[Test]
		public void ScriptingThrowsSoft()
		{
			string xml = @"
<Peach>
	<DataModel name=""DM"">
		<Choice name=""Choice"">
			<Block>
				<Number size=""16"" name=""Foo"">
					<Relation type=""size"" of=""Value"" expressionGet=""int(value.foo)""/>
				</Number>
				<String name=""Value""/>
			</Block>
			<String name=""Bar"" value=""Bar""/>
		</Choice>
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "Bar");

			DataCracker cracker = new DataCracker();

			// Runtime error in a scripting expression should propigate all the way up
			// as a SoftException and not result in us matching on choice 'Bar'
			Assert.Throws<SoftException>(() => cracker.CrackData(dom.dataModels[0], data));
		}

		[Test]
		public void ChoiceCache()
		{
			string xml = @"
<Peach>
	<DataModel name=""DM"">
		<Blob value='ff aa' valueType='hex' token='true' />

		<Choice>
			<Blob value='ff bb' valueType='hex' token='true' />
		</Choice>

	</DataModel>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);

			var data = new BitStream(new byte[] {0xff, 0xaa, 0xff, 0xbb});

			var cracker = new DataCracker();

			cracker.CrackData(dom.dataModels[0], data);

		}

		[Test]
		public void ClearChoices()
		{
			// Once cracking completes, non-selected choices should be removed

			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<Choice>
			<Number size='32'>
				<Relation type='size' of='data' />
			</Number>
			<Number size='32'>
				<Relation type='size' of='data' />
			</Number>
			<Number size='32'>
				<Relation type='size' of='data' />
			</Number>
			<Number size='32'>
				<Relation type='size' of='data' />
			</Number>
		</Choice>
		<Blob name='data'/>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='input'>
				<DataModel ref='DM' />
			</Action>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
			<Action type='getProperty' property='foo'>
				<DataModel ref='DM' />
			</Action>
			<Action type='setProperty' property='foo'>
				<DataModel ref='DM' />
			</Action>
		</State>
	</StateModel>
</Peach>
";

			var dom = DataModelCollector.ParsePit(xml);
			Assert.NotNull(dom);

			// After pit parse, all data models should have 4 choices and 4 blob relations
			var dm = dom.dataModels[0];

			Assert.AreEqual(4, ((Choice)dm[0]).choiceElements.Count);
			Assert.AreEqual(4, dm[1].relations.Of<Relation>().Count());


			for (var i = 0; i < 4; ++i)
			{
				dm = dom.stateModels[0].states[0].actions[i].dataModel;

				Assert.AreEqual(4, ((Choice)dm[0]).choiceElements.Count);
				Assert.AreEqual(4, dm[1].relations.Of<Relation>().Count());
			}

			var bs = new BitStream(new byte[] { 0, 0, 0, 0 });

			dm = dom.dataModels[0];
			bs.Position = 0;
			new DataCracker().CrackData(dm, bs);

			// After cracking top level model, non-selected choices should not be removed
			Assert.AreEqual(4, ((Choice)dm[0]).choiceElements.Count);
			Assert.AreEqual(5, dm[1].relations.Of<Relation>().Count());

			// NOTE: There are 5 Of relations, one for each choice option
			// and one for the currently selected choice

			dm = dom.stateModels[0].states[0].actions[0].dataModel;
			bs.Position = 0;
			new DataCracker().CrackData(dm, bs);

			// After cracking input model, non-selected choices should not be removed
			Assert.AreEqual(4, ((Choice)dm[0]).choiceElements.Count);
			Assert.AreEqual(5, dm[1].relations.Of<Relation>().Count());

			dm = dom.stateModels[0].states[0].actions[1].dataModel;
			bs.Position = 0;
			new DataCracker().CrackData(dm, bs);

			// After cracking output model, non-selected choices should not be removed
			Assert.AreEqual(4, ((Choice)dm[0]).choiceElements.Count);
			Assert.AreEqual(5, dm[1].relations.Of<Relation>().Count());

			dm = dom.stateModels[0].states[0].actions[2].dataModel;
			bs.Position = 0;
			new DataCracker().CrackData(dm, bs);

			// After cracking getProperty model, non-selected choices should not be removed
			Assert.AreEqual(4, ((Choice)dm[0]).choiceElements.Count);
			Assert.AreEqual(5, dm[1].relations.Of<Relation>().Count());

			dm = dom.stateModels[0].states[0].actions[1].dataModel;
			bs.Position = 0;
			new DataCracker().CrackData(dm, bs);

			dm = dom.stateModels[0].states[0].actions[3].dataModel;
			bs.Position = 0;
			new DataCracker().CrackData(dm, bs);

			// After cracking setProperty model, non-selected choices should not be removed
			Assert.AreEqual(4, ((Choice)dm[0]).choiceElements.Count);
			Assert.AreEqual(5, dm[1].relations.Of<Relation>().Count());
		}

		[Test]
		public void TestChoiceNoAnalyzer()
		{
			// If cracking fails on a choice path where the cracker
			// has successfully cracked an element with an analyzer
			// we need to make sure the analyzr doesn't try to run
			// after the model has completed cracking

			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<Choice name='C'>
			<Block name='Array' occurs='2'>
				<String name='Key' />
				<String name='Delim' value=':' token='true' />
				<String name='Value'>
					<Analyzer class='StringToken' />
				</String>
				<String name='EOL' value='\n' token='true' />
			</Block>
			<Blob name='Default' />
		</Choice>
	</DataModel>
</Peach>
";
			var dom = DataModelCollector.ParsePit(xml);

			var bs = Bits.Fmt("{0}", "Item1:The-Value\n");

			var cracker = new DataCracker();

			cracker.CrackData(dom.dataModels[0], bs);

			var expected = new[] { "DM", "DM.C", "DM.C.Default" };
			var actual = dom.dataModels[0].PreOrderTraverse().Select(e => e.fullName).ToList();

			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void CacheMissFallback()
		{
			// If cracking fails on a choice path where the cracker
			// has successfully cracked an element with an analyzer
			// we need to make sure the analyzr doesn't try to run
			// after the model has completed cracking

			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<Choice name='C'>
			<Block name='C1'>
				<String name='Foo' value='Foo' token='true' />
				<String name='Bar' value='Bar' token='true' />
			</Block>
			<Blob name='C2' />
		</Choice>
	</DataModel>
</Peach>
";
			var dom = DataModelCollector.ParsePit(xml);

			var bs = Bits.Fmt("{0}", "FooFoo");

			var cracker = new DataCracker();

			var visited = new List<string>();

			cracker.EnterHandleNodeEvent += (e, p, d) => visited.Add(e.Name);

			cracker.CrackData(dom.dataModels[0], bs);

			var expected = new[] { "DM", "DM.C", "DM.C.C2" };
			var actual = dom.dataModels[0].PreOrderTraverse().Select(e => e.fullName).ToList();

			CollectionAssert.AreEqual(expected, actual);

			expected = new[] { "DM", "C", "C1", "Foo", "Bar", "C2" };

			CollectionAssert.AreEqual(expected, visited);
		}

		[Test]
		public void TestShallowClone()
		{
			using (var tmp = new TempFile())
			{
				File.WriteAllText(tmp.Path, "2222444411113333");

				var xml = @"
<Peach>
	<!-- choice w/ nested choice -->
	<DataModel name='DM1'>
		<Choice name='C1'>
			<String name='S' value='1111' token='true' />
			<Blob name='B'  value='2222' token='true' />
			<Choice name='C2'>
				<Block name='BA'>
					<String name='Inner' value='3333' token='true' />
				</Block>
				<Block name='BB' />
			</Choice>
		</Choice>
	</DataModel>

	<!-- Derived model with overridden choice -->
	<DataModel name='DM2' ref='DM1'>
		<Block name='C1.C2.BB'>
			<String name='Inner' value='4444' token='true' />
		</Block>
	</DataModel>

	<!-- Encapsulate choice in array -->
	<DataModel name='DM3'>
		<Block name='A' ref='DM2' minOccurs='0' />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM3' />
				<Data fileName='{0}'>
					<!-- Change selected element from 2222 to 3333 -->
					<Field name='A[0].C1.C2.BA' value='' />
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<Publisher class='Null' />
		<StateModel ref='SM' />
	</Test>
</Peach>
".Fmt(tmp.Path);

				var dom = DataModelCollector.ParsePit(xml);
				var cfg = new RunConfiguration { singleIteration = true };
				var e = new Engine(null);

				e.startFuzzing(dom, cfg);

				var dm = dom.tests[0].stateModel.states[0].actions[0].dataModel;
				var final = dm.Value.ToArray();

				Assert.AreEqual("3333444411113333", Encoding.ASCII.GetString(final, 0, final.Length));

				var a = (Array)dm[0];

				Assert.NotNull(a.OriginalElement);
				Assert.AreEqual(4, a.Count);

				var dm2 = (DataElementContainer)a.OriginalElement;
				var choice = (Choice)dm2[0];

				var innerChoice = (Choice)choice.choiceElements[2];

				foreach (var item in a.Cast<DataElementContainer>())
				{
					var child = (Choice)item[0];

					// For the instantiated array entries, the choice options should all point
					// to the same list of available choices
					Assert.True(choice.choiceElements.GetHashCode() == child.choiceElements.GetHashCode(), "Choice options should be the same");

					var innerChild = (Choice)child.choiceElements[2];

					Assert.True(innerChoice.choiceElements.GetHashCode() == innerChild.choiceElements.GetHashCode(), "Choice options should be the same");

				}

				var elemNames = dm.Walk().Select(i => i.fullName).ToArray();
				var expected = new[]
				{
					"DM3",
					"DM3.A",
					"DM3.A.A_0",
					"DM3.A.A_0.C1",
					"DM3.A.A_0.C1.C2",
					"DM3.A.A_0.C1.C2.BA",
					"DM3.A.A_0.C1.C2.BA.Inner",
					"DM3.A.A_1",
					"DM3.A.A_1.C1",
					"DM3.A.A_1.C1.C2",
					"DM3.A.A_1.C1.C2.BB",
					"DM3.A.A_1.C1.C2.BB.Inner",
					"DM3.A.A_2",
					"DM3.A.A_2.C1",
					"DM3.A.A_2.C1.S",
					"DM3.A.A_3",
					"DM3.A.A_3.C1",
					"DM3.A.A_3.C1.C2",
					"DM3.A.A_3.C1.C2.BA",
					"DM3.A.A_3.C1.C2.BA.Inner"
				};

				CollectionAssert.AreEqual(expected, elemNames);
			}
		}

		[Test]
		[Ignore("See PF-270")]
		public void TestChoiceNoPlacement()
		{
			// TODO: Need a test for before, after and absolute placement!

			// If cracking fails on a choice path where the cracker
			// has successfully placed an element we need to
			// clean up our placed elements when trying the next option

			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<Choice name='C'>
			<Block name='Array' occurs='2'>
				<String name='Key' />
				<String name='Delim' value=':' token='true' />
				<String name='Value' />
				<String name='EOL' value='\n' token='true' />
				<String name='Body' length='5'>
					<Placement before='Marker' />
				</String>
			</Block>
			<Blob name='Default' />
		</Choice>
		<Block name='Marker' />
	</DataModel>
</Peach>
";
			var dom = DataModelCollector.ParsePit(xml);

			var bs = Bits.Fmt("{0}", "Item1:The-Value\n");

			var cracker = new DataCracker();

			cracker.CrackData(dom.dataModels[0], bs);

			var expected = new[] { "DM", "DM.C", "DM.C.Default", "DM.Marker" };
			var actual = dom.dataModels[0].PreOrderTraverse().Select(e => e.fullName).ToList();

			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void TestRelationMaintained()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<Block name='Items' minOccurs='0'>
			<Number name='Type' size='8' />
			<Choice name='Kind'>
				<Number name='L8' size='8' constraint='int(Type)==1)'>
					<Relation type='size' of='Value' />
				</Number>
				<Number name='L16' size='8'>
					<Relation type='size' of='Value' />
				</Number>
			</Choice>
			<Blob name='Value' />
		</Block>
	</DataModel>
</Peach>
";

			var dom = DataModelCollector.ParsePit(xml);

			Assert.NotNull(dom);

			var array = (Array)dom.dataModels[0][0];

			array.ExpandTo(1);

			var block = (Block)array[0];

			// Thge 0th blob should have two relations
			Assert.AreEqual(2, block[2].relations.Count);

			// The relations should point to the choice in the 0th element
			Assert.AreEqual("DM.Items.Items_0.Kind.L8", block[2].relations[0].From.fullName);
			Assert.AreEqual("DM.Items.Items_0.Kind.L16", block[2].relations[1].From.fullName);
		}

		[Test]
		public void TestShallowCopy()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<Block name='Items' minOccurs='0'>
			<Choice name='Kind'>
				<Blob name='B1' />
			</Choice>
		</Block>
	</DataModel>
</Peach>
";

			var dom = DataModelCollector.ParsePit(xml);

			Assert.NotNull(dom);

			var array = (Array)dom.dataModels[0][0];
			var ch = ((Choice)((Block)array.OriginalElement)[0]).choiceElements[0];
			array.ExpandTo(5);

			Assert.AreEqual(5, array.Count);

			foreach (var item in array.Cast<Block>())
			{
				var choice = (Choice)item[0];

				Assert.True(choice.choiceElements[0].GetHashCode() == ch.GetHashCode());
			}
		}

		[Test]
		public void TestAnalyzer()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<Choice name='CH'>
			<String name='Value'>
				<Analyzer class='StringToken' />
			</String>
			<Block name='NoValue' />
		</Choice>
	</DataModel>
</Peach>
";

			var dom = DataModelCollector.ParsePit(xml);

			Assert.NotNull(dom);

			var cracker = new DataCracker();
			var bs = new BitStream(Encoding.ASCII.GetBytes("Hello|World"));

			cracker.CrackData(dom.dataModels[0], bs);

			var choice = (Choice)dom.dataModels[0][0];

			Assert.That(choice.SelectedElement, Is.InstanceOf<Block>());

			var children = choice.SelectedElement.PreOrderTraverse().Select(i => i.fullName).ToList();

			Assert.AreEqual(new[]
			{
				"DM.CH.Value",
				"DM.CH.Value.Value",
				"DM.CH.Value.Value.Pre",
				"DM.CH.Value.Value.Token",
				"DM.CH.Value.Value.Post"
			},
			children);
		}
	}
}

// end
