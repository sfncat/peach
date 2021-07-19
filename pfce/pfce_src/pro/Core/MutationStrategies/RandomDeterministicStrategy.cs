

using System;
using System.Collections.Generic;
using Peach.Core;
using Random = Peach.Core.Random;

namespace Peach.Pro.Core.MutationStrategies
{
	[MutationStrategy("RandomDeterministic")]
	[Serializable]
	public class RandomDeterministicStrategy : Sequential
	{
		uint _mapping = 0;
		SequenceGenerator sequence = null;

		public RandomDeterministicStrategy(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		public override uint Iteration
		{
			get
			{
				return _mapping;
			}
			set
			{
				_mapping = value;

				if (!Context.controlIteration)
					base.Iteration = sequence.Get(value);
				else
					base.Iteration = value;
			}
		}

		protected override void OnDataModelRecorded()
		{
			// This strategy should randomize the order of mutators
			// that would be performed by the sequential mutation strategy.
			// The shuffle should always use the same seed.
			var rng = new Random(Seed);
			var elements = rng.Shuffle(_iterations.ToArray());
			_iterations.Clear();
			_iterations.AddRange(elements);

			if (this.Count > 0)
				sequence = new SequenceGenerator(this.Count);
		}
	}
}
