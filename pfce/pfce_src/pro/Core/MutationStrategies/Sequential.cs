

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NLog;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.MutationStrategies
{
	[MutationStrategy("Sequential")]
	[Serializable]
	public class Sequential : MutationStrategy
	{
		protected class Iterations : List<Tuple<string, Mutator, string>> { }

		[NonSerialized]
		protected static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		[NonSerialized]
		protected IEnumerator<Tuple<string, Mutator, string>> _enumerator;

		[NonSerialized]
		protected Iterations _iterations = new Iterations();

		private List<Type> _mutators = null;
		private uint _count = 1;
		private uint _iteration = 1;
		Peach.Core.Dom.Action _currentAction;
		State _currentState;

		public Sequential(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		public override void Initialize(RunContext context, Engine engine)
		{
			base.Initialize(context, engine);

			// Force seed to always be the same
			context.config.randomSeed = 31337;

			context.ActionStarting += ActionStarting;
			context.StateStarting += StateStarting;
			engine.IterationFinished += engine_IterationFinished;
			engine.IterationStarting += engine_IterationStarting;
			_mutators = new List<Type>();

			_mutators.AddRange(EnumerateValidMutators().Where(
	v => (bool)v.GetField(
			"affectDataModel",
			BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy)
				.GetValue(null)));

		}

		void engine_IterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
		{
			if (context.controlIteration && context.controlRecordingIteration)
			{
				// Starting to record
				_iterations = new Iterations();
				_count = 0;
			}
		}

		void engine_IterationFinished(RunContext context, uint currentIteration)
		{
			// If we were recording, end of iteration is end of recording
			if(context.controlIteration && context.controlRecordingIteration)
				OnDataModelRecorded();	
		}

		public override void Finalize(RunContext context, Engine engine)
		{
			base.Finalize(context, engine);

			context.ActionStarting -= ActionStarting;
			context.StateStarting -= StateStarting;
			engine.IterationStarting -= engine_IterationStarting;
			engine.IterationFinished -= engine_IterationFinished;
		}

		protected virtual void OnDataModelRecorded()
		{
		}

		public override bool UsesRandomSeed
		{
			get
			{
				return false;
			}
		}

		public override bool IsDeterministic
		{
			get
			{
				return true;
			}
		}

		public override uint Iteration
		{
			get
			{
				return _iteration;
			}
			set
			{
				SetIteration(value);
				SeedRandom();
			}
		}

		private void SetIteration(uint value)
		{
			System.Diagnostics.Debug.Assert(value > 0);

			if (Context.controlIteration && Context.controlRecordingIteration)
			{
				return;
			}

			if (_iteration == 1 || value < _iteration)
			{
				_iteration = 1;
				_enumerator = _iterations.GetEnumerator();
				_enumerator.MoveNext();
				_enumerator.Current.Item2.mutation = 0;
			}

			uint needed = value - _iteration;

			if (needed == 0)
				return;

			while (true)
			{
				var mutator = _enumerator.Current.Item2;
				uint remain = (uint)mutator.count - mutator.mutation;

				if (remain > needed)
				{
					mutator.mutation += needed;
					_iteration = value;
					return;
				}

				needed -= remain;
				_enumerator.MoveNext();
				_enumerator.Current.Item2.mutation = 0;
			}
		}

		private void ActionStarting(RunContext context, Peach.Core.Dom.Action action)
		{
			_currentAction = action;

			// Is this a supported action?
			if (!action.outputData.Any())
				return;

			if (!Context.controlIteration)
				MutateDataModel(action);

			else if (Context.controlIteration && Context.controlRecordingIteration)
				RecordDataModel(action);
		}

		void StateStarting(RunContext context, State state)
		{
			_currentState = state;

			//if (_context.controlIteration && _context.controlRecordingIteration)
			//{
			//	foreach (Type t in _mutators)
			//	{
			//		// can add specific mutators here
			//		if (SupportedState(t, state))
			//		{
			//			var mutator = GetMutatorInstance(t, state);
			//			var key = "Run_{0}.{1}".Fmt(state.runCount, state.name);
			//			_iterations.Add(new Tuple<string, Mutator, string>(key, mutator, null));
			//			_count += (uint)mutator.count;
			//		}
			//	}
			//}
		}

		// Recursivly walk all DataElements in a container.
		// Add the element and accumulate any supported mutators.
		private void GatherMutators(string instanceName, DataElementContainer cont)
		{
			List<DataElement> allElements = new List<DataElement>();
			RecursevlyGetElements(cont, allElements);
			foreach (DataElement elem in allElements)
			{
				var elementName = elem.fullName;

				foreach (Type t in _mutators)
				{
					// can add specific mutators here
					if (SupportedDataElement(t, elem))
					{
						var mutator = GetMutatorInstance(t, elem);
						_iterations.Add(new Tuple<string, Mutator, string>(elementName, mutator, instanceName));
						_count += (uint)mutator.count;
					}
				}
			}
		}

		private void RecordDataModel(Peach.Core.Dom.Action action)
		{
			// ParseDataModel should only be called during iteration 0
			System.Diagnostics.Debug.Assert(Context.controlIteration && Context.controlRecordingIteration);

			foreach (var item in action.outputData)
			{
				var instanceName = item.instanceName;
				GatherMutators(instanceName, item.dataModel);
			}
		}

		/// <summary>
		/// Allows mutation strategy to affect state change.
		/// </summary>
		/// <param name="state"></param>
		/// <returns></returns>
		public override State MutateChangingState(State state)
		{
			if (Context.controlIteration)
				return state;

			var key = "Run_{0}.{1}".Fmt(state.runCount, state.Name);
			if (key == _enumerator.Current.Item1)
			{
				Context.OnStateMutating(state, _enumerator.Current.Item2);
				logger.Debug("MutateChangingState: Fuzzing state change: {0}", state.Name);
				logger.Debug("MutateChangingState: Mutator: {0}", _enumerator.Current.Item2.Name);

				return _enumerator.Current.Item2.changeState(_currentState, _currentAction, state);
			}

			return state;
		}

		private void ApplyMutation(ActionData data)
		{
			// Ensure we are on the right model
			if (_enumerator.Current.Item3 != data.instanceName)
				return;

			var fullName = _enumerator.Current.Item1;
			var dataElement = data.dataModel.find(fullName);

			if (dataElement != null)
			{
				var mutator = _enumerator.Current.Item2;
				Context.OnDataMutating(data, dataElement, mutator);
				logger.Debug("ApplyMutation: Fuzzing: {0}", fullName);
				logger.Debug("ApplyMutation: Mutator: {0}", mutator.Name);
				mutator.sequentialMutation(dataElement);
			}
		}

		private void MutateDataModel(Peach.Core.Dom.Action action)
		{
			// MutateDataModel should only be called after ParseDataModel
			System.Diagnostics.Debug.Assert(_count >= 1);
			System.Diagnostics.Debug.Assert(_iteration > 0);
			System.Diagnostics.Debug.Assert(!Context.controlIteration);

			foreach (var item in action.outputData)
			{
				ApplyMutation(item);
			}
		}

		public override uint Count
		{
			get
			{
				return _count;
			}
		}
	}
}

// end
