using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Random = Peach.Core.Random;

namespace Peach.Pro.Core.Mutators.Utility
{
	/// <summary>
	/// Computes the edge cases of numbers inside a given space.
	/// </summary>
	public class EdgeCaseGenerator
	{
		// Don't need to add long.MinValue and ulong.MaxValue
		// to the edge cases since the algorithm will always
		// include 'min' and 'max' in the result
		static long[] edgeCases = new long[]
		{
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
		};

		// The maximum range to pick numbers around each edge
		const ulong maxRange = 0x4000;
		const ulong delta = 50;

		long minValue;
		ulong maxValue;

		List<long> edges = new List<long>();
		List<ulong> range = new List<ulong>();
		List<ulong> stddev = new List<ulong>();
		List<double> weights = new List<double>();

		internal ulong BadRandom
		{
			get;
			private set;
		}

		/// <summary>
		/// Generates numbers around the edge cases that occur
		/// between a minimum and maximum value.
		/// </summary>
		/// <param name="min">The minimum value of the number space.</param>
		/// <param name="max">The maximum value of the number space.</param>
		public EdgeCaseGenerator(long min, ulong max)
		{
			if (min > 0)
				throw new ArgumentOutOfRangeException("min", "Parameter must be less than or equal to zero.");

			if (max == 0)
				throw new ArgumentOutOfRangeException("max", "Parameter must be greater than zero.");

			minValue = min;
			maxValue = max;

			// First edge case is 'min'
			edges.Add(minValue);

			edges.AddRange(edgeCases
				// Skip while the edge cases are too negative
				.SkipWhile(i => unchecked(i - min <= 0))
				// Take wile edge cases are < 0 but not too positive
				.TakeWhile(i => unchecked(i < 0 || max - (ulong)i - 1 < max)));

			// Last edge case is 'max'
			edges.Add(unchecked((long)maxValue));

			// Compute range and standard deviation
			for (int i = 0; i < edges.Count; ++i)
			{
				var edge = edges[i];

				ulong v;

				if (edge <= 0 && edge != unchecked((long)maxValue))
				{
					// If edge <= 0, it is the distance to next edge
					v = unchecked((ulong)(edges[i + 1] - edge));
				}
				else
				{
					// If edge > 0, it is the distance to the previous edge
					v = unchecked((ulong)(edge - edges[i - 1]));
				}

				// Cap the +/- range to a maximum
				v = Math.Min(v, maxRange);

				range.Add(v);

				// We want the distribution to be a bell curve over
				// 2x the range.  This means stddev should be 2*range / 6
				// since 99% of the numbers will be 3 stddev away from the mean
				// Also, round up so if v is < 3, we get at least 1
				var s = (v + 2) / 3;

				stddev.Add(s);

				// The 1st and last edge get 1/2 the deviation since they are half curves
				// We still store the full deviation but do an abs() of the next rng
				if (i == 0 || i == edges.Count - 1)
					s = (s + 1) / 2; // Round up so we always have a valid stddev

				Deviation += s;
			}

			// Weight each edge based on its range.
			ulong sum = 0;

			for (int i = 0; i < edges.Count; ++i)
			{
				var weight = 1.0 - (1.0 * sum / Deviation);

				System.Diagnostics.Debug.Assert(weight <= 1);
				System.Diagnostics.Debug.Assert(weight > 0);

				weights.Add(weight);

				var s = stddev[i];

				// The 1st and last edge get 1/2 the weight since they are half curves
				// We still store the full deviation but do an abs() of the next rng
				if (i == 0 || i == edges.Count - 1)
					s /= 2;

				sum += s;
			}

			// Compute a list of +/- delta around each edge
			var vals = new List<long>();

			for (int i = 0; i < edges.Count; ++i)
			{
				if (i != 0)
				{
					for (int j = -50; j < 0; ++j)
						vals.Add(edges[i] + j);
				}

				vals.Add(edges[i]);

				if (i != edges.Count - 1)
				{
					for (int j = 1; j <= 50; ++j)
						vals.Add(edges[i] + j);
				}
			}

			Values = vals.ToArray();
		}

		/// <summary>
		/// The list of edge cases in the number space.
		/// </summary>
		public IList<long> Edges
		{
			get
			{
				return edges.AsReadOnly();
			}
		}

		/// <summary>
		/// The sum of all the standard deviations at each edge case
		/// </summary>
		public ulong Deviation
		{
			get;
			private set;
		}

		/// <summary>
		/// Precomputed list of values to use.
		/// </summary>
		public long[] Values
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the range of numbers to consider for a given edge.
		/// If the edge is less than ore equal to zero, it is the distance to the next edge.
		/// If the edge is greater than zero, it is the distance from the previous edge.
		/// </summary>
		/// <remarks>
		/// If the edge is short.MinValue, the next edge will be sbyte.MinValue
		/// and the resultant range will be (sbyte.MinValue - short.MinValue).
		/// 
		/// If the edge is short.MaxValue, the previous edge will be byte.MaxValue
		/// and the resultant edge will be (short.MaxValue - byte.MaxValue).
		/// </remarks>
		/// <param name="edgeIndex">The edge index to computer the range for.</param>
		/// <returns>The range to generate values in.</returns>
		public ulong Range(int edgeIndex)
		{
			return range[edgeIndex];
		}

		/// <summary>
		/// Produce next edge case number around the given edge index.
		/// </summary>
		/// <param name="random">Random number generator to use.</param>
		/// <param name="edgeIndex">The edge index to pick a random number in.</param>
		/// <returns>A random number in the range for the given edge index.</returns>
		public long Next(Random random, int edgeIndex)
		{
			var edge = edges[edgeIndex];
			var sigma = stddev[edgeIndex];

			while (true)
			{
				var num = random.NextGaussian();

				// The min/max edge are only half bell curves
				if ((num < 0 && edge == minValue) || (num > 0 && unchecked((ulong)edge) == maxValue))
					num *= -1;

				// Expand to full range here, as double can't represent
				// all numbers > 2^53, and we need to go 2^64
				// Keep centered at 0 to make sure the number is not
				// too small or too large based on the edge

				long asLong;

				// If we are producing a whole curve, round to nearest integer
				// but if we are producing the min/max half curves, round
				// down to the next smallest integer
				if (edge == minValue)
					asLong = (long)Math.Floor(num * sigma);
				else if (unchecked((ulong)edge) == maxValue)
					asLong = (long)Math.Ceiling(num * sigma);
				else
					asLong = (long)Math.Round(num * sigma);

				var ret = edge + asLong;

				if (unchecked(maxValue - (ulong)ret < maxValue))
				{
					// Number is less than max as viewed as a ulong
					// Check minValue >= 0 first sinze ret can be massively negative for ulongs
					if (minValue >= 0 || minValue <= ret)
						return ret;
				}
				else if (ret <= 0 && minValue <= ret)
				{
					// Number is greater than max viewed as a ulong, so only
					// accept negative numbers that aren't too negative
					return ret;
				}

				CountBadRandom();
			}
		}

		/// <summary>
		/// Produce next edge case number for a randomly selected edge index.
		/// </summary>
		/// <param name="random">Random number generator to use.</param>
		/// <returns>A random number.</returns>
		public long Next(Random random)
		{
			int i;
			return NextEdge(random, out i);
		}

		/// <summary>
		/// Produce next edge case number for a randomly selected edge index.
		/// </summary>
		/// <param name="random">Random number generator to use.</param>
		/// <param name="edgeIndex">The index of the picked edge.</param>
		/// <returns>A random number.</returns>
		internal long NextEdge(Random random, out int edgeIndex)
		{
			var r = random.NextDouble();

			int i = weights.Count - 1;

			// Weights are [ 1.0, 0.5 ], r is 0.5, expect i == 0
			// Since weights[0] is always 1.0 and random never returns 1.0
			while (weights[i] <= r)
				--i;

			edgeIndex = i;

			return Next(random, edgeIndex);
		}

		[Conditional("DEBUG")]
		private void CountBadRandom()
		{
			++BadRandom;
		}
	}
}
