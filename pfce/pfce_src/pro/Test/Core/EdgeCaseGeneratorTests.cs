using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Peach.Core;
using Peach.Pro.Core;
using Random = Peach.Core.Random;
using Peach.Core.Test;
using Peach.Pro.Core.Mutators.Utility;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	[Quick]
	[Peach]
	class EdgeCaseGeneratorTests
	{
		class Sample
		{
			public long min;
			public ulong max;
			public long[] edges;
		}

		[Test]
		public void TestEdges()
		{
			var samples = new Sample[]
			{
				new Sample()
				{
					min = 0,
					max = 1,
					edges = new long[]
					{
						0,
						1,
					}
				},
				new Sample()
				{
					min = 0,
					max = 127,
					edges = new long[]
					{
						0,
						127,
					}
				},
				new Sample()
				{
					min = -1,
					max = 2,
					edges = new long[]
					{
						-1,
						0,
						2,
					}
				},
				new Sample()
				{
					min = short.MinValue,
					max = ushort.MaxValue,
					edges = new long[]
					{
						short.MinValue,
						sbyte.MinValue,
						0,
						sbyte.MaxValue,
						byte.MaxValue,
						short.MaxValue,
						ushort.MaxValue,
					}
				},
				new Sample()
				{
					min = long.MinValue,
					max = long.MaxValue,
					edges = new long[]
					{
						long.MinValue,
						int.MinValue,
						short.MinValue,
						sbyte.MinValue,
						0,
						sbyte.MaxValue,
						byte.MaxValue,
						short.MaxValue,
						ushort.MaxValue,
						int.MaxValue,
						uint.MaxValue,
						long.MaxValue,
					}
				},
				new Sample()
				{
					min = 0,
					max = ulong.MaxValue,
					edges = new long[]
					{
						0,
						sbyte.MaxValue,
						byte.MaxValue,
						short.MaxValue,
						ushort.MaxValue,
						int.MaxValue,
						uint.MaxValue,
						long.MaxValue,
						-1, // ulong.MaxValue
					}
				},
			};

			foreach (var s in samples)
				Assert.AreEqual(s.edges, new EdgeCaseGenerator(s.min, s.max).Edges.ToArray());
		}

		[Test]
		public void TestRanges()
		{
			var e1 = new EdgeCaseGenerator(short.MinValue, (ulong)short.MaxValue);

			// 6 edges
			Assert.AreEqual(6, e1.Edges.Count);
			// Range 0: -32768 ====> 32640
			Assert.AreEqual(Math.Min(16384, -128 - -32768), e1.Range(0));
			// Range 1: -128 ====> 128
			Assert.AreEqual(0 - -128, e1.Range(1));
			// Range 2: 0 ====> 127
			Assert.AreEqual(127 - 0, e1.Range(2));
			// Range 3: 127 ====> 127
			Assert.AreEqual(127 - 0, e1.Range(3));
			// Range 4: 255 ====> 128
			Assert.AreEqual(255 - 127, e1.Range(4));
			// Range 5: 32767 ====> 32512
			Assert.AreEqual(Math.Min(16384, 32767 - 255), e1.Range(5));
		}

		[Test]
		public void TestRangesUlong()
		{
			var e1 = new EdgeCaseGenerator(0, (ulong)ulong.MaxValue);

			// 9 edges
			Assert.AreEqual(9, e1.Edges.Count);
			// Range 0: 0
			Assert.AreEqual(0x7f - 0, e1.Range(0));
			// Range 1: 0x7f
			Assert.AreEqual(0x7f - 0, e1.Range(1));
			// Range 2: 0xff
			Assert.AreEqual(0xff - 0x7f, e1.Range(2));
			// Range 3: 0x7fff
			Assert.AreEqual(Math.Min(16384, 0x7fff - 0xff), e1.Range(3));
			// Range 4: 0xffff
			Assert.AreEqual(Math.Min(16384, 0xffff - 0x7fff), e1.Range(4));
			// Range 5: 0x7fffffff
			Assert.AreEqual(Math.Min(16384, 0x7fffffff - 0xffff), e1.Range(5));
			// Range 6: 0xffffffff
			Assert.AreEqual(Math.Min(16384, 0xffffffff - 0x7fffffff), e1.Range(6));
			// Range 7: 0x7fffffffffffffff
			Assert.AreEqual(Math.Min(16384, 0x7fffffffffffffff - 0xffffffff), e1.Range(7));
			// Range 8: 0xffffffffffffffff
			Assert.AreEqual(Math.Min(16384, 0x8000000000000000), e1.Range(8));
		}

		public void HitsEdges(long minValue, ulong maxValue)
		{
			var rng = new Random(0);
			var e = new EdgeCaseGenerator(minValue, maxValue);

			var hits = new bool[e.Edges.Count];

			for (ulong i = 0; i < 20 * e.Deviation; ++i)
			{
				var x = e.Next(rng);

				for (int j = 0; j < hits.Length; ++j)
					hits[j] |= x == e.Edges[j];
			}

			var sb = new StringBuilder();

			for (int j = 0; j < hits.Length; ++j)
			{
				if (!hits[j])
					sb.AppendFormat("{0} ", e.Edges[j]);
			}

			var missed = sb.ToString();
			if (!string.IsNullOrEmpty(missed))
				Assert.Fail("Missed edges: {0}".Fmt(missed));

			// We should have never had to generate more than one
			// random number for a call to Next()
			//Assert.AreEqual(0, e.BadRandom);
		}

		[Test]
		public void HitsEdges()
		{
			HitsEdges(short.MinValue, (ulong)short.MaxValue);
			HitsEdges(int.MinValue, (ulong)int.MaxValue);
			//HitsEdges(long.MinValue, (ulong)long.MaxValue);
		}

		[Test]
		public void BasicCurve()
		{
			var rng = new Random(0);
			var e = new EdgeCaseGenerator(int.MinValue, int.MaxValue);

			Assert.AreEqual(0, e.Edges[3]);

			var dict = new Dictionary<long, int>();

			for (int i = 0; i < 1000000; ++i)
			{
				var x = e.Next(rng, 3);

				int cnt;
				dict.TryGetValue(x, out cnt);
				dict[x] = cnt + 1;
			}

			var pctRhs = 1.0 * dict[0] / (dict[0] + dict[1]);
			var pctLhs = 1.0 * dict[0] / (dict[0] + dict[-1]);

			//File.WriteAllLines("test_edge.simple.csv", dict.Select(kv => "{0},{1}".Fmt(kv.Key, kv.Value)));

			Assert.LessOrEqual(pctLhs, 0.51);
			Assert.LessOrEqual(pctRhs, 0.51);
			Assert.Greater(pctLhs, 0.50);
			Assert.Greater(pctRhs, 0.50);
		}

		[Test]
		public void Random()
		{
			var rng = new Random(0);
			var e = new EdgeCaseGenerator(sbyte.MinValue, (ulong)sbyte.MaxValue);

			bool gotMin = false;
			bool gotMax = false;
			bool gotZero = false;

			for (int i = 0; i < 1000; ++i)
			{
				var x = e.Next(rng);

				Assert.GreaterOrEqual(x, sbyte.MinValue);
				Assert.LessOrEqual(x, sbyte.MaxValue);

				gotMin |= x == sbyte.MinValue;
				gotMax |= x == sbyte.MaxValue;
				gotZero |= x == 0;
			}

			Assert.True(gotMin);
			Assert.True(gotMax);
			Assert.True(gotZero);

			Assert.AreEqual(0, e.BadRandom);
		}

		[Test]
		public void IntRandom()
		{
			var rng = new Random(0);
			var e = new EdgeCaseGenerator(int.MinValue, (ulong)int.MaxValue);

			var hits = new bool[e.Edges.Count];

			for (long i = 0; i < 10000000; ++i)
			{
				var x = e.Next(rng);

				for (int j = 0; j < hits.Length; ++j)
					hits[j] |= x == e.Edges[j];
			}

			var sb = new StringBuilder();

			for (int j = 0; j < hits.Length; ++j)
			{
				if (!hits[j])
					sb.AppendFormat("{0} ", e.Edges[j]);
			}

			var missed = sb.ToString();
			if (!string.IsNullOrEmpty(missed))
				Assert.Fail("Missed edges: {0}".Fmt(missed));

			// We should have had to generate more than one
			// random number for a call to Next()
			//Assert.Greater(0, e.BadRandom);
		}

		[Test]
		public void LongRandom()
		{
			var rng = new Random(0);
			var e = new EdgeCaseGenerator(long.MinValue, (ulong)long.MaxValue);

			var hits = new bool[e.Edges.Count];

			for (long i = 0; i < 10000000; ++i)
			{
				var x = e.Next(rng);

				for (int j = 0; j < hits.Length; ++j)
					hits[j] |= x == e.Edges[j];
			}

			var sb = new StringBuilder();

			for (int j = 0; j < hits.Length; ++j)
			{
				if (!hits[j])
					sb.AppendFormat("{0} ", e.Edges[j]);
			}

			var missed = sb.ToString();
			if (!string.IsNullOrEmpty(missed))
				Assert.Fail("Missed edges: {0}".Fmt(missed));

			// We should have never had to generate more than one
			// random number for a call to Next()
			Assert.AreEqual(0, e.BadRandom);
		}

		[Test]
		public void ULongRandom()
		{
			var rng = new Random(0);
			var e = new EdgeCaseGenerator(0, ulong.MaxValue);

			var hits = new bool[e.Edges.Count];

			for (long i = 0; i < 10000000; ++i)
			{
				var x = (ulong)e.Next(rng);

				for (int j = 0; j < hits.Length - 1; ++j)
					hits[j] |= x < (ulong)e.Edges[j + 1];
			}

			var sb = new StringBuilder();

			for (int j = 0; j < hits.Length - 1; ++j)
			{
				if (!hits[j])
					sb.AppendFormat("{0} ", (ulong)e.Edges[j]);
			}

			var missed = sb.ToString();
			if (!string.IsNullOrEmpty(missed))
				Assert.Fail("Missed edges: {0}".Fmt(missed));

			// We should have never had to generate more than one
			// random number for a call to Next()
			Assert.AreEqual(0, e.BadRandom);
		}

		//[Test]
		public void MakeCsv()
		{
			var rng = new Random(0);
			//var e = new EdgeCaseGenerator(long.MinValue, long.MaxValue);
			var e = new EdgeCaseGenerator(0, ulong.MaxValue);

			var dict = new Dictionary<long, int>();

			{
				int j = 0;

				for (long i = 0; i < 10000000; ++i)
				{
					var x = e.NextEdge(rng, out j);

					if (e.Edges[j] == long.MinValue)
					{
						x -= long.MinValue;
						x -= (8 * 32767); 
					}
					else if (e.Edges[j] == long.MaxValue)
					{
						x -= long.MaxValue;
						x += (8 * 32767);
					}
					else if (e.Edges[j] == int.MinValue)
					{
						x -= int.MinValue;
						x -= (4 * 32767);
					}
					else if (e.Edges[j] == int.MaxValue)
					{
						x -= int.MaxValue;
						x += (4 * 32767);
					}
					else if (e.Edges[j] == uint.MaxValue)
					{
						x -= uint.MaxValue;
						x += (6 * 32767);
					}
					else if (e.Edges[j] == -1)
					{
						x += (10 * 32767);
					}

					int cnt;
					dict.TryGetValue(x, out cnt);
					dict[x] = cnt + 1;
				}

//				File.WriteAllLines("test_edge.{0}.csv".Fmt("all"), dict.Select(kv => "{0},{1}".Fmt(kv.Key, kv.Value)));
			}
			//File.WriteAllLines("test_edge.u.{0}.csv".Fmt("all"), dict.Select(kv => "{0},{1}".Fmt(kv.Key, kv.Value)));
		}
	}
}
