using System.Collections.Generic;
using NUnit.Framework;

namespace Peach.Core.Test
{
	[TestFixture]
	[Peach]
	[Quick]
	public class ScriptingTests
	{
		[Test]
		[TestCase((byte)5)]
		[TestCase((sbyte)5)]
		[TestCase((ushort)5)]
		[TestCase((short)5)]
		[TestCase(5)]
		public void TestInt(object arg)
		{
			var python = new PythonScripting();

			var ret = python.Eval("ret + 1", new Dictionary<string, object>
			{
				{ "ret", arg }
			});

			Assert.AreEqual(6, ret);
			Assert.That(ret, Is.InstanceOf<int>());
		}

		[Test]
		[TestCase((uint)5)]
		[TestCase((long)5)]
		public void TestLong(object arg)
		{
			var python = new PythonScripting();

			var ret = python.Eval("ret + 1", new Dictionary<string, object>
			{
				{ "ret", arg }
			});

			Assert.AreEqual(6, ret);
			Assert.That(ret, Is.InstanceOf<long>());
		}

		[Test]
		public void TestUlongAdd()
		{
			var python = new PythonScripting();

			var ret = python.Eval("ret + 6", new Dictionary<string, object>
			{
				{ "ret", (ulong)0 }
			});

			Assert.That(ret, Is.InstanceOf<ulong>());

			Assert.AreEqual(6, ret);
		}

		[Test]
		public void TestUlongSubtract()
		{
			var python = new PythonScripting();

			var ret = python.Eval("ret - 6", new Dictionary<string, object>
			{
				{ "ret", (ulong)0 }
			});

			Assert.That(ret, Is.InstanceOf<long>());

			Assert.AreEqual(-6, ret);
		}

		[Test]
		public void TestUlongOverflow()
		{
			var python = new PythonScripting();

			Assert.Throws<SoftException>(() =>
				python.Eval("ret + 1", new Dictionary<string, object>
				{
					{ "ret", ulong.MaxValue }
				}));
		}

		[Test]
		public void TestLongUnderflow()
		{
			var python = new PythonScripting();

			Assert.Throws<SoftException>(() =>
				python.Eval("ret - 1", new Dictionary<string, object>
				{
					{ "ret", long.MinValue }
				}));
		}

		[Test]
		public void Test64bitConstant()
		{
			var python = new PythonScripting();

			var ret = python.Eval("0xB00000001B692 | 0x100000000000000", new Dictionary<string, object>());

			Assert.That(ret, Is.InstanceOf<ulong>());

			Assert.AreEqual(0xB00000001B692 | 0x100000000000000, ret);
		}
	}
}