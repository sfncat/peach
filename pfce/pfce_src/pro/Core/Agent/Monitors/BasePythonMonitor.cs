using Peach.Core.Agent;

namespace Peach.Pro.Core.Agent.Monitors
{
	public abstract class BasePythonMonitor : Monitor2
	{
		protected BasePythonMonitor(string name)
			: base(name)
		{
			__init__(name);
		}

		protected virtual void __init__(string name)
		{
		}
	}
}
