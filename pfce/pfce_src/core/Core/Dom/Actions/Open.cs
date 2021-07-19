using System;
using System.Xml;

namespace Peach.Core.Dom.Actions
{
	[Action("Open")]
	[Serializable]
	public class Open : Action
	{
		protected override void OnRun(Publisher publisher, RunContext context)
		{
			publisher.start();
			publisher.open();
		}

		public override void WritePitBody(XmlWriter pit)
		{
		}

	}
}
