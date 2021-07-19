
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.XPath;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using Peach.Core.Dom.XPath;
using Peach.Core.IO;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.PitParserTests
{
	[TestFixture]
	[Quick]
	[Peach]
	class XmlTests
	{
		//[Test]
		//public void NumberDefaults()
		//{
		//    string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
		//        "	<Defaults>" +
		//        "		<Number size=\"8\" endian=\"big\" signed=\"true\"/>" +
		//        "	</Defaults>" +
		//        "	<DataModel name=\"TheDataModel\">" +
		//        "		<Number name=\"TheNumber\" size=\"8\"/>" +
		//        "	</DataModel>" +
		//        "</Peach>";

		//    PitParser parser = new PitParser();
		//    Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
		//    Number num = dom.dataModels[0][0] as Number;

		//    Assert.IsTrue(num.Signed);
		//    Assert.IsFalse(num.LittleEndian);
		//}

		[Test]
		public void BasicXmlElement()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<XmlElement elementName=\"Foo\">" +
				"           <XmlAttribute attributeName=\"bar\">" +
				"               <String value=\"attribute value\"/> " +
				"           </XmlAttribute>" +
				"		    <XmlElement elementName=\"ChildElement\">" +
				"               <XmlAttribute attributeName=\"name\">" +
				"                   <String value=\"attribute value\"/> " +
				"               </XmlAttribute>" +
				"           </XmlElement>" +
				"       </XmlElement>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			var elem = dom.dataModels[0][0];

			Assert.NotNull(elem);
			Assert.IsTrue(elem is Peach.Core.Dom.XmlElement);
			Assert.AreEqual(2, ((Peach.Core.Dom.XmlElement)elem).Count);
		}

		[Test]
		public void BasicXmlCharacterData()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<XmlElement elementName=\"Foo\">" +
				"           <XmlAttribute attributeName=\"bar\">" +
				"               <String value=\"attribute value\"/> " +
				"           </XmlAttribute>" +
				"		    <XmlCharacterData>" +
				"               <String value=\"Value of the CDATA\"/> " +
				"           </XmlCharacterData>" +
				"       </XmlElement>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			var elem = dom.dataModels[0][0];

			var stream = elem.Value;
			var outXml = new BitReader(stream).ReadString();

			Assert.NotNull(elem);
			Assert.IsTrue(elem is Peach.Core.Dom.XmlElement);
			Assert.AreEqual(2, ((Peach.Core.Dom.XmlElement)elem).Count);
			Assert.AreEqual("<Foo bar=\"attribute value\"><![CDATA[Value of the CDATA]]></Foo>", outXml);
		}

		[Test]
		public void BlockXmlElement()
		{
			string xml = @"
<Peach>
	<DataModel name='DM1'>
		<Block name='Payload'>
			<XmlElement elementName='request'>
				<Block>
					<Transformer class='Base64Encode' />
					<Blob value='hi;' />
				</Block>
			</XmlElement>
		</Block>
	</DataModel>

	<DataModel name='DM2'>
		<XmlElement elementName='test'>
			<String value='ok:ok'>
				<Analyzer class='StringToken'/>
			</String>
		</XmlElement>
	</DataModel>

	<DataModel name='DM3'>
		<XmlElement elementName='test' occurs='2'>
			<String value='ok:ok'/>
		</XmlElement>
	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(3, dom.dataModels.Count);

			var act1 = dom.dataModels[0].Value.ToArray();
			var exp1 = Encoding.ASCII.GetBytes("<request>aGk7</request>");
			Assert.AreEqual(exp1, act1);

			var act2 = dom.dataModels[1].Value.ToArray();
			var exp2 = Encoding.ASCII.GetBytes("<test>ok:ok</test>");
			Assert.AreEqual(exp2, act2);

			var act3 = dom.dataModels[2].Value.ToArray();
			var exp3 = Encoding.ASCII.GetBytes("<test>ok:ok</test><test>ok:ok</test>");
			Assert.AreEqual(exp3, act3);
		}

		[Test]
		public void NumberXmlElement()
		{
			string xml = @"
<Peach>
	<DataModel name='DM1'>
		<Block name='Payload'>
			<XmlElement elementName='request'>
				<Number size='32' value='100'/>
			</XmlElement>
		</Block>
	</DataModel>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			Assert.AreEqual(1, dom.dataModels.Count);

			var act1 = dom.dataModels[0].Value.ToArray();
			var exp1 = Encoding.ASCII.GetBytes("<request>100</request>");
			Assert.AreEqual(exp1, act1);
		}

		[Test]
		public void SimpleXPath()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number name=\"TheNumber\" size=\"8\"/>" +
				"	</DataModel>" +
				"</Peach>";
		
			XPathDocument doc = new XPathDocument(new MemoryStream(Encoding.ASCII.GetBytes(xml)));
			XPathNavigator nav = doc.CreateNavigator();
			XPathNodeIterator it = nav.Select("//Number");
			
			List<string> res = new List<string>();
			while (it.MoveNext())
			{
				var val = it.Current.GetAttribute("name", "");
				res.Add(val);
			}
			
			Assert.AreEqual(1, res.Count);
			Assert.AreEqual("TheNumber", res[0]);
		}

		[Test]
		public void PeachXPath()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number name=\"TheNumber\" size=\"8\"/>" +
				"	</DataModel>" +

				"   <StateModel name=\"TheStateModel\" initialState=\"InitialState\">" +
				"       <State name=\"InitialState\">" +
				"           <Action name=\"Action1\" type=\"output\">" +
				"               <DataModel ref=\"TheDataModel\"/>" +
				"           </Action>" +
				"       </State>" +
				"   </StateModel>" +

				"   <Test name=\"Default\">" +
				"       <StateModel ref=\"TheStateModel\"/>" +
				"       <Publisher class=\"Null\"/>" +
				"   </Test>" +
				"</Peach>";
		
			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			PeachXPathNavigator nav = new PeachXPathNavigator(dom.tests[0].stateModel);
			XPathNodeIterator it = nav.Select("//TheNumber");
			
			List<string> res = new List<string>();
			
			while (it.MoveNext())
			{
				var val = it.Current.Name;
				res.Add(val);
			}
			
			Assert.AreEqual(1, res.Count);
			Assert.AreEqual("TheNumber", res[0]);
		}

		[Test]
		public void PeachXPath2()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Peach>" +
				"   <DataModel name=\"TheDataModel1\">" +
				"       <String name=\"String1\" value=\"1234567890\"/>" +
				"   </DataModel>" +
				"   <DataModel name=\"TheDataModel2\">" +
				"       <String name=\"String2\" value=\"Hello World!\"/>" +
				"   </DataModel>" +

				"   <StateModel name=\"TheStateModel\" initialState=\"InitialState\">" +
				"       <State name=\"InitialState\">" +
				"           <Action name=\"Action1\" type=\"output\">" +
				"               <DataModel ref=\"TheDataModel1\"/>" +
				"           </Action>" +
				
				"           <Action name=\"Action2\" type=\"slurp\" valueXpath=\"//String1\" setXpath=\"//String2\" />"+

				"           <Action name=\"Action3\" type=\"output\">" +
				"               <DataModel ref=\"TheDataModel2\"/>" +
				"           </Action>" +

				"           <Action name=\"Action4\" type=\"output\">" +
				"               <DataModel ref=\"TheDataModel2\"/>" +
				"           </Action>" +
				"       </State>" +
				"   </StateModel>" +

				"   <Test name=\"Default\">" +
				"       <StateModel ref=\"TheStateModel\"/>" +
				"       <Publisher class=\"Null\"/>" +
				"   </Test>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			PeachXPathNavigator nav = new PeachXPathNavigator(dom.tests[0].stateModel);
			XPathNodeIterator it = nav.Select("//String1");
			
			// Should find one element
			bool ret1 = it.MoveNext();
			Assert.True(ret1);
			
			// The result should be a DataElement
			DataElement valueElement = ((PeachXPathNavigator)it.Current).CurrentNode as DataElement;
			Assert.NotNull(valueElement);
			
			// There sould on;ly be one result
			bool ret2 = it.MoveNext();
			Assert.False(ret2);
		}
		
		[Test]
		public void XmlInChoice()
		{
			const string xml = @"
<Peach>
 <DataModel name='example1'>
    <Choice>
      <Block>
        <XmlElement elementName='methodCall'>
         <XmlElement elementName='methodName'><String value='Get'/></XmlElement> 
        </XmlElement>
      </Block>
      <Block>
        <XmlElement elementName='methodCall'>
         <XmlElement elementName='methodName2'><String value='Get2'/></XmlElement> 
        </XmlElement>
      </Block>
    </Choice>
  </DataModel>

  <DataModel name='example2'>
    <XmlElement elementName='methodCall'>
      <Choice>
         <XmlElement elementName='methodName'><String value='Get'/></XmlElement> 
         <XmlElement elementName='methodName2'><String value='Get2'/></XmlElement> 
      </Choice>
    </XmlElement>
  </DataModel>

  <DataModel name='example3'>
    <XmlElement elementName='methodCall'>
      <Choice>
         <Choice>
            <XmlElement elementName='methodName'><String value='Get'/></XmlElement> 
            <XmlElement elementName='methodName2'><String value='Get2'/></XmlElement> 
         </Choice>
      </Choice>
    </XmlElement>
  </DataModel>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);

			var str1 = dom.dataModels[0].InternalValue.BitsToString();
			var str2 = dom.dataModels[1].InternalValue.BitsToString();
			var str3 = dom.dataModels[1].InternalValue.BitsToString();

			Assert.AreEqual(str1, str2);
			Assert.AreEqual(str1, str3);
		}
	}
}
