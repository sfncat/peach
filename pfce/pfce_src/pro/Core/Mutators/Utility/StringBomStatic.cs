//
// Copyright (c) Peach Fuzzer, LLC
//

using System.IO;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Random = Peach.Core.Random;

namespace Peach.Pro.Core.Mutators.Utility
{
	/// <summary>
	/// Extension of StringStatic that injects BOM characters randomly-ish into the strings.
	/// </summary>
	public abstract class StringBomStatic : StringStatic
	{
		public StringBomStatic(DataElement obj)
			: base(obj)
		{
		}

		/// <summary>
		/// Returns an array of BOM characters to inject during mutation.
		/// </summary>
		protected abstract byte[][] BOM
		{
			get;
		}

		protected override void performMutation(DataElement obj, int index)
		{
			base.performMutation(obj, index);

			// If the base didn't actually mutate the element, we shouldn't either
			if (obj.mutationFlags == MutateOverride.None)
				return;

			var val = StringBomStatic.InjectBom(context.Random, obj.PreTransformedValue, BOM);

			obj.MutatedValue = new Variant(val);
			obj.mutationFlags |= MutateOverride.TypeTransform;
		}

		/// <summary>
		/// Picks a gaussian number N from centered at 1, up to 6.
		/// Picks N indices in data
		/// For each index, injects a randomly selected array of BOm characters.
		/// </summary>
		/// <param name="rng">Random number generator to use.</param>
		/// <param name="data">Source data.</param>
		/// <param name="bom">Array of BOM characters.</param>
		/// <returns>BitwiseStream containing injected BOM characters.</returns>
		internal static BitwiseStream InjectBom(Random rng, BitwiseStream data, byte[][] bom)
		{
			var num = rng.PickSix();
			var indices = rng.SortedPermutation(data.Length, num);

			if (indices.Length == 0)
			{
				System.Diagnostics.Debug.Assert(data.Length == 0);
				indices = new long[] { 1 };
			}

			// Inject bom byte sequence indices

			data.Seek(0, SeekOrigin.Begin);

			var ret = new BitStreamList();

			for (int i = 0; i < indices.Length; ++i)
			{
				// Permutation is [1,Length] inclusive and alsos orted
				var idx = indices[i];

				var len = idx - data.Position - 1;

				if (len > 0)
					ret.Add(data.SliceBits(len * 8));

				var r = rng.Next(0, bom.Length);
				var bs = new BitStream(bom[r]);

				ret.Add(bs);
			}

			var remain = data.LengthBits - data.PositionBits;
			if (remain > 0)
				ret.Add(data.SliceBits(remain));

			return ret;
		}
	}
}
