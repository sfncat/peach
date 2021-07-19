using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.PitParserTests
{
	[TestFixture]
	[Quick]
	[Peach]
	class IncludeTests
	{
		[Test]
		public void Test1()
		{
			string inc1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""HelloWorldTemplate"">
		<String name=""str"" value=""Hello World!""/>
		<String>
			<Relation type=""size"" of=""HelloWorldTemplate""/>
		</String>
	</DataModel>
</Peach>
";

			string template = @"
<Peach>
	<Include ns=""example"" src=""{0}"" />

	<StateModel name=""State"" initialState=""Initial"">
		<State name=""Initial"">
			<Action type=""output"">
				<DataModel name=""foo"" ref=""example:HelloWorldTemplate"" />
			</Action>
		</State>
	</StateModel>
	
	<Test name=""Default"">
		<StateModel ref=""State"" />
		<Publisher class=""File"">
			<Param name=""FileName"" value=""{1}""/>
		</Publisher>
	</Test>
	
</Peach>";

			using (var remote = new TempFile())
			using (var output = new TempFile())
			{
				string xml = string.Format(template, remote.Path, output.Path);
				File.WriteAllText(remote.Path, inc1);

				var dom = DataModelCollector.ParsePit(xml);

				var config = new RunConfiguration();
				config.singleIteration = true;

				var e = new Engine(null);
				e.startFuzzing(dom, config);

				Assert.AreEqual("Hello World!13", File.ReadAllText(output.Path));
			}
		}

		[Test]
		public void Test2()
		{
			string inc1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>

	<DataModel name=""BaseModel"">
		<String name=""str"" value=""Hello World!""/>
	</DataModel>

	<DataModel name=""HelloWorldTemplate"" ref=""BaseModel"">
	</DataModel>
</Peach>
";

			string template = @"
<Peach>
	<Include ns=""example"" src=""file:{0}"" />

	<DataModel name=""DM"">
		<Block ref=""example:HelloWorldTemplate""/>
	</DataModel>

	<StateModel name=""State"" initialState=""Initial"">
		<State name=""Initial"">
			<Action type=""output"">
				<DataModel ref=""DM"" />
			</Action>
		</State>
	</StateModel>
	
	<Test name=""Default"">
		<StateModel ref=""State"" />
		<Publisher class=""File"">
			<Param name=""FileName"" value=""{1}""/>
		</Publisher>
	</Test>
	
</Peach>";

			using (var remote = new TempFile())
			using (var output = new TempFile())
			{
				File.WriteAllText(remote.Path, inc1);
				var xml = template.Fmt(remote.Path, output.Path);

				var dom = DataModelCollector.ParsePit(xml);

				var config = new RunConfiguration();
				config.singleIteration = true;

				var e = new Engine(null);
				e.startFuzzing(dom, config);

				Assert.AreEqual("Hello World!", File.ReadAllText(output.Path));
			}
		}

		[Test]
		public void Test3()
		{
			string inc1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""BaseModel"">
		<String name=""str"" value=""Hello World!""/>
	</DataModel>

	<DataModel name=""DerivedModel"">
		<Block ref=""BaseModel"" />
	</DataModel>
</Peach>
";

			string inc2 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<Include ns=""abc"" src=""file:{0}"" />

	<DataModel name=""BaseModel2"" ref=""abc:DerivedModel""/>

</Peach>
";

			string template = @"
<Peach>
	<Include ns=""example"" src=""file:{0}"" />

	<DataModel name=""DM"">
		<Block ref=""example:BaseModel2""/>
	</DataModel>

	<StateModel name=""State"" initialState=""Initial"">
		<State name=""Initial"">
			<Action type=""output"">
				<DataModel ref=""DM"" />
			</Action>
		</State>
	</StateModel>
	
	<Test name=""Default"">
		<StateModel ref=""State"" />
		<Publisher class=""File"">
			<Param name=""FileName"" value=""{1}""/>
		</Publisher>
	</Test>
	
</Peach>";

			using (var remote1 = new TempFile())
			using (var remote2 = new TempFile())
			using (var output = new TempFile())
			{
				var xml = template.Fmt(remote2.Path, output.Path);
				File.WriteAllText(remote1.Path, inc1);
				File.WriteAllText(remote2.Path, inc2.Fmt(remote1.Path));

				var dom = DataModelCollector.ParsePit(xml);

				var config = new RunConfiguration();
				config.singleIteration = true;

				var e = new Engine(null);
				e.startFuzzing(dom, config);

				Assert.AreEqual("Hello World!", File.ReadAllText(output.Path));
			}
		}

		[Test]
		public void Test4()
		{
			string inc1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""HelloWorldTemplate"">
		<Number name=""Size"" size=""8"">
			<Relation type=""size"" of=""HelloWorldTemplate""/>
		</Number>
		<String name=""str"" value=""four""/>
	</DataModel>
</Peach>
";

			string template = @"
<Peach>
	<Include ns=""example"" src=""{0}"" />

	<DataModel name='Foo'>
		<String name='slurp' value='slurping' />
	</DataModel>

	<StateModel name=""State"" initialState=""Initial"">
		<State name=""Initial"">
			<Action type=""output"">
				<DataModel ref=""example:HelloWorldTemplate"" />
			</Action>
		</State>
	</StateModel>
	
	<StateModel name=""StateOverride"" initialState=""Initial"">
		<State name=""Initial"">
			<Action type=""output"">
				<DataModel ref=""example:HelloWorldTemplate"" />
				<Data>
					<Field name=""str"" value=""hello""/>
				</Data>
			</Action>
		</State>
	</StateModel>

	<StateModel name=""StateSlurp"" initialState=""Initial"">
		<State name=""Initial"">
			<Action type=""output"" publisher=""pub2"">
				<DataModel ref=""Foo"" />
			</Action>

			<Action type=""slurp"" valueXpath=""//slurp"" setXpath=""//str""/>

			<Action type=""output"">
				<DataModel ref=""example:HelloWorldTemplate"" />
			</Action>
		</State>
	</StateModel>

	<Test name=""Slurp"">
		<StateModel ref=""StateSlurp"" />
		<Publisher class=""File"">
			<Param name=""FileName"" value=""{1}""/>
		</Publisher>
		<Publisher class=""Null"" name=""pub2""/>
	</Test>

	<Test name=""Override"">
		<StateModel ref=""StateOverride"" />
		<Publisher class=""File"">
			<Param name=""FileName"" value=""{1}""/>
		</Publisher>
	</Test>

	<Test name=""Default"">
		<StateModel ref=""State"" />
		<Publisher class=""File"">
			<Param name=""FileName"" value=""{1}""/>
		</Publisher>
	</Test>
	
</Peach>";

			using (var remote = new TempFile())
			using (var output = new TempFile())
			{
				string xml = string.Format(template, remote.Path, output.Path);

				File.WriteAllText(remote.Path, inc1);

				var dom = DataModelCollector.ParsePit(xml);

				var config = new RunConfiguration();
				config.singleIteration = true;

				var e = new Engine(null);
				config.runName = "Default";
				e.startFuzzing(dom, config);

				byte[] result = File.ReadAllBytes(output.Path);

				Assert.AreEqual(Encoding.ASCII.GetBytes("\x0005four"), result);

				dom = DataModelCollector.ParsePit(xml);

				e = new Engine(null);
				config.runName = "Override";
				e.startFuzzing(dom, config);

				result = File.ReadAllBytes(output.Path);

				Assert.AreEqual(Encoding.ASCII.GetBytes("\x0006hello"), result);

				dom = DataModelCollector.ParsePit(xml);

				e = new Engine(null);
				config.runName = "Slurp";
				e.startFuzzing(dom, config);

				result = File.ReadAllBytes(output.Path);

				Assert.AreEqual(Encoding.ASCII.GetBytes("\x0009slurping"), result);
			}
		}

		[Test]
		public void Test5()
		{
			string inc1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<DataModel name=""Model1"">
		<String name=""str1"" value=""foo""/>
	</DataModel>

	<DataModel name=""Model2"">
		<String name=""str2"" value=""bar""/>
	</DataModel>
</Peach>
";

			string template = @"
<Peach>
	<Include ns=""example"" src=""{0}"" />

	<StateModel name=""State"" initialState=""Initial"">
		<State name=""Initial"">
			<Action type=""output"">
				<DataModel ref=""example:Model1"" />
			</Action>

			<Action type=""slurp"" valueXpath=""//example:Model1/str1"" setXpath=""//example:Model2//str2"" />

			<Action type=""output"">
				<DataModel ref=""example:Model2"" />
			</Action>
		</State>
	</StateModel>

	<Test name=""Default"">
		<StateModel ref=""State"" />
		<Publisher class=""Null""/>
	</Test>
</Peach>";

			using (var remote = new TempFile())
			{
				string xml = string.Format(template, remote.Path);
				File.WriteAllText(remote.Path, inc1);

				var dom = DataModelCollector.ParsePit(xml);

				var config = new RunConfiguration();
				config.singleIteration = true;

				var e = new Engine(null);
				e.startFuzzing(dom, config);

				var act = dom.tests[0].stateModel.states["Initial"].actions[2];
				Assert.AreEqual("foo", (string)act.dataModel[0].DefaultValue);
			}
		}

		[Test]
		public void IncludeScripting()
		{
			string inc1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Peach>
	<Import import=""re""/>

	<DataModel name=""DM"">
		<String name=""str1"" value=""foo"" constraint=""re.search('^Hello', value) != None""/>
	</DataModel>

</Peach>
";

			string template = @"
<Peach>
	<Include ns=""example"" src=""{0}"" />

	<StateModel name=""State"" initialState=""Initial"">
		<State name=""Initial"">
			<Action type=""input"">
				<DataModel ref=""example:DM"" />
			</Action>
		</State>
	</StateModel>

	<Test name=""Default"">
		<StateModel ref=""State"" />
		<Publisher class=""File"">
			<Param name=""FileName"" value=""{1}""/>
			<Param name=""Overwrite"" value=""false""/>
		</Publisher>
	</Test>
</Peach>";

			using (var remote = new TempFile())
			using (var tmp = new TempFile())
			{
				File.WriteAllText(tmp.Path, "Hello World");
				File.WriteAllText(remote.Path, inc1);

				string xml = string.Format(template, remote.Path, tmp.Path);

				var dom = DataModelCollector.ParsePit(xml);

				var config = new RunConfiguration();
				config.singleIteration = true;

				var e = new Engine(null);
				e.startFuzzing(dom, config);

				var act = dom.tests[0].stateModel.states["Initial"].actions[0];
				Assert.AreEqual("Hello World", (string)act.dataModel[0].DefaultValue);
			}
		}

		[Test]
		public void MultipleIncludes()
		{
			string inc1 = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='Frame'>
		<Blob name='Type' value='type'/>
		<Blob name='Payload' value='Frame Payload'/>
	</DataModel>

</Peach>
";

			string inc2 = @"<?xml version='1.0' encoding='utf-8'?>
<Peach>
	<DataModel name='Packet'>
		<Blob name='Src' value='src'/>
		<Blob name='Dst' value='dst'/>
		<Blob name='Payload' value='Packet Payload'/>
	</DataModel>

</Peach>
";

			using (var tmp1 = new TempFile())
			using (var tmp2 = new TempFile())
			{
				File.WriteAllText(tmp1.Path, inc1);
				File.WriteAllText(tmp2.Path, inc2);

				string xml = @"
<Peach>
	<Include ns='NS1' src='{0}' />
	<Include ns='NS2' src='{1}' />

	<DataModel name='Packet' ref='NS1:Frame'>
		<Block name='Payload' ref='NS2:Packet'/>
	</DataModel>
</Peach>".Fmt(tmp1.Path, tmp2.Path);

				var dom = DataModelCollector.ParsePit(xml);

				Assert.AreEqual(1, dom.dataModels.Count);

				var val = dom.dataModels[0].InternalValue.BitsToString();

				var exp = "typesrcdstPacket Payload";
				Assert.AreEqual(exp, val);
			}
		}
	}
}

