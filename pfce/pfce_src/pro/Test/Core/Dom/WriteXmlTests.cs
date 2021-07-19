using System.IO;
using System.Text;
using System.Xml;
using NUnit.Framework;
using Peach.Core.Dom;
using Peach.Core.Test;
using Peach.Pro.Core.Analyzers;
using Peach.Pro.Core.Dom;
using UTF8Encoding = Peach.Core.UTF8Encoding;

namespace Peach.Pro.Test.Core.Dom
{
	[TestFixture]
	[Quick]
	[Category("Peach")]
	class WriteXmlTests : DataModelCollector
	{
		[Test]
		public void WithExtras()
		{
			var pit = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='Example1'>
		<String fieldId='Foo-Bar' name='Str' value='Foo:Bar'>
			<Hint name='Hinty' value='things' />
		</String>
		<Number size='32'>
			<Relation type='size' of='Data' />
		</Number>
		<Blob name='Data' value='aaaaaaa'>
			<Fixup class='Md5'>
				<Param name='ref' value='Str' />
			</Fixup>
			<Placement after='flags'/>
		</Blob>

		<Choice name='choice'>
			<String/>
			<Number size='16'/>
			<Blob/>
		</Choice>	

		<Flags name='flags' size='16'>
			<Flag name='f1' size='1' position='1'/>
			<Flag name='f2' size='1' position='2'/>
		</Flags>

		<Transformer class='Base64Encode' />
	</DataModel>

	<StateModel name='TheStateModel' initialState='initial'>
		<State name='initial'>
		  <Action type='output'>
			<DataModel ref='Example1'/>
		  </Action>
		</State>
	</StateModel>

	<Test name='Default' maxOutputSize='200'>
		<StateModel ref='TheStateModel'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
";

			var dom = ParsePit(pit);
			dom.dataModels[0][0].analyzer = new StringTokenAnalyzer();

			var settings = new XmlWriterSettings
			{
				Encoding = System.Text.Encoding.UTF8,
				Indent = true
			};

			using (var sout = new MemoryStream())
			{
				using (var xml = XmlWriter.Create(sout, settings))
				{
					xml.WriteStartDocument();
					xml.WriteStartElement("Peach");

					dom.dataModels[0].WritePit(xml);

					xml.WriteEndElement();
					xml.WriteEndDocument();
				}

				var pitOut = UTF8Encoding.UTF8.GetString(sout.ToArray());

				StringAssert.Contains("<String name=\"Str\" fieldId=\"Foo-Bar\" value=\"Foo:Bar\">", pitOut);
				Assert.Less(-1, pitOut.IndexOf("Analyzer class=\"StringToken\""));
				Assert.Less(-1, pitOut.IndexOf("Transformer class=\"Base64Encode\""));
				Assert.Less(-1, pitOut.IndexOf("Hint name=\"Hinty\""));
				Assert.Less(-1, pitOut.IndexOf("Fixup class=\"Md5\""));
				Assert.Less(-1, pitOut.IndexOf("Relation type=\"size\" of=\"Data\""));
				Assert.Less(-1, pitOut.IndexOf("Placement after=\"flags\""));
			}
		}

		[Test]
		public void RelationsBasic()
		{
			var pit = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='Example1'>
		<Number size='32'>
			<Relation type='size' of='Data' />
		</Number>
		<Blob name='Data' value='aaaaaaa'/>

		<Number size='32'>
			<Relation type='offset' of='Data2' />
		</Number>
		<Blob name='Data2' value='aaaaaaa'/>

		<Number size='32'>
			<Relation type='count' of='Array' />
		</Number>
		<Blob name='Array' value='aaaaaaa' maxOccurs='-1'/>
	</DataModel>

	<StateModel name='TheStateModel' initialState='initial'>
		<State name='initial'>
		  <Action type='output'>
			<DataModel ref='Example1'/>
		  </Action>
		</State>
	</StateModel>

	<Test name='Default' maxOutputSize='200'>
		<StateModel ref='TheStateModel'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
";

			var dom = ParsePit(pit);

			var settings = new XmlWriterSettings
			{
				Encoding = System.Text.Encoding.UTF8,
				Indent = true
			};

			using (var sout = new MemoryStream())
			{
				using (var xml = XmlWriter.Create(sout, settings))
				{
					xml.WriteStartDocument();
					xml.WriteStartElement("Peach");

					dom.dataModels[0].WritePit(xml);

					xml.WriteEndElement();
					xml.WriteEndDocument();
				}

				var pitOut = UTF8Encoding.UTF8.GetString(sout.ToArray());

				Assert.Less(-1, pitOut.IndexOf("Relation type=\"size\" of=\"Data\""));
				Assert.Less(-1, pitOut.IndexOf("Relation type=\"offset\" of=\"Data2\""));
				Assert.Less(-1, pitOut.IndexOf("Relation type=\"count\" of=\"Array\""));
			}
		}

		[Test]
		public void RelationsOptions()
		{
			var pit = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='Example1'>
		<Number size='32'>
			<Relation type='size' of='Data' expressionGet='1' expressionSet='1' />
		</Number>
		<Blob name='Data' value='aaaaaaa'/>

		<Number size='32'>
			<Relation type='offset' of='Data2' relative='true' relativeTo='Data' />
		</Number>
		<Blob name='Data2' value='aaaaaaa'/>

		<Number size='32'>
			<Relation type='count' of='Array' expressionGet='1' expressionSet='1' />
		</Number>
		<Blob name='Array' value='aaaaaaa' maxOccurs='-1'/>
	</DataModel>

	<StateModel name='TheStateModel' initialState='initial'>
		<State name='initial'>
		  <Action type='output'>
			<DataModel ref='Example1'/>
		  </Action>
		</State>
	</StateModel>

	<Test name='Default' maxOutputSize='200'>
		<StateModel ref='TheStateModel'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
";

			var dom = ParsePit(pit);

			var settings = new XmlWriterSettings
			{
				Encoding = System.Text.Encoding.UTF8,
				Indent = true
			};

			using (var sout = new MemoryStream())
			{
				using (var xml = XmlWriter.Create(sout, settings))
				{
					xml.WriteStartDocument();
					xml.WriteStartElement("Peach");

					dom.dataModels[0].WritePit(xml);

					xml.WriteEndElement();
					xml.WriteEndDocument();
				}

				var pitOut = UTF8Encoding.UTF8.GetString(sout.ToArray());

				Assert.Less(-1, pitOut.IndexOf("Relation type=\"size\" of=\"Data\" expressionGet=\"1\" expressionSet=\"1\""));
				Assert.Less(-1, pitOut.IndexOf("Relation type=\"offset\" of=\"Data2\" relative=\"true\" relativeTo=\"Data\""));
				Assert.Less(-1, pitOut.IndexOf("Relation type=\"count\" of=\"Array\" expressionGet=\"1\" expressionSet=\"1\""));
			}
		}

		[Test]
		public void WriteDataModelRef()
		{
			const string pit = @"
<Peach>
	<DataModel name='Template'>
		<String name='Value' />
	</DataModel>

	<DataModel name='DM'>
		<Block name='Inner' ref='Template' />
	</DataModel>

	<StateModel name='TheStateModel' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel ref='DM' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheStateModel'/>
		<Publisher class='Null'/>
	</Test>
</Peach>";

			var dom = ParsePit(pit);

			var settings = new XmlWriterSettings
			{
				Encoding = Encoding.UTF8,
				Indent = true,
				IndentChars = " ",
				OmitXmlDeclaration = true
			};

			var sb = new StringBuilder();
			using (var sout = new StringWriter(sb))
			{
				using (var xml = XmlWriter.Create(sout, settings))
				{
					xml.WriteStartDocument();

					dom.WritePit(xml);

					xml.WriteEndDocument();
				}

				var pitOut = sb.ToString();

				Assert.NotNull(pitOut);

				StringAssert.Contains("<Block ref=\"Template\" name=\"Inner\">", pitOut);
			}
		}
	}
}
