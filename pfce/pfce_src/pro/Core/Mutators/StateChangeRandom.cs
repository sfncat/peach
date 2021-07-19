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
	[Mutator("StateChangeRandom")]
	[Description("Causes state changes to be random. The chance a state change will be modified is based on the number of states.")]
	public class StateChangeRandom : Mutator
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public static new readonly bool affectDataModel = false;
		public static new readonly bool affectStateModel = true;

		int _count = 0;
		uint _mutation = 0;
		int _stateCount = 0;
		Peach.Core.Dom.StateModel _model;

		public StateChangeRandom(StateModel model)
			: base(model)
		{
			_count = model.states.Count * model.states.Count;
			_stateCount = model.states.Count;
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
			_model = obj;
		}

		public override void randomMutation(Peach.Core.Dom.StateModel obj)
		{
			_model = obj;
		}

		public override Peach.Core.Dom.State changeState(Peach.Core.Dom.State currentState, Peach.Core.Dom.Action currentAction, Peach.Core.Dom.State nextState)
		{
			if (context.Random.NextInt32() % _stateCount == 0)
			{
				var newState = context.Random.Choice(_model.states);

				logger.Trace("changeState: Swap {0} for {1}.", nextState.Name, newState.Name);
				return newState;
			}

			return nextState;
		}
	}
}
