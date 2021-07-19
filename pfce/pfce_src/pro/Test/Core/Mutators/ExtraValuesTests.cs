using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Mutators
{
	[TestFixture]
	[Quick]
	[Peach]
	class ExtraValuesTests
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("ExtraValues");

			var hintEmpty = new Hint("ExtraValues", "");
			var hintValid = new Hint("ExtraValues", "foo;bar;baz");

			var blob = new Peach.Core.Dom.Blob("Blob");
			Assert.False(runner.IsSupported(blob));

			blob.Hints["ExtraValues"] = hintEmpty;
			Assert.False(runner.IsSupported(blob));

			blob.Hints["ExtraValues"] = hintValid;
			Assert.True(runner.IsSupported(blob));

			var num = new Peach.Core.Dom.Number("Number");
			Assert.False(runner.IsSupported(num));

			num.Hints["ExtraValues"] = hintEmpty;
			Assert.False(runner.IsSupported(num));

			num.Hints["ExtraValues"] = hintValid;
			Assert.True(runner.IsSupported(num));

			var str = new Peach.Core.Dom.String("String");
			Assert.False(runner.IsSupported(str));

			str.Hints["ExtraValues"] = hintEmpty;
			Assert.False(runner.IsSupported(str));

			str.Hints["ExtraValues"] = hintValid;
			Assert.True(runner.IsSupported(str));

			var blk = new Peach.Core.Dom.Block("Block");
			Assert.False(runner.IsSupported(blk));

			blk.Hints["ExtraValues"] = hintEmpty;
			Assert.False(runner.IsSupported(blk));

			blk.Hints["ExtraValues"] = hintValid;
			Assert.False(runner.IsSupported(blk));
		}

		[Test]
		public void TestValidValues()
		{
			var runner = new MutatorRunner("ExtraValues");

			// Empty ValidValues hint is unsupported
			var str = new Peach.Core.Dom.String("String");
			str.Hints["ValidValues"] = new Hint("ValidValues", ""); ;
			Assert.False(runner.IsSupported(str));

			// Non-empty ValidValues hint is supported
			str.Hints["ValidValues"] = new Hint("ValidValues", "one;two;three"); ;
			Assert.True(runner.IsSupported(str));

			var m1 = runner.Sequential(str);
			Assert.AreEqual(3, m1.Count()); // ValidValues hint has 3 mutations

			var vals = m1.Select(i => Encoding.ASCII.GetString(i.Value.ToArray())).ToArray();
			var expexted = new string[] { "one", "two", "three" };

			Assert.AreEqual(expexted, vals);

			str.Hints["ExtraValues"] = new Hint("ValidValues", "www;xxx;yyy;zzz"); ;

			var m2 = runner.Sequential(str);

			// ExtraValues hint trumps ValidValues hint
			Assert.AreEqual(4, m2.Count());

			vals = m2.Select(i => Encoding.ASCII.GetString(i.Value.ToArray())).ToArray();
			expexted = new string[] { "www", "xxx", "yyy", "zzz" };

			Assert.AreEqual(expexted, vals);
		}

		[Test]
		public void TestSequential()
		{
			var runner = new MutatorRunner("ExtraValues");

			var hint = new Hint("ExtraValues", "111;222;333;444");

			var str = new Peach.Core.Dom.String();
			str.Hints[hint.Name] = hint;

			var m1 = runner.Sequential(str);
			Assert.AreEqual(4, m1.Count());

			var v1 = m1.Select(i => Encoding.ASCII.GetString(i.Value.ToArray())).ToArray();
			var e1 = new string[] { "111", "222", "333", "444" };
			Assert.AreEqual(e1, v1);

			var num = new Peach.Core.Dom.Number() { LittleEndian = false, length = 32 };
			num.Hints[hint.Name] = hint;

			var m2 = runner.Sequential(num);
			Assert.AreEqual(4, m2.Count());

			var v2 = m2.Select(i => Endian.Big.GetInt32(i.Value.ToArray(), 32)).ToArray();
			var e2 = new int[] { 111, 222, 333, 444 };
			Assert.AreEqual(e2, v2);

			var blob = new Peach.Core.Dom.Blob();
			blob.Hints[hint.Name] = hint;

			var m3 = runner.Sequential(blob);
			Assert.AreEqual(4, m3.Count());

			var v3 = m3.Select(i => Encoding.ASCII.GetString(i.Value.ToArray())).ToArray();
			var e3 = new string[] { "111", "222", "333", "444" };
			Assert.AreEqual(e3, v3);
		}

		[Test]
		public void TestRandom()
		{
			var runner = new MutatorRunner("ExtraValues");

			var hint = new Hint("ExtraValues", "111;222;333;444");

			var str = new Peach.Core.Dom.String();
			str.Hints[hint.Name] = hint;

			var m1 = runner.Random(100, str);
			Assert.AreEqual(100, m1.Count());

			var v1 = m1.Select(i => Encoding.ASCII.GetString(i.Value.ToArray())).Total();

			Assert.AreEqual(4, v1.Keys.Count);

			Assert.True(v1.ContainsKey("111"));
			Assert.True(v1.ContainsKey("222"));
			Assert.True(v1.ContainsKey("333"));
			Assert.True(v1.ContainsKey("444"));
		}
	}
}
