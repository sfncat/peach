//
// Copyright (c) Peach Fuzzer, LLC
//

using System;
using System.ComponentModel;
using NLog;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Mutators
{
	[Mutator("StringLengthEdgeCase")]
	[Description("Produce Gaussian distributed string lengths around numerical edge cases.")]
	public class StringLengthEdgeCase : Utility.IntegerEdgeCases
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public StringLengthEdgeCase(DataElement obj)
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
			min = 0;
			max = (ulong)Utility.SizedHelpers.MaxSize(obj);
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is Peach.Core.Dom.String && obj.isMutable)
				return true;

			return false;
		}

		protected override void performMutation(DataElement obj, long value)
		{
			var limit = Utility.SizedHelpers.MaxSize(obj);
			if (value > limit)
			{
				logger.Trace("Skipping mutation, expansion to {0} would exceed max output size.", value);
				return;
			}

			Utility.SizedHelpers.ExpandStringTo(obj, value);
		}

		protected override void performMutation(DataElement obj, ulong value)
		{
			// Should never get a ulong
			throw new NotImplementedException();
		}

	}
}
