


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Xml;

namespace Peach.Core.Dom
{
	public delegate void StateStartingEventHandler(State state);
	public delegate void StateFinishedEventHandler(State state);
	public delegate void StateChangingStateEventHandler(State state, State toState);

	/// <summary>
	/// The State element defines a sequence of Actions to perform.  Actions can cause a 
	/// change to another State.  Such changes can occur dynamically based on content received or sent
	/// by attaching python expressions to actions via the onStart/onComplete/when attributes.
	/// </summary>
	[Serializable]
	public class State : INamed, IFieldNamed
	{
		#region Obsolete Functions

		[Obsolete("This property is obsolete and has been replaced by the Name property.")]
		public string name { get { return Name; } }

		#endregion

		[NonSerialized]
		private StateModel _parent;

		[NonSerialized]
		protected Dictionary<string, object> scope = new Dictionary<string, object>();

		public State()
		{
			actions = new NamedCollection<Action>();
		}

		/// <summary>
		/// Currently unused.  Exists for schema generation.
		/// </summary>
		[XmlElement("Godel")]
		[DefaultValue(null)]
		public Xsd.Godel schemaGodel
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		/// <summary>
		/// The name of this state.
		/// </summary>
		[XmlAttribute("name")]
		[DefaultValue(null)]
		public string Name { get; set; }

		/// <summary>
		/// The field id of this state.
		/// </summary>
		[XmlAttribute("fieldId")]
		[DefaultValue(null)]
		public string FieldId { get; set; }

		/// <summary>
		/// Expression to run when state is starting
		/// </summary>
		[XmlAttribute]
		[DefaultValue(null)]
		public string onStart { get; set; }

		/// <summary>
		/// Expression to run when state is completed
		/// </summary>
		[XmlAttribute]
		[DefaultValue(null)]
		public string onComplete { get; set; }

		/// <summary>
		/// The actions contained in this state.
		/// </summary>
		[PluginElement("type", typeof(Action), Combine = true)]
		[DefaultValue(null)]
		public NamedCollection<Action> actions { get; set; }

		/// <summary>
		/// The state model that owns this state.
		/// </summary>
		public StateModel parent
		{
			get { return _parent; }
			set { _parent = value; }
		}

		/// <summary>
		/// Has the state started?
		/// </summary>
		public bool started { get; private set; }

		/// <summary>
		/// Has the start completed?
		/// </summary>
		public bool finished { get; private set; }

		/// <summary>
		/// Has an error occurred?
		/// </summary>
		public bool error { get; private set; }

		/// <summary>
		/// How many times has this state run
		/// </summary>
		public uint runCount { get; set; }

		protected virtual void RunScript(string expr)
		{
			if (!string.IsNullOrEmpty(expr))
			{
				parent.parent.Python.Exec(expr, scope);
			}
		}

		int _actionIndex = -1;
		
		public Action NextAction()
		{
			var index = _actionIndex+1;
			if (index >= actions.Count)
				return null;

			return actions[index];
		}

		/// <summary>
		/// Will move forward one action in execution.
		/// </summary>
		/// <remarks>
		/// This method can be called by state model mutators.
		/// If this method is not called and a different action is returned by the
		/// mutator, the current action scheduled for execution will be tried again 
		/// on the next step through.
		/// </remarks>
		public void MoveToNextAction()
		{
			_actionIndex++;
		}

		public void Run(RunContext context)
		{
			try
			{
				// Setup scope for any scripting expressions
				scope["context"] = context;
				scope["Context"] = context;
				scope["state"] = this;
				scope["State"] = this;
				scope["StateModel"] = parent;
				scope["stateModel"] = parent;
				scope["Test"] = parent.parent;
				scope["test"] = parent.parent;
				scope["self"] = this;

				if (context.controlIteration && context.controlRecordingIteration)
					context.controlRecordingStatesExecuted.Add(this);
				else if (context.controlIteration)
					context.controlStatesExecuted.Add(this);

				started = true;
				finished = false;
				error = false;

				if (++runCount > 1)
					UpdateToOriginalDataModel(runCount);

				context.OnStateStarting(this);

				RunScript(onStart);

				Action lastAction = null;
				_actionIndex = -1;

				while (true)
				{
					// Action that is performed w/o mutation
					var realCurrentAction = NextAction();

					// Action to perform with mutation
					var currentAction = context.test.strategy.NextAction(this, lastAction, realCurrentAction);
					if (currentAction == null)
						break;

					currentAction.Run(context);

					lastAction = currentAction;

					// Only increment if we are not mutated
					if (realCurrentAction == currentAction)
						MoveToNextAction();
				}

				// onComplete script run from finally.
			}
			catch(ActionChangeStateException)
			{
				// this is not an error
				throw;
			}
			catch
			{
				error = true;
				throw;
			}
			finally
			{
				finished = true;

				try
				{
					RunScript(onComplete);
				}
				finally
				{
					// Ensure C# delegates get notified even if onComplete throws
					context.OnStateFinished(this);
				}
			}
		}

		public void UpdateToOriginalDataModel()
		{
			UpdateToOriginalDataModel(0);
		}

		private void UpdateToOriginalDataModel(uint runCount)
		{
			this.runCount = runCount;

			foreach (var action in actions)
				action.UpdateToOriginalDataModel();

			if (runCount > 1)
			{
				// If this is a record iteration, apply element mutability
				// after datasets and analyzers have been applied
				if (parent.parent.context.controlRecordingIteration)
					parent.parent.context.test.markMutableElements();
			}
		}

		[OnCloned]
		void OnCloned(State original, object context)
		{
			foreach (var item in actions)
				item.parent = this;
		}

		public void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement("State");

			pit.WriteAttributeString("name", Name);

			if (!string.IsNullOrEmpty(FieldId))
				pit.WriteAttributeString("fieldId", FieldId);

			if (!string.IsNullOrEmpty(onStart))
				pit.WriteAttributeString("onStart", onStart);

			if (!string.IsNullOrEmpty(onComplete))
				pit.WriteAttributeString("onComplete", onComplete);

			foreach (var action in actions)
				action.WritePit(pit);

			// TODO - GOdel

			pit.WriteEndElement();
		}
	}
}

// END
