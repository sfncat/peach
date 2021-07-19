using System;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Xml;
using NLog;

namespace Peach.Core.Dom.Actions
{
	[Action("ChangeState")]
	[Serializable]
	public class ChangeState : Action
	{
		static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Name of state to change to, type=ChangeState
		/// </summary>
		[XmlAttribute("ref")]
		[DefaultValue(null)]
		public string reference { get; set; }

		protected override void OnRun(Publisher publisher, RunContext context)
		{
			State newState;

			if (!parent.parent.states.TryGetValue(reference, out newState))
			{
				logger.Debug("Error, unable to locate state '{0}'", reference);
				throw new PeachException("Error, unable to locate state '" + reference + "' provided to action '" + Name + "'");
			}

			logger.Debug("Changing to state: {0}", reference);
			throw new ActionChangeStateException(newState);
		}

		public override void WritePitBody(XmlWriter pit)
		{
			pit.WriteAttributeString("ref", reference);
		}

	}
}
