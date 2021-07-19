//
// Copyright (c) Peach Fuzzer, LLC
//

using System;
using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Mutators
{
	[Mutator("NumberRandom")]
	[Description("Produce random number in range of underlying element.")]
	public class NumberRandom : Mutator
	{
		const int maxCount = 5000; // Maximum count is 5000

		int total;
		Func<Variant> gen;

		public NumberRandom(DataElement obj)
			: base(obj)
		{
			var asNum = obj as Peach.Core.Dom.Number;

			if (asNum == null)
			{
				System.Diagnostics.Debug.Assert(obj is Peach.Core.Dom.String);

				total = maxCount;
				gen = () => new Variant(context.Random.NextInt64().ToString());
			}
			else if (asNum.Signed)
			{
				// Square root of number space, capped at maxCount
				total = (int)Math.Min((ulong)Math.Sqrt((ulong)((long)asNum.MaxValue - asNum.MinValue)), maxCount);
 
				if (asNum.lengthAsBits < 32)
					gen = () => new Variant(context.Random.Next((int)asNum.MinValue, (int)asNum.MaxValue + 1));
				else if (asNum.lengthAsBits == 32)
					gen = () => new Variant(context.Random.NextInt32());
				else if (asNum.lengthAsBits < 64)
					gen = () => new Variant(context.Random.Next((long)asNum.MinValue, (long)asNum.MaxValue + 1));
				else if (asNum.lengthAsBits == 64)
					gen = () => new Variant(context.Random.NextInt64());
				else
					throw new NotSupportedException();
			}
			else
			{
				// Square root of number space, capped at maxCount
				total = (int)Math.Min((ulong)Math.Sqrt((ulong)((long)asNum.MaxValue - asNum.MinValue)), maxCount);

				if (asNum.lengthAsBits < 32)
					gen = () => new Variant(context.Random.Next((uint)asNum.MinValue, (uint)asNum.MaxValue + 1));
				else if (asNum.lengthAsBits == 32)
					gen = () => new Variant(context.Random.NextUInt32());
				else if (asNum.lengthAsBits < 64)
					gen = () => new Variant(context.Random.Next((ulong)asNum.MinValue, (ulong)asNum.MaxValue + 1));
				else if (asNum.lengthAsBits == 64)
					gen = () => new Variant(context.Random.NextUInt64());
				else
					throw new NotSupportedException();
			}
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is Peach.Core.Dom.String && obj.isMutable)
				return obj.Hints.ContainsKey("NumericalString");

			// Ignore numbers <= 8 bits, they will be mutated
			// with the NumericalVariance mutator
			return obj is Peach.Core.Dom.Number && obj.isMutable && obj.lengthAsBits > 8;
		}

		public override int count
		{
			get
			{
				return total;
			}
		}

		public override uint mutation
		{
			get;
			set;
		}

		public override void sequentialMutation(DataElement obj)
		{
			// Same as random, but seed is fixed
			randomMutation(obj);
		}

		public override void randomMutation(DataElement obj)
		{
			obj.MutatedValue = gen();
			obj.mutationFlags = MutateOverride.Default;
		}
	}
}
