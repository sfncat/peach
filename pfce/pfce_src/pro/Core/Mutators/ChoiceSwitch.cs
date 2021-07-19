//
// Copyright (c) Peach Fuzzer, LLC
//

using System.Collections.Generic;
using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Mutators
{
	[Mutator("ChoiceSwitch")]
	[Description("Changes which element is selected in a Choice statement.")]
	public class ChoiceSwitch : Mutator
	{
		List<int> options = new List<int>();

		public ChoiceSwitch(DataElement obj)
			: base(obj)
		{
			var asChoice = (Choice)obj;

			System.Diagnostics.Debug.Assert(asChoice.SelectedElement != null);

			for (int i = 0; i < asChoice.choiceElements.Count; ++i)
			{
				// Don't mutate to our currently selected choice
				if (asChoice.choiceElements[i] != asChoice.SelectedElement)
					options.Add(i);
			}
		}

		public override uint mutation
		{
			get;
			set;
		}

		public override int count
		{
			get
			{
				return options.Count;
			}
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			var asChoice = obj as Choice;
			if (asChoice != null && asChoice.isMutable && asChoice.choiceElements.Count > 1)
				return true;

			return false;
		}

		public override void sequentialMutation(DataElement obj)
		{
			performMutation(obj, (int)mutation);
		}

		public override void randomMutation(DataElement obj)
		{
			performMutation(obj, context.Random.Next(0, options.Count));
		}

		void performMutation(DataElement obj, int idx)
		{
			var asChoice = (Choice)obj;
			var selection = options[idx];

			asChoice.SelectElement(asChoice.choiceElements[selection]);
			obj.mutationFlags = MutateOverride.Default;
		}
	}
}
