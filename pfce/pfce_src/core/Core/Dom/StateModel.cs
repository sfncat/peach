


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Linq;
using Peach.Core.IO;

using NLog;
using System.ComponentModel;
using System.Xml;

namespace Peach.Core.Dom
{
	/// <summary>
	/// Defines a state machine to use during a fuzzing test.  State machines in Peach are intended to be
	/// fairly simple and allow for only the basic modeling typically required for fuzzing state aware protocols or 
	/// call sequences.  State machines are made up of one or more States which are in them selves make up of
	/// one or more Action.  As Actions are executed the data can be moved between them as needed.
	/// </summary>
	[Serializable]
	public class StateModel : INamed, IOwned<Dom>
	{
		public class PublishedData
		{
			public bool IsInput { get; set; }
			public string Name { get; set; }
			public string Key { get; set; }
			public BitwiseStream Value { get; set; }
		}

		#region Obsolete Functions

		[Obsolete("This property is obsolete and has been replaced by the Name property.")]
		public string name { get { return Name; } }

		#endregion

		[NonSerialized]
		private Dom _parent;

		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public StateModel()
		{
			states = new NamedCollection<State>();
		}

		protected readonly List<PublishedData> _dataActions = new List<PublishedData>();

		public IEnumerable<PublishedData> dataActions { get { return _dataActions; } }

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
		/// All states in state model.
		/// </summary>
		[XmlElement("State")]
		public NamedCollection<State> states { get; set; }

		/// <summary>
		/// The name of this state model.
		/// </summary>
		[XmlAttribute("name")]
		public string Name { get; set; }

		/// <summary>
		/// Name of the state to execute first.
		/// </summary>
		[XmlAttribute("initialState")]
		public string initialStateName { get; set; }

		/// <summary>
		/// Name of the state to execute last.
		/// </summary>
		[XmlAttribute("finalState")]
		[DefaultValue(null)]
		public string finalStateName { get; set; }

		/// <summary>
		/// The Dom that owns this state model.
		/// </summary>
		public Dom parent
		{
			get { return _parent; }
			set { _parent = value; }
		}

		/// <summary>
		/// The initial state to run when state machine executes.
		/// </summary>
		public State initialState { get; set; }

		/// <summary>
		/// The final state to run when state machine finishes.
		/// </summary>
		public State finalState { get; set; }

		/// <summary>
		/// Saves the data produced/consumed by an action for future logging.
		/// </summary>
		/// <param name="dataName"></param>
		/// <param name="value"></param>
		/// <param name="isInput"></param>
		public void SaveData(string dataName, BitwiseStream value, bool isInput)
		{
			var item = new PublishedData
			{
				Key = "{0}.{1}.bin".Fmt(_dataActions.Count + 1, dataName),
				Name = dataName,
				Value = value,
				IsInput = isInput
			};

			_dataActions.Add(item);
		}

		/// <summary>
		/// Start running the State Machine
		/// </summary>
		/// <remarks>
		/// This will start the initial State.
		/// </remarks>
		/// <param name="context"></param>
		public virtual void Run(RunContext context)
		{
			var currentState = initialState;

			try
			{
				foreach (var publisher in context.test.publishers)
				{
					publisher.Iteration = context.test.strategy.Iteration;
					publisher.IsControlIteration = context.controlIteration;
					publisher.IsIterationAfterFault = context.FaultOnPreviousIteration;
					publisher.IsControlRecordingIteration = context.controlRecordingIteration;
				}

				_dataActions.Clear();

				// Update all data model to clones of origionalDataModel
				// before we start down the state path.
				foreach (State state in states)
					state.UpdateToOriginalDataModel();

				// If this is a record iteration, apply element mutability
				// after datasets and analyzers have been applied
				if (context.controlRecordingIteration)
					context.test.markMutableElements();

				context.OnStateModelStarting(this);

				// Allow mutating the initial state
				var newState = context.test.strategy.MutateChangingState(currentState);

				if (newState == currentState)
					logger.Debug("Run(): Changing to state \"{0}\".", newState.Name);
				else
					logger.Debug("Run(): Changing state mutated.  Switching to \"{0}\" instead of \"{1}\".",
						newState.Name, currentState);

				currentState = newState;

				// Main execution loop
				while (true)
				{
					try
					{
						currentState.Run(context);
						break;
					}
					catch (ActionChangeStateException ase)
					{
						if (ase.changeToState == finalState)
							throw new PeachException("Change state actions cannot refer to final state.");

						newState = context.test.strategy.MutateChangingState(ase.changeToState);

						if (newState == ase.changeToState)
							logger.Debug("Run(): Changing to state \"{0}\".", newState.Name);
						else
							logger.Debug("Run(): Changing state mutated.  Switching to \"{0}\" instead of \"{1}\".",
								newState.Name, ase.changeToState);

						context.OnStateChanging(currentState, newState);
						currentState = newState;
					}
				}
			}
			catch (ActionException)
			{
				// Exit state model!
			}
			finally
			{
				try
				{
					if (finalState != null)
					{
						logger.Debug("Run(): Executing final state \"{0}\".", finalState.Name);
						context.OnStateChanging(currentState, finalState);
						finalState.Run(context);
					}
				}
				catch (ActionChangeStateException)
				{
					throw new PeachException("A change state is not allowed in a final state.");
				}
				finally
				{
					foreach (var publisher in context.test.publishers)
						publisher.close();

					context.OnStateModelFinished(this);
				}
			}
		}

		[OnCloned]
		void OnCloned(StateModel original, object context)
		{
			foreach (var item in states)
				item.parent = this;
		}

		public virtual bool HasFieldIds
		{
			get
			{
				foreach (var state in states)
				{
					if (!string.IsNullOrEmpty(state.FieldId))
						return true;

					foreach (var action in state.actions)
					{
						if (!string.IsNullOrEmpty(action.FieldId))
							return true;

						if (action.outputData.Any(
							actionData => actionData.dataModel
								.TuningTraverse(true, true)
								.Any(e => !string.IsNullOrEmpty(e.Key))
							))
						{
							return true;
						}
					}
				}

				return false;
			}
		}

		public IEnumerable<KeyValuePair<string, DataElement>> TuningTraverse(bool forDisplay = false)
		{
			var useFieldIds = HasFieldIds;
			foreach (var state in states)
			{
				foreach (var action in state.actions)
				{
					var parts = new List<string>();
					AddPart(useFieldIds, parts, state);
					AddPart(useFieldIds, parts, action);
					var prefix = string.Join(".", parts);

					foreach (var actionData in action.outputData)
					{
						foreach (var element in actionData.dataModel.TuningTraverse(useFieldIds, forDisplay))
						{
							var key = element.Key;
							if (!string.IsNullOrEmpty(element.Key) && !string.IsNullOrEmpty(prefix))
								key = string.Join(".", prefix, element.Key);
							yield return new KeyValuePair<string, DataElement>(key ?? "", element.Value);
						}
					}
				}
			}
		}

		void AddPart(bool useFieldIds, List<string> parts, IFieldNamed node)
		{
			if (useFieldIds)
			{
				if (!string.IsNullOrEmpty(node.FieldId))
					parts.Add(node.FieldId);
			}
			else
			{
				parts.Add(node.Name);
			}
		}

		/// <summary>
		/// Create a StateModelRef for this State Model
		/// </summary>
		/// <remarks>
		/// This allows different state model types to create
		/// specific StateModelRef instances for themselves.
		/// 
		/// Example is WebProxyModel.
		/// </remarks>
		/// <returns></returns>
		public virtual IStateModelRef CreateStateModelRef()
		{
			return new StateModelRef {refName = Name};
		}

		public virtual void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement("StateModel");

			pit.WriteAttributeString("name", Name);
			pit.WriteAttributeString("initialState", initialStateName);

			if(!string.IsNullOrEmpty(finalStateName))
				pit.WriteAttributeString("finalState", finalStateName);

			foreach (var state in states)
				state.WritePit(pit);


			// TODO - GOdel

			pit.WriteEndElement();
		}
	}
}

// END
