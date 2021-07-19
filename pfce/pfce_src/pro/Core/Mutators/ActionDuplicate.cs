//
// Copyright (c) Peach Fuzzer, LLC
//

using System;
using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Mutators
{
	[Mutator("ActionDuplicate")]
	[Description("Causes Actions to be repeated. Will not repeat actions with publisher of Peach.Agent.")]
	public class ActionDuplicate: Mutator
	{
		//static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public static new readonly bool affectDataModel = false;
		public static new readonly bool affectStateModel = true;

		/// <summary>
		/// Maximum number of repetitions to perform
		/// </summary>
		int _n = 50;

		/// <summary>
		/// Count is actions * N
		/// </summary>
		int _count = 0;
		uint _mutation = 0;

		/// <summary>
		/// Total count of actions
		/// </summary>
		int _actionCount = 0;

		public ActionDuplicate(StateModel model)
			: base(model)
		{
			foreach (var state in model.states)
				_actionCount += state.actions.Count;

			_count = _actionCount * _n;
		}

		public override int count
		{
			get { return _count; }
		}

		public override uint mutation
		{
			get
			{
				return _mutation;
			}
			set
			{
				_mutation = value;
			}
		}

		public override void sequentialMutation(Peach.Core.Dom.DataElement obj)
		{
			throw new NotImplementedException();
		}

		public override void randomMutation(Peach.Core.Dom.DataElement obj)
		{
			throw new NotImplementedException();
		}

		public override void sequentialMutation(Peach.Core.Dom.StateModel obj)
		{
			int actionIndex = (int)_mutation / _n;
			var duplicate = context.Random.Next(_n + 1);

			foreach (var state in obj.states)
			{
				if (actionIndex > state.actions.Count)
				{
					actionIndex -= state.actions.Count;
					continue;
				}

				var action = state.actions[actionIndex];

				for (int i = 0; i < duplicate; i++)
					state.actions.Insert(actionIndex, action);
			}
		}

		Peach.Core.Dom.Action _targetAction = null;
		int _targetDuplicate = -1;

		public override void randomMutation(Peach.Core.Dom.StateModel obj)
		{
			do
			{
				int actionIndex = context.Random.Next(_actionCount + 1);
				_targetDuplicate = context.Random.Next(_n + 1);

				foreach (var state in obj.states)
				{
					if (actionIndex >= state.actions.Count)
					{
						actionIndex -= state.actions.Count;
						continue;
					}

					_targetAction = state.actions[actionIndex];
					break;
				}
			}
			while (!SupportedActionType(_targetAction));
		}

		bool SupportedActionType(Peach.Core.Dom.Action action)
		{
			if (action is Peach.Core.Dom.Actions.Call)
				return true;
			if (action is Peach.Core.Dom.Actions.GetProperty)
				return true;
			if (action is Peach.Core.Dom.Actions.Output)
				return true;
			if (action is Peach.Core.Dom.Actions.SetProperty)
				return true;

			return false;
		}

		public override Peach.Core.Dom.State changeState(State currentState, Peach.Core.Dom.Action currentAction, State nextState)
		{
			return nextState;
		}

		bool _isDuplicating = false;

		public override Peach.Core.Dom.Action nextAction(State state, Peach.Core.Dom.Action lastAction, Peach.Core.Dom.Action nextAction)
		{
			if (nextAction != _targetAction && !_isDuplicating)
				return nextAction;

			_targetDuplicate -= 1;
			_isDuplicating = _targetDuplicate > 0;

			return _targetAction;
		}
	}
}
