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
	[Mutator("StringCaseLower")]
	[Description("Change the string to be all lowercase.")]
	public class StringCaseLower : Mutator
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public StringCaseLower(DataElement obj)
			: base(obj)
		{
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is Peach.Core.Dom.String && obj.isMutable)
			{
				// Esure the string changes when changing the case.
				// TODO: Investigate if it is faster to go 1 char at a time.
				var str = (string)obj.InternalValue;

				if (str != str.ToLower())
					return true;
			}

			return false;
		}

		public override int count
		{
			get
			{
				return 1;
			}
		}

		public override uint mutation
		{
			get;
			set;
		}

		public override void sequentialMutation(DataElement obj)
		{
			try
			{
				var str = (string)obj.InternalValue;

				obj.MutatedValue = new Variant(str.ToLower());
				obj.mutationFlags = MutateOverride.Default;
			}
			catch (NotSupportedException ex)
			{
				logger.Debug("Skipping mutation of {0}, {1}", obj.debugName, ex.Message);
			}
		}

		public override void randomMutation(DataElement obj)
		{
			sequentialMutation(obj);
		}
	}
}
