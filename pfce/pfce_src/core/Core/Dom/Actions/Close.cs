using System;
using System.Xml;

namespace Peach.Core.Dom.Actions
{
	[Action("Close")]
	[Serializable]
	public class Close : Action
	{
		protected override void OnRun(Publisher publisher, RunContext context)
		{
			publisher.start();
			publisher.close();
		}

		public override void WritePitBody(XmlWriter pit)
		{
		}
	}
}
