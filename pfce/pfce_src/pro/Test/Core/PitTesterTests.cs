using System;
using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.PitTester;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	[Quick]
	internal class PitTesterTests
	{
		[Test]
		public void TestIgnoreArrayField()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String value='pre ' />
		<Block name='item' minOccurs='0'>
			<String name='value'/>
		</Block>
		<String value=' post' />
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
				<Data>
					<Field name='item[0].value' value='aaa' />
					<Field name='item[1].value' value='bbb' />
					<Field name='item[2].value' value='aaa' />
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher name='Pub' class='Null'/>
	</Test>
</Peach>
";

			const string test = @"
<TestData pit='test.xml'>
	<Ignore xpath='//value' />

	<Test name='Default'>
		<Open   action='TheState.Initial.Action' publisher='Pub'/>
		<Output action='TheState.Initial.Action' publisher='Pub'>
<![CDATA[
0000000: 7072 6520 6161 6161 6161 6161 6120 706f  pre aaaaaaaaa po
0000010: 7374                                     st
]]>
		</Output>
		<Close  action='TheState.Initial.Action' publisher='Pub'/>
	</Test>
</TestData>
";

			// Ensure we can run when there is an ignore that matches a de-selected choice
			using (var tmpDir = new TempDirectory())
			{
				var pitFile = Path.Combine(tmpDir.Path, "test.xml");
				var pitTest = pitFile + ".test";

				File.WriteAllText(pitFile, xml);
				File.WriteAllText(pitTest, test);

				ThePitTester.TestPit(tmpDir.Path, pitTest, null, true, 1);
			}
		}

		[Test]
		public void TestIgnoreChoice()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<Choice name='c'>
			<String name='str1' />
			<String name='str2' />
		</Choice>
		<String value='\r\n' />
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
				<Data>
					<Field name='c.str1' value='String One' />
				</Data>
				<Data>
					<Field name='c.str2' value='String Two' />
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher name='Pub' class='Null'/>
	</Test>
</Peach>
";

			const string test = @"
<TestData pit='test.xml'>
	<Ignore xpath='//str1' />
	<Ignore xpath='//str2' />

	<Test name='Default'>
		<Open   action='TheState.Initial.Action' publisher='Pub'/>
		<Output action='TheState.Initial.Action' publisher='Pub'>
<![CDATA[
0000   00 00 00 00 00 00 00 00 00 00 0d 0a              ............
]]>
		</Output>
		<Close  action='TheState.Initial.Action' publisher='Pub'/>
	</Test>
</TestData>
";

			// Ensure we can run when there is an ignore that matches a de-selected choice
			using (var tmpDir = new TempDirectory())
			{
				var pitFile = Path.Combine(tmpDir.Path, "test.xml");
				var pitTest = pitFile + ".test";

				File.WriteAllText(pitFile, xml);
				File.WriteAllText(pitTest, test);

				ThePitTester.TestPit(tmpDir.Path, pitTest, null, true, 1);
			}
		}

		[Test]
		public void TestSlurpChoiceOfArrayOfChoice()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<Choice name='c1' minOccurs='0'>
			<String name='str' value=' ' />
			<Block name='blk'>
				<Choice name='c2'>
					<String name='str' />
					<Block name='inner'>
						<String name='prefix' value='Hello' />
						<String name='tgt' value='World' />
					</Block>
				</Choice>
			</Block>
		</Choice>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
				<Data>
					<Field name='c1[0].blk.c2.inner' value='' />
					<Field name='c1[1].str' value=' ' />
					<Field name='c1[2].blk.c2.inner.prefix' value='Foo' />
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher name='Pub' class='Null'/>
	</Test>
</Peach>
";

			const string test = @"
<TestData pit='test.xml'>
	<Slurp setXpath='//tgt' value='Hello' />

	<Test name='Default'>
		<Open   action='TheState.Initial.Action' publisher='Pub'/>
		<Output action='TheState.Initial.Action' publisher='Pub'>
<![CDATA[
0000000: 4865 6c6c 6f48 656c 6c6f 2046 6f6f 4865  HelloHello FooHe
0000010: 6c6c 6f                                  llo
]]>
		</Output>
		<Close  action='TheState.Initial.Action' publisher='Pub'/>
	</Test>
</TestData>
";

			// Ensure we can run when there is an ignore that matches a de-selected choice
			using (var tmpDir = new TempDirectory())
			{
				var pitFile = Path.Combine(tmpDir.Path, "test.xml");
				var pitTest = pitFile + ".test";

				File.WriteAllText(pitFile, xml);
				File.WriteAllText(pitTest, test);

				ThePitTester.TestPit(tmpDir.Path, pitTest, null, true, 1);
			}
		}


		[Test]
		public void UnhandledException()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str1' />
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher name='Pub' class='Tcp'>
			<Param name='Host' value='localhost' />
			<Param name='Port' value='65500' />
		</Publisher>
	</Test>
</Peach>
";

			const string test = @"
<TestData pit='test.xml'>
	<Test name='Default'>
		<Open   action='TheState.Initial.Action' publisher='Pub'/>
	</Test>
</TestData>
";

			// Ensure we can run when there is an ignore that matches a de-selected choice
			using (var tmpDir = new TempDirectory())
			{
				var pitFile = Path.Combine(tmpDir.Path, "test.xml");
				var pitTest = pitFile + ".test";

				File.WriteAllText(pitFile, xml);
				File.WriteAllText(pitTest, test);

				Assert.That(() =>
				{
					ThePitTester.TestPit(tmpDir.Path, pitTest, null, true, 1);
				},
					Throws.TypeOf<AggregateException>()
						.With.InnerException.Message.StartsWith("Encountered an unhandled exception on iteration 1, seed ")
						.With.InnerException.Message.Contains("Missing record in test data")
				);
			}
		}

		[Test]
		public void SlurpStringRandomFixup()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str1' value='0'>
			<Fixup class='SequenceRandom' />
		</String>
		<Number name='num' size='8'>
			<Fixup class='SequenceRandom' />
		</Number>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action name='Act1' type='output'>
				<DataModel ref='DM'/>
			</Action>
			<Action name='Act2' type='output'>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default' maxOutputSize='65535'>
		<StateModel ref='TheState'/>
		<Publisher name='Pub' class='Null' />
	</Test>
</Peach>
";

			const string test = @"
<TestData pit='test.xml'>
	<Slurp setXpath='//Act1/DM/num' valueType='hex' value='00'/>
	<Slurp setXpath='//Act1/DM/str1' value='1234567890'/>
	<Slurp setXpath='//Act2/DM/num' value='0'/>
	<Slurp setXpath='//Act2/DM/str1' value='31337'/>

	<Test name='Default'>
		<Open   action='TheState.Initial.Act1' publisher='Pub'/>
		<Output action='TheState.Initial.Act1' publisher='Pub'>
<![CDATA[
0000000: 3132 3334 3536 3738 3930 00              1234567890.
]]>
		</Output>
		<Output action='TheState.Initial.Act2' publisher='Pub'>
<![CDATA[
0000000: 3331 3333 3700                           31337.
]]>
		</Output>
	</Test>
</TestData>
";

			// Ensure we can run when there is an ignore that matches a de-selected choice
			using (var tmpDir = new TempDirectory())
			{
				var pitFile = Path.Combine(tmpDir.Path, "test.xml");
				var pitTest = pitFile + ".test";

				File.WriteAllText(pitFile, xml);
				File.WriteAllText(pitTest, test);

				ThePitTester.TestPit(tmpDir.Path, pitTest, null, true, 500);
			}
		}

		[Test]
		public void SlurpOverField()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='str1' value='0' />
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action name='Act1' type='output'>
				<DataModel ref='DM'/>
				<Data>
					<Field name='str1' value='Hello'/>
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default' maxOutputSize='65535'>
		<StateModel ref='TheState'/>
		<Publisher name='Pub' class='Null' />
	</Test>
</Peach>
";

			const string test = @"
<TestData pit='test.xml'>
	<Slurp setXpath='//Act1/DM/str1' value='1234567890'/>

	<Test name='Default'>
		<Open   action='TheState.Initial.Act1' publisher='Pub'/>
		<Output action='TheState.Initial.Act1' publisher='Pub'>
<![CDATA[
0000000: 3132 3334 3536 3738 3930                 1234567890
]]>
		</Output>
	</Test>
</TestData>
";

			using (var tmpDir = new TempDirectory())
			{
				var pitFile = Path.Combine(tmpDir.Path, "test.xml");
				var pitTest = pitFile + ".test";

				File.WriteAllText(pitFile, xml);
				File.WriteAllText(pitTest, test);

				ThePitTester.TestPit(tmpDir.Path, pitTest, null, true, 500);
			}
		}

		[Test]
		public void SlurpOverCrackedVolatileFixup()
		{
			var xml = @"
<Peach>
	<DataModel name='DM'>
		<Choice minOccurs='1'>
			<Block name='h1'>
				<String value='h1:' token='true' />
				<String name='value' />
				<String value='|' token='true' />
			</Block>
			<Block name='h2'>
				<String value='h2:' token='true' />
				<String name='value'>
					<Fixup class='UnixTime'>
						<Param name='Format' value='r' />
					</Fixup>
				</String>
				<String value='|' token='true' />
			</Block>
		</Choice>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action name='Act1' type='output'>
				<DataModel ref='DM'/>
				<Data fileName='{0}' />
			</Action>
		</State>
	</StateModel>

	<Test name='Default' maxOutputSize='65535'>
		<StateModel ref='TheState'/>
		<Publisher name='Pub' class='Null' />
	</Test>
</Peach>
";

			const string test = @"
<TestData pit='test.xml'>
	<Slurp setXpath='//h2/value' value='overridden'/>

	<Test name='Default'>
		<Open   action='TheState.Initial.Act1' publisher='Pub'/>
		<Output action='TheState.Initial.Act1' publisher='Pub'>
<![CDATA[
00000000   68 31 3A 66 6F 6F 7C 68  32 3A 6F 76 65 72 72 69   h1:foo|h2:overri
00000010   64 64 65 6E 7C 68 31 3A  62 61 7A 7C 68 32 3A 6F   dden|h1:baz|h2:o
00000020   76 65 72 72 69 64 64 65  6E 7C                     verridden|      
]]>
		</Output>
	</Test>
</TestData>
";
			using (var tmpDir = new TempDirectory())
			{
				var pitFile = Path.Combine(tmpDir.Path, "test.xml");
				var pitTest = pitFile + ".test";
				var pitSample = pitFile + ".sample";

				File.WriteAllText(pitFile, xml.Fmt(pitSample));
				File.WriteAllText(pitTest, test);
				File.WriteAllText(pitSample, "h1:foo|h2:bar|h1:baz|h2:qux|");

				ThePitTester.TestPit(tmpDir.Path, pitTest, null, true, 1);
			}
		}

		[Test]
		public void TestSkippedActions()
		{
			const string xml = @"
<Peach>
	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output' when='context.controlIteration' >
				<DataModel name='DM'>
					<String value='output' />
				</DataModel>
			</Action>
			<Action type='input' when='context.controlIteration' >
				<DataModel name='DM'>
					<String value='input1' token='true' />
				</DataModel>
			</Action>
			<Action type='input'>
				<DataModel name='DM'>
					<String value='input2' token='true' />
				</DataModel>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher name='Pub' class='Null'/>
	</Test>
</Peach>
";

			const string test = @"
<TestData pit='test.xml'>
	<Test name='Default'>
		<Open   action='TheState.Initial.Action' publisher='Pub'/>
		<Output action='TheState.Initial.Action' publisher='Pub'>
<![CDATA[
0000   6F 75 74 70 75 74                                 output
]]>
		</Output>
		<Input action='TheState.Initial.Action_1' publisher='Pub'>
<![CDATA[
0000   69 6E 70 75 74 31                                 input1
]]>
		</Input>
		<Input action='TheState.Initial.Action_2' publisher='Pub'>
<![CDATA[
0000   69 6E 70 75 74 32                                 input2
]]>
		</Input>
		<Close  action='TheState.Initial.Action' publisher='Pub'/>
	</Test>
</TestData>
";

			// Ensure we can run when there is an ignore that matches a de-selected choice
			using (var tmpDir = new TempDirectory())
			{
				var pitFile = Path.Combine(tmpDir.Path, "test.xml");
				var pitTest = pitFile + ".test";

				File.WriteAllText(pitFile, xml);
				File.WriteAllText(pitTest, test);

				ThePitTester.TestPit(tmpDir.Path, pitTest, null, false, 5);
			}
		}

		[Test]
		public void TestRecursiveActions()
		{
			const string xml = @"
<Peach>
	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='input'>
				<DataModel name='DM'>
					<Blob />
				</DataModel>
			</Action>

			<Action type='changeState' ref='Initial' when='state.actions[0].dataModel.Value.Length > 0' />
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher name='Pub' class='Null'/>
	</Test>
</Peach>
";

			const string test = @"
<TestData pit='test.xml'>
	<Test name='Default'>
		<Open   action='TheState.Initial.Action' publisher='Pub'/>
		<Input action='TheState.Initial.Action' publisher='Pub'>
<![CDATA[
0000   69 6E 70 75 74 31                                 input1
]]>
		</Input>
		<Input action='TheState.Initial.Action' publisher='Pub'>
<![CDATA[
0000   69 6E 70 75 74 32                                 input2
]]>
		</Input>
		<Input action='TheState.Initial.Action' publisher='Pub'>
<![CDATA[
]]>
		</Input>
		<Close  action='TheState.Initial.Action' publisher='Pub'/>
	</Test>
</TestData>
";

			// Ensure we can run when there is an ignore that matches a de-selected choice
			using (var tmpDir = new TempDirectory())
			{
				var pitFile = Path.Combine(tmpDir.Path, "test.xml");
				var pitTest = pitFile + ".test";

				File.WriteAllText(pitFile, xml);
				File.WriteAllText(pitTest, test);

				ThePitTester.TestPit(tmpDir.Path, pitTest, null, false, 5);
			}
		}

		[Test]
		public void TestFromFileFixup()
		{
			const string config = @"
<PitDefines>
	<All>
		<Define name='Pre' key='Pre' value='' />
		<Define name='Post' key='Post' value='' />
		<Define name='MyTextFile' key='MyTextFile' value='' />
		<Define name='MyHexFile' key='MyHexFile' value='' />
	</All>
</PitDefines>
";

			const string xml = @"
<Peach>
	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel name='DM'>
					<String value='##Pre##' />
					<Blob>
						<Fixup class='FromFile'>
							<Param name='Filename' value='##MyTextFile##' />
							<Param name='Encoding' value='Pem' />
						</Fixup>
					</Blob>
					<Blob>
						<Fixup class='FromFile'>
							<Param name='Filename' value='##MyHexFile##' />
						</Fixup>
					</Blob>
					<String value='##Post##' />
				</DataModel>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher name='Pub' class='Null'/>
	</Test>
</Peach>
";

			const string test = @"
<TestData pit='test.xml'>

	<Define key='Pre' value='foo' />

	<TextFile key='MyTextFile'>
<![CDATA[
-----BEGIN CERTIFICATE-----
SGVsbG8=
-----END CERTIFICATE-----
]]>
	</TextFile>

	<Define key='Post' value='bar' />

	<HexFile key='MyHexFile'>
<![CDATA[
0000   69 6E 70 75 74 31                                 input1
]]>
	</HexFile>

	<Test name='Default'>
		<Open   action='TheState.Initial.Action' publisher='Pub'/>
		<Output action='TheState.Initial.Action' publisher='Pub'>
<![CDATA[
0000   66 6F 6F 48 65 6C 6C 6F 69 6E 70 75 74 31 62 61   fooHelloinput1ba
0010   72                                                r               
]]>
		</Output>
	</Test>
</TestData>
";

			// Ensure we can run when there is an ignore that matches a de-selected choice
			using (var tmpDir = new TempDirectory())
			{
				var pitFile = Path.Combine(tmpDir.Path, "test.xml");
				var pitTest = pitFile + ".test";
				var pitConfig = pitFile + ".config";

				File.WriteAllText(pitFile, xml);
				File.WriteAllText(pitTest, test);
				File.WriteAllText(pitConfig, config);

				ThePitTester.TestPit(tmpDir.Path, pitTest, null, false, 1);
			}
		}

		[Test]
		public void ComparePublishedOutputAgainstExplicitCDATA()
		{
			const string xml = @"
<Peach>
	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action name='In' type='input'>
				<DataModel name='In'>
					<Number name='Byte' size='8'/>
				</DataModel>
			</Action>
			<Action type='slurp' valueXpath='//In//Byte' setXpath='//Out//Byte'/>
			<Action name='Out' type='output'>
				<DataModel name='Out'>
					<Number name='Byte' size='8'/>
				</DataModel>
				<Data fileName='{0}'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher name='Pub' class='Null'/>
	</Test>
</Peach>
";

			const string test = @"
<TestData pit='test.xml'>
	<Test name='Default'>
		<Open   action='TheState.Initial.In' publisher='Pub'/>
		<Input  action='TheState.Initial.In' publisher='Pub'>
<![CDATA[
0000   66                                                f               
]]>
		</Input>
		<Output action='TheState.Initial.Out' publisher='Pub'>
<![CDATA[
0000   66                                                f               
]]>
		</Output>
	</Test>
</TestData>
";

			using (var tmpDir = new TempDirectory())
			{
				var datasetFile = Path.Combine(tmpDir.Path, "data.bin");
				File.WriteAllBytes(datasetFile, new byte[] { 0x00 });

				var pitFile = Path.Combine(tmpDir.Path, "test.xml");
				var pitTest = pitFile + ".test";

				File.WriteAllText(pitFile, xml.Fmt(datasetFile));
				File.WriteAllText(pitTest, test);

				ThePitTester.TestPit(tmpDir.Path, pitTest, null, false, 1);
			}
		}

		[Test]
		public void VerifyOutputAgainstSourceDataFile()
		{
			const string xml = @"
<Peach>
	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action name='Out' type='output'>
				<DataModel name='Out'>
					<Number name='Byte' size='8'/>
				</DataModel>
				<Data fileName='{0}'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher name='Pub' class='Null'/>
	</Test>
</Peach>
";

			const string test = @"
<TestData pit='test.xml'>
	<Test name='Default'>
		<Open   action='TheState.Initial.Out' publisher='Pub'/>
		<Output action='TheState.Initial.Out' verifyAgainst='dataFile' publisher='Pub'/>
	</Test>
</TestData>
";
			using (var tmpDir = new TempDirectory())
			{
				var datasetFile = Path.Combine(tmpDir.Path, "data.bin");
				File.WriteAllBytes(datasetFile, new byte[] { 0x00 });

				var pitFile = Path.Combine(tmpDir.Path, "test.xml");
				var pitTest = pitFile + ".test";

				File.WriteAllText(pitFile, xml.Fmt(datasetFile));
				File.WriteAllText(pitTest, test);

				ThePitTester.TestPit(tmpDir.Path, pitTest, null, false, 1);
			}
		}

		[Test]
		public void DisallowOutputCDATAWhenVerifyingAgainstDataSetFile()
		{
			const string xml = @"
<Peach>
	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action name='Out' type='output'>
				<DataModel name='Out'>
					<Number name='Byte' size='8'/>
				</DataModel>
				<Data fileName='{0}'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher name='Pub' class='Null'/>
	</Test>
</Peach>
";

			const string test = @"
<TestData pit='test.xml'>
	<Test name='Default'>
		<Open   action='TheState.Initial.Out' publisher='Pub'/>
		<Output action='TheState.Initial.Out' verifyAgainst='dataFile' publisher='Pub'>
<![CDATA[
0000   66                                                f
]]>
		</Output>
	</Test>
</TestData>
";
			using (var tmpDir = new TempDirectory())
			{
				var datasetFile = Path.Combine(tmpDir.Path, "data.bin");
				File.WriteAllBytes(datasetFile, new byte[] { 0x00 });

				var pitFile = Path.Combine(tmpDir.Path, "test.xml");
				var pitTest = pitFile + ".test";

				File.WriteAllText(pitFile, xml.Fmt(datasetFile));
				File.WriteAllText(pitTest, test);

				Assert.That(() =>
				{
					ThePitTester.TestPit(tmpDir.Path, pitTest, null, false, 1);
				},
					Throws.TypeOf<AggregateException>()
						.With.InnerException.TypeOf<PeachException>()
						.With.InnerException.Message.Contains("Unexpected CDATA")
				);
			}
		}

		[Test]
		public void RequireOutputCDATAWhenVerifyingAgainstCDATA()
		{
			const string xml = @"
<Peach>
	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action name='Out' type='output'>
				<DataModel name='Out'>
					<Number name='Byte' size='8'/>
				</DataModel>
				<Data fileName='{0}'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher name='Pub' class='Null'/>
	</Test>
</Peach>
";

			const string test = @"
<TestData pit='test.xml'>
	<Test name='Default'>
		<Open   action='TheState.Initial.Out' publisher='Pub'/>
		<Output action='TheState.Initial.Out' verifyAgainst='cdata' publisher='Pub'/>
	</Test>
</TestData>
";

			using (var tmpDir = new TempDirectory())
			{
				var datasetFile = Path.Combine(tmpDir.Path, "data.bin");
				File.WriteAllBytes(datasetFile, new byte[] { 0x00 });

				var pitFile = Path.Combine(tmpDir.Path, "test.xml");
				var pitTest = pitFile + ".test";

				File.WriteAllText(pitFile, xml.Fmt(datasetFile));
				File.WriteAllText(pitTest, test);

				Assert.That(() =>
				{
					ThePitTester.TestPit(tmpDir.Path, pitTest, null, false, 1);
				},
					Throws.TypeOf<AggregateException>()
						.With.InnerException.TypeOf<PeachException>()
						.With.InnerException.Message.Contains("CDATA missing")
				);
			}
		}

		[Test]
		public void TestTextOutput()
		{
			const string xml = @"
<Peach>
	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action name='Out' type='output'>
				<DataModel name='Out'>
					<String value='Hello World' />
				</DataModel>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher name='Pub' class='Null'/>
	</Test>
</Peach>
";

			const string test = @"
<TestData pit='test.xml'>
	<Test name='Default'>
		<Open   action='TheState.Initial.Out' publisher='Pub'/>
		<Output action='TheState.Initial.Out' valueType='text' publisher='Pub'>Hello World</Output>
	</Test>
</TestData>
";

			using (var tmpDir = new TempDirectory())
			{

				var pitFile = Path.Combine(tmpDir.Path, "test.xml");
				var pitTest = pitFile + ".test";

				File.WriteAllText(pitFile, xml);
				File.WriteAllText(pitTest, test);

				ThePitTester.TestPit(tmpDir.Path, pitTest, null, false, 1);
			}
		}


		[Test]
		public void TestXmlOutput()
		{
			const string xml = @"
<Peach>
	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action name='Out' type='output'>
				<DataModel name='Out'>
					<XmlElement elementName='Foo'>
						<XmlAttribute attributeName='attr'>
							<String value='attr-value' />
						</XmlAttribute>
						<XmlElement elementName='InnerFoo' />
						<XmlElement elementName='InnerFoo'>
							<String value='Inner Text' />
						</XmlElement>
					</XmlElement>
				</DataModel>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher name='Pub' class='Null'/>
	</Test>
</Peach>
";

			const string test = @"
<TestData pit='test.xml'>
	<Test name='Default'>
		<Open   action='TheState.Initial.Out' publisher='Pub'/>
		<Output action='TheState.Initial.Out' valueType='xml' publisher='Pub'>
<![CDATA[
<Foo	attr='attr-value' >

		<InnerFoo          />
        <InnerFoo>Inner Text</InnerFoo>

</Foo>
]]>
		</Output>
	</Test>
</TestData>
";

			using (var tmpDir = new TempDirectory())
			{

				var pitFile = Path.Combine(tmpDir.Path, "test.xml");
				var pitTest = pitFile + ".test";

				File.WriteAllText(pitFile, xml);
				File.WriteAllText(pitTest, test);

				ThePitTester.TestPit(tmpDir.Path, pitTest, null, false, 1);
			}
		}

		[Test]
		public void TestRunMultipleTestDefinitions()
		{
			const string xml = @"
<Peach>
	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action name='In' type='input'>
				<DataModel name='In'>
					<Number name='Byte' size='8'/>
				</DataModel>
			</Action>
			<Action type='slurp' valueXpath='//In//Byte' setXpath='//Out//Byte'/>
			<Action name='Out' type='output'>
				<DataModel name='Out'>
					<Number name='Byte' size='8'/>
				</DataModel>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher name='Pub' class='Null'/>
	</Test>
</Peach>
";

			const string test = @"
<TestData pit='test.xml'>
	<Test name='Default'>
		<Open   action='TheState.Initial.In' publisher='Pub'/>
		<Input  action='TheState.Initial.In' publisher='Pub'>
<![CDATA[
0000   66                                                f               
]]>
		</Input>
		<Output action='TheState.Initial.Out' publisher='Pub'>
<![CDATA[
0000   66                                                f               
]]>
		</Output>
	</Test>
<Test name='Default'>
		<Open   action='TheState.Initial.In' publisher='Pub'/>
		<Input  action='TheState.Initial.In' publisher='Pub'>
<![CDATA[
0000   65                                                e               
]]>
		</Input>
		<Output action='TheState.Initial.Out' publisher='Pub'>
<![CDATA[
0000   65                                                e               
]]>
		</Output>
	</Test>
</TestData>
";
			using (var tmpDir = new TempDirectory())
			{

				var pitFile = Path.Combine(tmpDir.Path, "test.xml");
				var pitTest = pitFile + ".test";

				File.WriteAllText(pitFile, xml);
				File.WriteAllText(pitTest, test);

				ThePitTester.TestPit(tmpDir.Path, pitTest, null, false, 1);
			}
		}
	}
}
