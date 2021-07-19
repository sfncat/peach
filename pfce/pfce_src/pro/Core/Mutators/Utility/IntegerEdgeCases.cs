//
// Copyright (c) Peach Fuzzer, LLC
//

using System;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Mutators.Utility
{
	/// <summary>
	/// Generate integer edge cases. The numbers produced are distributed 
	/// over a bell curve with the edge case as the center.
	/// </summary>
	public abstract class IntegerEdgeCases : Mutator
	{
		Func<long> sequential;
		Func<long> random;
		int space;
		int fullSpace;
		long min;
		ulong max;

		public IntegerEdgeCases(DataElement obj)
			: base(obj)
		{
			GetLimits(obj, out min, out max);

			var delta = unchecked((long)max - min);

			if (delta >= 0 && delta <= 0xff)
			{
				// We are <= a single byte, set the space size of the range
				space = (int)delta + 1;
				fullSpace = space;
				sequential = () => min + mutation;
				random = () => context.Random.Next(min, (long)max + 1);
			}
			else
			{
				// For more than a single byte, use edge case generator
				var gen = new EdgeCaseGenerator(min, max);

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
		/// <param name="obj">The element this mutator is bound to.</param>
		/// <param name="min">The minimum value of the number space.</param>
		/// <param name="max">The maximum value of the number space.</param>
		protected abstract void GetLimits(DataElement obj, out long min, out ulong max);

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

		void performMutation(DataElement obj, Func<long> gen)
		{
			var value = gen();

			// Mark the element as mutated
			obj.mutationFlags = MutateOverride.Default;

			// EdgeCaseGenerator gurantees value is between min/max
			// Promote to ulong if appropriate
			if (value < 0 && min >= 0)
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
		}
	}
}
