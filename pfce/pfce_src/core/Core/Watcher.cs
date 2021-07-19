


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using Peach.Core.Agent;
using Peach.Core.Dom;
using Action = Peach.Core.Dom.Action;

namespace Peach.Core
{
	/// <summary>
	/// Watches the Peach Engine events.  This is how to 
	/// add a UI or logging.
	/// </summary>
	[Serializable]
	public abstract class Watcher
	{
		public void Initialize(Engine engine, RunContext context)
		{
			engine.TestStarting += Engine_TestStarting;
			engine.TestFinished += Engine_TestFinished;
			engine.TestError += Engine_TestError;
			engine.TestWarning += Engine_TestWarning;
			engine.IterationStarting += Engine_IterationStarting;
			engine.IterationFinished += Engine_IterationFinished;
			engine.Fault += Engine_Fault;
			engine.ReproFault += Engine_ReproFault;
			engine.ReproFailed += Engine_ReproFailed;
			engine.HaveCount += Engine_HaveCount;
			engine.HaveParallel += Engine_HaveParallel;

			context.DataMutating += DataMutating;
			context.StateMutating += StateMutating;
			context.StateModelStarting += StateModelStarting;
			context.StateModelFinished += StateModelFinished;
			context.StateStarting += StateStarting;
			context.StateFinished += StateFinished;
			context.StateChanging += StateChanging;
			context.ActionStarting += ActionStarting;
			context.ActionFinished += ActionFinished;

			context.AgentConnect += Agent_AgentConnect;
			context.AgentDisconnect += Agent_AgentDisconnect;
			context.CreatePublisher += Agent_CreatePublisher;
			context.StartMonitor += Agent_StartMonitor;
			context.StopAllMonitors += Agent_StopAllMonitors;
			context.SessionStarting += Agent_SessionStarting;
			context.SessionFinished += Agent_SessionFinished;
			context.IterationStarting += Agent_IterationStarting;
			context.IterationFinished += Agent_IterationFinished;
			context.DetectedFault += Agent_DetectedFault;
			context.GetMonitorData += Agent_GetMonitorData;
			context.Message += Agent_Message;
		}

		public void Finalize(Engine engine, RunContext context)
		{
			context.DataMutating -= DataMutating;
			context.StateMutating -= StateMutating;
			context.StateModelStarting -= StateModelStarting;
			context.StateModelFinished -= StateModelFinished;
			context.StateStarting -= StateStarting;
			context.StateFinished -= StateFinished;
			context.StateChanging -= StateChanging;
			context.ActionStarting -= ActionStarting;
			context.ActionFinished -= ActionFinished;

			context.AgentConnect -= Agent_AgentConnect;
			context.AgentDisconnect -= Agent_AgentDisconnect;
			context.CreatePublisher -= Agent_CreatePublisher;
			context.StartMonitor -= Agent_StartMonitor;
			context.StopAllMonitors -= Agent_StopAllMonitors;
			context.SessionStarting -= Agent_SessionStarting;
			context.SessionFinished -= Agent_SessionFinished;
			context.IterationStarting -= Agent_IterationStarting;
			context.IterationFinished -= Agent_IterationFinished;
			context.DetectedFault -= Agent_DetectedFault;
			context.GetMonitorData -= Agent_GetMonitorData;
			context.Message -= Agent_Message;
		}

		#region Agent Events

		protected virtual void Agent_AgentConnect(RunContext context, AgentClient agent)
		{
		}

		protected virtual void Agent_AgentDisconnect(RunContext context, AgentClient agent)
		{
		}

		protected virtual void Agent_CreatePublisher(RunContext context, AgentClient agent, string name, string cls)
		{
		}

		protected virtual void Agent_StartMonitor(RunContext context, AgentClient agent, string name, string cls)
		{
		}

		protected virtual void Agent_StopAllMonitors(RunContext context, AgentClient agent)
		{
		}

		protected virtual void Agent_SessionStarting(RunContext context, AgentClient agent)
		{
		}

		protected virtual void Agent_SessionFinished(RunContext context, AgentClient agent)
		{
		}

		protected virtual void Agent_IterationStarting(RunContext context, AgentClient agent)
		{
		}

		protected virtual void Agent_IterationFinished(RunContext context, AgentClient agent)
		{
		}

		protected virtual void Agent_DetectedFault(RunContext context, AgentClient agent)
		{
		}

		protected virtual void Agent_GetMonitorData(RunContext context, AgentClient agent)
		{
		}

		protected virtual void Agent_Message(RunContext context, AgentClient agent, string msg)
		{
		}

		#endregion

		protected virtual void DataMutating(RunContext context, ActionData actionData, DataElement element, Mutator mutator)
		{
		}

		protected virtual void StateMutating(RunContext context, State state, Mutator mutator)
		{
		}

		protected virtual void ActionFinished(RunContext context, Action action)
		{
		}

		protected virtual void ActionStarting(RunContext context, Action action)
		{
		}

		protected virtual void StateChanging(RunContext context, State oldState, State newState)
		{
		}

		protected virtual void StateFinished(RunContext context, State state)
		{
		}

		protected virtual void StateStarting(RunContext context, State state)
		{
		}

		protected virtual void StateModelFinished(RunContext context, StateModel model)
		{
		}

		protected virtual void StateModelStarting(RunContext context, StateModel model)
		{
		}

		protected virtual void Engine_HaveCount(RunContext context, uint totalIterations)
		{
		}

		protected virtual void Engine_HaveParallel(RunContext context, uint startIteration, uint stopIteration)
		{
		}

		protected virtual void Engine_ReproFailed(RunContext context, uint currentIteration)
		{
		}

		protected virtual void Engine_ReproFault(RunContext context, uint currentIteration, StateModel stateModel, Fault[] faultData)
		{
		}

		protected virtual void Engine_Fault(RunContext context, uint currentIteration, StateModel stateModel, Fault[] faultData)
		{
		}

		protected virtual void Engine_IterationFinished(RunContext context, uint currentIteration)
		{
		}

		protected virtual void Engine_IterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
		{
		}

		protected virtual void Engine_TestError(RunContext context, Exception e)
		{
		}

		protected virtual void Engine_TestFinished(RunContext context)
		{
		}

		protected virtual void Engine_TestStarting(RunContext context)
		{
		}

		protected virtual void Engine_TestWarning(RunContext context, string msg)
		{
		}

		protected virtual void Engine_RunError(RunContext context, Exception e)
		{
		}

		protected virtual void Engine_RunFinished(RunContext context)
		{
		}

		protected virtual void Engine_RunStarting(RunContext context)
		{
		}
	}
}

// end
