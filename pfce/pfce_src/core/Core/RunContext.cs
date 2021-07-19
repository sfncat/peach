


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using Peach.Core.Agent;
using Peach.Core.Dom;

namespace Peach.Core
{
	/// <summary>
	/// Contains state information regarding the current fuzzing run.
	/// </summary>
	[Serializable]
	public class RunContext
	{
		#region Events

		#region Mutation Events

		public delegate void DataMutationEventHandler(RunContext context, ActionData actionData, DataElement element, Mutator mutator);

		public event DataMutationEventHandler DataMutating;

		public void OnDataMutating(ActionData actionData, DataElement element, Mutator mutator)
		{
			if (DataMutating != null)
				DataMutating(this, actionData, element, mutator);
		}

		public delegate void StateMutationEventHandler(RunContext context, State state, Mutator mutator);

		public event StateMutationEventHandler StateMutating;

		public void OnStateMutating(State state, Mutator mutator)
		{
			if (StateMutating != null)
				StateMutating(this, state, mutator);
		}

		#endregion

		#region State Model Events

		public delegate void StateModelEventHandler(RunContext context, StateModel stateModel);

		public event StateModelEventHandler StateModelStarting;

		public void OnStateModelStarting(StateModel stateModel)
		{
			if (StateModelStarting != null)
				StateModelStarting(this, stateModel);
		}

		public event StateModelEventHandler StateModelFinished;

		public void OnStateModelFinished(StateModel stateModel)
		{
			if (StateModelFinished != null)
				StateModelFinished(this, stateModel);
		}

		#endregion

		#region State Events

		public delegate void StateStartingEventHandler(RunContext context, State state);

		public event StateStartingEventHandler StateStarting;

		public void OnStateStarting(State state)
		{
			if (StateStarting != null)
				StateStarting(this, state);
		}

		public delegate void StateFinishedEventHandler(RunContext context, State state);

		public event StateFinishedEventHandler StateFinished;

		public void OnStateFinished(State state)
		{
			if (StateFinished != null)
				StateFinished(this, state);
		}

		public delegate void StateChangingEventHandler(RunContext context, State oldState, State newState);

		public event StateChangingEventHandler StateChanging;

		public void OnStateChanging(State oldState, State newState)
		{
			if (StateChanging != null)
				StateChanging(this, oldState, newState);
		}

		#endregion

		#region Action Events

		public delegate void ActionEventHandler(RunContext context, Dom.Action Action);

		public event ActionEventHandler ActionStarting;

		public void OnActionStarting(Dom.Action action)
		{
			if (ActionStarting != null)
				ActionStarting(this, action);
		}

		public event ActionEventHandler ActionFinished;

		public void OnActionFinished(Dom.Action action)
		{
			if (ActionFinished != null)
				ActionFinished(this, action);
		}

		#endregion

		#region Agent Events

		public delegate void AgentEventHandler(RunContext context, AgentClient agent);
		public delegate void MessageEventHandler(RunContext context, AgentClient agent, string msg);
		public delegate void CreateEventHandler(RunContext context, AgentClient agent, string name, string cls);

		public event AgentEventHandler AgentConnect;

		public void OnAgentConnect(AgentClient agent)
		{
			if (AgentConnect != null)
				AgentConnect(this, agent);
		}

		public event AgentEventHandler AgentDisconnect;

		public void OnAgentDisconnect(AgentClient agent)
		{
			if (AgentDisconnect != null)
				AgentDisconnect(this, agent);
		}

		public event CreateEventHandler CreatePublisher;

		public void OnCreatePublisher(AgentClient agent, string name, string cls)
		{
			if (CreatePublisher != null)
				CreatePublisher(this, agent, name, cls);
		}

		public event CreateEventHandler StartMonitor;

		public void OnStartMonitor(AgentClient agent, string name, string cls)
		{
			if (StartMonitor != null)
				StartMonitor(this, agent, name, cls);
		}

		public event AgentEventHandler StopAllMonitors;

		public void OnStopAllMonitors(AgentClient agent)
		{
			if (StopAllMonitors != null)
				StopAllMonitors(this, agent);
		}

		public event AgentEventHandler SessionStarting;

		public void OnSessionStarting(AgentClient agent)
		{
			if (SessionStarting != null)
				SessionStarting(this, agent);
		}

		public event AgentEventHandler SessionFinished;

		public void OnSessionFinished(AgentClient agent)
		{
			if (SessionFinished != null)
				SessionFinished(this, agent);
		}

		public event AgentEventHandler IterationStarting;

		public void OnIterationStarting(AgentClient agent)
		{
			if (IterationStarting != null)
				IterationStarting(this, agent);
		}

		public event AgentEventHandler IterationFinished;

		public void OnIterationFinished(AgentClient agent)
		{
			if (IterationFinished != null)
				IterationFinished(this, agent);
		}

		public event AgentEventHandler DetectedFault;

		public void OnDetectedFault(AgentClient agent)
		{
			if (DetectedFault != null)
				DetectedFault(this, agent);
		}

		public event AgentEventHandler GetMonitorData;

		public void OnGetMonitorData(AgentClient agent)
		{
			if (GetMonitorData != null)
				GetMonitorData(this, agent);
		}

		public event MessageEventHandler Message;

		public void OnMessage(AgentClient agent, string msg)
		{
			if (Message != null)
				Message(this, agent, msg);
		}

		#endregion

		#endregion

		/// <summary>
		/// Triggers a fault for the currently executing test case.
		/// </summary>
		/// <param name="title">Title of fault</param>
		/// <param name="description">Description of fault</param>
		/// <param name="majorHash">Major hash for fault. Set to null or empty string to skip bucketing.</param>
		/// <param name="minorHash">Minor hash for fault. Set to null or empty string to skip bucketing.</param>
		/// <param name="exploitability">Exploitability for fault</param>
		/// <param name="detectionSource">Detection source, such as Monitor class attribute.</param>
		/// <param name="detectionName">Detection name, such as name attribute</param>
		/// <param name="agentName">Name of agent fault was reported by</param>
		public void Fault(string title, string description, string majorHash, string minorHash, string exploitability, string detectionSource, string detectionName = null, string agentName = null)
		{
			throw new FaultException(new FaultSummary
			{
				Title = title,
				Description = description,
				MajorHash = majorHash,
				MinorHash = minorHash,
				Exploitablity = exploitability,
				DetectionSource = detectionSource,
				DetectionName = detectionName,
				AgentName = agentName
			});
		}

		/// <summary>
		/// Configuration settings for this run
		/// </summary>
		public RunConfiguration config = null;

		/// <summary>
		/// Dom to use for this run
		/// </summary>
		[Obsolete]
		public Dom.Dom dom
		{
			get { return test != null ? test.parent : null; }
		}

		/// <summary>
		/// Engine instance for this run
		/// </summary>
		[Obsolete]
		[NonSerialized]
		public Engine engine = null;

		/// <summary>
		/// Current test being run
		/// </summary>
		/// <remarks>
		/// Currently the Engine code sets this.
		/// </remarks>
		[NonSerialized]
		public Test test = null;

		/// <summary>
		/// Current agent manager for this run.
		/// </summary>
		/// <remarks>
		/// Currently the Engine code sets this.
		/// </remarks>
		[NonSerialized]
		public AgentManager agentManager = null;

		/// <summary>
		/// An object store that will last entire run.  For use
		/// by Peach code to store some state.
		/// </summary>
		[NonSerialized]
		public Dictionary<string, object> stateStore = new Dictionary<string, object>();

		/// <summary>
		/// An object store that will last current iteration.
		/// </summary>
		[NonSerialized]
		public Dictionary<string, object> iterationStateStore = new Dictionary<string, object>();

		/// <summary>
		/// The current iteration of fuzzing.
		/// </summary>
		public uint currentIteration = 0;

		#region Control Iterations

		/// <summary>
		/// Is this a control iteration.  Control iterations are used
		/// to verify the system can still reliably fuzz and are performed
		/// with out any mutations applied.
		/// </summary>
		/// <remarks>
		/// The first iteration is a special control iteration.  We also
		/// perform control iterations after we have collected a fault.
		/// 
		/// In later version we will likely inject control iterations every 
		/// N iterations where N is >= 100.
		/// </remarks>
		public bool controlIteration = false;

		/// <summary>
		/// Is this control operation also a recording iteration?
		/// </summary>
		/// <remarks>
		/// Recording iterations set our controlActionsExecuted and 
		/// controlStatesExecuted arrays.
		/// </remarks>
		public bool controlRecordingIteration = false;

		/// <summary>
		/// Actions performed during first control iteration.  Used to validate
		/// control iterations that come later have same action coverage.
		/// </summary>
		public List<Dom.Action> controlRecordingActionsExecuted = new List<Dom.Action>();

		/// <summary>
		/// States performed during first control iteration.  Used to validate
		/// control iterations that come later have same state coverage.
		/// </summary>
		/// <remarks>
		/// This may not be required with action coverage.
		/// </remarks>
		public List<Dom.State> controlRecordingStatesExecuted = new List<State>();

		/// <summary>
		/// Actions performed during later control iterations.  Used to validate
		/// control iterations that come later have same action coverage.
		/// </summary>
		public List<Dom.Action> controlActionsExecuted = new List<Dom.Action>();

		/// <summary>
		/// States performed during later control iterations.  Used to validate
		/// control iterations that come later have same state coverage.
		/// </summary>
		/// <remarks>
		/// This may not be required with action coverage.
		/// </remarks>
		public List<Dom.State> controlStatesExecuted = new List<State>();

		#endregion

		#region Faults

		/// <summary>
		/// Was there a fault detected on the previous iteration.
		/// </summary>
		public bool FaultOnPreviousIteration { get; set; }

		/// <summary>
        /// Faults for current iteration of fuzzing.  This collection
        /// is cleared after each iteration.
        /// </summary>
        /// <remarks>
        /// This collection should only be added to from the CollectFaults event.
        /// </remarks>
        public List<Fault> faults = new List<Fault>();

		/// <summary>
		/// Controls if we continue fuzzing or exit
		/// after current iteration.  This can be used
		/// by UI code to stop Peach.
		/// </summary>
		private bool _continueFuzzing = true;

		public bool continueFuzzing 
		{
			get
			{
				if (!_continueFuzzing)
					return false;
				if (config != null && config.shouldStop != null)
					return !config.shouldStop();
				return true;
			}
			set
			{
				_continueFuzzing = value;
			}
		}

		#endregion

		#region Reproduce Fault

		public bool controlIterationAfterFault = false;

		public bool disableReproduction = false;

		/// <summary>
		/// True when we have found a fault and are in the process
		/// of reproducing it.
		/// </summary>
		/// <remarks>
		/// Many times, especially with network fuzzing, the iteration we detect a fault on is not the
		/// correct iteration, or the fault requires multiple iterations to reproduce.
		/// 
		/// Peach will start reproducing at the current iteration count then start moving backwards
		/// until we locate the iteration causing the crash, or reach our max back search value.
		/// </remarks>
		public bool reproducingFault = false;

		/// <summary>
		/// The initial iteration we detected fault on
		/// </summary>
		public uint reproducingInitialIteration = 0;

		/// <summary>
		/// Did the fault we are trying to reproduce occur on a control iteration.
		/// </summary>
		public bool reproducingControlIteration = false;

		/// <summary>
		/// Did the fault we are trying to reproduce occur on a control iteration.
		/// </summary>
		public bool reproducingControlRecordingIteration = false;

		/// <summary>
		/// Number of iterations to jump.
		/// </summary>
		/// <remarks>
		/// Initializes to 1, then multiply against reproducingSkipMultiple
		/// </remarks>
		public uint reproducingIterationJumpCount = 1;

		#endregion
	}
}

// end
