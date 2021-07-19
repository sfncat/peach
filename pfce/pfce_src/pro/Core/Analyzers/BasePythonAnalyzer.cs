using System;
using System.Collections.Generic;
using Peach.Core;

namespace Peach.Pro.Core.Analyzers
{
	[Serializable]
	public abstract class BasePythonAnalyzer : Analyzer
	{
		protected BasePythonAnalyzer()
		{
			__init__();
		}

		protected virtual void __init__()
		{
		}

		protected BasePythonAnalyzer(Dictionary<string, Variant> args)
		{
			__init__(args);
		}

		protected virtual void __init__(Dictionary<string, Variant> args)
		{
		}

		[ShouldClone]
		private bool ShouldClone(object context)
		{
			return false;
		}
	}
}
