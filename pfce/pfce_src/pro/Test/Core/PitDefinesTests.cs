using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using NUnit.Framework;
using Peach.Core;
using Peach.Pro.Core;
using Peach.Pro.Core.WebServices;
using Peach.Pro.Core.WebServices.Models;
using File = System.IO.File;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	[Quick]
	[Peach]
	public class PitDefinesTests
	{
		[Test]
		public void TestParse()
		{
			const string xml = @"<?xml version='1.0' encoding='utf-8'?>
<PitDefines>
	<All>
	</All>

	<None>
	</None>

	<Windows>
	</Windows>

	<Linux>
	</Linux>

	<OSX>
	</OSX>

	<Unix>
	</Unix>
</PitDefines>
";

			var defs1 = XmlTools.Deserialize<PitDefines>(new StringReader("<PitDefines/>"));
			Assert.AreEqual(0, defs1.Platforms.Count);

			var defs2 = XmlTools.Deserialize<PitDefines>(new StringReader("<PitDefines><All/></PitDefines>"));
			Assert.AreEqual(1, defs2.Platforms.Count);
			Assert.AreEqual(Platform.OS.All, defs2.Platforms[0].Platform);
			Assert.AreEqual(0, defs2.Platforms[0].Defines.Count);

			var defs = XmlTools.Deserialize<PitDefines>(new StringReader(xml));
			Assert.NotNull(defs);
			Assert.AreEqual(6, defs.Platforms.Count);
		}

		[Test]
		public void TestTypes()
		{
			using (var f = new TempFile())
			{
				File.WriteAllText(f.Path, @"<?xml version='1.0' encoding='utf-8'?>
<PitDefines>
	<All>
		<String   key='key0' value='value0' name='name0' description='description0'/>
		<Ipv4     key='key1' value='value1' name='name1' description='description1'/>
		<Ipv6     key='key2' value='value2' name='name2' description='description2'/>
		<Iface    key='key3' value='value3' name='name3' description='description3'/>
		<Strategy key='key4' value='value4' name='name4' description='description4'/>
		<Hwaddr   key='key5' value='value5' name='name5' description='description5'/>
		<Range    key='key6' value='value6' name='name6' description='description6' min='0' max='100'/>
		<Enum     key='key7' value='value7' name='name7' description='description7' enumType='System.IO.FileAccess'/>
		<Define   key='key8' value='value8' name='name8' description='description8'/>
	</All>
</PitDefines>
");

				var defines = PitDefines.ParseFile(f.Path);
				Assert.NotNull(defines);
				Assert.AreEqual(1, defines.Platforms.Count);
				Assert.AreEqual(Platform.OS.All, defines.Platforms[0].Platform);
				var defs = defines.Platforms[0].Defines;
				Assert.AreEqual(defs[0].ConfigType, ParameterType.String);
				Assert.AreEqual(defs[1].ConfigType, ParameterType.Ipv4);
				Assert.AreEqual(defs[2].ConfigType, ParameterType.Ipv6);
				Assert.AreEqual(defs[3].ConfigType, ParameterType.Iface);
				Assert.AreEqual(defs[4].ConfigType, ParameterType.Enum);
				Assert.AreEqual(defs[5].ConfigType, ParameterType.Hwaddr);
				Assert.AreEqual(defs[6].ConfigType, ParameterType.Range);
				Assert.AreEqual(defs[7].ConfigType, ParameterType.Enum);
				Assert.AreEqual(defs[8].ConfigType, ParameterType.User);
			}
		}

		[Test]
		public void Schema()
		{
			var schema = XmlTools.GetSchema(typeof(PitDefines));
			var sb = new StringBuilder();
			var wr = XmlWriter.Create(sb, new XmlWriterSettings() { Indent = true });
			schema.Write(wr);
			var asStr = sb.ToString();
			Assert.NotNull(asStr);
			Console.WriteLine(asStr);
		}

		[Test]
		public void InvalidXmlUri()
		{
			const string xml = @"<?xml version='1.0' encoding='utf-8'?>
<PitDefines></Bad>";

			using (var f = new TempFile(xml))
			{
				Assert.Throws<PeachException>(() => XmlTools.Deserialize<PitDefines>(f.Path));
			}
		}

		[Test]
		public void InvalidXmlString()
		{
			const string xml = @"<?xml version='1.0' encoding='utf-8'?>
<PitDefines></Bad>";

			using (var rdr = new StringReader(xml))
			{
				Assert.Throws<PeachException>(() => XmlTools.Deserialize<PitDefines>(rdr));
			}
		}

		[Test]
		public void TestEvaluateSimple()
		{
			const string xml = @"
<PitDefines>
	<All>
		<Define name='k1' key='k1' value='##k2##' />
		<Define name='k2' key='k2' value='##k3##-2' />
		<Define name='k3' key='k3' value='##k4##-3' />
		<Define name='k4' key='k4' value='##k5##/##k5##' />
		<Define name='k5' key='k5' value='foo' />
		<Define name='k6' key='k6' value='##k2##-##k3##' />
	</All>
</PitDefines>
";

			using (var rdr = new StringReader(xml))
			{
				var defs = XmlTools.Deserialize<PitDefines>(rdr);

				var dst = defs.Evaluate();

				Assert.AreEqual(6, dst.Count);

				Assert.AreEqual("k1", dst[0].Key);
				Assert.AreEqual("foo/foo-3-2", dst[0].Value);
				Assert.AreEqual("k2", dst[1].Key);
				Assert.AreEqual("foo/foo-3-2", dst[1].Value);
				Assert.AreEqual("k3", dst[2].Key);
				Assert.AreEqual("foo/foo-3", dst[2].Value);
				Assert.AreEqual("k4", dst[3].Key);
				Assert.AreEqual("foo/foo", dst[3].Value);
				Assert.AreEqual("k5", dst[4].Key);
				Assert.AreEqual("foo", dst[4].Value);
				Assert.AreEqual("k6", dst[5].Key);
				Assert.AreEqual("foo/foo-3-2-foo/foo-3", dst[5].Value);
			}
		}

		[Test]
		public void TestEvaluateMissing()
		{
			const string xml = @"
<PitDefines>
	<All>
		<Define name='k1' key='k1' value='##k2##' />
	</All>
</PitDefines>
";

			using (var rdr = new StringReader(xml))
			{
				var defs = XmlTools.Deserialize<PitDefines>(rdr);

				var dst = defs.Evaluate();

				Assert.AreEqual(1, dst.Count);

				Assert.AreEqual("k1", dst[0].Key);
				Assert.AreEqual("##k2##", dst[0].Value);
			}
		}

		[Test]
		public void TestEvaluatePitLibraryPath()
		{
			const string xml = @"
<PitDefines>
	<All>
		<Define name='SamplePath' key='SamplePath' value='##PitLibraryPath##/Samples' />
		<Define name='PitLibraryPath' key='PitLibraryPath' value='.' />
	</All>
</PitDefines>
";

			using (var f = new TempFile(xml))
			{
				var defs = PitDefines.ParseFile(f.Path, "Peach/Pits");

				Assert.AreEqual(7, defs.SystemDefines.Count);
				Assert.AreEqual(1, defs.Platforms.Count);
				Assert.AreEqual(2, defs.Platforms[0].Defines.Count);

				var dst = defs.Evaluate();

				Assert.AreEqual(8, dst.Count);

				// PitLibraryPath is injected as a system define
				// and takes prededence over any .config define

				Assert.AreEqual("SamplePath", dst[0].Key);
				Assert.AreEqual("Peach/Pits/Samples", dst[0].Value);
				Assert.AreEqual("Peach.OS", dst[1].Key);
				Assert.AreEqual("Peach.Pwd", dst[2].Key);
				Assert.AreEqual("Peach.Cwd", dst[3].Key);
				Assert.AreEqual("Peach.LogRoot", dst[4].Key);
				Assert.AreEqual("Peach.Plugins", dst[5].Key);
				Assert.AreEqual("Peach.Scripts", dst[6].Key);
				Assert.AreEqual("PitLibraryPath", dst[7].Key);
				Assert.AreEqual("Peach/Pits", dst[7].Value);
			}
		}

		[Test]
		public void TestEvaluateOverrides()
		{
			const string xml = @"
<PitDefines>
	<All>
		<Define name='SamplePath1' key='SamplePath1' value='##PitLibraryPath##/Samples1' />
		<Define name='SamplePath2' key='SamplePath2' value='##PitLibraryPath##/Samples2' />
		<Define name='PitLibraryPath' key='PitLibraryPath' value='.' />
	</All>
</PitDefines>
";

			var overrides = new[]
			{
				new KeyValuePair<string, string>("SamplePath1", "foo"),
				new KeyValuePair<string, string>("PitLibraryPath", "bar")
			};

			using (var f = new TempFile(xml))
			{
				var defs = PitDefines.ParseFile(f.Path, overrides, Guid.Empty);

				Assert.AreEqual(9, defs.SystemDefines.Count);
				Assert.AreEqual(1, defs.Platforms.Count);
				Assert.AreEqual(3, defs.Platforms[0].Defines.Count);

				var dst = defs.Evaluate();

				Assert.AreEqual(9, dst.Count);

				// overrides get injected as system defines and
				// take precedence over any .config define

				Assert.AreEqual("SamplePath2", dst[0].Key);
				Assert.AreEqual("bar/Samples2", dst[0].Value);
				Assert.AreEqual("Peach.OS", dst[1].Key);
				Assert.AreEqual("Peach.Pwd", dst[2].Key);
				Assert.AreEqual("Peach.Cwd", dst[3].Key);
				Assert.AreEqual("Peach.LogRoot", dst[4].Key);
				Assert.AreEqual("Peach.Plugins", dst[5].Key);
				Assert.AreEqual("Peach.Scripts", dst[6].Key);
				Assert.AreEqual("SamplePath1", dst[7].Key);
				Assert.AreEqual("foo", dst[7].Value);
				Assert.AreEqual("PitLibraryPath", dst[8].Key);
				Assert.AreEqual("bar", dst[8].Value);
			}
		}

		[Test]
		public void TestEvaluatePrecedence()
		{
			const string xml = @"
<PitDefines>
	<All>
		<Define name='SamplePath' key='SamplePath' value='foo' />
		<Define name='SamplePath' key='SamplePath' value='bar' />
		<Define name='Executable' key='Executable' value='foo' />
	</All>
	<All>
		<Define name='Executable' key='Executable' value='bar' />
	</All>
</PitDefines>
";

			var overrides = new[]
			{
				new KeyValuePair<string, string>("Arg", "foo"),
				new KeyValuePair<string, string>("Arg", "bar")
			};

			using (var f = new TempFile(xml))
			{
				var defs = PitDefines.ParseFile(f.Path, overrides, Guid.Empty);

				Assert.AreEqual(9, defs.SystemDefines.Count);
				Assert.AreEqual(2, defs.Platforms.Count);
				Assert.AreEqual(3, defs.Platforms[0].Defines.Count);
				Assert.AreEqual(1, defs.Platforms[1].Defines.Count);

				var dst = defs.Evaluate();

				Assert.AreEqual(10, dst.Count);

				Assert.AreEqual("SamplePath", dst[0].Key);
				Assert.AreEqual("bar", dst[0].Value);
				Assert.AreEqual("Executable", dst[1].Key);
				Assert.AreEqual("bar", dst[1].Value);
				Assert.AreEqual("Peach.OS", dst[2].Key);
				Assert.AreEqual("Peach.Pwd", dst[3].Key);
				Assert.AreEqual("Peach.Cwd", dst[4].Key);
				Assert.AreEqual("Peach.LogRoot", dst[5].Key);
				Assert.AreEqual("Peach.Plugins", dst[6].Key);
				Assert.AreEqual("Peach.Scripts", dst[7].Key);
				Assert.AreEqual("PitLibraryPath", dst[8].Key);
				Assert.AreEqual("Arg", dst[9].Key);
				Assert.AreEqual("bar", dst[9].Value);
			}
		}

		[Test]
		public void IncompleteXmlUri()
		{
			const string xml = @"<?xml version='1.0' encoding='utf-8'?>
<PitDefines>
	<All>
		<Enum key='key' value='value' name='name' description='description'/>
	</All>
</PitDefines>";

			using (var f = new TempFile(xml))
			{
				Assert.Throws<PeachException>(() => XmlTools.Deserialize<PitDefines>(f.Path));
			}
		}

		[Test]
		public void IncompleteXmlString()
		{
			const string xml = @"<?xml version='1.0' encoding='utf-8'?>
<PitDefines>
	<All>
		<Enum key='key' value='value' name='name' description='description'/>
	</All>
</PitDefines>";

			using (var rdr = new StringReader(xml))
			{
				Assert.Throws<PeachException>(() => XmlTools.Deserialize<PitDefines>(rdr));
			}
		}

		[Test]
		public void TestGroup()
		{
			const string xml = @"
<PitDefines>
	<All>
		<Group name='Group'>
			<Define name='k1' key='k1' value='##k2##' />
			<Space />
			<Define name='k2' key='k2' value='foo' />
		</Group>
		<String key='key0' value='value0' name='name0' description='description0'/>
	</All>
	<None>
		<Group name='Group'>
			<Define name='k1' key='k1' value='bar' />
			<Define name='k2' key='k2' value='baz' />
		</Group>
	</None>
</PitDefines>
";

			using (var rdr = new StringReader(xml))
			{
				var defs = XmlTools.Deserialize<PitDefines>(rdr);
				Assert.AreEqual(2, defs.Platforms.Count);
				Assert.AreEqual(Platform.OS.All, defs.Platforms[0].Platform);
				Assert.AreEqual(defs.Platforms[0].Defines[0].ConfigType, ParameterType.Group);
				Assert.AreEqual(defs.Platforms[0].Defines[0].Defines[0].ConfigType, ParameterType.User);
				Assert.AreEqual(defs.Platforms[0].Defines[0].Defines[1].ConfigType, ParameterType.Space);
				Assert.AreEqual(defs.Platforms[0].Defines[0].Defines[2].ConfigType, ParameterType.User);
				Assert.AreEqual(defs.Platforms[0].Defines[1].ConfigType, ParameterType.String);

				var dst = defs.Evaluate();

				Assert.AreEqual(3, dst.Count);

				Assert.AreEqual("k1", dst[0].Key);
				Assert.AreEqual("foo", dst[0].Value);
				Assert.AreEqual("k2", dst[1].Key);
				Assert.AreEqual("foo", dst[1].Value);
				Assert.AreEqual("key0", dst[2].Key);
				Assert.AreEqual("value0", dst[2].Value);
			}
		}

		[Test]
		public void TestNoSystem()
		{
			const string xml = @"
<PitDefines>
	<All>
		<String key='key0' value='value0' name='name0' description='description0' />
		<String key='PitLibraryPath' value='.' name='Pit Library Path' description='Path to pit library' />
	</All>
</PitDefines>
";

			using (var tmp = new TempFile(xml))
			{
				var defs = PitDefines.ParseFile(tmp.Path, "/path/to/pits");

				var web = defs.ToWeb(new List<Param>());

				Assert.AreEqual(2, web.Count);
				Assert.AreEqual("All", web[0].Name);
				Assert.AreEqual(1, web[0].Items.Count);
				Assert.AreEqual("key0", web[0].Items[0].Key);
			}
		}

		[Test]
		public void TestJson()
		{
			const string xml = @"
<PitDefines>
	<Group name='Outer'>
		<Group name='Inner' collapsed='true' description='innerDesc'>
			<Define name='k1' key='k1' value='##k2##' />
			<Space />
			<Define name='k2' key='k2' value='foo' />
		</Group>
		<String key='key0' value='value0' name='name0' description='description0'/>
	</Group>
</PitDefines>
";

			using (var rdr = new StringReader(xml))
			{
				var defs = XmlTools.Deserialize<PitDefines>(rdr);
				var web = defs.ToWeb(new List<Param>());
				var json = web.ToJson();

				var exp = @"[
  {
    ""Description"": """",
    ""Items"": [
      {
        ""Collapsed"": true,
        ""Description"": ""innerDesc"",
        ""Items"": [
          {
            ""Description"": """",
            ""Key"": ""k1"",
            ""Name"": ""k1"",
            ""Options"": [],
            ""Type"": ""User"",
            ""Value"": ""##k2##""
          },
          {
            ""Type"": ""Space""
          },
          {
            ""Description"": """",
            ""Key"": ""k2"",
            ""Name"": ""k2"",
            ""Options"": [],
            ""Type"": ""User"",
            ""Value"": ""foo""
          }
        ],
        ""Name"": ""Inner"",
        ""OS"": ""All"",
        ""Type"": ""Group""
      },
      {
        ""Description"": ""description0"",
        ""Key"": ""key0"",
        ""Name"": ""name0"",
        ""Options"": [],
        ""Type"": ""String"",
        ""Value"": ""value0""
      }
    ],
    ""Name"": ""Outer"",
    ""OS"": ""All"",
    ""Type"": ""Group""
  },
  {
    ""Collapsed"": true,
    ""Description"": ""These values are controlled by Peach."",
    ""Items"": [],
    ""Key"": ""SystemDefines"",
    ""Name"": ""System Defines"",
    ""OS"": """",
    ""Type"": ""Group""
  }
]".Replace("\r\n", Environment.NewLine);

				Assert.AreEqual(exp, json);
			}
		}

		[Test]
		public void TestUserDefines()
		{
			const string xml = @"
<PitDefines>
	<Group name='Outer'>
		<String key='key0' value='value0' name='name0' description='description0'/>
	</Group>
</PitDefines>
";

			using (var rdr = new StringReader(xml))
			{
				var defs = XmlTools.Deserialize<PitDefines>(rdr);
				var web = defs.ToWeb(new List<Param>()
				{
					new Param 
					{
						Key = "CustomKey",
						Name = "Custom Name",
						Description = "Custom Description",
						Value = "Custom Value",
					}
				});
				var json = web.ToJson();
				Console.WriteLine(json);

				var exp = @"[
  {
    ""Description"": """",
    ""Items"": [
      {
        ""Description"": ""description0"",
        ""Key"": ""key0"",
        ""Name"": ""name0"",
        ""Options"": [],
        ""Type"": ""String"",
        ""Value"": ""value0""
      }
    ],
    ""Name"": ""Outer"",
    ""OS"": ""All"",
    ""Type"": ""Group""
  },
  {
    ""Description"": """",
    ""Items"": [
      {
        ""Description"": ""Custom Description"",
        ""Key"": ""CustomKey"",
        ""Name"": ""Custom Name"",
        ""Type"": ""User""
      }
    ],
    ""Key"": ""UserDefines"",
    ""Name"": ""User Defines"",
    ""OS"": """",
    ""Type"": ""Group""
  },
  {
    ""Collapsed"": true,
    ""Description"": ""These values are controlled by Peach."",
    ""Items"": [],
    ""Key"": ""SystemDefines"",
    ""Name"": ""System Defines"",
    ""OS"": """",
    ""Type"": ""Group""
  }
]".Replace("\r\n", Environment.NewLine);

				Assert.AreEqual(exp, json);
			}
		}
	}
}
