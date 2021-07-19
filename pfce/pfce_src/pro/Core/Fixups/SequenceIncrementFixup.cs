using System;
using System.Collections.Generic;
using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Fixups
{
	[Description("Standard sequential increment fixup.")]
	[Fixup("SequenceIncrement", true)]
	[Fixup("SequenceIncrementFixup")]
	[Fixup("sequence.SequenceIncrementFixup")]
	[Parameter("Offset", typeof(uint?), "Sets the per-iteration initial value to Offset * (Iteration - 1)", "")]
	[Parameter("Once", typeof(bool), "Only increment once per iteration", "false")]
	[Parameter("Group", typeof(string), "Name of group to increment", "")]
	[Parameter("InitialValue", typeof(uint), "Initial number to start at", "1")]
	[Serializable]
	public class SequenceIncrementFixup : Peach.Core.Fixups.VolatileFixup
	{
		public uint? Offset { get; private set; }
		public bool Once { get; private set; }
		public string Group { get; private set; }
		public uint InitialValue { get; private set; }

		string _stateKey = null;

		public SequenceIncrementFixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
			ParameterParser.Parse(this, args);

			if (string.IsNullOrEmpty(Group))
				_stateKey = "Peach.SequenceIncrementFixup." + parent.fullName;
			else
				_stateKey = "Peach.SequenceIncrementFixup.Group." + Group;

			if (parent is Peach.Core.Dom.String)
				parent.DefaultValue = new Variant(0);
		}

		protected override Variant OnActionRun(RunContext ctx)
		{
			if (!(parent is Peach.Core.Dom.Number) && !(parent is Peach.Core.Dom.String && parent.Hints.ContainsKey("NumericalString")))
				throw new PeachException("SequenceIncrementFixup has non numeric parent '" + parent.fullName + "'.");

			var increment = true;
			ulong max = parent is Number ? ((Number)parent).MaxValue : ulong.MaxValue;
			ulong value = 0;
			object obj = null;
			var initialValue = false;

			if (ctx.stateStore.TryGetValue(_stateKey, out obj))
				value = (ulong)obj;
			else
				initialValue = true;

			if (ctx.iterationStateStore.ContainsKey(_stateKey))
				increment &= !Once;
			else if (Offset.HasValue)
				value = (ulong)Offset.Value * (ctx.currentIteration - 1);

			// For 2 bit number, offset is 2, 2 actions per iter:
			// Iter:  1a,1b,2a,2b,3a,3b,4a,4b,5a,5b,6a,6b
			// It-1:  0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5
			// Pre:   0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10,11
			// Want:  0, 1, 2, 0, 1, 2, 0, 1, 2, 0, 1, 2
			// Final: 1, 2, 3, 1, 2, 3, 1, 2, 3, 1, 2, 3
			if (value > max)
				value = value % max;

			if (increment)
			{
				if (initialValue)
					value = InitialValue;
				else
					value++;

				if (value > max)
					value -= max;

				ctx.stateStore[_stateKey] = value;
				ctx.iterationStateStore[_stateKey] = value;
			}

			return new Variant(value);
		}
	}
}

// end
