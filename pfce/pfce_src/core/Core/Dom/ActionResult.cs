using System;

namespace Peach.Core.Dom
{
	[Serializable]
	public class ActionResult : ActionData
	{
		public ActionResult(string name)
		{
			Name = name;
		}
	}
}
