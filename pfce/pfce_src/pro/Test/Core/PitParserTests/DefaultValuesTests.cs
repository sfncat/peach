using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.PitParserTests
{
	[TestFixture]
	[Quick]
	[Peach]
	class DefaultValuesTests
	{
		public void TestEncoding(Encoding enc, bool defaultArgs)
		{
			int codepage = 0;
			string encoding = "";

			if (enc != null)
			{
				codepage = enc.CodePage;
				if (enc is UnicodeEncoding)
					encoding = Encoding.Unicode.HeaderName;
				else
					encoding = enc.HeaderName;
			}

			string val = !string.IsNullOrEmpty(encoding) ? "encoding=\"" + encoding + "\"" : "";
			string xml = "<?xml version=\"1.0\" " + val + "?>\r\n" +
				"<Peach>\r\n" +
				"	<DataModel name=\"Foo\">\r\n" +
				"		<String value=\"##VAR1##\"/>\r\n" +
				"		<String value=\"##VAR2##\"/>\r\n" +
				"	</DataModel>\r\n" +
				"</Peach>";

			Dictionary<string, object> parserArgs = new Dictionary<string, object>();

			if (defaultArgs)
			{
				var defaultValues = new Dictionary<string, string>();
				defaultValues["VAR1"] = "TheDataModel";
				defaultValues["VAR2"] = "SomeString";

				parserArgs[PitParser.DEFINED_VALUES] = defaultValues;
			}

			using (var pitFile = new TempFile())
			{
				using (FileStream f = File.OpenWrite(pitFile.Path))
				{
					using (StreamWriter sw = new StreamWriter(f, System.Text.Encoding.GetEncoding(codepage)))
					{
						sw.Write(xml);
					}
				}

				Peach.Core.Dom.Dom dom = Analyzer.defaultParser.asParser(parserArgs, pitFile.Path);
				dom.evaulateDataModelAnalyzers();

				Assert.AreEqual(1, dom.dataModels.Count);
				Assert.AreEqual(2, dom.dataModels[0].Count);

				if (defaultArgs)
				{
					Assert.AreEqual("TheDataModel", (string)dom.dataModels[0][0].DefaultValue);
					Assert.AreEqual("SomeString", (string)dom.dataModels[0][1].DefaultValue);
				}
				else
				{
					Assert.AreEqual("##VAR1##", (string)dom.dataModels[0][0].DefaultValue);
					Assert.AreEqual("##VAR2##", (string)dom.dataModels[0][1].DefaultValue);
				}
			}
		}

		[Test]
		public void TestDefault()
		{
			TestEncoding(null, true);
			TestEncoding(null, false);
		}

		[Test]
		public void TestUtf8()
		{
			TestEncoding(Encoding.UTF8, true);
			TestEncoding(Encoding.UTF8, false);
		}

		[Test]
		public void TestUtf16()
		{
			TestEncoding(Encoding.Unicode, true);
			TestEncoding(Encoding.Unicode, false);
		}

		[Test]
		public void TestUtf32()
		{
			TestEncoding(Encoding.UTF32, true);
			TestEncoding(Encoding.UTF32, false);
		}

		[Test]
		public void TestUtf16BE()
		{
			TestEncoding(Encoding.BigEndianUnicode, true);
			TestEncoding(Encoding.BigEndianUnicode, false);
		}

		[Test]
		public void DefinesBeforeValidate()
		{
			string xml = @"
<Peach>
	<Include ns='test' src='file:##FILE##'/>
</Peach>
";
			using (var tempFile = new TempFile())
			{
				File.WriteAllText(tempFile.Path, "<Peach><DataModel name='DM'/></Peach>");

				var defines = new Dictionary<string, string>();
				defines["FILE"] = tempFile.Path;

				var args = new Dictionary<string, object>();
				args[PitParser.DEFINED_VALUES] = defines;

				Assert.DoesNotThrow(() => DataModelCollector.ParsePit(xml, args));
			}
		}
	}
}
