using NUnit.Framework;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Dom
{
	[TestFixture]
	[Quick]
	class DoubleTests : DataModelCollector
	{
		[Test]
		[Category("Peach")]
		public void SimpleInteralValueTest1()
		{
			var db = new Peach.Core.Dom.Double();

			var actual = (double)db.InternalValue;
			const double expected = 0.0;
			Assert.AreEqual(expected, actual);
			Assert.AreEqual(64, db.length);
		}

		[Test]
		[Category("Peach")]
		public void SimpleInteralValueTest2()
		{
			var db = new Peach.Core.Dom.Double
			{
				length = 32
			};

			var actual = (double)db.InternalValue;
			const double expected = 0.0;
			Assert.AreEqual(expected, actual);
			Assert.AreEqual(32, db.length);
		}

		[Test]
		[Category("Peach")]
		public void SimpleInteralValueTest3()
		{
			var db = new Peach.Core.Dom.Double
			{
				length = 32, DefaultValue = new Peach.Core.Variant(1.0E+3)
			};

			var actual = (double)db.InternalValue;
			const double expected = 1000.0;
			Assert.AreEqual(expected, actual);
			Assert.AreEqual(32, db.length);
		}

		[Test]
		[Category("Peach")]
		public void SimpleInteralValueTest4()
		{
			var db = new Peach.Core.Dom.Double
			{
				length = 64, 
				LittleEndian = false, 
				DefaultValue = new Peach.Core.Variant(1.0)
			};

			var actual = (double)db.InternalValue;
			const double expected = 1.0;
			Assert.AreEqual(expected, actual);
			Assert.AreEqual(64, db.length);
		}

	}
}
