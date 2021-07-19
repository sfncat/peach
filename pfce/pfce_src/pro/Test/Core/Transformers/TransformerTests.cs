using NUnit.Framework;
using Peach.Core;
using Peach.Core.Cracker;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Transformers
{
	[TestFixture]
	[Quick]
	[Peach]
	class TransformerTests
	{
		[Test]
		public void TwoTransformers()
		{
			const string xml = @"
				<Peach>
					<DataModel name='TheDataModel'>
						<String name='str' value='Hello World'>
							<Transformer class='Null'/>
							<Transformer class='Null'/>
						</String>
					</DataModel>
				</Peach>";

			var ex = Assert.Throws<PeachException>(() => DataModelCollector.ParsePit(xml));
			Assert.AreEqual("Error, multiple transformers are defined on element 'str'.", ex.Message);
		}

		[Test]
		public void BadNestedTransformers()
		{
			const string xml = @"
				<Peach>
					<DataModel name='TheDataModel'>
						<String name='str' value='127.0.0.1'>
							<Transformer class='Hex'>
								<Transformer class='Null'/>
								<Transformer class='Ipv4StringToOctet'/>
							</Transformer>
						</String>
					</DataModel>
				</Peach>";

			var ex = Assert.Throws<PeachException>(() => DataModelCollector.ParsePit(xml));
			Assert.AreEqual("Error, multiple nested transformers are defined on element 'str'.", ex.Message);
		}

		[Test]
		public void NestedTransformers()
		{
			const string xml = @"
				<Peach>
					<DataModel name='TheDataModel'>
						<String name='str' value='127.0.0.1'>
							<Transformer class='Null'>
								<Transformer class='Ipv4StringToOctet'>
									<Transformer class='Hex'/>
								</Transformer>
							</Transformer>
						</String>
					</DataModel>
				</Peach>";

			var dom = DataModelCollector.ParsePit(xml);
			var actual = dom.dataModels[0].Value.ToArray();
			var expected = Encoding.ASCII.GetBytes("7f000001"); // 127.0.0.1
			Assert.AreEqual(expected, actual);

			var data = Bits.Fmt("{0}", Encoding.ASCII.GetBytes("0a01ff02")); //10.1.55.2

			var cracker = new DataCracker();
			cracker.CrackData(dom.dataModels[0], data);

			var value = (string)dom.dataModels[0][0].DefaultValue;
			Assert.AreEqual("10.1.255.2", value);
		}
	}
}
