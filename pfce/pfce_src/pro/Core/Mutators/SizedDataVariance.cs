//
// Copyright (c) Peach Fuzzer, LLC
//

using System;
using System.ComponentModel;
using System.Linq;
using NLog;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Mutators
{
	[Mutator("SizedDataVariance")]
	[Description("Change the length of sized data to count - N to count + N.")]
	public class SizedDataVariance : Utility.IntegerVariance
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public SizedDataVariance(DataElement obj)
			: base(obj, false)
		{
		}

		protected override NLog.Logger Logger
		{
			get
			{
				return logger;
			}
		}

		protected virtual bool OverrideRelation
		{
			get
			{
				return true;
			}
		}

		protected override void GetLimits(DataElement obj, out bool signed, out long value, out long min, out long max)
		{
			var other = obj.relations.From<SizeRelation>().First(r => r.Of.InScope()).Of;

			signed = false;
			min = 0;
			max = Utility.SizedHelpers.MaxSize(other);

			// If we are a number, make sure our max is not larger than max long
			// since stream lengths are tracked as longs
			var asNum = obj as Peach.Core.Dom.Number;
			if (asNum != null)
				max = (long)Math.Min((ulong)max, asNum.MaxValue);
			else
				System.Diagnostics.Debug.Assert(obj is Peach.Core.Dom.String);

			// Since we cap max at ushort.MaxValue, make sure value is not larger!
			value = Math.Min((long)obj.InternalValue, max);
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			// Any mutable object with a size relation
			if (obj.isMutable && obj.relations.From<SizeRelation>().Any(r => r.Of.InScope()))
				return true;

			return false;
		}

		protected override void performMutation(DataElement obj, long value)
		{
			var rel = obj.relations.From<SizeRelation>().FirstOrDefault(r => r.Of.InScope());

			if (rel != null)
			{
				var limit = Utility.SizedHelpers.MaxSize(rel.Of);
				if (value > limit)
				{
					logger.Trace("Skipping mutation, expansion to {0} would exceed max output size.", value);
					return;
				}
			}

			Utility.SizedHelpers.ExpandTo(obj, value, OverrideRelation);
		}

		protected override void performMutation(DataElement obj, ulong value)
		{
			// Should never get a ulong
			throw new NotImplementedException();
		}
	}
}
