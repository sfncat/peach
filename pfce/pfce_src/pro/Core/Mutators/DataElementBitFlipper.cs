//
// Copyright (c) Peach Fuzzer, LLC
//

using System;
using System.ComponentModel;
using System.IO;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Pro.Core.Mutators.Utility;

namespace Peach.Pro.Core.Mutators
{
	/// <summary>
	/// Flips bits on thepre-transformed value of non-container elements
	/// as well as post-transformed value on container elements.
	/// The number of bits flipped is a gaussian distribution from [1,6]
	/// </summary>
	[Mutator("DataElementBitFlipper")]
	[Description("Flip bits in a data element.")]
	public class DataElementBitFlipper : Mutator
	{
		int total;
		Func<DataElement, BitwiseStream> getData;
		MutateOverride flags;

		public DataElementBitFlipper(DataElement obj)
			: base(obj)
		{
			if (obj is DataElementContainer)
			{
				flags = MutateOverride.Default | MutateOverride.TypeTransform | MutateOverride.Transformer;
				getData = e => e.Value;
			}
			else
			{
				flags = MutateOverride.Default | MutateOverride.TypeTransform;
				getData = e => e.PreTransformedValue;
			}

			// For sequential, use the length of the data
			total = (int)Math.Min(int.MaxValue, getData(obj).LengthBits);
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			// Don't attach to non-mutable elements
			if (!obj.isMutable)
				return false;

			if (!getTypeTransformHint(obj))
				return false;

			// Attach to all non-container elements and take the
			// pre-transformed value and flip the bits randomly
			if (!(obj is DataElementContainer))
				return obj.PreTransformedValue.LengthBits > 0;

			// Attach to all container elements that have a transformer and take
			// the post-transformed value and flip the bits randomly
			if (obj.transformer != null)
				return obj.Value.LengthBits > 0;

			return false;
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
			// Sequential is the same as random
			randomMutation(obj);
		}

		public override void randomMutation(DataElement obj)
		{
			var data = getData(obj);

			// Pick number from 1-6 (stddev = 5/3
			var num = context.Random.PickSix();

			// Pick num unique indices
			var indices = context.Random.SortedPermutation(data.LengthBits, num);

			// flip bits at indices

			data.Seek(0, SeekOrigin.Begin);

			var ret = new BitStreamList();

			for (int i = 0; i < indices.Length; ++i)
			{
				// Permutation is [1,Length] inclusive and alsos orted
				var idx = indices[i];

				var len = idx - data.PositionBits - 1;

				if (len > 0)
					ret.Add(data.SliceBits(len));

				int v = data.ReadBit();
				System.Diagnostics.Debug.Assert(v != -1);
				System.Diagnostics.Debug.Assert(data.PositionBits == idx);

				var bs = new BitStream();
				bs.WriteBit(v == 0 ? 1 : 0);

				ret.Add(bs);
			}

			var remain = data.LengthBits - data.PositionBits;
			if (remain > 0)
				ret.Add(data.SliceBits(remain));

			obj.MutatedValue = new Variant(ret);
			obj.mutationFlags = flags;
		}
	}
}
