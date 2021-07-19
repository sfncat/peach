using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NLog;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Pro.Core.Mutators.Utility;
using Action = Peach.Core.Dom.Action;
using Logger = NLog.Logger;

namespace Peach.Pro.Core.MutationStrategies
{
	public abstract class WeightedMutationStrategy : MutationStrategy
	{
		[DebuggerDisplay("{InstanceName} {ElementName} Mutators = {Mutators.Count}")]
		protected class MutableItem : INamed, IWeighted
		{
			#region Obsolete Functions

			[Obsolete("This property is obsolete and has been replaced by the Name property.")]
			public string name { get { return Name; } }

			#endregion

			public MutableItem(string instanceName, string elementName, ElementWeight weight)
				: this(instanceName, elementName, new Mutator[0])
			{
				Weight = (int)weight;
			}

			public MutableItem(string instanceName, string elementName, ICollection<Mutator> mutators)
			{
				InstanceName = instanceName;
				ElementName = elementName;
				Mutators = new WeightedList<Mutator>(mutators);
				Weight = 1;
			}

			public string Name { get { return InstanceName; } }
			public int Weight { get; set; }
			public string InstanceName { get; private set; }
			public string ElementName { get; private set; }
			public WeightedList<Mutator> Mutators { get; private set; }
			public int SelectionWeight { get { return Mutators.SelectionWeight; } }

			public int TransformWeight(Func<int, int> how)
			{
				return Weight * Mutators.TransformWeight(how);
			}
		}

		[DebuggerDisplay("{Name} Count = {Count}")]
		[DebuggerTypeProxy(typeof(DebugView))]
		protected class MutationScope : WeightedList<MutableItem>
		{
			class DebugView
			{
				readonly MutationScope _obj;

				public DebugView(MutationScope obj)
				{
					_obj = obj;
				}

				[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
				public MutableItem[] Items
				{
					get { return _obj.ToArray(); }
				}
			}

			public MutationScope(string name)
			{
				Name = name;
			}

			public MutationScope(string name, IEnumerable<MutableItem> collection)
				: base(collection)
			{
				Name = name;
			}

			public string Name
			{
				get;
				private set;
			}

			public int ChildScopes
			{
				get;
				set;
			}
		}

		static Logger logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Mutators that affect the data model
		/// </summary>
		protected List<Type> dataMutators = new List<Type>();

		/// <summary>
		/// Used on control record iterations to collect
		/// mutable elements across all states and actions.
		/// </summary>
		protected MutationScope mutationScopeGlobal;

		/// <summary>
		/// Used on control record iterations to collect
		/// mutable elements on a per state basis.
		/// </summary>
		protected List<MutationScope> mutationScopeState;

		/// <summary>
		/// Used on control record iterations to collect
		/// mutable elements on a per action basis.
		/// </summary>
		protected List<MutationScope> mutationScopeAction;

		/// <summary>
		/// List of all mutable items at each mutation scope.
		/// </summary>
		protected WeightedList<MutationScope> mutableItems = new WeightedList<MutationScope>();

		/// <summary>
		/// The selected mutations for a given fuzzing iteration
		/// </summary>
		protected MutableItem[] mutations;

		/// <summary>
		/// Controls how weights are applied to the weighted list.
		/// </summary>
		protected readonly Func<int, int> _tuneWeights;

		/// <summary>
		/// List of mutations that have taken place during the most recent iteration
		/// </summary>
		protected readonly List<string> mutationHistory = new List<string>();

		/// <summary>
		/// Maximum number of fields to mutate at once.
		/// </summary>
		public int MaxFieldsToMutate
		{
			get;
			set;
		}

		protected WeightedMutationStrategy(Dictionary<string, Variant> args)
			: base(args)
		{
			MaxFieldsToMutate = 6;
			if (args.ContainsKey("MaxFieldsToMutate"))
				MaxFieldsToMutate = int.Parse((string)args["MaxFieldsToMutate"]);

			var weighting = 10;
			if (args.ContainsKey("Weighting"))
				weighting = int.Parse((string)args["Weighting"]);

			if (weighting < 1)        // If param<1, weight=1
				_tuneWeights = x => 1;
			else if (weighting == 1)  // If param=1, weight=SelectionWeight
				_tuneWeights = x => x;
			else                      // If param>1, weight=LOG(SelectionWeight,param)
				_tuneWeights = x => (int)Math.Round(Math.Log(x, weighting)) + 1;

			mutationScopeGlobal = new MutationScope("All");
			mutationScopeState = new List<MutationScope>();
			mutationScopeAction = new List<MutationScope>();
		}

		protected int GetMutationCount()
		{
			while (true)
			{
				// For half bell curves, sigma should be 1/3 of our range
				var sigma = MaxFieldsToMutate / 3.0;

				var num = Random.NextGaussian(0, sigma);

				// Only want half a bell curve
				num = Math.Abs(num);

				var asInt = (int)Math.Floor(num) + 1;

				if (asInt > MaxFieldsToMutate)
					continue;

				return asInt;
			}
		}

		protected void RecordDataModel(Action action)
		{
			var scopeState = mutationScopeState.Last();

			var name = "Run_{0}.{1}.{2}".Fmt(action.parent.runCount, action.parent.Name, action.Name);
			var scopeAction = new MutationScope(name);

			foreach (var item in action.outputData)
			{
				var allElements = new List<DataElement>();
				RecursevlyGetElements(item.dataModel, allElements);

				foreach (var elem in allElements)
				{
					if (elem.Weight == ElementWeight.Off)
						continue;

					var rec = new MutableItem(item.instanceName, elem.fullName, elem.Weight);
					var e = elem;

					rec.Mutators.AddRange(dataMutators
						.Where(m => SupportedDataElement(m, e))
						.Select(m => GetMutatorInstance(m, e))
						.Where(m => m.SelectionWeight > 0));

					if (rec.Mutators.Count > 0)
					{
						mutationScopeGlobal.Add(rec);
						scopeState.Add(rec);
						scopeAction.Add(rec);
					}
				}
			}

			mutationScopeAction.Add(scopeAction);

			// If the action scope has mutable items, then the
			// state scope has a valid child scope.  This is
			// used later to prune empty state scopes.
			if (scopeAction.Count > 0)
				scopeState.ChildScopes += 1;
		}

		[Conditional("DEBUG")]
		void RecordMutation(string instanceName, string elementName, string mutatorName)
		{
			mutationHistory.Add(instanceName + "." + elementName + " + " + mutatorName);
		}

		private void ApplyMutation(ActionData data)
		{
			var instanceName = data.instanceName;

			foreach (var item in mutations)
			{
				if (item.InstanceName != instanceName)
					continue;

				var elem = data.dataModel.find(item.ElementName);
				if (elem != null && elem.mutationFlags == MutateOverride.None)
				{
					var mutator = Random.WeightedChoice(item.Mutators);
					Context.OnDataMutating(data, elem, mutator);
					logger.Debug("Action_Starting: Fuzzing: {0}", item.ElementName);
					logger.Debug("Action_Starting: Mutator: {0}", mutator.Name);
					mutator.randomMutation(elem);

					RecordMutation(instanceName, item.ElementName, mutator.Name);

					// Trigger re-generation of data
					// needed for Frag element.
					//var obj = data.dataModel.Value;
				}
				else
				{
					logger.Debug("Action_Starting: Skipping Fuzzing: {0}", item.ElementName);
				}
			}
		}

		protected void MutateDataModel(Action action)
		{
			// MutateDataModel should only be called after ParseDataModel
			Debug.Assert(Iteration > 0);

			foreach (var item in action.outputData)
			{
				ApplyMutation(item);
			}
		}

	}
}
