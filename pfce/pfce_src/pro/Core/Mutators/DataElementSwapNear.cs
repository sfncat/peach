//
// Copyright (c) Peach Fuzzer, LLC
//

using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Mutators
{
	/// <summary>
	/// Swap current element with neighbor.
	/// </summary>
	[Mutator("DataElementSwapNear")]
	[Description("Swap a data element with its next sibling")]
	public class DataElementSwapNear : Mutator
	{
		public DataElementSwapNear(DataElement obj)
			: base(obj)
		{
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj.isMutable && obj.parent != null && !(obj is Flag) && obj.nextSibling() != null)
				return true;

			return false;
		}

		public override int count
		{
			get { return 1; }
		}

		public override uint mutation
		{
			get;
			set;
		}

		public override void sequentialMutation(DataElement obj)
		{
			randomMutation(obj);
		}

		public override void randomMutation(DataElement obj)
		{
			var parent = (DataElementContainer)obj.parent;
			var src = parent.IndexOf(obj);
			var dst = src + 1;

			if (dst < parent.Count)
				parent.SwapElements(src, dst);
		}
	}
}
