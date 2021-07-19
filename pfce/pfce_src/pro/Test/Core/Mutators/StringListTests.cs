using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Mutators
{
	[TestFixture]
	[Quick]
	[Peach]
	class StringListTests
	{
		string file;

		[SetUp]
		public void SetUp()
		{
			file = Path.GetTempFileName();
			File.WriteAllLines(file, new[] { "foo", "bar", "baz" });
		}

		[TearDown]
		public void TearDown()
		{
			File.Delete(file);
			file = null;
		}

		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("StringList");

			var hintEmpty = new Hint("StringList", "");
			var hintValid = new Hint("StringList", file);

			var str = new Peach.Core.Dom.String("String");
			Assert.False(runner.IsSupported(str));

			str.Hints["StringList"] = hintEmpty;
			Assert.False(runner.IsSupported(str));

			str.Hints["StringList"] = hintValid;
			Assert.True(runner.IsSupported(str));
		}

		[Test]
		public void TestBadHint()
		{
			var runner = new MutatorRunner("StringList");

			var hintInvalid = new Hint("StringList", "some_missing_file");

			var str = new Peach.Core.Dom.String("String");
			str.Hints["StringList"] = hintInvalid;

			Assert.True(runner.IsSupported(str));

			try
			{
				runner.Sequential(str).FirstOrDefault();

				Assert.Fail("Should have thrown");
			}
			catch (Exception ex)
			{
				var orig = ex.GetBaseException();

				Assert.True(orig is PeachException);
			}
		}

		[Test]
		public void TestSequential()
		{
			var runner = new MutatorRunner("StringList");

			var hint = new Hint("StringList", file);

			var str = new Peach.Core.Dom.String();
			str.Hints[hint.Name] = hint;

			var m1 = runner.Sequential(str);
			Assert.AreEqual(3, m1.Count());

			var v1 = m1.Select(i => Encoding.ASCII.GetString(i.Value.ToArray())).ToArray();
			var e1 = new string[] { "foo", "bar", "baz" };
			Assert.AreEqual(e1, v1);
		}

		[Test]
		public void TestRandom()
		{
			var runner = new MutatorRunner("StringList");

			var hint = new Hint("StringList", file);

			var str = new Peach.Core.Dom.String();
			str.Hints[hint.Name] = hint;

			var m1 = runner.Random(100, str);
			Assert.AreEqual(100, m1.Count());

			var v1 = m1.Select(i => Encoding.ASCII.GetString(i.Value.ToArray())).Total();

			Assert.AreEqual(3, v1.Keys.Count);

			Assert.True(v1.ContainsKey("foo"));
			Assert.True(v1.ContainsKey("bar"));
			Assert.True(v1.ContainsKey("baz"));
		}
	}
}
