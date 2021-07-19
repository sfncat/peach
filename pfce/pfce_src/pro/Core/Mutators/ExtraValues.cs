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
	[Mutator("ExtraValues")]
	[Description("Mutates using a user defined list of extra values.")]
	[Hint("ExtraValues", "Semicolon seperated list of values to use for mutations.")]
	public class ExtraValues : Mutator
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		string[] values;
		Func<string, Variant> variant;

		public ExtraValues(DataElement obj)
			: base(obj)
		{
			var str = getHint(obj, "ExtraValues");

			if (string.IsNullOrEmpty(str))
			{
				// Support old ValidValues hint
				str = getHint(obj, "ValidValues");
				System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(str));
				logger.Warn("{0} has deprecated hint 'ValidValues'. The hint has been renamed 'ExtraValues'.", obj.debugName);
			}

			values = str.Split(';');

			if (obj is Peach.Core.Dom.Blob)
				variant = s => new Variant(Encoding.UTF8.GetBytes(s));
			else
				variant = s => new Variant(s);
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if ((obj is Peach.Core.Dom.String || obj is Peach.Core.Dom.Number || obj is Blob) && obj.isMutable)
			{
				if (!string.IsNullOrEmpty(getHint(obj, "ExtraValues")))
					return true;

				// Backwards compatible with old valid values mutator
				if (!string.IsNullOrEmpty(getHint(obj, "ValidValues")))
					return true;
			}

			return false;
		}

		public override int count
		{
			get
			{
				return values.Length;
			}
		}

		public override uint mutation
		{
			get;
			set;
		}

		public override void sequentialMutation(DataElement obj)
		{
			performMutation(obj, (int)mutation);
		}

		public override void randomMutation(DataElement obj)
		{

			performMutation(obj, context.Random.Next(values.Length));
		}

		void performMutation(DataElement obj, int index)
		{
			obj.MutatedValue = variant(values[index]);
			obj.mutationFlags = MutateOverride.Default;
		}
	}
}
