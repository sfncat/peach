using NUnit.Framework;
using Peach.Core.Dom;
using Peach.Core.Test;
using Peach.Pro.Core.Dom;

namespace Peach.Pro.Test.Core.Dom
{
	[TestFixture]
	[Quick]
	class BoolTests : DataModelCollector
	{
		[Test]
		[Category("Peach")]
		public void SimpleTest()
		{
			var b = new Bool();

			Assert.AreEqual(1, b.lengthAsBits);
			Assert.AreEqual(LengthType.Bits, b.lengthType);
		}
	}
}
