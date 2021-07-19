


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using NLog;
using Peach.Core.IO;

namespace Peach.Core.Dom
{
	/// <summary>
	/// Used to indicate a class is a valid Action and 
	/// provide it's invoking name used in the Pit XML file.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class ActionAttribute : PluginAttribute
	{
		public ActionAttribute(string name)
			: base(typeof(Action), name, true)
		{
		}
	}

	public delegate void ActionStartingEventHandler(Action action);
	public delegate void ActionFinishedEventHandler(Action action);

	/// <summary>
	/// Used by the xpath navigator to get all action data children.
	/// </summary>
	public interface IActionDataXpath
	{
		IEnumerable<ActionData> XpathData { get; }
	}

	/// <summary>
	/// Base class for state model actions such as sending output, calling a method, etc.
	/// </summary>
	[DebuggerDisplay("{name}: {type}")]
	[Serializable]
	public abstract class Action : INamed, IFieldNamed, IActionDataXpath
	{
		#region Obsolete Functions

		[Obsolete("This property is obsolete and has been replaced by the Name property.")]
		public string name { get { return Name; } }

		#endregion

		static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();

		[NonSerialized]
		protected Dictionary<string, object> scope = new Dictionary<string, object>();

		[NonSerialized]
		private State _parent;

		#region Schema Elements

		/// <summary>
		/// Currently unused.  Exists for schema generation.
		/// </summary>
		[XmlElement("DataModel")]
		[DefaultValue(null)]
		public Xsd.DataModel schemaModel
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		/// <summary>
		/// Currently unused.  Exists for schema generation.
		/// </summary>
		[XmlElement("Data")]
		[DefaultValue(null)]
		public List<Xsd.Data> schemaData
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
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

		#endregion

		#region Common Action Properties

		/// <summary>
		/// Action was started
		/// </summary>
		public bool started { get; private set; }

		/// <summary>
		/// Action finished
		/// </summary>
		public bool finished { get; private set; }

		/// <summary>
		/// Action errored
		/// </summary>
		public bool error { get; private set; }

		#endregion

		#region Common Action Attributes

		/// <summary>
		/// Name of this action
		/// </summary>
		[XmlAttribute("name")]
		[DefaultValue(null)]
		public string Name { get; set; }

		/// <summary>
		/// The field id of this action.
		/// </summary>
		[XmlAttribute("fieldId")]
		[DefaultValue(null)]
		public string FieldId { get; set; }

		/// <summary>
		/// Name of publisher to use
		/// </summary>
		[XmlAttribute]
		[DefaultValue(null)]
		public string publisher { get; set; }

		/// <summary>
		/// Only run action when expression is true
		/// </summary>
		[XmlAttribute]
		[DefaultValue(null)]
		public string when { get; set; }

		/// <summary>
		/// Expression to run when action is starting
		/// </summary>
		[XmlAttribute]
		[DefaultValue(null)]
		public string onStart { get; set; }

		/// <summary>
		/// Expression to run when action is completed
		/// </summary>
		[XmlAttribute]
		[DefaultValue(null)]
		public string onComplete { get; set; }

		#endregion

		/// <summary>
		/// The type of this action
		/// </summary>
		public string type
		{
			get
			{
				var attr = ClassLoader.GetAttributes<ActionAttribute>(GetType(), null).FirstOrDefault();
				return attr != null ? attr.Name : "Unknown";
			}
		}

		/// <summary>
		/// The state this action belongs to
		/// </summary>
		public State parent
		{
			get
			{
				return _parent;
			}
			set
			{
				_parent = value;
			}
		}

		/// <summary>
		/// Provides backwards compatibility to the unit tests.
		/// Will be removed in the future.
		/// </summary>
		public DataModel dataModel
		{
			get
			{
				if (allData.Count() != 1)
					throw new NotSupportedException();

				return allData.First().dataModel;
			}
		}

		protected virtual void RunScript(string expr)
		{
			if (!string.IsNullOrEmpty(expr))
			{
				parent.parent.parent.Python.Exec(expr, scope);
			}
		}

		/// <summary>
		/// Update any DataModels we contain to new clones of
		/// origionalDataModel.
		/// </summary>
		/// <remarks>
		/// This should be performed in StateModel to every State/Action at
		/// start of the iteration.
		/// </remarks>
		public virtual void UpdateToOriginalDataModel()
		{
			foreach (var item in allData)
			{
				item.UpdateToOriginalDataModel();
			}
		}

		/// <summary>
		/// Run the action on the publisher
		/// </summary>
		/// <param name="publisher"></param>
		/// <param name="context"></param>
		protected abstract void OnRun(Publisher publisher, RunContext context);

		/// <summary>
		/// All Data (DataModels &amp; DataSets) used by this action
		/// that should be nagivable via xpath
		/// </summary>
		public virtual IEnumerable<ActionData> XpathData
		{
			get
			{
				return allData;
			}
		}

		/// <summary>
		/// All Data (DataModels &amp; DataSets) used by this action.
		/// </summary>
		public virtual IEnumerable<ActionData> allData
		{
			get
			{
				yield break;
			}
		}

		/// <summary>
		/// Raw data used for input (cracking) by this action.
		/// This can include data where cracking failed.
		/// </summary>
		public virtual IEnumerable<BitwiseStream> inputData
		{
			get
			{
				yield break;
			}
		}

		/// <summary>
		/// All Data (DataModels &amp; DataSets) used for output (fuzzing) by this action.
		/// </summary>
		public virtual IEnumerable<ActionData> outputData
		{
			get
			{
				yield break;
			}
		}

		public void Run(RunContext context)
		{
			// Log entry later if marked with when.
			// this will make the debug output look nicer.
			if(when == null)
				logger.Debug("Run({0}): {1}", Name, GetType().Name);

			// Setup scope for any scripting expressions
			scope["context"] = context;
			scope["Context"] = context;
			scope["action"] = this;
			scope["Action"] = this;
			scope["state"] = parent;
			scope["State"] = parent;
			scope["StateModel"] = parent.parent;
			scope["stateModel"] = parent.parent;
			scope["Test"] = parent.parent.parent;
			scope["test"] = parent.parent.parent;
			scope["self"] = this;

			if (when != null)
			{
				object value = parent.parent.parent.Python.Eval(when, scope);
				if (!(value is bool))
				{
					var msg = "Run({0}): {1}: When return is not boolean, skipping. Returned: {2}".Fmt(Name, GetType().Name, value);
					throw new SoftException(msg);
				}

				if (!(bool)value)
				{
					logger.Debug("Run({0}): {1}: Skipping, when returned false", Name, GetType().Name);
					return;
				}

				logger.Debug("Run({0}): {1}", Name, GetType().Name);
			}

			Publisher publisher = null;
			if (this.publisher != null && this.publisher != "Peach.Agent")
			{
				if (!context.test.publishers.ContainsKey(this.publisher))
				{
					logger.Debug("Run: Publisher '{0}' not found!", this.publisher);
					throw new PeachException("Error, Action '" + Name + "' couldn't find publisher named '" + this.publisher + "'.");
				}

				publisher = context.test.publishers[this.publisher];
			}
			else
			{
				publisher = context.test.publishers[0];
			}

			if (context.controlIteration && context.controlRecordingIteration)
			{
				logger.Trace("Run: Adding action to controlRecordingActionsExecuted");
				context.controlRecordingActionsExecuted.Add(this);
			}
			else if (context.controlIteration)
			{
				logger.Trace("Run: Adding action to controlActionsExecuted");
				context.controlActionsExecuted.Add(this);
			}

			started = true;
			finished = false;
			error = false;

			// Notify the data model the action is about to run
			foreach (var item in outputData)
				item.dataModel.Run(context);

			try
			{
				context.OnActionStarting(this);

				RunScript(onStart);

				// Save output data
				foreach (var item in outputData)
					parent.parent.SaveData(item.outputName, item.dataModel.Value, false);

				try
				{
					OnRun(publisher, context);
				}
				finally
				{
					// Save input data
					foreach (var item in inputData)
						parent.parent.SaveData(item.Name, item, true);
				}
			}
			catch (ActionChangeStateException)
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
					context.OnActionFinished(this);
				}
			}
		}

		/// <summary>
		/// Base classes implement to add attributes and inner elements
		/// </summary>
		/// <param name="pit"></param>
		public abstract void WritePitBody(XmlWriter pit);

		private string WritePitGetType()
		{
			var actionType = type.ToLower();
			if (actionType == "getproperty")
				return "getProperty";
			if (actionType == "setproperty")
				return "setProperty";
			return actionType;
		}

		public void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement("Action");

			pit.WriteAttributeString("name", Name);

			if (!string.IsNullOrEmpty(publisher))
				pit.WriteAttributeString("publisher", publisher);

			if (!string.IsNullOrEmpty(when))
				pit.WriteAttributeString("when", when);

			if (!string.IsNullOrEmpty(type))
				pit.WriteAttributeString("type", WritePitGetType());

			if (!string.IsNullOrEmpty(FieldId))
				pit.WriteAttributeString("fieldId", FieldId);

			if (!string.IsNullOrEmpty(onStart))
				pit.WriteAttributeString("onStart", onStart);

			if (!string.IsNullOrEmpty(onComplete))
				pit.WriteAttributeString("onComplete", onComplete);

			WritePitBody(pit);

			// TODO - Data sets
			//foreach (var actionData in allData)
			//{
			//	foreach (var dataSet in actionData.dataSets)
			//		dataSet.WritePit(pit);
			//}

			// TODO - GOdel

			pit.WriteEndElement();
		}
	}

	[Serializable]
	public class ActionChangeStateException : Exception
	{
		public State changeToState;

		public ActionChangeStateException(State changeToState)
		{
			this.changeToState = changeToState;
		}
	}
}

// END
