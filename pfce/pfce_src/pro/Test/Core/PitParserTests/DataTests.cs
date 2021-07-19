using System.Collections.Generic;
using System.IO;
using NLog;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.PitParserTests
{
	[TestFixture]
	[Quick]
	[Peach]
	class DataTests
	{
		[Test]
		public void DataFieldLiteral()
		{
			string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Peach>
	<StateModel name=""StateModel"" initialState=""Initial"">
		<State name=""Initial"">
			<Action name=""Output"" type=""output"">
				<DataModel name=""DataModel"">
					<String name=""String""/>
				</DataModel>
 				<Data name=""Data"">
					<Field name=""String"" value=""'Hello, world!'"" valueType=""literal"" />
				</Data>
			</Action>
		</State>
	</StateModel>
	<Test name=""Default"">
		<Publisher class=""Null"" />
		<StateModel ref=""StateModel"" />
	</Test>
</Peach>
";

			var dom = DataModelCollector.ParsePit(xml);
			var config = new RunConfiguration { singleIteration = true };
			var e = new Engine(null);

			e.startFuzzing(dom, config);

			var elem = (String)dom.tests[0].stateModel.states[0].actions[0].dataModel[0];
			Assert.AreEqual("Hello, world!", (string)elem.DefaultValue);
		}

		[Test]
		public void NullDataFieldLiteral()
		{
			string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Peach>
	<StateModel name=""StateModel"" initialState=""Initial"">
		<State name=""Initial"">
			<Action name=""Output"" type=""output"">
				<DataModel name=""DataModel"">
					<String name=""String""/>
				</DataModel>
 				<Data name=""Data"">
					<Field name=""String"" value=""None"" valueType=""literal"" />
				</Data>
			</Action>
		</State>
	</StateModel>
	<Test name=""Default"">
		<Publisher class=""Null"" />
		<StateModel ref=""StateModel"" />
	</Test>
</Peach>
";

			var ex = Assert.Throws<PeachException>(() => DataModelCollector.ParsePit(xml));

			Assert.AreEqual("Error, the value of the eval statement of Field 'String' returned null.", ex.Message);
		}

		[Test]
		public void BadTypeDataFieldLiteral()
		{
			string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Peach>
	<StateModel name=""StateModel"" initialState=""Initial"">
		<State name=""Initial"">
			<Action name=""Output"" type=""output"">
				<DataModel name=""DataModel"">
					<String name=""String""/>
				</DataModel>
 				<Data name=""Data"">
					<Field name=""String"" value=""{}"" valueType=""literal"" />
				</Data>
			</Action>
		</State>
	</StateModel>
	<Test name=""Default"">
		<Publisher class=""Null"" />
		<StateModel ref=""StateModel"" />
	</Test>
</Peach>
";

			var ex = Assert.Throws<PeachException>(() => DataModelCollector.ParsePit(xml));

			Assert.AreEqual("Error, the value of the eval statement of Field 'String' returned unsupported type 'IronPython.Runtime.PythonDictionary'.", ex.Message);
		}

		[Test]
		public void InvalidDataFieldLiteral()
		{
			string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Peach>
	<StateModel name=""StateModel"" initialState=""Initial"">
		<State name=""Initial"">
			<Action name=""Output"" type=""output"">
				<DataModel name=""DataModel"">
					<String name=""String""/>
				</DataModel>
 				<Data name=""Data"">
					<Field name=""String"" value=""foo.bar()"" valueType=""literal"" />
				</Data>
			</Action>
		</State>
	</StateModel>
	<Test name=""Default"">
		<Publisher class=""Null"" />
		<StateModel ref=""StateModel"" />
	</Test>
</Peach>
";

			var ex = Assert.Throws<PeachException>(() => DataModelCollector.ParsePit(xml));

			Assert.AreEqual("Failed to evaluate expression [foo.bar()], name 'foo' is not defined.", ex.Message);
		}

		[Test]
		public void DataFieldFileNamesWithXpath()
		{
			const string req1 = "Foo: bar\r\nHost: xxx\r\nConnection: close\r\n\r\n";
			const string req2 = "Host: xxx\r\nFoo: bar\r\nConnection: close\r\n\r\n";

			using (var tmp = new TempDirectory())
			{
				File.WriteAllText(Path.Combine(tmp.Path, "sample1.txt"), req1);
				File.WriteAllText(Path.Combine(tmp.Path, "sample2.txt"), req2);

				var xml = @"<?xml version='1.0' encoding='UTF-8'?>
<Peach>
	<DataModel name='Header'>
		<String name='Header' />
		<String name='Sep' value=': ' token='true' />
		<String name='Value' />
		<String name='End' value='\r\n' token='true' />
	</DataModel>

	<DataModel name='DM'>
		<Choice minOccurs='-1'>
			<Block name='Host' ref='Header'>
				<String name='Header' value='Host' token='true' />
			</Block>
			<Block name='Generic' ref='Header' />
		</Choice>
		<String name='End' value='\r\n' token='true' />
	</DataModel>

	<StateModel name='StateModel' initialState='Initial'>
		<State name='Initial'>
			<Action name='Output' type='output'>
				<DataModel ref='DM' />
 				<Data name='Data' fileName='{0}'>
					<Field xpath='//Host/Value' value='mycustomhost' />
				</Data>
			</Action>
		</State>
	</StateModel>
	<Test name='Default'>
		<Publisher class='Null' />
		<StateModel ref='StateModel' />
		<Strategy class='RandomStrategy'>
			<Param name='SwitchCount' value='2' />
		</Strategy>
	</Test>
</Peach>
".Fmt(Path.Combine(tmp.Path, "*"));

				var dom = DataModelCollector.ParsePit(xml);
				var cfg = new RunConfiguration {range = true, rangeStart = 1, rangeStop = 20};
				var e = new Engine(null);

				var lines = new List<string>();

				e.TestStarting += ctx =>
				{
					ctx.ActionFinished += (c, a) =>
					{
						if (c.controlIteration)
						{
							var val = Encoding.ASCII.GetString(a.dataModel.Value.ToArray());
							lines.Add(val);
						}
					};
				};

				e.startFuzzing(dom, cfg);

				CollectionAssert.IsNotEmpty(lines);

				CollectionAssert.Contains(lines, "Foo: bar\r\nHost: mycustomhost\r\nConnection: close\r\n\r\n");
				CollectionAssert.Contains(lines, "Host: mycustomhost\r\nFoo: bar\r\nConnection: close\r\n\r\n");
			}
		}
	}
}
