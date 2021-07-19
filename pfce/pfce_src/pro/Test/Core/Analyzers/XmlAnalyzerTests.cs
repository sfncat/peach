
using System.IO;
using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Core.Test;
using Peach.Pro.Test.Core.StateModel;

namespace Peach.Pro.Test.Core.Analyzers
{
	[TestFixture]
	[Quick]
	[Peach]
    class XmlAnalyzerTests : DataModelCollector
    {
        [Test]
        public void BasicTest()
        {
            string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
                "	<DataModel name=\"TheDataModel\">" +
                "       <String value=\"&lt;Root&gt;&lt;Element1 attrib1=&quot;Attrib1Value&quot; /&gt;&lt;/Root&gt;\"> "+
                "           <Analyzer class=\"Xml\" /> " +
                "       </String>"+
                "	</DataModel>" +
                "</Peach>";

            PitParser parser = new PitParser();
            Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

            Assert.IsTrue(dom.dataModels["TheDataModel"][0] is Peach.Core.Dom.XmlElement);

            var elem1 = dom.dataModels["TheDataModel"][0] as Peach.Core.Dom.XmlElement;

            Assert.AreEqual("Root", elem1.elementName);
        }

		[Test]
		public void AdvancedTest()
		{
			string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">

		<String value=""&lt;Root&gt;
		                &lt;Element1 attrib1=&quot;Attrib1Value&quot;&gt;
		                Hello
		                &lt;/Element1&gt;
		                &lt;/Root&gt;"">
			<Analyzer class=""Xml""/>
		</String>

	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.IsTrue(dom.dataModels["TheDataModel"][0] is Peach.Core.Dom.XmlElement);

			var elem1 = dom.dataModels["TheDataModel"][0] as Peach.Core.Dom.XmlElement;

			Assert.AreEqual("Root", elem1.elementName);

			var result = dom.dataModels[0].Value;
			Assert.NotNull(result);
		}

		[Test]
		public void CdataTest()
		{
			var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">

		<String value=""&lt;value&gt;&lt;![CDATA[DescriptionFile]]&gt;&lt;/value&gt;"">
			<Analyzer class=""Xml""/>
		</String>

	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			Assert.IsTrue(dom.dataModels["TheDataModel"][0] is XmlElement);

			var result = dom.dataModels[0].Value;
			var reader = new BitReader(result);
			var buff = reader.ReadBytes((int)result.Length);
			var xmlResult = UTF8Encoding.UTF8.GetString(buff);

			Assert.AreEqual("<value><![CDATA[DescriptionFile]]></value>", xmlResult);
		}

		[Test]
		public void UnicodeTest()
		{
			string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""TheDataModel"">

		<String type=""utf8"" value=""&lt;Root&gt;
		                &lt;Element1 attrib1=&quot;{0} Attrib1Value&quot;&gt;
		                Hello {1}
		                &lt;/Element1&gt;
		                &lt;/Root&gt;"">
			<Analyzer class=""Xml""/>
		</String>

	</DataModel>
</Peach>".Fmt("\u0134", "\x0298");

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.UTF8.GetBytes(xml)));

			Assert.IsTrue(dom.dataModels["TheDataModel"][0] is Peach.Core.Dom.XmlElement);

			var elem1 = dom.dataModels["TheDataModel"][0] as Peach.Core.Dom.XmlElement;

			Assert.AreEqual("Root", elem1.elementName);

			var result = dom.dataModels[0].Value;
			var str = Encoding.UTF8.GetString(result.ToArray());
			Assert.NotNull(result);
			Assert.NotNull(str);
		}

		[Test]
		public void CrackXml1()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String>
			<Analyzer class='Xml'/>
		</String>
	</DataModel>
</Peach>
";

			string payload = @"<element>&lt;foo&gt;</element>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", payload);

			var cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(payload, dom.dataModels[0].InternalValue.BitsToString());

		}

		[Test]
		public void CrackXml2()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String>
			<Analyzer class='Xml'/>
		</String>
	</DataModel>
</Peach>
";

			string payload = @"<Peach xmlns=""http://peachfuzzer.com/2012/Peach"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""><Foo xsi:type=""Bar"" /></Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", payload);

			var cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			var actual = dom.dataModels[0].InternalValue.BitsToString();
			Assert.AreEqual(payload, actual);
		}

		[Test]
		public void CrackXml3()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String>
			<Analyzer class='Xml'/>
		</String>
	</DataModel>
</Peach>
";

			string payload = @"<?xml version=""1.0"" encoding=""utf-8""?><Peach xmlns=""http://peachfuzzer.com/2012/Peach"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""><Foo xsi:type=""Bar"" /></Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", payload);

			var cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			var actual = dom.dataModels[0].InternalValue.BitsToString();
			Assert.AreEqual(payload, actual);
		}

		[Test]
		public void CrackXml4()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String>
			<Analyzer class='Xml'/>
		</String>
	</DataModel>
</Peach>
";

			string payload = @"<?xml version=""1.0"" encoding=""utf-16"" standalone=""yes""?><Peach xmlns=""http://peachfuzzer.com/2012/Peach"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""><Foo xsi:type=""Bar"" /></Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", payload);

			var cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			var bytes = dom.dataModels[0].Value.ToArray();
			var actual = Encoding.Unicode.GetString(bytes);
			Assert.AreEqual(payload, actual);
		}

		[Test]
		public void MarkMutable()
		{
			var tmp = Path.GetTempFileName();

			try
			{
				File.WriteAllText(tmp, @"<Root><Elem>1</Elem><Elem>2</Elem></Root>");

				var xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='Root' type='utf8'>
			<Analyzer class='Xml'/>
		</String>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
				<Data fileName='{0}'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>

		<Exclude />
		<Include xpath='//Value'/>
	</Test>
</Peach>
".Fmt(tmp);

				var dom = ParsePit(xml);
				var cfg = new RunConfiguration { singleIteration = true };
				var e = new Engine(null);

				e.startFuzzing(dom, cfg);

				var dm = dom.tests[0].stateModel.states[0].actions[0].dataModel;

				Assert.NotNull(dm);

				foreach (var elem in dm.PreOrderTraverse())
				{
					if (elem.Name == "Value")
					{
						Assert.True(elem.isMutable, "{0} should be mutable".Fmt(elem.debugName));
					}
					else
					{
						Assert.False(elem.isMutable, "{0} should be non-mutable".Fmt(elem.debugName));
					}
				}
			}
			finally
			{
				File.Delete(tmp);
			}
		}

		[Test]
		public void Fuzz1()
		{
			// Trying to emit xmlns="" is invalid, have to remove xmlns attr
			// Swap attribute with element neighbor == no change

			string tmp = Path.GetTempFileName();

			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String type='utf8'>
			<Analyzer class='Xml'/>
		</String>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
				<Data fileName='{0}'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<Strategy class='Sequential'/>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
".Fmt(tmp);

			string payload = @"<Peach xmlns=""http://peachfuzzer.com/2012/Peach"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""><Foo xsi:type=""Bar"">Text</Foo></Peach>";
			File.WriteAllText(tmp, payload);

			RunEngine(xml);

			// Ran at least 9000 iterations
			Assert.Greater(dataModels.Count, 9000);

			var initial = dataModels[0];

			// For the analyzed data model, ensure
			// the string type is propigated as well as the
			// Peach.TypeTransform=false hint

			foreach (var item in initial.PreOrderTraverse())
			{
				var asStr = item as Peach.Core.Dom.String;
				if (asStr != null)
				{
					Assert.AreEqual(StringType.utf8, asStr.stringType);
					Assert.True(asStr.isMutable);
					Assert.True(asStr.Hints.ContainsKey("Peach.TypeTransform"));

					var h = asStr.Hints["Peach.TypeTransform"];

					Assert.AreEqual("Peach.TypeTransform", h.Name);
					Assert.AreEqual("false", h.Value);
				}
			}
		}

		[Test]
		public void Fuzz2()
		{
			// Trying to emit xmlns="" is invalid, have to remove xmlns attr
			// Swap attribute with element neighbor == no change

			string tmp = Path.GetTempFileName();

			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String>
			<Analyzer class='Xml'/>
		</String>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
				<Data fileName='{0}'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<Strategy class='Sequential'/>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
".Fmt(tmp);

			string payload = @"<Peach xmlns=""http://peachfuzzer.com/2012/Peach"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""><Foo xsi:type=""Bar"">Text</Foo></Peach>";
			File.WriteAllText(tmp, payload);

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			uint count = 0;

			var config = new RunConfiguration();
			var e = new Engine(this);
			e.IterationStarting += (ctx, curr, total) => ++count;
			e.startFuzzing(dom, config);

			int same = 0;

			for (int i = 0; i < dataModels.Count; ++i)
			{
				var final = Encoding.ISOLatin1.GetString(dataModels[i].Value.ToArray());
				if (final == payload)
				{
					var strategy = this.iterStrategies[i];
					Assert.NotNull(strategy);
					++same;
				}
			}

			Assert.Greater(count, 9000);
			Assert.AreEqual(count, dataModels.Count);
			Assert.Less(same, 10);
		}

		[Test]
		public void IgnoreDtd()
		{
			var payload = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple Computer//DTD PLIST 1.0//EN""
    ""http://www.applefoobaddomain.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0""/>
";

			string tmp = Path.GetTempFileName();

			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String>
			<Analyzer class='Xml'/>
		</String>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
				<Data fileName='{0}'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<Strategy class='Sequential'/>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
".Fmt(tmp);

			File.WriteAllText(tmp, payload);

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			var config = new RunConfiguration() { singleIteration = true };
			var e = new Engine(null);

			e.startFuzzing(dom, config);
		}

		[Test]
		public void PeriodsInElements()
		{
			var payload = @"<?xml version='1.0' encoding='UTF-8'?>
<A>
  <B..1 some.attr='x'>foo</B..1>
  <B.2>bar</B.2>
</A>
";

			string tmp = Path.GetTempFileName();

			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String>
			<Analyzer class='Xml'/>
		</String>
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
				<Data fileName='{0}'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<Strategy class='Sequential'/>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
	</Test>
</Peach>
".Fmt(tmp);

			File.WriteAllText(tmp, payload);

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			var config = new RunConfiguration() { singleIteration = true };
			var e = new Engine(null);

			Assert.DoesNotThrow(() => e.startFuzzing(dom, config));

			var bs = dom
				.stateModels[0]
				.states[0]
				.actions[0]
				.allData.First()
				.dataModel.Children().First()
				.Children().ToList();

			var b1 = bs[0] as XmlElement;
			Assert.NotNull(b1);
			Assert.AreEqual("B..1", b1.elementName);
			Assert.AreEqual("B__1", b1.Name);

			var b2 = bs[1] as XmlElement;
			Assert.NotNull(b1);
			Assert.AreEqual("B.2", b2.elementName);
			Assert.AreEqual("B_2", b2.Name);

			var b1Attr = b1[0] as XmlAttribute;
			Assert.NotNull(b1Attr);
			Assert.AreEqual("some.attr", b1Attr.attributeName);
			Assert.AreEqual("some_attr", b1Attr.Name);
		}

    }
}

// end
