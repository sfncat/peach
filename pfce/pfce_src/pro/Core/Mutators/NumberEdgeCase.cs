//
// Copyright (c) Peach Fuzzer, LLC
//

using System.ComponentModel;
using NLog;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Mutators
{
	[Mutator("NumberEdgeCase")]
	[Description("Produce Gaussian distributed numbers around numerical edge cases.")]
	public class NumberEdgeCase : Utility.IntegerEdgeCases
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public NumberEdgeCase(DataElement obj)
			: base(obj)
		{
		}

		protected override NLog.Logger Logger
		{
			get
			{
				return logger;
			}
		}

		protected override void GetLimits(DataElement obj, out long min, out ulong max)
		{
			var asNum = obj as Peach.Core.Dom.Number;
			if (asNum != null)
			{
				min = asNum.MinValue;
				max = asNum.MaxValue;
			}
			else
			{
				System.Diagnostics.Debug.Assert(obj is Peach.Core.Dom.String);

				min = long.MinValue;
				max = long.MaxValue;
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

		protected override void performMutation(DataElement obj, long value)
		{
			obj.MutatedValue = new Variant(value);
			obj.mutationFlags = MutateOverride.Default;
		}

		protected override void performMutation(DataElement obj, ulong value)
		{
			obj.MutatedValue = new Variant(value);
			obj.mutationFlags = MutateOverride.Default;
		}
	}
}
