//
// Copyright (c) Peach Fuzzer, LLC
//

using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Mutators.Utility
{
	/// <summary>
	/// Extension of StringLengthVariance that injects BOM characters randomly-ish into the strings.
	/// </summary>
	public abstract class StringBomLength : StringLengthVariance
	{
		public StringBomLength(DataElement obj)
			: base(obj)
		{
		}

		protected abstract override NLog.Logger Logger
		{
			get;
		}

		/// <summary>
		/// Returns an array of BOM characters to inject during mutation.
		/// </summary>
		protected abstract byte[][] BOM
		{
			get;
		}

		protected override void performMutation(DataElement obj, long value)
		{
			base.performMutation(obj, value);

			// If the base didn't actually mutate the element, we shouldn't either
			if (obj.mutationFlags == MutateOverride.None)
				return;

			var val = StringBomStatic.InjectBom(context.Random, obj.PreTransformedValue, BOM);

			obj.MutatedValue = new Variant(val);
			obj.mutationFlags |= MutateOverride.TypeTransform;
		}
	}
}
