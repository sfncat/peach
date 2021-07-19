//
// Copyright (c) Peach Fuzzer, LLC
//

using System;
using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Mutators
{
	[Mutator("ArrayRandomizeOrder")]
	[Description("Randomize the order of the array")]
	public class ArrayRandomizeOrder : Mutator
	{
		int combinations;

		public ArrayRandomizeOrder(DataElement obj)
			: base(obj)
		{

			var asSeq = (Peach.Core.Dom.Sequence)obj;

			// for small count, use factorial, otherwise cap at 100
			switch (asSeq.Count)
			{
				case 0:
				case 1:
					throw new ArgumentException();
				case 2:
					combinations = 2;
					break;
				case 3:
					combinations = 6;
					break;
				case 4:
					combinations = 24;
					break;
				default:
					combinations = 100;
					break;
			}
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			var asSeq = obj as Peach.Core.Dom.Sequence;

			if (asSeq != null && asSeq.isMutable && asSeq.Count > 1)
				return true;

			return false;
		}

		public override int count
		{
			get
			{
				return combinations;
			}
		}

		public override uint mutation
		{
			get;
			set;
		}

		public override void sequentialMutation(DataElement obj)
		{
			performMutation((Peach.Core.Dom.Sequence)obj);
		}

		public override void randomMutation(DataElement obj)
		{
			performMutation((Peach.Core.Dom.Sequence)obj);
		}

		void performMutation(Peach.Core.Dom.Sequence obj)
		{
			obj.mutationFlags = MutateOverride.Default;

			try
			{
				// Defer all updates
				obj.BeginUpdate();

				// Fisher-Yates shuffle directly on the array
				int n = obj.Count;
				int k = 0;

				while (n > 1)
				{
					k = context.Random.Next(0, n);
					n--;

					obj.SwapElements(k, n);
				}
			}
			finally
			{
				// Invalidate at the end
				obj.EndUpdate();
			}
		}
	}
}
