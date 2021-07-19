using System.Collections.Generic;
using Peach.Core;

namespace Peach.Pro.Core.Loggers
{
	public abstract class BasePythonLogger :Logger
	{
		protected BasePythonLogger(Dictionary<string, Variant> args)
		{
			__init__(args);
		}

		protected virtual void __init__(Dictionary<string, Variant> args)
		{
		}
	}
}
