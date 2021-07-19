using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using Peach.Core.Test;


// Disable obsolete warnings so we can test PitParser.parseDefines()
#pragma warning disable 618

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	[Quick]
	[Peach]
	class RunTests
	{
		[Test]
		public void BadDataModelNoName()
		{
			const string xml = @"
<Peach>
<DataModel ref='foo'/>
</Peach>";

			var ex = Assert.Throws<PeachException>(() => DataModelCollector.ParsePit(xml));
			Assert.AreEqual("Error, DataModel could not resolve ref 'foo'. XML:\n<DataModel ref=\"foo\" />", ex.Message);
		}

		[Test]
		public void BadDataModelName()
		{
			const string xml = @"
<Peach>
<DataModel name='DM' ref='foo'/>
</Peach>";

			var ex = Assert.Throws<PeachException>(() => DataModelCollector.ParsePit(xml));
			Assert.AreEqual("Error, DataModel 'DM' could not resolve ref 'foo'. XML:\n<DataModel name=\"DM\" ref=\"foo\" />", ex.Message);
		}

		[Test]
		public void BadBlockRef()
		{
			const string xml = @"
<Peach>
	<DataModel name='Header'>
		<String name='Header'/>
	</DataModel>

	<DataModel name='Final'>
		<Block name='H1' ref='Header'/>
		<Block name='H2' ref='Header'/>
	</DataModel>
</Peach>";
			
			var ex = Assert.Throws<PeachException>(() => DataModelCollector.ParsePit(xml));
			Assert.AreEqual("Error, Block 'H2' resolved ref 'Header' to unsupported element String 'Final.H1.Header'. XML:\n<Block name=\"H2\" ref=\"Header\" />", ex.Message);
		}

		[Test]
		public void MultipleFields()
		{
			const string xml = @"
<Peach>
	<Data>
		<Field name='foo' value='bar'/>
		<Field name='foo' value='bar'/>
	</Data>
</Peach>";

			var ex = Assert.Throws<PeachException>(() => DataModelCollector.ParsePit(xml));
			Assert.AreEqual("Error, Data element has multiple entries for field 'foo'.", ex.Message);
		}

		[Test]
		public void MultipleFieldsRef()
		{
			const string xml = @"
<Peach>
	<Data name='Base'>
		<Field name='foo' value='bar'/>
	</Data>

	<Data name='Derived' ref='Base'>
		<Field name='foo' value='baz'/>
	</Data>
</Peach>";

			PitParser parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));
			Assert.AreEqual(2, dom.datas.Count);
			Assert.AreEqual(1, dom.datas[0].Count);
			Assert.AreEqual(1, dom.datas[1].Count);
			Assert.True(dom.datas[0][0] is DataField);
			Assert.True(dom.datas[1][0] is DataField);
			Assert.AreEqual(1, ((DataField)dom.datas[0][0]).Fields.Count);
			Assert.AreEqual("bar", (string)((DataField)dom.datas[0][0]).Fields[0].Value);
			Assert.AreEqual(1, ((DataField)dom.datas[1][0]).Fields.Count);
			Assert.AreEqual("baz", (string)((DataField)dom.datas[1][0]).Fields[0].Value);
		}

		[Test]
		public void ParseDefines()
		{
			string temp1 = Path.GetTempFileName();
			string temp2 = Path.GetTempFileName();

			string def1 = @"
<PitDefines>
	<All>
		<Define key='k1' value='v1'/>
		<Define key='k2' value='v2'/>
	</All>
</PitDefines>
";

			string def2 = @"
<PitDefines>
	<Include include='{0}'/>

	<All>
		<Define key='k1' value='override'/>
		<Define key='k3' value='v3'/>
	</All>
</PitDefines>
".Fmt(temp1);

			File.WriteAllText(temp1, def1);
			File.WriteAllText(temp2, def2);

			var defs = PitParser.parseDefines(temp2);

			Assert.AreEqual(3, defs.Count);
			Assert.AreEqual("k1", defs[0].Key);
			Assert.AreEqual("k2", defs[1].Key);
			Assert.AreEqual("k3", defs[2].Key);
			Assert.AreEqual("override", defs[0].Value);
			Assert.AreEqual("v2", defs[1].Value);
			Assert.AreEqual("v3", defs[2].Value);
		}

		[Test]
		public void ParseDefinesDuplicate()
		{
			string temp1 = Path.GetTempFileName();
			string def1 = @"
<PitDefines>
	<All>
		<Define key='k1' value='v1'/>
		<Define key='k1' value='v2'/>
	</All>
</PitDefines>
";

			File.WriteAllText(temp1, def1);

			try
			{
				PitParser.parseDefines(temp1);
				Assert.Fail("should throw");
			}
			catch (PeachException ex)
			{
				Assert.True(ex.Message.EndsWith("contains multiple entries for key 'k1'."));
			}
		}

		[Test]
		public void ParseDefinesFileNotFound()
		{
			var ex = Assert.Throws<PeachException>(() => PitParser.parseDefines("filenotfound.xml"));
			Assert.AreEqual("Error, defined values file \"filenotfound.xml\" does not exist.", ex.Message);
		}

		[Test]
		public void TestMissingData()
		{
			string xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String/>
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
				<Data fileName='missing.txt'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
	</Test>
</Peach>";

			var ex = Assert.Throws<PeachException>(() => DataModelCollector.ParsePit(xml));
			Assert.AreEqual("Error parsing Data element, file or folder does not exist: missing.txt", ex.Message);
		}

		[Test]
		public void TestFieldAndFiles()
		{
			using (var dir = new TempDirectory())
			{
				var xml = @"
<Peach>
	<DataModel name='TheDataModel'>
		<String name='key'/>
		<String value=':' token='true'/>
		<String name='value' />
	</DataModel>

	<StateModel name='TheState' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel ref='TheDataModel'/>
				<Data fileName='{0}'>
					<Field name='key' value='key_override' />
				</Data>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='TheState'/>
		<Publisher class='Null'/>
		<Strategy class='Random'>
			<Param name='SwitchCount' value='2'/>
		</Strategy>
	</Test>
</Peach>".Fmt(dir.Path);

				File.WriteAllText(Path.Combine(dir.Path, "one.txt"), "aaa_key:aaa_value");
				File.WriteAllText(Path.Combine(dir.Path, "two.txt"), "bbb_key:bbb_value");

				var dom = DataModelCollector.ParsePit(xml);
				var config = new RunConfiguration
				{
					range = true,
					rangeStart = 1,
					rangeStop = 10,
					randomSeed = 1
				};
				var e = new Engine(null);

				var selected = new List<string>();

				e.IterationFinished += (ctx, it) =>
				{
					if (ctx.controlRecordingIteration)
						selected.Add(ctx.test.stateModel.states[0].actions[0].dataModel.InternalValue.BitsToString());
				};

				e.startFuzzing(dom, config);

				Assert.True(selected.Contains("key_override:aaa_value"), "Field aaa_key should be overridden");
				Assert.True(selected.Contains("key_override:bbb_value"), "Field bb_key should be overridden");
			}
		}

		internal class WantBytesPub : Peach.Core.Publishers.StreamPublisher
		{
			static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

			protected override NLog.Logger Logger
			{
				get { return logger; }
			}


			public WantBytesPub()
				: base(new Dictionary<string, Variant>())
			{
				Name = "Pub";
				stream = new MemoryStream();
			}

			public override void WantBytes(long count)
			{
				if (stream.Length == 0)
				{
					stream.Write(Encoding.ASCII.GetBytes("12345678"), 0, 8);
					stream.Seek(0, SeekOrigin.Begin);
				}
			}
		}

		[Test]
		public void WantBytes()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Blob/>
	</DataModel>

	<StateModel name='SM' initialState='initial'>
		<State name='initial'>
			<Action type='input'>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
	</Test>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));
			dom.tests[0].publishers[0] = new WantBytesPub();

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(null);
			e.startFuzzing(dom, config);

			var value = dom.tests[0].stateModel.states["initial"].actions[0].dataModel.Value;
			Assert.AreEqual(8, value.Length);
		}

		[Test]
		public void ArrayDisable()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Number name='num' size='32' minOccurs='0' occurs='2' value='1'/>
		<String name='str'/>
	</DataModel>

	<StateModel name='StateModel' initialState='State1'>
		<State name='State1'>
			<Action type='output'>
				<DataModel ref='DM'/>
				<Data >
					<Field name='num[-1]' value='' />
					<Field name='str' value='Hello World' />
				</Data>
			</Action>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
	</Test>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(null);
			e.startFuzzing(dom, config);

			var value1 = dom.tests[0].stateModel.states["State1"].actions[0].dataModel.Value;
			var exp1 = Bits.Fmt("{0}", "Hello World");
			Assert.AreEqual(exp1.ToArray(), value1.ToArray());

			var value2 = dom.tests[0].stateModel.states["State1"].actions[1].dataModel.Value;
			var exp2 = Bits.Fmt("{0:L32}{1:L32}", 1, 1);
			Assert.AreEqual(exp2.ToArray(), value2.ToArray());
		}

		[Test]
		public void ArrayOverride()
		{
			string xml = @"
<Peach>
	<DataModel name='ArrayTest'>
		<Blob name='Data' minOccurs='3' maxOccurs='5' length='2' value='44 44' valueType='hex'  /> 
	</DataModel>

	<StateModel name='StateModel' initialState='State1'>
		<State name='State1'>
			<Action type='output'>
				<DataModel ref='ArrayTest'/>
				<Data >
					<Field name='Data[2]' value='41 41' valueType='hex' />
					<Field name='Data[1]' value='42 42' valueType='hex' />
					<Field name='Data[0]' value='45 45' valueType='hex'/>
				</Data>
			</Action> 
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='StateModel'/>
		<Publisher class='Null'/>
	</Test>
</Peach>";

			PitParser parser = new PitParser();
			Peach.Core.Dom.Dom dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			RunConfiguration config = new RunConfiguration();
			config.singleIteration = true;

			Engine e = new Engine(null);
			e.startFuzzing(dom, config);

			var value = dom.tests[0].stateModel.states["State1"].actions[0].dataModel.Value;
			Assert.AreEqual(6, value.Length);

			var expected = new byte[] { 0x45, 0x45, 0x42, 0x42, 0x41, 0x41 };
			Assert.AreEqual(expected, value.ToArray());
		}

		[Test]
		public void MissingPublisher()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<Blob/> 
	</DataModel>

	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output' publisher='Bad'>
				<DataModel ref='DM'/>
			</Action> 
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
	</Test>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(ASCIIEncoding.ASCII.GetBytes(xml)));

			var config = new RunConfiguration();
			config.pitFile = "MissingPublisher";
			config.singleIteration = true;

			var e = new Engine(null);
			var ex = Assert.Throws<PeachException>(() => e.startFuzzing(dom, config));
			Assert.AreEqual("Error, Action 'Action' couldn't find publisher named 'Bad'.", ex.Message);
		}

		[Test]
		public void SingleIterationSkipTo()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<Blob/>
	</DataModel>

	<StateModel name='SM' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
	</Test>
</Peach>";

			var parser = new PitParser();
			var dom = parser.asParser(null, new MemoryStream(Encoding.ASCII.GetBytes(xml)));

			var config = new RunConfiguration
			{
				singleIteration = true,
				skipToIteration = 7,
			};

			var ran = false;

			var e = new Engine(null);

			e.IterationStarting += (c, i, t) =>
			{
				ran = true;
				Assert.AreEqual(7, i);
				Assert.True(c.controlIteration);
			};

			e.startFuzzing(dom, config);

			Assert.True(ran);
		}

		[Test]
		public void TestRunDuration()
		{
			var totalIterations = 0;
			Exception caught = null;

			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='Value' value='Hello World' />
	</DataModel>

	<StateModel name='SM' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
	</Test>
</Peach>";

			var th = new Thread(() =>
			{
				try
				{
				var dom = DataModelCollector.ParsePit(xml);
				var cfg = new RunConfiguration
				{
					Duration = TimeSpan.FromSeconds(2)
				};

				var e = new Engine(null);

				e.IterationFinished += (ctx, i) =>
				{
					++totalIterations;
				};

				e.startFuzzing(dom, cfg);
					
				}
				catch (Exception ex)
				{
					caught = ex;
				}
			});

			th.Start();

			if (!th.Join(TimeSpan.FromSeconds(10)))
			{
				th.Abort();
				Assert.Fail("Engine did not copmlete within 10 seconds");
			}

			Assert.Null(caught);

			Assert.Greater(totalIterations, 10);
		}

		[Test]
		public void TestRunDurationAbort()
		{
			var totalIterations = 0;
			Exception caught = null;

			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='Value' value='Hello World' />
	</DataModel>

	<StateModel name='SM' initialState='initial'>
		<State name='initial'>
			<Action type='output'>
				<DataModel ref='DM'/>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM'/>
		<Publisher class='Null'/>
	</Test>
</Peach>";

			var th = new Thread(() =>
			{
				try
				{
					var dom = DataModelCollector.ParsePit(xml);
					var cfg = new RunConfiguration
					{
						Duration = TimeSpan.FromSeconds(1),
						AbortTimeout = TimeSpan.FromSeconds(1)
					};

					var e = new Engine(null);

					e.IterationStarting += (ctx, i, t) =>
					{
						Thread.Sleep(TimeSpan.FromSeconds(30));
						++totalIterations;
					};

					e.startFuzzing(dom, cfg);

				}
				catch (Exception ex)
				{
					caught = ex;
				}
			});

			th.Start();

			if (!th.Join(TimeSpan.FromSeconds(10)))
			{
				th.Abort();
				Assert.Fail("Engine did not copmlete within 10 seconds");
			}

			Assert.Null(caught);

			Assert.AreEqual(totalIterations, 0);
		}
	}
}
