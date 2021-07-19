//
// Copyright (c) Peach Fuzzer, LLC
//

using System.ComponentModel;
using NLog;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Mutators
{
	[Mutator("SizedEdgeCase")]
	[Description("Change the size and length of sized data to numerical edge cases")]
	public class SizedEdgeCase : SizedDataEdgeCase
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public SizedEdgeCase(DataElement obj)
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

		protected override bool OverrideRelation
		{
			get
			{
				return false;
			}
		}
	}
}
