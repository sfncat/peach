using System.Collections.Generic;
using Peach.Core;

namespace Peach.Pro.Core.Publishers
{
	public abstract class BasePythonPublisher : Publisher
	{
		protected BasePythonPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
			__init__();
		}

		protected virtual void __init__()
		{
		}
	}
}
