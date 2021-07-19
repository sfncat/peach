

using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.CrackingTests
{
	[TestFixture]
	[Quick]
	[Peach]
	class StringTests
	{
		[Test] public void CrackStringInvalidUnicodeFailure()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
			             "	<DataModel name=\"TheDataModel\">" +
			             "		<String/>" +
			             "		<String value='\r\n' token='true'/>" +
			             "		<String/>" +
			             "	</DataModel>" +
			             "</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = new BitStream(new byte[]{ 0x31, 0x35, 0x38, 0xDE, 0x0D, 0x0A, 0x43, 0x6F });

			var cracker = new DataCracker();
			Assert.Throws<CrackingFailure>(() => cracker.CrackData(dom.dataModels[0], data));
		}

		[Test]
		public void CrackSizedString()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
			             "	<DataModel name=\"TheDataModel\">" +
			             "		<String length=\"5\"/>" +
			             "	</DataModel>" +
			             "</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "Hello");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello", (string)dom.dataModels[0][0].DefaultValue);
		}

		[Test]
		public void CrackString1()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "Hello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello World", (string)dom.dataModels[0][0].DefaultValue);
		}

		[Test]
		public void CrackString2()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String length=\"5\" />" +
				"		<String />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "12345Hello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("12345", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("Hello World", (string)dom.dataModels[0][1].DefaultValue);
		}

		[Test]
		public void CrackString3()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String />" +
				"		<String length=\"5\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "Hello World12345");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello World", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("12345", (string)dom.dataModels[0][1].DefaultValue);
		}

		[Test]
		public void CrackString4()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String nullTerminated=\"true\" />" +
				"		<String />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "Hello World\0Foo Bar");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello World", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("Foo Bar", (string)dom.dataModels[0][1].DefaultValue);
		}

		[Test]
		public void CrackString5()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String />" +
				"		<Number size=\"16\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}{1:L16}", "Hello World", 3111);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello World", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual(3111, (int)dom.dataModels[0][1].DefaultValue);
		}

		[Test]
		public void CrackString6()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String name=\"TheString\" />" +
				"		<Block name=\"TheBlock\">" +
				"			<Number name=\"TheNumber\" size=\"16\" />" +
				"		</Block>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}{1:L16}", "Hello World", 3111);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello World", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual(3111, (int)((DataElementContainer)dom.dataModels[0][1])[0].DefaultValue);
		}

		[Test]
		public void CrackString7()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String />" +
				"		<Block>" +
				"			<Number size=\"16\" />" +
				"		</Block>" +
				"		<Block>" +
				"			<Number size=\"16\" />" +
				"		</Block>" +
				"		<Block>" +
				"			<Number size=\"16\" />" +
				"		</Block>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}{1:L16}{2:L16}{3:L16}", "Hello World", 3111, 3112, 3113);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello World", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual(3111, (int)((DataElementContainer)dom.dataModels[0][1])[0].DefaultValue);
			Assert.AreEqual(3112, (int)((DataElementContainer)dom.dataModels[0][2])[0].DefaultValue);
			Assert.AreEqual(3113, (int)((DataElementContainer)dom.dataModels[0][3])[0].DefaultValue);
		}


		[Test]
		public void CrackString8()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String nullTerminated=\"true\" length=\"8\" value=\"Foo\" />" +
				"		<String />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "Hello\0\0\0Foo Bar");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello\0\0\0", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("Foo Bar", (string)dom.dataModels[0][1].DefaultValue);
		}

		[Test]
		public void CrackAsciiLengthChars()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String type=\"utf8\" />" +
				"		<String type=\"ascii\" length=\"5\" lengthType=\"chars\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0:utf8}{1:ascii}", "Hello World", "12345");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello World", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("12345", (string)dom.dataModels[0][1].DefaultValue);
		}

		[Test]
		public void CrackAsciiVariableChars()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String type=\"utf8\" />" +
				"		<String type=\"utf32\" length=\"5\" lengthType=\"chars\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0:utf8}{1:ascii}", "Hello World", "12345");

			DataCracker cracker = new DataCracker();
			Assert.Throws<CrackingFailure>(delegate() {
				cracker.CrackData(dom.dataModels[0], data);
			});
		}

		[Test]
		public void CrackAsciiInvalidChars()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String type=\"ascii\" length=\"5\" lengthType=\"chars\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}{1}", "1234", (byte)0xff);

			DataCracker cracker = new DataCracker();
			Assert.Throws<CrackingFailure>(delegate()
			{
				cracker.CrackData(dom.dataModels[0], data);
			});
		}

		[Test]
		public void CrackAsciiLengthCharsBefore()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String type=\"ascii\" length=\"5\" lengthType=\"chars\" />" +
				"		<String type=\"utf8\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0:ascii}{1:utf8}", "12345", "Hello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("12345", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("Hello World", (string)dom.dataModels[0][1].DefaultValue);
		}

		[Test]
		public void CrackUtf32LengthCharsBefore()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String type=\"utf32\" length=\"5\" lengthType=\"chars\" />" +
				"		<String type=\"utf8\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0:utf32}{1:utf8}", "12345", "Hello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("12345", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("Hello World", (string)dom.dataModels[0][1].DefaultValue);
		}

		[Test]
		public void CrackUtf16LengthCharsBefore()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String type=\"utf16\" length=\"5\" lengthType=\"chars\" />" +
				"		<String type=\"utf8\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0:utf16}{1:utf8}", "12345", "Hello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("12345", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("Hello World", (string)dom.dataModels[0][1].DefaultValue);
		}

		[Test]
		public void CrackUtf8LengthCharsBefore()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String type=\"utf8\" length=\"1\" lengthType=\"chars\" />" +
				"		<String type=\"utf8\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0:utf8}{1:utf8}", "\u30ab", "Hello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("\u30ab", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("Hello World", (string)dom.dataModels[0][1].DefaultValue);
		}

		[Test]
		public void CrackNullTermToken()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String value=\"Hello\" token=\"true\" nullTerminated=\"true\" />" +
				"		<String value=\"World\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0:utf8}", "Hello\0World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("World", (string)dom.dataModels[0][1].DefaultValue);
		}

		[Test]
		public void CrackNullTermLengthToken()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String value=\"Hello\" length=\"6\" nullTerminated=\"true\" token=\"true\" />" +
				"		<String value=\"World\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0:utf8}", "Hello\0World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello\0", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("World", (string)dom.dataModels[0][1].DefaultValue);
		}

		[Test]
		public void CrackNullTermString()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String type=\"utf8\" nullTerminated=\"true\" />" +
				"		<String type=\"utf8\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0:utf8}", "\u00abX\u00abX\0Hello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("\u00abX\u00abX", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("Hello World", (string)dom.dataModels[0][1].DefaultValue);
		}

		[Test]
		public void CrackTokenNext()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String type=\"utf8\"/>" +
				"		<String type=\"utf8\" value=\"\u00abX\" token=\"true\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(Encoding.UTF8.GetBytes(xml)));

			var data = Bits.Fmt("{0:utf8}", "Hello World\u00abX");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello World", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("\u00abX", (string)dom.dataModels[0][1].DefaultValue);
		}

		[Test]
		public void CrackLastUnsized()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String type=\"utf8\"/>" +
				"		<Number size=\"8\"/>" +
				"		<String type=\"utf8\" value=\"\u00abX\" token=\"true\" />" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(Encoding.UTF8.GetBytes(xml)));

			var data = Bits.Fmt("{0:utf8}{1}{2:utf8}", "Hello World", (byte)255, "\u00abX");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello World", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual(255, (int)dom.dataModels[0][1].DefaultValue);
			Assert.AreEqual("\u00abX", (string)dom.dataModels[0][2].DefaultValue);
		}

		[Test]
		public void CrackNumericalOn()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(Encoding.UTF8.GetBytes(xml)));

			Assert.False(dom.dataModels[0][0].Hints.ContainsKey("NumericalString"));

			var data = Bits.Fmt("{0:utf8}", "100");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.True(dom.dataModels[0][0].Hints.ContainsKey("NumericalString"));
		}

		[Test]
		public void CrackNumericalOff()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String value=\"100\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(Encoding.UTF8.GetBytes(xml)));

			Assert.True(dom.dataModels[0][0].Hints.ContainsKey("NumericalString"));

			var data = Bits.Fmt("{0:utf8}", "Hello World");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.False(dom.dataModels[0][0].Hints.ContainsKey("NumericalString"));
		}

		[Test]
		public void FieldNumerical()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String name=\"str\"/>" +
				"	</DataModel>" +
				"	<StateModel name=\"TheStateModel\" initialState=\"InitialState\">" +
				"		<State name=\"InitialState\">" +
				"			<Action type=\"output\">" +
				"				<DataModel ref=\"TheDataModel\"/>" +
				"				<Data>" +
				"					<Field name=\"str\" value=\"100\"/>" +
				"				</Data>" +
				"			</Action>" +
				"		</State>" +
				"	</StateModel>" +
				"	<Test name=\"Default\">" +
				"		<StateModel ref=\"TheStateModel\"/>" +
				"		<Publisher class=\"Null\"/>" +
				"		<Strategy class=\"RandomDeterministic\"/>" +
				"	</Test>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(Encoding.UTF8.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(null);
			e.startFuzzing(dom, config);

			Assert.False(dom.dataModels[0][0].Hints.ContainsKey("NumericalString"));
			Assert.True(dom.tests[0].stateModel.states["InitialState"].actions[0].dataModel[0].Hints.ContainsKey("NumericalString"));
		}

	}
}

// end
