using System;
using System.Collections.Generic;
using System.Linq;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Core.Test
{
	public class MutatorRunner
	{
		public interface Mutation
		{
			BitwiseStream Value { get; }
			Variant InternalValue { get; }
			DataElement Element { get; }
		}

		class SequentialMutation : Mutation
		{
			Runner runner;
			int mutation;
			DataElement value;

			public SequentialMutation(Runner runner, int mutation)
			{
				this.runner = runner;
				this.mutation = mutation;
			}

			public DataElement Element
			{
				get
				{
					if (value == null)
						MakeValue();

					return value;
				}
			}

			public BitwiseStream Value
			{
				get
				{
					if (value == null)
						MakeValue();

					return value.Value;
				}
			}

			public Variant InternalValue
			{
				get
				{
					if (value == null)
						MakeValue();

					return value.InternalValue;
				}
			}

			private void MakeValue()
			{
				runner.Iteration = (uint)mutation + 1;
				runner.Mutator.mutation = (uint)mutation;
				value = runner.Element.root.Clone();

				// If root is a DataModel, have to manually set actionData
				var dm = value.root as DataModel;
				if (dm != null)
					dm.actionData = ((DataModel)runner.Element.root).actionData;

				Mutate(runner.Mutator, value.find(runner.Element.fullName));
			}

			protected virtual void Mutate(Mutator mutator, DataElement obj)
			{
				mutator.sequentialMutation(obj);
			}
		}

		class RandomMutation : SequentialMutation
		{
			public RandomMutation(Runner runner, int mutation)
				: base(runner, mutation)
			{
			}

			protected override void Mutate(Mutator mutator, DataElement obj)
			{
				mutator.randomMutation(obj);
			}
		}

		class Runner : MutationStrategy
		{
			uint iteration;

			public Mutator Mutator { get; set; }
			public DataElement Element { get; set; }

			public Runner(Type type, DataElement element)
				: base(null)
			{
				Element = element;

				if (type != null)
				{
					Mutator = (Mutator)Activator.CreateInstance(type, element);
					Mutator.context = this;
				}
			}

			public void Initialize()
			{
				Initialize(new RunContext() { config = new RunConfiguration() }, null);
			}

			public override bool UsesRandomSeed
			{
				get { throw new NotImplementedException(); }
			}

			public override bool IsDeterministic
			{
				get { throw new NotImplementedException(); }
			}

			public override uint Count
			{
				get { throw new NotImplementedException(); }
			}

			public override uint Iteration
			{
				get
				{
					return iteration;
				}
				set
				{
					iteration = value;
					SeedRandom();
				}
			}

			public bool IsSupported(Type mutator, DataElement elem)
			{
				return SupportedDataElement(mutator, elem);
			}
		}

		Runner runner;
		Type type;

		public MutatorRunner(string name)
		{
			runner = new Runner(null, null);
			runner.Initialize();
			type = ClassLoader.FindPluginByName<MutatorAttribute>(name);

			if (type == null)
				throw new ArgumentException("Could not find mutator named '{0}'.".Fmt(name));
		}

		public bool IsSupported(DataElement element)
		{
			return runner.IsSupported(type, element);
		}

		public uint LastSeed
		{
			get;
			private set;
		}

		public uint? SeedOverride
		{
			get;
			set;
		}

		public IEnumerable<Mutation> Sequential(DataElement element)
		{
			return Sequential(element, null);
		}

		public IEnumerable<Mutation> Sequential(DataElement element, System.Action cb)
		{
			var strategy = new Runner(type, element);
			strategy.Initialize();

			if (SeedOverride.HasValue)
				strategy.Context.config.randomSeed = SeedOverride.Value;

			LastSeed = strategy.Context.config.randomSeed;

			if (cb != null)
				cb();

			var ret = new List<Mutation>();

			for (int i = 0; i < strategy.Mutator.count; ++i)
				ret.Add(new SequentialMutation(strategy, i));

			return ret;
		}

		public IEnumerable<Mutation> Random(int count, DataElement element)
		{
			return Random(count, element, null);
		}

		public IEnumerable<Mutation> Random(int count, DataElement element, System.Action cb)
		{
			var strategy = new Runner(type, element);
			strategy.Initialize();

			if (SeedOverride.HasValue)
				strategy.Context.config.randomSeed = SeedOverride.Value;

			LastSeed = strategy.Context.config.randomSeed;

			if (cb != null)
				cb();

			var ret = new List<Mutation>();

			for (int i = 0; i < count; ++i)
				ret.Add(new RandomMutation(strategy, i));

			return ret;
		}
	}
}
