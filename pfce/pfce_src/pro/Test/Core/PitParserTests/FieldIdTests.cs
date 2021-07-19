using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Dom.Actions;
using Peach.Core.Dom.XPath;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.PitParserTests
{
	[TestFixture]
	[Quick]
	internal class FieldIdTests
	{
		[Test]
		public void TestPitParse()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM1'>
		<String name='String' />
	</DataModel>

	<DataModel name='DM2'>
		<String name='Str1' />
		<Block name='Blk' fieldId='B'>
			<String name='Str2' />
		</Block>
		<Blob name='Blob' fieldId='C' />
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial' fieldId='a'>
			<Action type='output' fieldId='z'>
				<DataModel name='DM' />
			</Action>

			<Action type='call' fieldId='b' method='foo'>
				<Param>
					<DataModel name='DM' fieldId='c'>
						<Stream streamName='foo' fieldId='d' />

						<Json fieldId='e'>
							<Double size='64' fieldId='f' />
							<Sequence fieldId='g'>
								<Null fieldId='h' />
								<Bool fieldId='i' />
							</Sequence>
						</Json>

						<Frag fieldId='j'>
							<Block name='Template' fieldId='k' />
							<Block name='Payload' fieldId='l' />
						</Frag>

						<Blob fieldId='m' />
						<Choice fieldId='n' />
						<Number size='32' fieldId='o' />
						<Padding alignment='32' fieldId='p' />
						<String minOccurs='0' fieldId='q' />

						<Flags fieldId='r' size='32'>
							<Flag size='1' position='0' fieldId='s' />
						</Flags>

						<XmlElement fieldId='t' elementName='foo'>
							<XmlAttribute fieldId='u' attributeName='bar' />
						</XmlElement>

						<Asn1Type tag='1' fieldId='v' />
						<Asn1Tag fieldId='w' />
						<Asn1Length fieldId='x' />
						<BACnetTag fieldId='y' />
						<VarNumber fieldId='z' />
					</DataModel>
					<Data fieldId='Foo'>
						<Field name='n' value='v' />
					</Data>
				</Param>
			</Action>
		</State>
	</StateModel>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);

			Assert.NotNull(dom);

			Assert.Null(dom.dataModels[0].FieldId);
			Assert.Null(dom.dataModels[0][0].FieldId);

			Assert.Null(dom.dataModels[1].FieldId);
			Assert.Null(dom.dataModels[1][0].FieldId);
			Assert.AreEqual("B", dom.dataModels[1][1].FieldId);
			Assert.Null(((Block)dom.dataModels[1][1])[0].FieldId);
			Assert.AreEqual("C", dom.dataModels[1][2].FieldId);

			var s = dom.stateModels[0].states[0];

			Assert.AreEqual("a", s.FieldId);
			Assert.AreEqual("z", s.actions[0].FieldId);
			Assert.Null(s.actions[0].outputData.First().dataModel.FieldId);

			var a = (Call)s.actions[1];
			Assert.AreEqual("b", a.FieldId);

			var fields = a.parameters[0].dataModel.PreOrderTraverse().Select(e => e.FieldId).ToList();

			foreach (var i in a.parameters[0].allData)
				Assert.AreEqual("Foo", i.FieldId);

			var exp = new[]
			{
				"c",  // DataModel
				"d",  // Stream
				null, // Stream.Name
				null, // Stream.Attr
				null, // Stream.Content
				"e",  // Json
				"f",  // Double
				"g",  // Sequence
				"h",  // Null
				"i",  // Bool
				"j",  // Frag
				null, // Rendering
				"k",  // Template
				"l",  // Payload
				"m",  // Blob
				"n",  // Choice
				"o",  // Number
				"p",  // Padding
				"q",  // Array
				null, // String
				"r",  // Flags
				"s",  // Flag
				"t",  // XmlElement
				"u",  // XmlAttribute
				"v",  // Asn1Type
				null, // Asn1Type.class
				null, // Asn1Type.pc
				null, // Asn1Type.tag
				null, // Asn1Type.length
				"w",  // Asn1Tag
				"x",  // Asn1Length
				"y",  // BacNetTag
				null, // BacNetTag.Tag
				null, // BacNetTag.Class
				null, // BacNetTag.LenValueType
				"z"   // VarNumber
			};

			Assert.AreEqual(exp, fields);
		}

		[Test]
		public void TestXpath()
		{
			const string xml = @"
<Peach>
	<StateModel name='SM' initialState='Initial'>
		<State name='Initial' />
		<State name='a' fieldId='a'>
			<Action type='output' name='b' fieldId='b'>
				<DataModel name='DM' />
			</Action>

			<Action type='output'>
				<DataModel name='c' fieldId='c' />
			</Action>

			<Action type='call' name='d' fieldId='d' method='foo'>
				<Param>
					<DataModel name='e' fieldId='e'>
						<Blob />
						<Block name='f' fieldId='f'>
							<String name='g' fieldId='g' />
						</Block>
					</DataModel>
				</Param>
			</Action>
		</State>
	</StateModel>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);

			Assert.NotNull(dom);

			var resolver = new PeachXmlNamespaceResolver();
			var navi = new PeachXPathNavigator(dom.stateModels[0]);
			var iter = navi.Select("//*[@fieldId]", resolver);

			var result = new List<string>();

			while (iter.MoveNext())
			{
				var curr = (INamed)((PeachXPathNavigator)iter.Current).CurrentNode;

				result.Add(curr.Name);
			}

			CollectionAssert.IsNotEmpty(result);

			var exp = new[] { "a", "b", "c", "d", "e", "f", "g" };
			Assert.AreEqual(exp, result);
		}

		[Test]
		public void TestFullFieldId()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='pre' />
		<Block name='item' fieldId='row' >
			<String name='key' />
			<String name='value' fieldId='value' />
		</Block>
		<String name='post' />
	</DataModel>

	<StateModel name='SM' initialState='S1'>
		<State name='S1'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
		</State>

		<State name='S2'>
			<Action type='output'>
				<DataModel ref='DM' fieldId='S2' />
			</Action>
		</State>

		<State name='S3'>
			<Action type='output' fieldId='S3'>
				<DataModel ref='DM' />
			</Action>
		</State>

		<State name='S4' fieldId='S4'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
		</State>

		<State name='S5' fieldId='S5'>
			<Action type='output' fieldId='A'>
				<DataModel ref='DM' fieldId='DM' />
			</Action>
		</State>
	</StateModel>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			Assert.NotNull(dom);

			var result = new List<string>();

			foreach (var item in dom.dataModels[0].Walk())
			{
				result.Add(string.Format("{0} -> {1}", item.fullName, item.FullFieldId ?? ""));
			}

			foreach (var s in dom.stateModels[0].states)
			{
				var data = s.actions[0].outputData.First();

				foreach (var item in data.dataModel.Walk())
				{
					result.Add(string.Format("{0} -> {1}", item.fullName, DataElement.FieldIdConcat(data.FullFieldId, item.FullFieldId)));
				}
			}

			var expected = new[]
			{
				"DM -> ",
				"DM.pre -> ",
				"DM.item -> row",
				"DM.item.key -> row",
				"DM.item.value -> row.value",
				"DM.post -> ",
				"DM -> ",
				"DM.pre -> ",
				"DM.item -> row",
				"DM.item.key -> row",
				"DM.item.value -> row.value",
				"DM.post -> ",
				"DM -> S2",
				"DM.pre -> S2",
				"DM.item -> S2.row",
				"DM.item.key -> S2.row",
				"DM.item.value -> S2.row.value",
				"DM.post -> S2",
				"DM -> S3",
				"DM.pre -> S3",
				"DM.item -> S3.row",
				"DM.item.key -> S3.row",
				"DM.item.value -> S3.row.value",
				"DM.post -> S3",
				"DM -> S4",
				"DM.pre -> S4",
				"DM.item -> S4.row",
				"DM.item.key -> S4.row",
				"DM.item.value -> S4.row.value",
				"DM.post -> S4",
				"DM -> S5.A.DM",
				"DM.pre -> S5.A.DM",
				"DM.item -> S5.A.DM.row",
				"DM.item.key -> S5.A.DM.row",
				"DM.item.value -> S5.A.DM.row.value",
				"DM.post -> S5.A.DM"
			};

			CollectionAssert.AreEqual(expected, result);
		}

		[Test]
		public void TestTuningTraverseNoFieldIds()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='pre' />
		<Block name='item' occurs='1'>
			<String name='key' />
			<Block name='value'>
				<String name='nested' />
			</Block>
		</Block>
		<String name='post' />
		<Asn1Type name='Asn1Type' tag='1'>
			<String name='value' />
		</Asn1Type>
	</DataModel>

	<StateModel name='SM' initialState='S1'>
		<State name='S1'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
		</State>
	</StateModel>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			Assert.NotNull(dom);

			var result = dom.stateModels[0]
				.TuningTraverse()
				.Select(x => "{0} -> {1}".Fmt(x.Value.fullName, x.Key))
				.ToList();

			var expected = new[]
			{
				"DM -> S1.Action.DM",
				"DM.pre -> S1.Action.DM.pre",
				"DM.item -> S1.Action.DM.item",
				"DM.item.item -> S1.Action.DM.item",
				"DM.item.item.key -> S1.Action.DM.item.key",
				"DM.item.item.value -> S1.Action.DM.item.value",
				"DM.item.item.value.nested -> S1.Action.DM.item.value.nested",
				"DM.post -> S1.Action.DM.post",
				"DM.Asn1Type -> S1.Action.DM.Asn1Type",
				"DM.Asn1Type.class -> S1.Action.DM.Asn1Type",
				"DM.Asn1Type.pc -> S1.Action.DM.Asn1Type",
				"DM.Asn1Type.tag -> S1.Action.DM.Asn1Type",
				"DM.Asn1Type.length -> S1.Action.DM.Asn1Type",
				"DM.Asn1Type.value -> S1.Action.DM.Asn1Type.value",
			};

			CollectionAssert.AreEqual(expected, result);
		}

		[Test]
		public void TestTuningTraverseWithFieldIds()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='pre' />
		<Block name='item' occurs='1' fieldId='row'>
			<String name='key' />
			<String name='value' fieldId='value' />
		</Block>
		<String name='post' />
		<Asn1Type name='Asn1Type' tag='1' fieldId='foo'>
			<String name='value' fieldId='inner' />
		</Asn1Type>
	</DataModel>

	<StateModel name='SM' initialState='S1'>
		<State name='S1'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
		</State>
	</StateModel>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			Assert.NotNull(dom);

			var result = dom.stateModels[0]
				.TuningTraverse()
				.Select(x => "{0} -> {1}".Fmt(x.Value.fullName, x.Key))
				.ToList();

			var expected = new[]
			{
				"DM -> ",
				"DM.pre -> ",
				"DM.item -> row",
				"DM.item.item -> row",
				"DM.item.item.key -> row",
				"DM.item.item.value -> row.value",
				"DM.post -> ",
				"DM.Asn1Type -> foo",
				"DM.Asn1Type.class -> foo",
				"DM.Asn1Type.pc -> foo",
				"DM.Asn1Type.tag -> foo",
				"DM.Asn1Type.length -> foo",
				"DM.Asn1Type.value -> foo.inner",
			};

			CollectionAssert.AreEqual(expected, result);
		}

		[Test]
		public void TestValidName()
		{
			// Ensure all fieldId values are valid element names so they are xpath selectable
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='pre' fieldId='1bad' />
	</DataModel>
</Peach>
";

			var ex = Assert.Throws<PeachException>(() => DataModelCollector.ParsePit(xml));
			StringAssert.StartsWith("Error, Pit file failed to validate", ex.Message);
		}
	}
}
