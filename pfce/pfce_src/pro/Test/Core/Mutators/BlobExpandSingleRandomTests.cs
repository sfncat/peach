using System.Linq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Dom.Actions;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Mutators
{
	[TestFixture]
	[Quick]
	[Peach]
	class BlobExpandSingleRandomTests
	{
		[Test]
		public void TestSupported()
		{
			var runner = new MutatorRunner("BlobExpandSingleRandom");

			Assert.True(runner.IsSupported(new Blob()
			{
				DefaultValue = new Variant(Encoding.ASCII.GetBytes("Hello")),
			}));

			Assert.True(runner.IsSupported(new Blob()
			{
				DefaultValue = new Variant(new byte[0]),
			}));

			Assert.False(runner.IsSupported(new Blob()
			{
				DefaultValue = new Variant(Encoding.ASCII.GetBytes("Hello")),
				isMutable = false,
			}));
		}

		[Test]
		public void TestCounts()
		{
			var runner = new MutatorRunner("BlobExpandSingleRandom");

			var m1 = runner.Sequential(new Blob() { DefaultValue = new Variant(new byte[1]) });
			Assert.AreEqual(255, m1.Count());

			var m2 = runner.Sequential(new Blob() { DefaultValue = new Variant(new byte[50]) });
			Assert.AreEqual(255, m2.Count());

			var m3 = runner.Sequential(new Blob() { DefaultValue = new Variant(new byte[500]) });
			Assert.AreEqual(255, m3.Count());

			var m4 = runner.Sequential(new Blob() { DefaultValue = new Variant(new byte[0]) });
			Assert.AreEqual(255, m4.Count());
		}

		[Test]
		public void TestSequential()
		{
			var runner = new MutatorRunner("BlobExpandSingleRandom");
			var src = new byte[10];
			var m = runner.Sequential(new Blob() { DefaultValue = new Variant(src) });

			int len = 11;
			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreEqual(len++, val.Length);
			}
		}

		[Test]
		public void TestSequentialOne()
		{
			var runner = new MutatorRunner("BlobExpandSingleRandom");
			var src = new byte[1];
			var m = runner.Sequential(new Blob() { DefaultValue = new Variant(src) });

			int len = 2;
			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreEqual(len++, val.Length);
			}
		}

		[Test]
		public void TestRandom()
		{
			var runner = new MutatorRunner("BlobExpandSingleRandom");
			var src = new byte[10];
			var m = runner.Random(100, new Blob() { DefaultValue = new Variant(src) });

			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreNotEqual(src.Length, val.Length);
				Assert.AreNotEqual(src, val);
			}
		}

		[Test]
		public void TestRandomOne()
		{
			var runner = new MutatorRunner("BlobExpandSingleRandom");
			var src = new byte[1];
			var m = runner.Random(100, new Blob() { DefaultValue = new Variant(src) });

			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreNotEqual(src.Length, val.Length);
				Assert.AreNotEqual(src, val);
			}
		}

		[Test]
		public void TestRandomZero()
		{
			var runner = new MutatorRunner("BlobExpandSingleRandom");
			var src = new byte[0];
			var m = runner.Random(100, new Blob() { DefaultValue = new Variant(src) });

			foreach (var item in m)
			{
				var val = item.Value.ToArray();

				Assert.AreNotEqual(src, val);
			}
		}

		[Test]
		public void TestRelationOverflow()
		{
			var runner = new MutatorRunner("BlobExpandSingleRandom");

			var model = new DataModel("DM")
			{
				new Number("num")
				{
					lengthType = LengthType.Bits,
					length = 8,
					Signed = false
				},
				new Blob("blob")
				{
					DefaultValue = new Variant(new byte[100])
				}
			};

			// Constructor auto-adds relation to parent
			var r = new SizeRelation(model[0]) { Of = model[1] };
			Assert.That(model[0].relations.Contains(r));

			model.actionData = new ActionData
			{
				action = new Output
				{
					parent = new State
					{
						parent = new Peach.Core.Dom.StateModel
						{
							parent = new Peach.Core.Dom.Dom
							{
								context = new RunContext
								{
									controlIteration = false
								}
							}
						}
					}
				}
			};


			var m = runner.Random(100, model[1]);

			var gotOverflow = false;

			foreach (var val in m.Select(item => item.Element.root.Value.ToArray()))
			{
				Assert.NotNull(val);

				if (val.Length <= 256)
					continue;

				// If mutator expanded buffer to be > 255 bytes
				// the relation should return 0xff when we are mutating

				Assert.AreEqual(0xff, val[0]);

				gotOverflow = true;
				break;
			}

			Assert.True(gotOverflow, "Mutator should have triggered overflow");
		}
	}
}
