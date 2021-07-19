using System;
using System.Collections.Generic;
using Random = Peach.Core.Random;

namespace Peach.Pro.Core.Mutators.Utility
{
	/// <summary>
	/// Generates a normal distribution of numbers
	/// for a specified range centered around a specified value.
	/// 
	/// The range of the distribution will be from [min,value] and [value,max].
	/// The size of each half of the distribution is capped to have a maximum
	/// standard deviation of 16384.
	/// </summary>
	public class VarianceGenerator
	{
		const long maxRange = 0x4000;
		const long delta = 50;

		long value;
		long min;
		long max;

		double weight;    // weight of the 'right' curve
		long sigmaLhs;  // sigma to use for the curve to the left hand side of value
		long sigmaRhs;  // sigma to use for the curve to the right hand side of value

		internal ulong BadRandom
		{
			get;
			private set;
		}

		public VarianceGenerator(ulong value, ulong min, ulong max, bool useValue)
		{
			if (min > max)
				throw new ArgumentOutOfRangeException("min", "Parameter cannot be greater than max.");
			if (value > max)
				throw new ArgumentOutOfRangeException("value", "Parameter cannot be greater than max.");
			if (value < min)
				throw new ArgumentOutOfRangeException("value", "Parameter cannot be less than min.");


			Initialize(unchecked((long)value), unchecked((long)min), unchecked((long)max), useValue);
		}

		public VarianceGenerator(long value, long min, long max, bool useValue)
		{
			if (min > max)
				throw new ArgumentOutOfRangeException("min", "Parameter cannot be greater than max.");
			if (value > max)
				throw new ArgumentOutOfRangeException("value", "Parameter cannot be greater than max.");
			if (value < min)
				throw new ArgumentOutOfRangeException("value", "Parameter cannot be less than min.");

			Initialize(value, min, max, useValue);
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
		/// The minimum number of random numbers that sohuld be generated.
		/// </summary>
		public long[] Values
		{
			get;
			private set;
		}

		public long Next(Random random)
		{
			var r = random.NextDouble();

			// Weight is 0.5, r is 0.5, use left sigma
			// Wight is 0.5, r is 0.49999, use right sigma

			if (r < weight)
				// Ignore rhs 0 when a left curve exists
				return Next(random, sigmaRhs, sigmaLhs == 0);
			else
				// Ignore lhs 0 when right curve exists
				return Next(random, sigmaLhs, sigmaRhs == 0);
		}

		long Next(Random random, long sigma, bool floor)
		{
			while (true)
			{
				var num = random.NextGaussian();

				// Only want half a bell curve
				num = Math.Abs(num);

				// Expand to full range here, as double can't represent
				// all numbers > 2^53, and we need to go 2^64
				// Keep centered at 0 to make sure the number is not
				// too small or too large based on the edge
				// Use the floor function since we are only
				// computing half curves.
				long asLong;
				
				// If we have a left and right side, we just round to the
				// nearest integer.  If we are at an edge we want to round
				// down since there is only a single curve.
				if (!floor)
					asLong = (long)Math.Round(num * sigma);
				else if (sigma < 0)
					asLong = (long)Math.Ceiling(num * sigma);
				else
					asLong = (long)Math.Floor(num * sigma);

				// If we are on the right side curve, make sure we don't
				// overflow max when shifting to be centered at value
				if (asLong > 0 && (ulong)asLong > (ulong)(max - value))
				{
					CountBadRandom();
					continue;
				}

				// If we are on the left  side curve, make sure we don't
				// overflow min when shifting to be centered at value
				if (asLong < 0 && (ulong)-asLong > (ulong)(value - min))
				{
					CountBadRandom();
					continue;
				}

				return value + asLong;
			}
		}

		void Initialize(long value, long min, long max, bool useValue)
		{
			this.value = value;
			this.min = min;
			this.max = max;

			// We want each side of value to be half a bell curve over
			// the range.  This means stddev should be 2 * range / 6
			// since 99% of the numbers will be 3 stddev away from the mean

			sigmaLhs = GetSigma(value, min);
			sigmaRhs = GetSigma(max, value);

			Deviation = unchecked((ulong)(sigmaLhs + sigmaRhs));

			if (sigmaLhs == 0)
				weight = 1;
			else if (sigmaRhs == 0)
				weight = 0;
			else
				weight = 1.0 * sigmaRhs / (sigmaLhs + sigmaRhs);

			// Make left hand side negative
			sigmaLhs *= -1;

			var vals = new List<long>();

			// populate fixed values
			var val = value - (long)Math.Min((ulong)delta, (ulong)(value - min));
			var end = value + (long)Math.Min((ulong)delta, (ulong)(max - value));

			do
			{
				if (useValue || val != value)
					vals.Add(val);
			}
			while (val++ != end);

			Values = vals.ToArray();
		}

		long GetSigma(long upper, long lower)
		{
			var ret = (long)((ulong)(upper - lower) / 3);

			ret = Math.Min(ret, maxRange);

			return ret;
		}

		void CountBadRandom()
		{
			++BadRandom;
		}
	}
}
