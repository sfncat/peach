using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Peach.Core;
using Peach.Pro.Core;
using Peach.Core.Test;
using Peach.Pro.Core.Mutators.Utility;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	[Quick]
	[Peach]
	class VarianceGeneratorTests
	{
		[Test]
		public void TestThreeBit()
		{
			var g = new VarianceGenerator(0, -2, 1, false);

			Assert.AreEqual(new[] { -2, -1, 1 }, g.Values);

			g = new VarianceGenerator(0, -2, 1, true);

			Assert.AreEqual(new[] { -2, -1, 0, 1 }, g.Values);
		}

		[Test]
		public void TestSbyte()
		{
			var rng = new Random(0);
			var g = new VarianceGenerator(0, sbyte.MinValue, sbyte.MaxValue, false);

			var hits = new int[256];

			for (int i = 0; i < 1000000; ++i)
			{
				var x = g.Next(rng);

				Assert.GreaterOrEqual(x, sbyte.MinValue);
				Assert.LessOrEqual(x, sbyte.MaxValue);

				++hits[x - sbyte.MinValue];
			}

			var sb = new StringBuilder();

			for (int j = 0; j < hits.Length; ++j)
			{
				if (hits[j] == 0)
					sb.AppendFormat("{0} ", j + sbyte.MinValue);
			}

			var str = sb.ToString();
			if (!string.IsNullOrEmpty(str))
				Assert.Fail("Missed numbers: {0}".Fmt(str));

			// Ensure 0 is about the same as -1 and 1
			double pctLhs = 1.0 * hits[128] / (hits[127] + hits[128]);
			double pctRhs = 1.0 * hits[128] / (hits[129] + hits[128]);

			Assert.LessOrEqual(pctLhs, 0.51);
			Assert.LessOrEqual(pctRhs, 0.51);
			Assert.Greater(pctLhs, 0.50);
			Assert.Greater(pctRhs, 0.50);
		}

		[Test]
		public void TestByteMin()
		{
			var rng = new Random(0);
			var g = new VarianceGenerator(byte.MinValue, byte.MinValue, byte.MaxValue, false);

			var hits = new int[256];

			for (int i = 0; i < 1000000; ++i)
			{
				var x = g.Next(rng);

				Assert.GreaterOrEqual(x, byte.MinValue);
				Assert.LessOrEqual(x, byte.MaxValue);

				++hits[x];
			}

			var sb = new StringBuilder();

			for (int j = 0; j < hits.Length; ++j)
			{
				if (hits[j] == 0)
					sb.AppendFormat("{0} ", j + sbyte.MinValue);
			}

			var str = sb.ToString();
			if (!string.IsNullOrEmpty(str))
				Assert.Fail("Missed numbers: {0}".Fmt(str));

			// Ensure 0 is about the same as 1
			double pct = 1.0 * hits[0] / (hits[0] + hits[1]);

			Assert.LessOrEqual(pct, 0.51);
			Assert.Greater(pct, 0.50);
		}

		[Test]
		public void TestByteMax()
		{
			var rng = new Random(0);
			var g = new VarianceGenerator(byte.MaxValue, byte.MinValue, byte.MaxValue, false);

			var hits = new int[256];

			for (int i = 0; i < 1000000; ++i)
			{
				var x = g.Next(rng);

				Assert.GreaterOrEqual(x, byte.MinValue);
				Assert.LessOrEqual(x, byte.MaxValue);

				++hits[x];
			}

			var sb = new StringBuilder();

			for (int j = 0; j < hits.Length; ++j)
			{
				if (hits[j] == 0)
					sb.AppendFormat("{0} ", j + sbyte.MinValue);
			}

			var str = sb.ToString();
			if (!string.IsNullOrEmpty(str))
				Assert.Fail("Missed numbers: {0}".Fmt(str));

			// Ensure 0 is about the same as 1
			double pct = 1.0 * hits[255] / (hits[254] + hits[255]);

			Assert.LessOrEqual(pct, 0.51);
			Assert.Greater(pct, 0.50);
		}

		[Test]
		public void TestLongZero()
		{
			var rng = new Random(0);
			var g = new VarianceGenerator(0, long.MinValue, long.MaxValue, false);

			Assert.AreEqual(100, g.Values.Length);
			for (int i = 0; i < g.Values.Length / 2; ++i)
				Assert.AreEqual(-50 + i, g.Values[i]);
			for (int i = g.Values.Length / 2; i < g.Values.Length; ++i)
				Assert.AreEqual(-50 + 1 + i, g.Values[i]);

			for (int i = 0; i < 1000000; ++i)
			{
				var x = g.Next(rng);

				// Long should not produce any values +/- about 3 * 327667
				Assert.GreaterOrEqual(x, -3 * short.MaxValue);
				Assert.LessOrEqual(x, 3 * short.MaxValue);
			}
		}

		[Test]
		public void TestLongMin()
		{
			var rng = new Random(0);
			var g = new VarianceGenerator(long.MinValue + 255, long.MinValue, long.MaxValue, false);

			Assert.AreEqual(100, g.Values.Length);
			for (int i = 0; i < g.Values.Length / 2; ++i)
				Assert.AreEqual(long.MinValue + 255 - 50 + i, g.Values[i]);
			for (int i = g.Values.Length / 2; i < g.Values.Length; ++i)
				Assert.AreEqual(long.MinValue + 255 - 50 + 1 + i, g.Values[i]);

			for (int i = 0; i < 1000000; ++i)
			{
				var x = g.Next(rng);

				// Long should not produce any values +/- about 3 * 327667
				Assert.LessOrEqual(x, long.MinValue + (3 * short.MaxValue));
			}
		}

		[Test]
		public void TestLongMax()
		{
			var rng = new Random(0);
			var g = new VarianceGenerator(long.MaxValue - 255, long.MinValue, long.MaxValue, false);

			Assert.AreEqual(100, g.Values.Length);
			for (int i = 0; i < g.Values.Length / 2; ++i)
				Assert.AreEqual(long.MaxValue - 255 - 50 + i, g.Values[i]);
			for (int i = g.Values.Length / 2; i < g.Values.Length; ++i)
				Assert.AreEqual(long.MaxValue - 255 - 50 + 1 + i, g.Values[i]);

			for (int i = 0; i < 1000000; ++i)
			{
				var x = g.Next(rng);

				// Long should not produce any values +/- about 3 * 327667
				Assert.GreaterOrEqual(x, long.MaxValue - (3 * short.MaxValue));
			}
		}


		[Test]
		public void TestULongZero()
		{
			var rng = new Random(0);
			var g = new VarianceGenerator(0, ulong.MinValue, ulong.MaxValue, false);

			Assert.AreEqual(50, g.Values.Length);
			for (int i = 0; i < g.Values.Length; ++i)
				Assert.AreEqual(1 + i, g.Values[i]);

			for (int i = 0; i < 1000000; ++i)
			{
				var x = (ulong)g.Next(rng);

				// Long should not produce any values +/- about 3 * 327667
				Assert.LessOrEqual(x, 3 * short.MaxValue);
			}
		}

		[Test]
		public void TestULongMin()
		{
			var rng = new Random(0);
			var g = new VarianceGenerator(255, ulong.MinValue, ulong.MaxValue, false);

			Assert.AreEqual(100, g.Values.Length);
			for (int i = 0; i < g.Values.Length / 2; ++i)
				Assert.AreEqual(255 - 50 + i, g.Values[i]);
			for (int i = g.Values.Length / 2; i < g.Values.Length; ++i)
				Assert.AreEqual(255 - 50 + 1 + i, g.Values[i]);

			for (int i = 0; i < 1000000; ++i)
			{
				var x = (ulong)g.Next(rng);

				// Long should not produce any values +/- about 3 * 327667
				Assert.LessOrEqual(x, 3 * short.MaxValue);
			}
		}

		[Test]
		public void TestULongLarge()
		{
			var rng = new Random(0);
			var g = new VarianceGenerator(ulong.MaxValue - 255, ulong.MinValue, ulong.MaxValue, false);

			Assert.AreEqual(100, g.Values.Length);
			for (uint i = 0; i < g.Values.Length / 2; ++i)
				Assert.AreEqual(ulong.MaxValue - 255 - 50 + i, (ulong)g.Values[i]);
			for (uint i = (uint)g.Values.Length / 2; i < g.Values.Length; ++i)
				Assert.AreEqual(ulong.MaxValue - 255 - 50 + 1 + i, (ulong)g.Values[i]);

			for (int i = 0; i < 1000000; ++i)
			{
				var x = (ulong)g.Next(rng);

				// Long should not produce any values +/- about 3 * 327667
				Assert.GreaterOrEqual(x, ulong.MaxValue - (3 * short.MaxValue));
			}
		}

		[Test]
		public void TestULongMax()
		{
			var rng = new Random(0);
			var g = new VarianceGenerator(ulong.MaxValue, ulong.MinValue, ulong.MaxValue, false);

			Assert.AreEqual(50, g.Values.Length);
			for (uint i = 0; i < g.Values.Length; ++i)
				Assert.AreEqual(ulong.MaxValue - 50 + i, (ulong)g.Values[i]);

			for (int i = 0; i < 1000000; ++i)
			{
				var x = (ulong)g.Next(rng);

				// Long should not produce any values +/- about 3 * 327667
				Assert.GreaterOrEqual(x, ulong.MaxValue - (3 * short.MaxValue));
			}
		}

		//[Test]
		public void MakeCsv()
		{
			var rng = new Random(0);
			var g = new VarianceGenerator(ulong.MaxValue - (4 * 4096), ulong.MinValue, ulong.MaxValue, false);

			var dict = new Dictionary<ulong, int>();

			for (long i = 0; i < 10000000; ++i)
			{
				var x = (ulong)g.Next(rng);

				int cnt;
				dict.TryGetValue(x, out cnt);
				dict[x] = cnt + 1;
			}

			File.WriteAllLines("variance.csv", dict.Select(kv => "{0},{1}".Fmt(kv.Key + int.MaxValue, kv.Value)));
		}
	}
}
