using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.IO;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Transformers
{
	[TestFixture]
	[Quick]
	[Peach]
	class TruncateTests : DataModelCollector
	{
		[Test]
		public void BlobTest()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<Blob name='Data' value='Hello'>
			<Transformer class='Truncate'>
				<Param name='Length' value='3' />
			</Transformer>
		</Blob>
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
		<Publisher class='Null'/>
		<Strategy class='Random' />
	</Test>
</Peach>
";

			RunEngine(xml, true);

			var expected = Encoding.ASCII.GetBytes("Hel");
			Assert.AreEqual(1, values.Count);
			var actual = values[0].ToArray();
			Console.WriteLine(BitConverter.ToString(actual));
			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void OffsetTest()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='Data' value='Hello'>
			<Transformer class='Truncate'>
				<Param name='Length' value='3' />
				<Param name='Offset' value='1' />
			</Transformer>
		</String>
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
		<Publisher class='Null'/>
		<Strategy class='Random' />
	</Test>
</Peach>
";

			RunEngine(xml, true);

			var expected = Encoding.ASCII.GetBytes("ell");
			Assert.AreEqual(1, values.Count);
			Assert.AreEqual(expected, values[0].ToArray());
		}

		[Test]
		public void LargeLengthTest()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String name='Data' value='Hello'>
			<Transformer class='Truncate'>
				<Param name='Length' value='10' />
				<Param name='Offset' value='2' />
			</Transformer>
		</String>
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
		<Publisher class='Null'/>
		<Strategy class='Random' />
	</Test>
</Peach>
";

			var dom = ParsePit(xml);

			var config = new RunConfiguration();
			config.singleIteration = true;

			var e = new Engine(this);
			e.startFuzzing(dom, config);

			var expected = Encoding.ASCII.GetBytes("llo");
			Assert.AreEqual(1, values.Count);
			Assert.AreEqual(expected, values[0].ToArray());
		}

		[Test]
		public void MissingParam()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String>
			<Transformer class='Truncate'/>
		</String>
	</DataModel>
</Peach>
";

			var ex = Assert.Throws<PeachException>(() => ParsePit(xml));
			StringAssert.StartsWith("Error, Transformer 'Truncate' is missing required parameter 'Length'.", ex.Message);
		}

		[Test]
		public void CrackTest()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String>
			<Transformer class='Truncate'>
				<Param name='Length' value='1' />
			</Transformer>
		</String>
	</DataModel>
</Peach>
";

			var dom = ParsePit(xml);
			var data = Bits.Fmt("{0}", (byte)'0');
			var cracker = new DataCracker();
			Assert.Throws<NotImplementedException>(() => cracker.CrackData(dom.dataModels[0], data));
		}

		[Test]
		public void CrackBadLengthTest()
		{
			string xml = @"
<Peach>
	<DataModel name='DM'>
		<String/>
		<Transformer class='Hex'/>
	</DataModel>
</Peach>
";

			var dom = ParsePit(xml);
			var data = Bits.Fmt("{0}", (byte)'0');
			var cracker = new DataCracker();
			var ex = Assert.Throws<SoftException>(() => cracker.CrackData(dom.dataModels[0], data));
			Assert.AreEqual("Hex decode failed, invalid length.", ex.Message);
		}
	}
}
