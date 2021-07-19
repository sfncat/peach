using System;

using Random = Peach.Core.Random;

namespace Peach.Pro.Core.Mutators.Utility
{
	/// <summary>
	/// Generates a normal distribution of double precision numbers
	/// for a specified range centered around a specified value.
	/// The range of the distribution will be from [min,value] and [value,max].
	/// </summary>
	public class DoubleVarianceGenerator
	{
		readonly double _maxRange;

		double _value;
		double _min;
		double _max;

		double _weight;    // weight of the 'right' curve
		double _sigmaLhs;  // sigma to use for the curve to the left hand side of value
		double _sigmaRhs;  // sigma to use for the curve to the right hand side of value

		internal ulong BadRandom
		{
			get;
			private set;
		}

		public DoubleVarianceGenerator(double value, double min, double max, double maxRange)
		{
			if (min > max)
				throw new ArgumentOutOfRangeException("min", "Parameter cannot be greater than max.");
			if (value > max)
				throw new ArgumentOutOfRangeException("value", "Parameter cannot be greater than max.");
			if (value < min)
				throw new ArgumentOutOfRangeException("value", "Parameter cannot be less than min.");

			_maxRange = maxRange;

			Initialize(value, min, max);
		}

		public double Next(Random random)
		{
			var r = random.NextDouble();

			if (r < _weight)
				// Ignore rhs 0 when a left curve exists
				return Next(random, _sigmaRhs);
			else
				// Ignore lhs 0 when right curve exists
				return Next(random, _sigmaLhs);
		}

		double Next(Random random, double sigma)
		{
			while (true)
			{
				var num = random.NextGaussian();

				// Only want half a bell curve
				num = Math.Abs(num);

				var asLong = num * sigma;

				// If we are on the right side curve, make sure we don't
				// overflow max when shifting to be centered at value
				if (asLong > 0 && asLong > (_max - _value))
				{
					CountBadRandom();
					continue;
				}

				// If we are on the left  side curve, make sure we don't
				// overflow min when shifting to be centered at value
				if (asLong < 0 && -asLong > (_value - _min))
				{
					CountBadRandom();
					continue;
				}

				return _value + asLong;
			}
		}

		void Initialize(double value, double min, double max)
		{
			_value = value;
			_min = min;
			_max = max;

			// We want each side of value to be half a bell curve over
			// the range.  This means stddev should be 2 * range / 6
			// since 99% of the numbers will be 3 stddev away from the mean

			_sigmaLhs = GetSigma(value, min);
			_sigmaRhs = GetSigma(max, value);

			// ReSharper disable CompareOfFloatsByEqualityOperator
			if (_sigmaLhs == 0)
				_weight = 1;
			else if (_sigmaRhs == 0)
				_weight = 0;
			else
				_weight = 1.0 * _sigmaRhs / (_sigmaLhs + _sigmaRhs);
			// ReSharper restore CompareOfFloatsByEqualityOperator

			// Make left hand side negative
			_sigmaLhs *= -1;
		}

		double GetSigma(double upper, double lower)
		{
			var ret = (upper - lower) / 3;

			ret = Math.Min(ret, _maxRange);

			return ret;
		}

		void CountBadRandom()
		{
			++BadRandom;
		}
	}
}
