using System;
using System.Collections.Generic;
using System.IO;
using NLog;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.IO;
using Peach.Core.Publishers;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Dom
{
	[TestFixture]
	[Quick]
	class VarNumberTests : DataModelCollector
	{
		class TestPublisher : StreamPublisher
		{
			private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
			protected override NLog.Logger Logger { get { return logger; } }

			public List<BitwiseStream> Outputs = new List<BitwiseStream>();
			public List<MemoryStream> InputData = new List<MemoryStream>();
			public int InputDataPosition = 0;

			public TestPublisher()
				: base(new Dictionary<string, Variant>())
			{
				Name = "Pub";
			}

			protected override void OnOpen()
			{
				if (InputData.Count > 0)
				{
					InputDataPosition = 0;
					stream = InputData[InputDataPosition];
					stream.Position = 0;
				}
			}

			protected override void OnClose()
			{
				if (InputData.Count > 0)
				{
					InputDataPosition = 0;
					stream = InputData[InputDataPosition];
					stream.Position = 0;
				}
			}

			protected override void OnOutput(BitwiseStream data)
			{
				Outputs.Add(data);
			}

			protected override void OnInput()
			{
				if (InputData.Count <= InputDataPosition)
					throw new SoftException("Out of input data!");

				stream = InputData[InputDataPosition];
				stream.Position = 0;
				base.OnInput();

				InputDataPosition++;
			}
		}

		[Test]
		[Category("Peach")]
		public void OutputSizeAndValueTest()
		{
			var xml =
				"<?xml version='1.0' encoding='utf-8'?>\n" +
				"<Peach>\n" +
				" <DataModel name='Example1'>\n" +
				"    <VarNumber name='byte0'  value='0' /> " +
				"    <VarNumber name='byte1'  value='1' /> " +
				"    <VarNumber name='short' value='32766' /> " +
				"    <VarNumber name='int'   value='2147483647' /> " +
				"    <VarNumber name='long'  value='223372036854775807' /> " +
				"  </DataModel>\n" +
				"  \n" +
				"  <StateModel name='TheStateModel' initialState='initial'>\n" +
				"    <State name='initial'>\n" +
				"      <Action type='outfrag'>\n" +
				"        <DataModel ref='Example1'/>\n" +
				"      </Action>\n" +
				"    </State>\n" +
				"  </StateModel>\n" +
				"  \n" +
				"  <Test name='Default' maxOutputSize='200'>\n" +
				"\t\t<StateModel ref='TheStateModel'/>\n" +
				"\t\t<Publisher class='Null'/>\n" +
				"\t</Test>\n" +
				"</Peach>\n";

			var dom = ParsePit(xml);
			var elemByte0 = dom.dataModels["Example1"][0];
			var elemByte1 = dom.dataModels["Example1"][1];
			var elemShort = dom.dataModels["Example1"][2];
			var elemInt = dom.dataModels["Example1"][3];
			var elemLong = dom.dataModels["Example1"][4];

			Assert.AreEqual(1, elemByte0.Value.Length);
			Assert.AreEqual(1, elemByte1.Value.Length);
			Assert.AreEqual(2, elemShort.Value.Length);
			Assert.AreEqual(4, elemInt.Value.Length);
			Assert.AreEqual(8, elemLong.Value.Length);

			Assert.AreEqual(0u, (ulong)elemByte0.DefaultValue);
			Assert.AreEqual(1u, (ulong)elemByte1.DefaultValue);
			Assert.AreEqual(32766u, (ulong)elemShort.DefaultValue);
			Assert.AreEqual(2147483647u, (ulong)elemInt.DefaultValue);
			Assert.AreEqual(223372036854775807u, (ulong)elemLong.DefaultValue);
		}

		[Test]
		[Category("Peach")]
		public void InputBytesTest()
		{
			var xml =
				"<?xml version='1.0' encoding='utf-8'?>\n" +
				"<Peach>\n" +
				" <DataModel name='Example1'>\n" +
				"	 <Number size='8'>" +
				"       <Relation type='size' of='short' />"+
				"    </Number>"+
				"    <VarNumber name='short'  value='1' /> " +
				"  </DataModel>\n" +
				"  \n" +
				"  <StateModel name='TheStateModel' initialState='initial'>\n" +
				"    <State name='initial'>\n" +
				"      <Action type='input'>\n" +
				"        <DataModel ref='Example1'/>\n" +
				"      </Action>\n" +
				"    </State>\n" +
				"  </StateModel>\n" +
				"  \n" +
				"  <Test name='Default' maxOutputSize='200'>\n" +
				"\t\t<StateModel ref='TheStateModel'/>\n" +
				"\t\t<Publisher class='Null'/>\n" +
				"\t</Test>\n" +
				"</Peach>\n";

			var dom = ParsePit(xml);
			dom.tests[0].publishers[0] = new TestPublisher();
			var pub = (TestPublisher)dom.tests[0].publishers[0];

			pub.InputData.Add(new MemoryStream());

			var bs = new BitStream();
			var writer = new BitWriter(bs);
			writer.WriteByte(2);
			writer.WriteByte(0x64);
			writer.WriteByte(0xc8);
			bs.Position = 0;
			bs.CopyTo(pub.InputData[0]);

			var config = new RunConfiguration();
			config.singleIteration = true;

			var e = new Engine(null);

			e.startFuzzing(dom, config);

			var elemShort = dom.tests[0].stateModel.states[0].actions[0].dataModel[1];

			Assert.AreEqual(0x64c8, (int)elemShort.DefaultValue);
		}

		[Test]
		[Category("Peach")]
		public void SlurpTest()
		{
			var xml =
				"<?xml version='1.0' encoding='utf-8'?>\n" +
				"<Peach>\n" +
				" <DataModel name='Example1'>\n" +
				"    <VarNumber name='byte'  value='1' /> " +
				"    <VarNumber name='short' value='32766' /> " +
				"    <VarNumber name='int'   value='2147483647' /> " +
				"    <VarNumber name='long'  value='223372036854775807' /> " +
				"  </DataModel>\n" +
				"  \n" +
				"  <StateModel name='TheStateModel' initialState='initial'>\n" +
				"    <State name='initial'>\n" +
				"      <Action name='a1' type='output'>\n" +
				"        <DataModel ref='Example1'/>\n" +
				"      </Action>\n" +
				"	   <Action type='slurp' valueXpath='//a1//short' setXpath='//a2//long' />"+
				"      <Action name='a2' type='output'>\n" +
				"        <DataModel ref='Example1'/>\n" +
				"      </Action>\n" +
				"    </State>\n" +
				"  </StateModel>\n" +
				"  \n" +
				"  <Test name='Default' maxOutputSize='200'>\n" +
				"\t\t<StateModel ref='TheStateModel'/>\n" +
				"\t\t<Publisher class='Null'/>\n" +
				"\t</Test>\n" +
				"</Peach>\n";

			var dom = ParsePit(xml);
			dom.tests[0].publishers[0] = new TestPublisher();
			var pub = (TestPublisher)dom.tests[0].publishers[0];

			pub.InputData.Add(new MemoryStream());

			var bs = new BitStream();
			var writer = new BitWriter(bs);
			writer.WriteByte(100);
			bs.Position = 0;
			bs.CopyTo(pub.InputData[0]);

			var config = new RunConfiguration();
			config.singleIteration = true;

			var e = new Engine(null);

			e.startFuzzing(dom, config);

			var elemShort = dom.tests[0].stateModel.states[0].actions[0].dataModel[1];
			var elemLong = dom.tests[0].stateModel.states[0].actions[2].dataModel[3];

			Assert.AreEqual((int)elemShort.DefaultValue, (int)elemLong.DefaultValue);
		}

		static string BitewiseStreamToString(BitwiseStream stream)
		{
			var buff = new byte[stream.Length];

			stream.Position = 0;
			stream.Read(buff, 0, buff.Length);

			return UTF8Encoding.UTF8.GetString(buff);
		}
	}
}
