

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NLog;
using Peach.Core;
using Peach.Core.Dom;
using Action = Peach.Core.Dom.Action;
using Logger = NLog.Logger;
using Random = Peach.Core.Random;

/*
 * If not 1st iteration, pick fandom data model to change
 * 
 */
namespace Peach.Pro.Core.MutationStrategies
{
	[DefaultMutationStrategy]
	[MutationStrategy("Random")]
	[Alias("RandomStrategy")]
	[Parameter("SwitchCount", typeof(int), "Number of iterations to perform per-mutator befor switching.", "200")]
	[Parameter("MaxFieldsToMutate", typeof(int), "Maximum fields to mutate at once.", "6")]
	[Parameter("StateMutation", typeof(bool), "Enable state mutations.", "false")]
	[Parameter("Weighting", typeof(int), "Controls mutation weight evaulation.", "10")]
	public class RandomStrategy : WeightedMutationStrategy
	{
		[DebuggerDisplay("{Name} - {Options.Count} Options")]
		protected class DataSetTracker : INamed
		{
			#region Obsolete Functions

			[Obsolete("This property is obsolete and has been replaced by the Name property.")]
			public string name { get { return Name; } }

			#endregion

			public DataSetTracker(string modelName, List<Data> options)
			{
				ModelName = modelName;
				Options = options;
				Iteration = 1;
			}

			public string Name { get { return ModelName; } }
			public string ModelName { get; private set; }
			public List<Data> Options { get; private set; }
			public uint Iteration { get; set; }
		}

		static Logger logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Collection of all dataSets across all fully qualified model names.
		/// NamedCollection guarantees element order based on insertion.
		/// </summary>
		NamedCollection<DataSetTracker> dataSets = new NamedCollection<DataSetTracker>();

		/// <summary>
		/// The iteration when the last data set switch occured.
		/// </summary>
		uint lastSwitchIteration = 1;

		/// <summary>
		/// How often to switch files.
		/// </summary>
		uint switchCount = 200;

		/// <summary>
		/// Random number generator used for switching data sets.
		/// This is independent from context.Random so that if we skip to iteration
		/// 505, we will use a random number generator seeded with the 'switch'
		/// iteration of 401.
		/// </summary>
		Random randomDataSet;

		/// <summary>
		/// Mutators that affect the state model
		/// </summary>
		List<Mutator> stateMutators = new List<Mutator>();

		/// <summary>
		/// The currently selected state model mutator.
		/// Null if no state model mutator is selected.
		/// </summary>
		Mutator stateModelMutation;

		/// <summary>
		/// The most recent state that has started.
		/// </summary>
		State currentState;

		/// <summary>
		/// The most recent action that was started.
		/// </summary>
		Action currentAction;

		/// <summary>
		/// Current fuzzing iteration number
		/// </summary>
		uint iteration;

		bool stateMutations;

		public RandomStrategy(Dictionary<string, Variant> args)
			: base(args)
		{
			if (args.ContainsKey("SwitchCount"))
				switchCount = uint.Parse((string)args["SwitchCount"]);
			if (args.ContainsKey("StateMutation"))
				stateMutations = bool.Parse((string)args["StateMutation"]);
		}

		#region Mutation Strategy Overrides

		public override void Initialize(RunContext context, Engine engine)
		{
			base.Initialize(context, engine);

			context.ActionStarting += ActionStarting;
			context.StateStarting += StateStarting;
			engine.IterationStarting += IterationStarting;
			engine.IterationFinished += IterationFinished;
			context.StateModelStarting += StateModelStarting;

			foreach (var m in EnumerateValidMutators())
			{
				if (m.GetStaticField<bool>("affectDataModel"))
					dataMutators.Add(m);

				if (stateMutations && m.GetStaticField<bool>("affectStateModel"))
					stateMutators.Add(GetMutatorInstance(m, context.test.stateModel));
			}

			RecordDataSets();

			logger.Debug("Initialized with seed {0}", Seed);
		}

		public override void Finalize(RunContext context, Engine engine)
		{
			base.Finalize(context, engine);

			context.ActionStarting -= ActionStarting;
			context.StateStarting -= StateStarting;
			engine.IterationStarting -= IterationStarting;
			engine.IterationFinished -= IterationFinished;
			context.StateModelStarting -= StateModelStarting;
		}

		public override bool UsesRandomSeed
		{
			get
			{
				return true;
			}
		}

		public override bool IsDeterministic
		{
			get
			{
				return false;
			}
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
				SeedRandomDataSet();
			}
		}

		public override uint Count
		{
			get
			{
				return uint.MaxValue;
			}
		}

		#endregion

		void IterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
		{
			// Reset per-iteration state
			mutations = null;
			stateModelMutation = null;
			currentAction = null;
			currentState = null;
			mutationHistory.Clear();

			if (context.controlIteration && context.controlRecordingIteration)
			{
				mutableItems.Clear();

				mutationScopeGlobal = new MutationScope("All");
				mutationScopeState = new List<MutationScope>();
				mutationScopeAction = new List<MutationScope>();

				SyncDataSets();
			}
			else
			{
				// Random.Next() Doesn't include max and we want it to
				var fieldsToMutate = GetMutationCount();

				if (mutableItems.Count == 0)
				{
					logger.Trace("No mutable items.");
					mutations = new MutableItem[0];
				}
				else
				{
					// Our scope choice should auto-weight to Action, State, All
					// the more states and actions the less All will get chosen
					// might need to improve this in the future.
					var scope = Random.WeightedChoice(mutableItems);

					logger.Trace("SCOPE: {0}", scope.Name);

					mutations = Random.WeightedSample(scope, fieldsToMutate);
				}
			}
		}

		void IterationFinished(RunContext context, uint currentIteration)
		{
			if (context.controlIteration && context.controlRecordingIteration)
			{
				// Build the final list of mutable items
				// NOTE: We can't alter the contents of a scope after adding it to
				// mutableItems because the weights won't be updated.

				// Add mutations scoped by state, where there is at least one available
				// mutation and more than one action scopes.  If there is only one action
				// scope then the state scope and action scope are identical.
				mutableItems.AddRange(mutationScopeState.Where(m => m.Count > 0 && m.ChildScopes > 1));

				// Add mutations scoped by action only for actions that have
				// mutable items.
				mutableItems.AddRange(mutationScopeAction.Where(m => m.Count > 0));

				// If there is only a single mutable action, it means global scope
				// should be the same as the single action
				if (mutableItems.Count == 1)
				{
					// No states should have contributed to mutableItems
					Debug.Assert(!mutationScopeState.Any(m => m.Count > 0 && m.ChildScopes > 1));
					// The sum of mutations should be the same
					Debug.Assert(mutableItems.Select(m => m.Count).Sum() == mutationScopeGlobal.Count);
					// Clear mutable items since global is the same as the single action
					mutableItems.Clear();
				}

				// If there are state model mutators, add those.
				if (stateMutators.Count > 0)
				{
					mutableItems.Add(new MutationScope("StateModel", new[] {
						new MutableItem(context.test.stateModel.Name, "", stateMutators),
					}));
				}

				// Add global scope 1st
				if (mutationScopeGlobal.Count > 0)
					mutableItems.Add(mutationScopeGlobal);

				mutableItems.TransformWeight(_tuneWeights);

				// Cleanup containers used to collect the different scopes
				mutationScopeGlobal = null;
				mutationScopeAction = null;
				mutationScopeState = null;
			}
		}

		void StateModelStarting(RunContext context, StateModel stateModel)
		{
			if (!context.controlIteration)
			{
				// All state mutations are in the same scope.
				// When state model mutation is selected, there should only
				// be a single picked mutation.
				var m = mutations.FirstOrDefault(i => i.InstanceName == stateModel.Name);
				if (m != null)
				{
					Debug.Assert(mutations.Length == 1);
					stateModelMutation = Random.WeightedChoice(m.Mutators);
					stateModelMutation.randomMutation(stateModel);
				}
			}
		}

		void ActionStarting(RunContext context, Action action)
		{
			currentAction = action;

			// Is this a supported action?
			if (!action.outputData.Any())
				return;

			if (context.controlIteration && context.controlRecordingIteration)
			{
				RecordDataModel(action);
			}
			else if (!context.controlIteration)
			{
				MutateDataModel(action);
			}
		}

		void StateStarting(RunContext context, State state)
		{
			currentState = state;

			if (context.controlIteration && Context.controlRecordingIteration)
			{
				var name = "Run_{0}.{1}".Fmt(state.runCount, state.Name);
				var scope = new MutationScope(name);
				mutationScopeState.Add(scope);
			}
		}

		#region DataSet Tracking And Switching

		private uint GetSwitchIteration()
		{
			// Returns the iteration we should switch our dataSet based off our
			// current iteration. For example, if switchCount is 10, this function
			// will return 1, 11, 21, 31, 41, 51, etc.
			var ret = Iteration - ((Iteration - 1) % switchCount);
			return ret;
		}

		private void SeedRandomDataSet()
		{
			var switchIteration = GetSwitchIteration();

			if (lastSwitchIteration != switchIteration && dataSets.Any(d => d.Options.Count > 1))
			{
				logger.Debug("Switch iteration, setting controlIteration and controlRecordingIteration.");

				// Only enable switch iteration if there is at least one data set
				// with two or more options.
				randomDataSet = null;
			}

			if (randomDataSet == null)
			{
				randomDataSet = new Random(Seed + switchIteration);

				Context.controlIteration = true;
				Context.controlRecordingIteration = true;
				lastSwitchIteration = switchIteration;
			}
		}

		private void RecordDataSets()
		{
			var states = Context.test.stateModel.states;

			foreach (var item in states.SelectMany(s => s.actions).SelectMany(a => a.outputData))
			{
				var options = item.allData.ToList();

				if (options.Count <= 0)
					continue;

				// Don't use the instance name here, we only pick the data set
				// once per state, not each time the state is re-entered.
				Debug.Assert(!dataSets.Contains(item.modelName));
				dataSets.Add(new DataSetTracker(item.modelName, options));
			}
		}

		private void SyncDataSets()
		{
			Debug.Assert(Iteration != 0);

			// Compute the iteration we need to switch on
			var switchIteration = GetSwitchIteration();

			var states = Context.test.stateModel.states;

			foreach (var item in states.SelectMany(s => s.actions).SelectMany(a => a.outputData))
			{
				ApplyDataSet(item, switchIteration);
			}
		}

		private void ApplyDataSet(ActionData item, uint switchIteration)
		{
			// Note: use the model name, not the instance name so
			// we only set the data set once for re-enterant states.
			var modelName = item.modelName;

			DataSetTracker val;
			if (!dataSets.TryGetValue(modelName, out val))
				return;

			// If the last switch was within the current iteration range then we don't have to switch.
			if (switchIteration == val.Iteration)
				return;

			// Don't switch files if we are only using a single file :)
			if (val.Options.Count(x => !x.Ignore) < 2)
				return;

			do
			{
				var opt = randomDataSet.Choice(val.Options);

				// If data set was determined to be bad, ignore it
				if (opt.Ignore)
					continue;

				try
				{
					// Apply the data set option
					item.Apply(opt);

					// Save off the last switch iteration
					val.Iteration = switchIteration;

					// Done!
					return;
				}
				catch (PeachException ex)
				{
					logger.Debug(ex.Message);
					logger.Debug("Unable to apply data '{0}', removing from sample list.", opt.Name);

					// Mark data set as ignored.
					// This is so skip-to will still be deterministic
					opt.Ignore = true;
				}
			}
			while (val.Options.Any(x => !x.Ignore));

			throw new PeachException("Error, RandomStrategy was unable to apply data for \"" + item.dataModel.fullName + "\"");
		}

		#endregion

		public override State MutateChangingState(State nextState)
		{
			if (stateModelMutation != null)
			{
				Debug.Assert(!Context.controlIteration);

				Context.OnStateMutating(nextState, stateModelMutation);

				logger.Debug("MutateChangingState: Fuzzing state change: {0}", nextState.Name);
				logger.Debug("MutateChangingState: Mutator: {0}", stateModelMutation.Name);

				return stateModelMutation.changeState(currentState, currentAction, nextState);
			}

			return nextState;
		}

		public override Action NextAction(State state, Action lastAction, Action nextAction)
		{
			if (stateModelMutation != null)
			{
				Debug.Assert(!Context.controlIteration);
				return stateModelMutation.nextAction(state, lastAction, nextAction);
			}

			return nextAction;
		}
	}
}

// end
