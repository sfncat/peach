using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Newtonsoft.Json;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Encoding = System.Text.Encoding;

namespace Peach.Pro.Test.Core.WebServices
{
	[TestFixture]
	[Quick]
	[Peach]
	class JsonToPit
	{
		[Test]
		public void Test()
		{
			var json = @"
[
    {
        ""monitors"": [
            {
                ""monitorClass"":""Pcap"",
                ""map"":[
                    {""name"":""Device"", value:""MyInterface""},
                    {""name"":""Filter"", value:""MyFilter""}
                ],
                ""description"":""Network capture on interface {PcapDevice} using {PcapFilter}, collect from {AgentUrl}""
            }
        ]
    }
]";

			var pit = @"
<Peach>
	<Include ns='foo' src='other'/>

	<Agent>
		<Monitor class='Foo' />
	</Agent>

	<Agent location='tcp://1.1.1.1'/>

	<Test name='Default'>
		<Publisher name='Pub'/>
		<Strategy class='Random'/>
		<Agent ref='Foo'/>
		<Agent ref='Bar'/>
		<Logger class='Simple'/>
	</Test>
</Peach>
";

			var agents = JsonConvert.DeserializeObject<List<Pro.Core.WebServices.Models.Agent>>(json);

			Assert.NotNull(agents);
			Assert.AreEqual(1, agents.Count);

			var doc = new XmlDocument();
			doc.LoadXml(pit);

			var nav = doc.CreateNavigator();

			while (true)
			{
				var oldAgent = nav.SelectSingleNode("//Agent");

				if (oldAgent != null)
					oldAgent.DeleteSelf();
				else
					break;
			}

			var sb = new StringBuilder();
			using (var wtr = XmlWriter.Create(new StringWriter(sb), new XmlWriterSettings() { Indent = true, OmitXmlDeclaration = true}))
			{
				doc.WriteTo(wtr);
			}

			var final = sb.ToString();
			Assert.NotNull(final);

			var expected =
@"<Peach>
  <Include ns=""foo"" src=""other"" />
  <Test name=""Default"">
    <Publisher name=""Pub"" />
    <Strategy class=""Random"" />
    <Logger class=""Simple"" />
  </Test>
</Peach>";

			expected = expected.Replace("\r\n", "\n");
			final = final.Replace("\r\n", "\n");

			Assert.AreEqual(expected, final);
		}

		[Test]
		public void NamespacedXml()
		{
			var xml =
@"<?xml version='1.0' encoding='utf-8'?>
<Peach xmlns='http://peachfuzzer.com/2012/Peach'
       xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
       xsi:schemaLocation='http://peachfuzzer.com/2012/Peach peach.xsd'
       author='Pit Author Name'
       description='IMG PIT'
       version='0.0.1'>
	<Test name='Default' />
</Peach>
";

			using (var tmp = new TempFile())
			{
				File.WriteAllText(tmp.Path, xml);

				var doc = new XmlDocument();

				using (var rdr = XmlReader.Create(tmp.Path))
				{
					doc.Load(rdr);
				}

				var nav = doc.CreateNavigator();

				var nsMgr = new XmlNamespaceManager(nav.NameTable);
				nsMgr.AddNamespace("p", "http://peachfuzzer.com/2012/Peach");

				var test = nav.Select("/p:Peach/p:Test", nsMgr);

				Assert.True(test.MoveNext());
				Assert.AreEqual("Default", test.Current.GetAttribute("name", ""));
			}
		}


		[Test]
		public void WriteXmlWithAttrs()
		{
			var xml =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach xmlns=""http://peachfuzzer.com/2012/Peach"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""http://peachfuzzer.com/2012/Peach peach.xsd"" author=""Pit Author Name"">
  <Test name=""Default"" />
</Peach>";

			using (var src = new TempFile())
			using (var dst = new TempFile())
			{
				File.WriteAllText(src.Path, xml);

				var doc = new XmlDocument();

				using (var rdr = XmlReader.Create(src.Path))
				{
					doc.Load(rdr);
				}

				var settings = new XmlWriterSettings()
				{
					Indent = true,
					Encoding = Encoding.UTF8,
					IndentChars = "  ",
				};

				using (var writer = XmlWriter.Create(dst.Path, settings))
				{
					doc.WriteTo(writer);
				}

				var srcStr = File.ReadAllText(src.Path).Replace("\r\n", "\n");
				var dstStr = File.ReadAllText(dst.Path).Replace("\r\n", "\n");

				Assert.AreEqual(srcStr, dstStr);
			}
		}

		[Test]
		public void AddXmlElement()
		{
			var xml =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach xmlns=""http://peachfuzzer.com/2012/Peach"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""http://peachfuzzer.com/2012/Peach peach.xsd"" author=""Pit Author Name"">
  <Test name=""Default"" />
</Peach>";

			var expected =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach xmlns=""http://peachfuzzer.com/2012/Peach"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""http://peachfuzzer.com/2012/Peach peach.xsd"" author=""Pit Author Name"">
  <Agent name=""TheAgent"" />
  <Test name=""Default"" />
</Peach>";

			using (var src = new TempFile())
			using (var dst = new TempFile())
			{
				File.WriteAllText(src.Path, xml);

				var doc = new XmlDocument();

				using (var rdr = XmlReader.Create(src.Path))
				{
					doc.Load(rdr);
				}

				var nav = doc.CreateNavigator();

				var nsMgr = new XmlNamespaceManager(nav.NameTable);
				nsMgr.AddNamespace("p", "http://peachfuzzer.com/2012/Peach");

				var test = nav.SelectSingleNode("/p:Peach/p:Test", nsMgr);

				Assert.NotNull(test);

				using (var w = test.InsertBefore())
				{
					w.WriteStartElement("Agent", "http://peachfuzzer.com/2012/Peach");
					w.WriteAttributeString("name", "TheAgent");
					w.WriteEndElement();
				}

				var settings = new XmlWriterSettings()
				{
					Indent = true,
					Encoding = Encoding.UTF8,
					IndentChars = "  ",
				};

				using (var writer = XmlWriter.Create(dst.Path, settings))
				{
					doc.WriteTo(writer);
				}

				var srcStr = expected.Replace("\r\n", "\n");
				var dstStr = File.ReadAllText(dst.Path).Replace("\r\n", "\n");

				Assert.AreEqual(srcStr, dstStr);
			}
		}

		public class IntMember
		{
			public int Value { get; set; }
		}

		[Test]
		public void JsonInt()
		{
			const string json = "{ \"Value\" : 500 }";

			var obj = JsonConvert.DeserializeObject<IntMember>(json);

			Assert.NotNull(obj);
			Assert.AreEqual(500, obj.Value);
		}

		class Model
		{
			public ulong Id { get; set; }
		}

		[Test]
		public void LongTest()
		{
			var m1 = new Model { Id = long.MaxValue };
			var asJson = JsonConvert.SerializeObject(m1);
			Assert.AreEqual("{\"Id\":9223372036854775807}", asJson);
			var m2 = JsonConvert.DeserializeObject<Model>(asJson);
			Assert.AreEqual(m1.Id, m2.Id);
		}
	}
}
