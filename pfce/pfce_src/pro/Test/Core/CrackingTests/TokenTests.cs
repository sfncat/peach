

using System;
using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Core.Publishers;
using Peach.Core.Test;
using Logger = NLog.Logger;

namespace Peach.Pro.Test.Core.CrackingTests
{
	[TestFixture]
	[Quick]
	[Peach]
	public class TokenTests
	{

		[Test]
		public void CrackUrl()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String value=\"?\" token=\"true\" />"+

				"		<Block>" +
				"		  <String name=\"key1\" />" +
				"		  <String value=\"=\" token=\"true\" />" +
				"		  <String name=\"value1\" />" +
				"		</Block>" +
				"		<String value=\"&amp;\" token=\"true\" />" +
				"		<Block>" +
				"		  <String name=\"key2\" />" +
				"		  <String value=\"=\" token=\"true\" />" +
				"		  <String name=\"value2\" />" +
				"		</Block>" +
				"		<String value=\"&amp;\" token=\"true\" />" +
				"		<Block name=\"LastKV\">" +
				"		  <String name=\"key3\" />" +
				"		  <String value=\"=\" token=\"true\" />" +
				"		  <String name=\"value3\" />" +
				"		</Block>" +
				"	</DataModel>" +
				"</Peach>";

			// Positive test

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "?k1=v1&k2=v2&k3=v3");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("k3", ((string)((DataElementContainer)dom.dataModels[0]["LastKV"])[0].DefaultValue));
			Assert.AreEqual("v3", ((string)((DataElementContainer)dom.dataModels[0]["LastKV"])[2].DefaultValue));
		}

		[Test]
		public void CrackTokenNumber()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Number size=\"16\" value=\"300\" token=\"true\" />" +
				"		<String value=\"Foo Bar\" />" +
				"	</DataModel>" +
				"</Peach>";
			{
				// Positive test

				PitParser parser = new PitParser();
				Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

				var data = Bits.Fmt("{0:L16}{1:ascii}", 300, "Hello World");

				DataCracker cracker = new DataCracker();
				cracker.CrackData(dom.dataModels[0], data);

				Assert.AreEqual(300, (int)dom.dataModels[0][0].DefaultValue);
				Assert.AreEqual("Hello World", (string)dom.dataModels[0][1].DefaultValue);
			}
			{
				// Negative test

				PitParser parser = new PitParser();
				Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

				var data = Bits.Fmt("{0:L16}{1:ascii}", 200, "Hello World");

				DataCracker cracker = new DataCracker();
				TestDelegate myTestDelegate = () => cracker.CrackData(dom.dataModels[0], data);
				Assert.Throws<CrackingFailure>(myTestDelegate);

				Assert.AreEqual(300, (int)dom.dataModels[0][0].DefaultValue);
				Assert.AreEqual("Foo Bar", (string)dom.dataModels[0][1].DefaultValue);
			}
		}

		// TODO - Create unicode token string tests!!

		[Test]
		public void CrackTokenString()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String name=\"String1\" value=\"300\" token=\"true\" />" +
				"		<String name=\"String2\" value=\"Foo Bar\" />" +
				"	</DataModel>" +
				"</Peach>";
			{
				// Positive test

				PitParser parser = new PitParser();
				Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

				var data = Bits.Fmt("{0}", "300Hello World");

				DataCracker cracker = new DataCracker();
				cracker.CrackData(dom.dataModels[0], data);

				Assert.AreEqual("300", (string)dom.dataModels[0][0].DefaultValue);
				Assert.AreEqual("Hello World", (string)dom.dataModels[0][1].DefaultValue);
			}
			{
				// Negative test

				PitParser parser = new PitParser();
				Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

				var data = Bits.Fmt("{0}", "200Hello World");

				DataCracker cracker = new DataCracker();
				TestDelegate myTestDelegate = () => cracker.CrackData(dom.dataModels[0], data);
				Assert.Throws<CrackingFailure>(myTestDelegate);

				Assert.AreEqual("300", (string)dom.dataModels[0][0].DefaultValue);
				Assert.AreEqual("Foo Bar", (string)dom.dataModels[0][1].DefaultValue);
			}
		}

		[Test]
		public void CrackTokenBlob()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Blob name=\"Blob1\" value=\"300\" token=\"true\" />" +
				"		<String name=\"String1\" value=\"Foo Bar\" />" +
				"	</DataModel>" +
				"</Peach>";
			{
				// Positive test

				PitParser parser = new PitParser();
				Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

				var data = Bits.Fmt("{0}", "300Hello World");

				DataCracker cracker = new DataCracker();
				cracker.CrackData(dom.dataModels[0], data);

				Assert.AreEqual("300", dom.dataModels[0][0].DefaultValue.BitsToString());
				Assert.AreEqual("Hello World", (string)dom.dataModels[0][1].DefaultValue);
			}
			{
				// Negative test

				PitParser parser = new PitParser();
				Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

				var data = Bits.Fmt("{0}", "200Hello World");

				DataCracker cracker = new DataCracker();
				TestDelegate myTestDelegate = () => cracker.CrackData(dom.dataModels[0], data);
				Assert.Throws<CrackingFailure>(myTestDelegate);

				Assert.AreEqual("300", dom.dataModels[0][0].DefaultValue.BitsToString());
				Assert.AreEqual("Foo Bar", (string)dom.dataModels[0][1].DefaultValue);
			}
		}

		[Test]
		public void CrackTokenFlag()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<Flags name=\"Flags1\" size=\"8\">"+
				"			<Flag name=\"Flag1\" size=\"1\" position=\"0\" value=\"1\" token=\"true\"/> "+
				"		</Flags>"+
				"		<String value=\"Foo Bar\" />" +
				"	</DataModel>" +
				"</Peach>";
			{
				// Positive test

				PitParser parser = new PitParser();
				Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

				var data = Bits.Fmt("{0}{1}", (byte)255, "Hello World");

				DataCracker cracker = new DataCracker();
				cracker.CrackData(dom.dataModels[0], data);

				Assert.AreEqual(1, (int)((Flags)dom.dataModels[0][0])[0].DefaultValue);
				Assert.AreEqual("Hello World", (string)dom.dataModels[0][1].DefaultValue);
			}
			{
				// Negative test

				PitParser parser = new PitParser();
				Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

				var data = Bits.Fmt("{0:L16}{1}", 0, "Hello World");

				DataCracker cracker = new DataCracker();
				TestDelegate myTestDelegate = () => cracker.CrackData(dom.dataModels[0], data);
				Assert.Throws<CrackingFailure>(myTestDelegate);

				Assert.AreEqual(1, (int)((Flags)dom.dataModels[0][0])[0].DefaultValue);
				Assert.AreEqual("Foo Bar", (string)dom.dataModels[0][1].DefaultValue);
			}
		}

		[Test]
		public void CrackCompilcatedToken()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"m\">" +
				"		<Number name=\"n0\" size=\"16\"/>" +
				"		<Block name=\"b0\">" +
				"			<String name=\"s1\"/>" +
				"			<Number name=\"n1\" size=\"16\"/>" +
				"			<Block name=\"b1\">" +
				"				<Number name=\"n2\" size=\"16\"/>" +
				"				<Block name=\"b2\">" +
				"					<Block name=\"b3\">" +
				"						<Number name=\"n3\" size=\"16\"/>" +
				"					</Block>" +
				"					<Number name=\"n4\" size=\"16\"/>" +
				"				</Block>" +
				"			</Block>" +
					"		<Number name=\"n5\" size=\"16\"/>" +
				"		</Block>" +
				"		<Block name=\"b4\"/>" +
				"		<Number name=\"n6\" size=\"16\"/>" +
				"		<String name=\"s2\" valueType=\"hex\" value=\"0x0d 0x0a\" token=\"true\"/>" +
				"	</DataModel>" +
				"</Peach>";
				// Positive test

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0:L16}{1:ascii}{2:L16}{3:L16}{4:L16}{5:L16}{6:L16}{7:L16}{8:B8}{9:L8}",
				1, "Hello World", 2, 3, 4, 5, 6, 7, 0x0d, 0x0a);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(1, (int)dom.dataModels[0].find("m.n0").DefaultValue);
			Assert.AreEqual(2, (int)dom.dataModels[0].find("m.b0.n1").DefaultValue);
			Assert.AreEqual(3, (int)dom.dataModels[0].find("m.b0.b1.n2").DefaultValue);
			Assert.AreEqual(4, (int)dom.dataModels[0].find("m.b0.b1.b2.b3.n3").DefaultValue);
			Assert.AreEqual(5, (int)dom.dataModels[0].find("m.b0.b1.b2.n4").DefaultValue);
			Assert.AreEqual(6, (int)dom.dataModels[0].find("m.b0.n5").DefaultValue);
			Assert.AreEqual(7, (int)dom.dataModels[0].find("m.n6").DefaultValue);

			Assert.AreEqual("\r\n", (string)dom.dataModels[0].find("m.s2").DefaultValue);
			Assert.AreEqual("Hello World", (string)dom.dataModels[0].find("m.b0.s1").DefaultValue);
		}

		[Test]
		public void CrackTokenEmptyString()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String name=\"Element0\"/>" +
				"		<String value=\"QQ\" token=\"true\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "Hello WorldQQ");

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual("Hello World", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("QQ", (string)dom.dataModels[0][1].DefaultValue);
		}

		[Test]
		public void CrackMissingToken()
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String name=\"Element0\"/>" +
				"		<String value=\"QQ\" token=\"true\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", "Hello World");

			DataCracker cracker = new DataCracker();
			Assert.Throws<CrackingFailure>(() => cracker.CrackData(dom.dataModels[0], data));
		}

		public Peach.Core.Dom.Dom TokenAfterArrayToken(string value)
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String name=\"A\"/>" +
				"		<String name=\"T1\" value=\" \" token=\"true\"/>" +
				"		<String name=\"B\"/>" +
				"		<String name=\"T2\" value=\"-\" minOccurs=\"0\" token=\"true\"/>" +
				"		<String name=\"T3\" value=\" \" token=\"true\"/>" +
				"		<String name=\"C\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", value);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			return dom;
		}

		[Test]
		public void ZeroTokenAfterArrayToken()
		{
			var dom = TokenAfterArrayToken("aaa bbb ccc");

			Assert.AreEqual(6, dom.dataModels[0].Count);
			Assert.AreEqual("aaa", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual(" ", (string)dom.dataModels[0][1].DefaultValue);
			Assert.AreEqual("bbb", (string)dom.dataModels[0][2].DefaultValue);
			Assert.AreEqual(0, ((Peach.Core.Dom.Array)dom.dataModels[0][3]).Count);
			Assert.AreEqual(" ", (string)dom.dataModels[0][4].DefaultValue);
			Assert.AreEqual("ccc", (string)dom.dataModels[0][5].DefaultValue);
		}

		[Test]
		public void OneTokenAfterArrayToken()
		{
			var dom = TokenAfterArrayToken("aaa bbb- ccc");

			Assert.AreEqual(6, dom.dataModels[0].Count);
			Assert.AreEqual("aaa", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual(" ", (string)dom.dataModels[0][1].DefaultValue);
			Assert.AreEqual("bbb", (string)dom.dataModels[0][2].DefaultValue);
			Assert.AreEqual(1, ((Peach.Core.Dom.Array)dom.dataModels[0][3]).Count);
			Assert.AreEqual(" ", (string)dom.dataModels[0][4].DefaultValue);
			Assert.AreEqual("ccc", (string)dom.dataModels[0][5].DefaultValue);
		}

		[Test]
		public void ManyTokenAfterArrayToken()
		{
			var dom = TokenAfterArrayToken("aaa bbb-------------- ccc");

			Assert.AreEqual(6, dom.dataModels[0].Count);
			Assert.AreEqual("aaa", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual(" ", (string)dom.dataModels[0][1].DefaultValue);
			Assert.AreEqual("bbb", (string)dom.dataModels[0][2].DefaultValue);
			Assert.AreEqual(14, ((Peach.Core.Dom.Array)dom.dataModels[0][3]).Count);
			Assert.AreEqual(" ", (string)dom.dataModels[0][4].DefaultValue);
			Assert.AreEqual("ccc", (string)dom.dataModels[0][5].DefaultValue);
		}

		public Peach.Core.Dom.Dom DoCrackArrayToken(string value)
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String name=\"str1\"/>" +
				"		<Block minOccurs=\"0\">" +
				"			<String value=\"+\" token=\"true\"/>" +
				"			<String name=\"str2\"/>" +
				"		</Block>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", value);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			return dom;
		}

		[Test]
		public void CrackArrayZeroToken()
		{
			var dom = DoCrackArrayToken("Hello");
			Assert.AreEqual(1, dom.dataModels.Count);
			Assert.AreEqual(2, dom.dataModels[0].Count);
			var str = dom.dataModels[0][0] as Peach.Core.Dom.String;
			Assert.NotNull(str);
			Assert.AreEqual("Hello", (string)str.DefaultValue);
			var array = dom.dataModels[0][1] as Peach.Core.Dom.Array;
			Assert.NotNull(array);
			Assert.AreEqual(0, array.Count);
		}

		[Test]
		public void CrackArrayOneToken()
		{
			var dom = DoCrackArrayToken("Hello+World");
			Assert.AreEqual(1, dom.dataModels.Count);
			Assert.AreEqual(2, dom.dataModels[0].Count);
			var str = dom.dataModels[0][0] as Peach.Core.Dom.String;
			Assert.NotNull(str);
			Assert.AreEqual("Hello", (string)str.DefaultValue);
			var array = dom.dataModels[0][1] as Peach.Core.Dom.Array;
			Assert.NotNull(array);
			Assert.AreEqual(1, array.Count);
			Assert.AreEqual("+", (string)((Peach.Core.Dom.Block)array[0])[0].DefaultValue);
			Assert.AreEqual("World", (string)((Peach.Core.Dom.Block)array[0])[1].DefaultValue);
		}

		[Test]
		public void CrackArrayManyToken()
		{
			var dom = DoCrackArrayToken("Hello+World+More+Data");
			Assert.AreEqual(1, dom.dataModels.Count);
			Assert.AreEqual(2, dom.dataModels[0].Count);
			var str = dom.dataModels[0][0] as Peach.Core.Dom.String;
			Assert.NotNull(str);
			Assert.AreEqual("Hello", (string)str.DefaultValue);
			var array = dom.dataModels[0][1] as Peach.Core.Dom.Array;
			Assert.NotNull(array);
			Assert.AreEqual(3, array.Count);
			Assert.AreEqual("+", (string)((Peach.Core.Dom.Block)array[0])[0].DefaultValue);
			Assert.AreEqual("World", (string)((Peach.Core.Dom.Block)array[0])[1].DefaultValue);
			Assert.AreEqual("+", (string)((Peach.Core.Dom.Block)array[1])[0].DefaultValue);
			Assert.AreEqual("More", (string)((Peach.Core.Dom.Block)array[1])[1].DefaultValue);
			Assert.AreEqual("+", (string)((Peach.Core.Dom.Block)array[2])[0].DefaultValue);
			Assert.AreEqual("Data", (string)((Peach.Core.Dom.Block)array[2])[1].DefaultValue);
		}

		public Peach.Core.Dom.Dom DoCrackTokenBeforeArray(string value)
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String name=\"str1\"/>" +
				"		<String value=\"+\" token=\"true\"/>" +
				"		<Block minOccurs=\"0\">" +
				"			<String name=\"str2\"/>" +
				"			<String value=\"+\" token=\"true\"/>" +
				"		</Block>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", value);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			return dom;
		}

		[Test]
		public void CrackTokenBeforeArrayZero()
		{
			var dom = DoCrackTokenBeforeArray("Hello+");
			Assert.AreEqual(1, dom.dataModels.Count);
			Assert.AreEqual(3, dom.dataModels[0].Count);
			Assert.AreEqual("Hello", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("+", (string)dom.dataModels[0][1].DefaultValue);
			var array = dom.dataModels[0][2] as Peach.Core.Dom.Array;
			Assert.NotNull(array);
			Assert.AreEqual(0, array.Count);
		}

		[Test]
		public void CrackTokenBeforeArrayOne()
		{
			var dom = DoCrackTokenBeforeArray("Hello+World+");
			Assert.AreEqual(1, dom.dataModels.Count);
			Assert.AreEqual(3, dom.dataModels[0].Count);
			Assert.AreEqual("Hello", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("+", (string)dom.dataModels[0][1].DefaultValue);
			var array = dom.dataModels[0][2] as Peach.Core.Dom.Array;
			Assert.NotNull(array);
			Assert.AreEqual(1, array.Count);
			Assert.AreEqual("World", (string)((Peach.Core.Dom.Block)array[0])[0].DefaultValue);
			Assert.AreEqual("+", (string)((Peach.Core.Dom.Block)array[0])[1].DefaultValue);
		}

		[Test]
		public void CrackTokenBeforeArrayMany()
		{
			var dom = DoCrackTokenBeforeArray("Hello+World+More+Data+");
			Assert.AreEqual(1, dom.dataModels.Count);
			Assert.AreEqual(3, dom.dataModels[0].Count);
			Assert.AreEqual("Hello", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("+", (string)dom.dataModels[0][1].DefaultValue);
			var array = dom.dataModels[0][2] as Peach.Core.Dom.Array;
			Assert.NotNull(array);
			Assert.AreEqual(3, array.Count);
			Assert.AreEqual("World", (string)((Peach.Core.Dom.Block)array[0])[0].DefaultValue);
			Assert.AreEqual("+", (string)((Peach.Core.Dom.Block)array[0])[1].DefaultValue);
			Assert.AreEqual("More", (string)((Peach.Core.Dom.Block)array[1])[0].DefaultValue);
			Assert.AreEqual("+", (string)((Peach.Core.Dom.Block)array[1])[1].DefaultValue);
			Assert.AreEqual("Data", (string)((Peach.Core.Dom.Block)array[2])[0].DefaultValue);
			Assert.AreEqual("+", (string)((Peach.Core.Dom.Block)array[2])[1].DefaultValue);
		}

		public Peach.Core.Dom.Dom DoCrackTokenArray(string value)
		{
			string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Peach>\n" +
				"	<DataModel name=\"TheDataModel\">" +
				"		<String name=\"str1\"/>" +
				"		<Block minOccurs=\"0\">" +
				"			<String value=\"+\" token=\"true\"/>" +
				"		</Block>" +
				"		<String name=\"str2\"/>" +
				"	</DataModel>" +
				"</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", value);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			return dom;
		}

		[Test]
		public void CrackTokenArrayZero()
		{
			var ex = Assert.Throws<CrackingFailure>(() => DoCrackTokenArray("HelloWorld"));
			Assert.AreEqual("String 'TheDataModel.str1' failed to crack. Element is unsized.", ex.Message);
		}

		[Test]
		public void CrackTokenArrayOne()
		{
			var dom = DoCrackTokenArray("Hello+World");
			Assert.AreEqual(1, dom.dataModels.Count);
			Assert.AreEqual(3, dom.dataModels[0].Count);
			Assert.AreEqual("Hello", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("World", (string)dom.dataModels[0][2].DefaultValue);
			var array = dom.dataModels[0][1] as Peach.Core.Dom.Array;
			Assert.NotNull(array);
			Assert.AreEqual(1, array.Count);
			Assert.AreEqual("+", (string)((Peach.Core.Dom.Block)array[0])[0].DefaultValue);
		}

		[Test]
		public void CrackTokenArrayMany()
		{
			var dom = DoCrackTokenArray("Hello+++World");
			Assert.AreEqual(1, dom.dataModels.Count);
			Assert.AreEqual(3, dom.dataModels[0].Count);
			Assert.AreEqual("Hello", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("World", (string)dom.dataModels[0][2].DefaultValue);
			var array = dom.dataModels[0][1] as Peach.Core.Dom.Array;
			Assert.NotNull(array);
			Assert.AreEqual(3, array.Count);
			Assert.AreEqual("+", (string)((Peach.Core.Dom.Block)array[0])[0].DefaultValue);
			Assert.AreEqual("+", (string)((Peach.Core.Dom.Block)array[1])[0].DefaultValue);
			Assert.AreEqual("+", (string)((Peach.Core.Dom.Block)array[2])[0].DefaultValue);
		}

        [Test]
        public void SizedByToken()
        {
            //Test that unknown sizes can be successfully bounded by linear tokens.

            string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
               "<Peach>" +
               "   <DataModel name=\"TheDataModel\">" +
                "      <String name=\"Token1\" value=\"!PRE!\" token=\"true\"/>" +
               "       <Block name=\"TheBlock\">" +
               "           <Transformer class=\"Base64Encode\"/>" +
               "           <String name=\"Data\" value=\"SUCCESS\" token=\"true\"/>" +
               "       </Block>" +
               "      <String name=\"Token2\" value=\"!POST!\" token=\"true\"/>" +
               "   </DataModel>" +
               "</Peach>";

            PitParser parser = new PitParser();
            Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

            var data = Bits.Fmt("{0}", "!PRE!U1VDQ0VTUw==!POST!");

            DataCracker cracker = new DataCracker();
            cracker.CrackData(dom.dataModels[0], data);
        }

        [Test]
        public void SizedByToken2()
        {
            //Test that unknown sizes can be successfully bounded by linear tokens.

            string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
               "<Peach>" +
               "   <DataModel name=\"TheDataModel\">" +
                "      <String name=\"Token1\" value=\"!PRE!\" token=\"true\"/>" +
               "       <Block name=\"TheBlock\">" +
               "           <Transformer class=\"Base64Encode\"/>" +
               "           <String name=\"Data\" />" +
               "       </Block>" +
               "      <String name=\"Token2\" value=\"!POST!\" token=\"true\"/>" +
               "   </DataModel>" +
               "</Peach>";

            PitParser parser = new PitParser();
            Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

            var data = Bits.Fmt("{0}", "!PRE!U1VDQ0VTUw==!POST!");

            DataCracker cracker = new DataCracker();
            cracker.CrackData(dom.dataModels[0], data);

            Assert.AreEqual("SUCCESS", (string)dom.dataModels[0].find("Data").DefaultValue);
        }




		public Peach.Core.Dom.Dom SharedTokensAfterChoiceWithSizedElements(string value)
		{
			string xml = @"<?xml version='1.0' encoding='utf-8'?>
	<Peach>
		<DataModel name='r1'>
			<Blob name='FirstToken' value='31' valueType='hex' token='true' />
			<Blob name='SecondToken' />
		</DataModel>

		<DataModel name='r2' ref='r1'>
			<Blob name='SecondToken' value='32' valueType='hex' token='true'/>
			<String length='3'/>
		</DataModel>

		<DataModel name='r3' ref='r1'>
			<Blob name='SecondToken' value='33' valueType='hex' token='true'/>
			<String/>
		</DataModel>
	
		<DataModel name='r4'>
			<Choice name='c1' minOccurs='0'>
				<Block name='b2' ref='r2' />
			</Choice>
			<Block name='b3' ref='r3' />
		</DataModel>

</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", value);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[3], data);

			return dom;
		}

		[Test]
		public void OneSharedTokensAfterChoiceWithSizedElements()
		{
			var dom = SharedTokensAfterChoiceWithSizedElements("12foo12bar13baz");

			Assert.AreEqual(2, dom.dataModels[3].Count);
			var minoccursArray = (Peach.Core.Dom.Array)dom.dataModels[3][0];
			Assert.AreEqual(2, minoccursArray.Count);

			var choiceref1 = (Peach.Core.Dom.Choice)minoccursArray[0];
			Assert.AreEqual(1, choiceref1.Count);
			var blockref1 = (Peach.Core.Dom.Block)choiceref1[0];
			Assert.AreEqual(3, blockref1.Count);
			Assert.AreEqual(new byte[] { 0x31 }, ((Blob)blockref1[0]).DefaultValue.BitsToArray());
			Assert.AreEqual(new byte[] { 0x32 }, ((Blob)blockref1[1]).DefaultValue.BitsToArray());
			Assert.AreEqual("foo", (string)blockref1[2].DefaultValue);

			var choiceref2 = (Peach.Core.Dom.Choice)minoccursArray[1];
			Assert.AreEqual(1, choiceref2.Count);
			var blockref2 = (Peach.Core.Dom.Block)choiceref2[0];
			Assert.AreEqual(3, blockref2.Count);
			Assert.AreEqual(new byte[] { 0x31 }, ((Blob)blockref2[0]).DefaultValue.BitsToArray());
			Assert.AreEqual(new byte[] { 0x32 }, ((Blob)blockref2[1]).DefaultValue.BitsToArray());
			Assert.AreEqual("bar", (string)blockref2[2].DefaultValue);

			var lastBlock = (Peach.Core.Dom.Block)dom.dataModels[3][1];
			Assert.AreEqual(3, lastBlock.Count);
			Assert.AreEqual(new byte[] { 0x31 }, ((Blob)lastBlock[0]).DefaultValue.BitsToArray());
			Assert.AreEqual(new byte[] { 0x33 }, ((Blob)lastBlock[1]).DefaultValue.BitsToArray());
			Assert.AreEqual("baz", (string)lastBlock[2].DefaultValue);
		}

		public Peach.Core.Dom.Dom SharedTokensAfterArrayWithUnsizedElements(string value)
		{
			string xml = @"<?xml version='1.0' encoding='utf-8'?>
	<Peach>
		<DataModel name='r1'>
			<Blob name='FirstToken' value='31' valueType='hex' token='true' />
			<Blob name='SecondToken' />
			<String/>
		</DataModel>

		<DataModel name='r2' ref='r1'>
			<Blob name='SecondToken' value='32' valueType='hex' token='true'/>
		</DataModel>

		<DataModel name='r3' ref='r1'>
			<Blob name='SecondToken' value='33' valueType='hex' token='true'/>
		</DataModel>
	
		<DataModel name='r4'>
			<Block name='a2' minOccurs='0'>
				<Block ref='r2' />
			</Block>
			<Block name='b3' ref='r3' />
		</DataModel>

</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", value);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[3], data);

			return dom;
		}

		[Test]
		public void OneSharedTokensAfterArrayWithUnsizedElements()
		{
			var dom = SharedTokensAfterArrayWithUnsizedElements("12foo12bar13baz");

			Assert.AreEqual(2, dom.dataModels[3].Count);
			var minoccursArray = (Peach.Core.Dom.Array)dom.dataModels[3][0];
			Assert.AreEqual(2, minoccursArray.Count);

			var blockref1 = (Peach.Core.Dom.Block)((Peach.Core.Dom.Block)minoccursArray[0])[0];
			Assert.AreEqual(3, blockref1.Count);
			Assert.AreEqual(new byte[] { 0x31 }, ((Blob)blockref1[0]).DefaultValue.BitsToArray());
			Assert.AreEqual(new byte[] { 0x32 }, ((Blob)blockref1[1]).DefaultValue.BitsToArray());
			Assert.AreEqual("foo", (string)blockref1[2].DefaultValue);

			var blockref2 = (Peach.Core.Dom.Block)((Peach.Core.Dom.Block)minoccursArray[1])[0];
			Assert.AreEqual(3, blockref2.Count);
			Assert.AreEqual(new byte[] { 0x31 }, ((Blob)blockref2[0]).DefaultValue.BitsToArray());
			Assert.AreEqual(new byte[] { 0x32 }, ((Blob)blockref2[1]).DefaultValue.BitsToArray());
			Assert.AreEqual("bar", (string)blockref2[2].DefaultValue);

			var lastBlock = (Peach.Core.Dom.Block)dom.dataModels[3][1];
			Assert.AreEqual(3, lastBlock.Count);
			Assert.AreEqual(new byte[] { 0x31 }, ((Blob)lastBlock[0]).DefaultValue.BitsToArray());
			Assert.AreEqual(new byte[] { 0x33 }, ((Blob)lastBlock[1]).DefaultValue.BitsToArray());
			Assert.AreEqual("baz", (string)lastBlock[2].DefaultValue);

		}

		public Peach.Core.Dom.Dom SharedTokensAfterChoiceWithUnsizedElements(string value)
		{
			string xml = @"<?xml version='1.0' encoding='utf-8'?>
	<Peach>
		<DataModel name='r1'>
			<Blob name='FirstToken' value='31' valueType='hex' token='true' />
			<Blob name='SecondToken' />
			<String />
		</DataModel>

		<DataModel name='r2' ref='r1'>
			<Blob name='SecondToken' value='32' valueType='hex' token='true'/>
		</DataModel>


		<DataModel name='r3' ref='r1'>
			<Blob name='SecondToken' value='33' valueType='hex' token='true'/>
		</DataModel>
	
		<DataModel name='r4'>
			<Choice minOccurs='0'>
				<Block ref='r2' />
			</Choice>
			<Block ref='r3' />
		</DataModel>

</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", value);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[3], data);

			return dom;
		}

		[Test]
		public void OneSharedTokensAfterChoiceWithUnsizedElements()
		{
			var dom = SharedTokensAfterChoiceWithUnsizedElements("12foo12bar13baz");

			Assert.AreEqual(2, dom.dataModels[3].Count);
			var minoccursArray = (Peach.Core.Dom.Array)dom.dataModels[3][0];
			Assert.AreEqual(2, minoccursArray.Count);

			var blockref1 = (Peach.Core.Dom.Block)((Peach.Core.Dom.Choice)minoccursArray[0])[0];
			Assert.AreEqual(3, blockref1.Count);
			Assert.AreEqual(new byte[] { 0x31 }, ((Blob)blockref1[0]).DefaultValue.BitsToArray());
			Assert.AreEqual(new byte[] { 0x32 }, ((Blob)blockref1[1]).DefaultValue.BitsToArray());
			Assert.AreEqual("foo", (string)blockref1[2].DefaultValue);

			var blockref2 = (Peach.Core.Dom.Block)((Peach.Core.Dom.Choice)minoccursArray[1])[0];
			Assert.AreEqual(3, blockref2.Count);
			Assert.AreEqual(new byte[] { 0x31 }, ((Blob)blockref2[0]).DefaultValue.BitsToArray());
			Assert.AreEqual(new byte[] { 0x32 }, ((Blob)blockref2[1]).DefaultValue.BitsToArray());
			Assert.AreEqual("bar", (string)blockref2[2].DefaultValue);

			var lastBlock = (Peach.Core.Dom.Block)dom.dataModels[3][1];
			Assert.AreEqual(3, lastBlock.Count);
			Assert.AreEqual(new byte[] { 0x31 }, ((Blob)lastBlock[0]).DefaultValue.BitsToArray());
			Assert.AreEqual(new byte[] { 0x33 }, ((Blob)lastBlock[1]).DefaultValue.BitsToArray());
			Assert.AreEqual("baz", (string)lastBlock[2].DefaultValue);
		}


		public Peach.Core.Dom.Dom MinMaxZeroWithUnsized(string value)
		{
			string xml = @"<?xml version='1.0' encoding='utf-8'?>
	<Peach>
		<DataModel name='r1'>
			<Block name='b1' minOccurs='1' maxOccurs='1'>
				<Blob name='FirstToken' value='31' valueType='hex' token='true' />
				<String/>
			</Block>
			<String value='z' token='true' />
		</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", value);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			return dom;
		}

		[Test]
		public void OneMinMaxZeroWithUnsized()
		{
			var dom = MinMaxZeroWithUnsized("12foo12bar13baz");

			Assert.AreEqual(2, dom.dataModels[0].Count);
			var array = dom.dataModels[0][0] as Peach.Core.Dom.Array;
			Assert.NotNull(array);
			Assert.AreEqual(1, array.Count);
			var block = array[0] as Block;
			Assert.NotNull(block);
			Assert.AreEqual(2, block.Count);
			Assert.AreEqual("2foo12bar13ba", (string)block[1].DefaultValue);

		}

		public Peach.Core.Dom.Dom TokenAfterSizedArray(string value)
		{
			string xml = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='MealModel'>
		<String name='Appetizer' />
		<String value='/' token='true' />
		<String name='Entree' />
		<Block name='Dessert' minOccurs='0'>
			<String value='/' token='true' />
			<Choice name='DessertChoice'>
				<String name='Pie' value='Pie' token='true' />
				<String name='Cake' value='Cake' token='true' />
			</Choice>
		</Block>
		<String name='Terminator' value=';' token='true' />
	</DataModel>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", value);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			return dom;
		}

		[Test]
		public void ZeroTokenAfterSizedArray()
		{
			var dom = TokenAfterSizedArray("Soup/Steak;");

			Assert.AreEqual(5, dom.dataModels[0].Count);

			Assert.AreEqual("Soup", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("/", (string)dom.dataModels[0][1].DefaultValue);
			Assert.AreEqual("Steak", (string)dom.dataModels[0][2].DefaultValue);
			Assert.AreEqual(";", (string)dom.dataModels[0][4].DefaultValue);
			var array = dom.dataModels[0][3] as Peach.Core.Dom.Array;
			Assert.NotNull(array);
			Assert.AreEqual(0, array.Count);
		}

		[Test]
		public void OneTokenAfterSizedArray()
		{
			var dom = TokenAfterSizedArray("Soup/Steak/Pie;");

			Assert.AreEqual(5, dom.dataModels[0].Count);

			Assert.AreEqual("Soup", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("/", (string)dom.dataModels[0][1].DefaultValue);
			Assert.AreEqual("Steak", (string)dom.dataModels[0][2].DefaultValue);
			Assert.AreEqual(";", (string)dom.dataModels[0][4].DefaultValue);
			var array = dom.dataModels[0][3] as Peach.Core.Dom.Array;
			Assert.NotNull(array);
			Assert.AreEqual(1, array.Count);
			var block = array[0] as Block;
			Assert.NotNull(block);
			Assert.AreEqual(2, block.Count);
			Assert.AreEqual("/", (string)block[0].DefaultValue);
			var choice = block[1] as Choice;
			Assert.AreEqual("Pie", (string)choice.SelectedElement.DefaultValue);
		}


		[Test]
		public void TestHttp()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='Request'/>
		<String value='\r\n' token='true'/>
		<Block minOccurs='0'>
			<String name='Header'/>
			<String value=': ' token='true'/>
			<String name='Value'/>
			<String value='\r\n' token='true'/>
		</Block>
		<String value='\r\n' token='true'/>
		<String name='Body'/>
	</DataModel>
</Peach>
";

			string value = "HTTP/1.1 200 OK\r\nContent-Length: 2\r\nContent-Type: text/html\r\n\r\nabc";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", value);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			Assert.AreEqual(5, dom.dataModels[0].Count);
			Assert.AreEqual("HTTP/1.1 200 OK", (string)dom.dataModels[0][0].DefaultValue);
			Assert.AreEqual("abc", (string)dom.dataModels[0][4].DefaultValue);
			var array = dom.dataModels[0][2] as Peach.Core.Dom.Array;
			Assert.NotNull(array);
			Assert.AreEqual(2, array.Count);
			Assert.AreEqual("Content-Length", (string)((Block)array[0])[0].DefaultValue);
			Assert.AreEqual("2", (string)((Block)array[0])[2].DefaultValue);
			Assert.AreEqual("Content-Type", (string)((Block)array[1])[0].DefaultValue);
			Assert.AreEqual("text/html", (string)((Block)array[1])[2].DefaultValue);
		}

		Peach.Core.Dom.Dom TokenBeforeOptionalArray(string value)
		{
			string xml = @"
<Peach>
	<DataModel name='TV'>
		<String name='Type' length='1'/>
		<String value='=' token='true'/>
		<String name='Value'/>
		<String value='\r\n' token='true'/>
	</DataModel>

	<DataModel name='Model'>
		<Block ref='TV'>
			<String name='Type' value='s' token='true'/>
		</Block>
		<Block ref='TV' minOccurs='0'>
			<String name='Type' value='e' token='true'/>
		</Block>
	</DataModel>
</Peach>
";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var data = Bits.Fmt("{0}", value);

			DataCracker cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[1], data);

			return dom;
		}

		[Test]
		public void TestTokenBeforeOptionalArray()
		{
			var dom = TokenBeforeOptionalArray("s=Cookies\r\n");

			Assert.AreEqual(2, dom.dataModels.Count);
			Assert.AreEqual(2, dom.dataModels[1].Count);

			var blk1 = dom.dataModels[1][0] as Peach.Core.Dom.Block;
			Assert.NotNull(blk1);
			Assert.AreEqual(4, blk1.Count);
			Assert.AreEqual("s", (string)blk1[0].DefaultValue);
			Assert.AreEqual("=", (string)blk1[1].DefaultValue);
			Assert.AreEqual("Cookies", (string)blk1[2].DefaultValue);
			Assert.AreEqual("\r\n", (string)blk1[3].DefaultValue);

			var array = dom.dataModels[1][1] as Peach.Core.Dom.Array;
			Assert.NotNull(array);
			Assert.AreEqual(0, array.Count);
		}

		[Test]
		public void TestTokenBeforeOptionalArray1()
		{
			var dom = TokenBeforeOptionalArray("s=Cookies\r\ne=MoreCookies\r\n");

			Assert.AreEqual(2, dom.dataModels.Count);
			Assert.AreEqual(2, dom.dataModels[1].Count);

			var blk1 = dom.dataModels[1][0] as Peach.Core.Dom.Block;
			Assert.NotNull(blk1);
			Assert.AreEqual(4, blk1.Count);
			Assert.AreEqual("s", (string)blk1[0].DefaultValue);
			Assert.AreEqual("=", (string)blk1[1].DefaultValue);
			Assert.AreEqual("Cookies", (string)blk1[2].DefaultValue);
			Assert.AreEqual("\r\n", (string)blk1[3].DefaultValue);

			var array = dom.dataModels[1][1] as Peach.Core.Dom.Array;
			Assert.NotNull(array);
			Assert.AreEqual(1, array.Count);

			var blk2 = array[0] as Peach.Core.Dom.Block;
			Assert.NotNull(blk2);
			Assert.AreEqual(4, blk2.Count);
			Assert.AreEqual("e", (string)blk2[0].DefaultValue);
			Assert.AreEqual("=", (string)blk2[1].DefaultValue);
			Assert.AreEqual("MoreCookies", (string)blk2[2].DefaultValue);
			Assert.AreEqual("\r\n", (string)blk2[3].DefaultValue);

		}

		class DeferredPublisher : StreamPublisher
		{
			static readonly Logger ClassLogger = NLog.LogManager.GetCurrentClassLogger();

			readonly byte[] _sourceData;

			public DeferredPublisher(byte[] sourceData)
				: base(new System.Collections.Generic.Dictionary<string,Variant>())
			{
				stream = new MemoryStream();
				_sourceData = sourceData;
			}

			protected override Logger Logger
			{
				get { return ClassLogger; }
			}

			public override void WantBytes(long count)
			{
				var pos = stream.Position;
				var have = stream.Length - pos;
				var avail = _sourceData.Length - stream.Length;

				if (have >= count)
					return;

				var get = Math.Min(count - have, avail);

				stream.Position = stream.Length;
				stream.Write(_sourceData, (int)stream.Length, (int)get);
				stream.Position = pos;
			}
		}

		[Test]
		public void WantBytesForToken()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<Number size='32'/>
		<String />
		<Number size='32'/>
		<Number value='0' size='32' token='true'/>
	</DataModel>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);

			const string src = "1234HelloWorld5678\x00\x00\x00\x00";

			var data = new BitStream(new DeferredPublisher(Encoding.ASCII.GetBytes(src)));

			var cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			var elem = dom.dataModels[0][1] as Peach.Core.Dom.String;
			Assert.NotNull(elem);
			Assert.AreEqual("HelloWorld", (string)elem.DefaultValue);
		}

		[Test]
		public void WantBytesForTokenTooShort()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<Number size='32'/>
		<String />
		<Number size='32'/>
		<Number value='0' size='32' token='true'/>
	</DataModel>
</Peach>";

			var dom = DataModelCollector.ParsePit(xml);

			const string src = "1234He";

			var data = new BitStream(new DeferredPublisher(Encoding.ASCII.GetBytes(src)));

			var cracker = new DataCracker();

			Assert.Throws<CrackingFailure>(() =>  cracker.CrackData(dom.dataModels[0], data));
		}
	}
}

// end

