using System;
using System.Xml;

namespace Peach.Core.Dom.Actions
{
	[Action("Stop")]
	[Serializable]
	public class Stop : Action
	{
		protected override void OnRun(Publisher publisher, RunContext context)
		{
			publisher.close();
			publisher.stop();
		}

		public override void WritePitBody(XmlWriter pit)
		{
		}

	}
}
