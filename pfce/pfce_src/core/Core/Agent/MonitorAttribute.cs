using System;

namespace Peach.Core.Agent
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class MonitorAttribute : PluginAttribute
	{
		// ReSharper disable once UnusedParameter.Local
		[Obsolete("This constructor is obsolete. Use the constructor without the isDefault argument.")]
		public MonitorAttribute(string name, bool isDefault)
			: base(typeof(IMonitor), name, true)
		{
		}

		public MonitorAttribute(string name)
			: base(typeof(IMonitor), name, true)
		{
		}
	}
}
