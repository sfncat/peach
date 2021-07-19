//
// Copyright (c) Peach Fuzzer, LLC
//

using System;
using System.Collections.Generic;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Mutators.Utility
{
	public abstract class IntegerVariance : Mutator
	{
		Func<long> sequential;
		Func<long> random;
		int space;
		int fullSpace;
		bool signed;
		long value;
		long min;
		long max;
		bool useValue;

		/// <summary>
		/// Base class for mutations that use integer variance around the default value.
		/// </summary>
		/// <param name="obj">Element the mutator is attached to.</param>
		/// <param name="useValue">Should the default value be included in the variance.</param>
		public IntegerVariance(DataElement obj, bool useValue)
			: base(obj)
		{
			this.useValue = useValue;

			GetLimits(obj, out signed, out value, out min, out max);

			var delta = max - min;

			// delta is 3, value is 1, want -2, -1, 0
			// delta is 3, value is 0, want -2, -1, 1
			if (delta >= 0 && delta <= 0xff)
			{
				// We are <= a single byte, just use every value in the range
				var vals = new List<long>();

				for (var i = min; i <= max; ++i)
					if (useValue || i != value)
						vals.Add(i);

				space = vals.Count;
				fullSpace = space;
				sequential = () => vals[(int)mutation];
				random = () => vals[context.Random.Next(space)];
			}
			else
			{
				// For more than a single byte, use edge case generator
				VarianceGenerator gen;

				if (!signed)
					gen = new VarianceGenerator((ulong)value, (ulong)min, (ulong)max, useValue);
				else
					gen = new VarianceGenerator(value, min, max, useValue);

				space = gen.Values.Length;
				fullSpace = (int)Math.Min((ulong)int.MaxValue, gen.Deviation);
				sequential = () => gen.Values[mutation];
				random = () => gen.Next(context.Random);
			}
		}

		/// <summary>
		/// The logger to use.
		/// </summary>
		protected abstract NLog.Logger Logger
		{
			get;
		}

		/// <summary>
		/// Get the minimum and maximum values to generate edge cases for.
		/// </summary>
		/// <remarks>
		/// If value is unsigned, just cast it to a long.
		/// </remarks>
		/// <param name="obj">The element this mutator is bound to.</param>
		/// <param name="signed">Is the number space signed.</param>
		/// <param name="value">The value to center the variance distribution around.</param>
		/// <param name="min">The minimum value of the number space.</param>
		/// <param name="max">The maximum value of the number space.</param>
		protected abstract void GetLimits(DataElement obj, out bool signed, out long value, out long min, out long max);

		/// <summary>
		/// Mutate the data element.
		/// </summary>
		/// <param name="obj">The element to mutate.</param>
		/// <param name="value">The value to use when mutating.</param>
		protected abstract void performMutation(DataElement obj, long value);

		/// <summary>
		/// Mutate the data element.  This is called when the value to be used
		/// for mutation is larger than long.MaxValue.
		/// </summary>
		/// <param name="obj">The element to mutate.</param>
		/// <param name="value">The value to use when mutating.</param>
		protected abstract void performMutation(DataElement obj, ulong value);

		public sealed override uint mutation
		{
			get;
			set;
		}

		public sealed override int count
		{
			get
			{
				return space;
			}
		}

		public sealed override int weight
		{
			get
			{
				return fullSpace;
			}
		}

		public sealed override void sequentialMutation(DataElement obj)
		{
			performMutation(obj, sequential);
		}

		public sealed override void randomMutation(DataElement obj)
		{
			performMutation(obj, random);
		}

		private void performMutation(DataElement obj, Func<long> gen)
		{
			while (true)
			{
				var value = gen();

				// If we get our default value, pick again as that
				// is not really a mutation
				if (!useValue && value == this.value)
					continue;

				// VarianceGenerator gurantees value is between min/max
				// Promote to ulong if appropriate
				if (value < 0 && !signed)
				{
					var asUlong = unchecked((ulong)value);
					Logger.Trace("performMutation(value={0}", asUlong);
					performMutation(obj, asUlong);
				}
				else
				{
					Logger.Trace("performMutation(value={0}", value);
					performMutation(obj, value);
				}

				return;
			}
		}
	}
}
