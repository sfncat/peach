using System.Collections.Generic;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Transformers
{
	public abstract class BasePythonTransformer : Transformer
	{
		public BasePythonTransformer(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
			__init__(parent, args);
		}

		protected virtual void __init__(DataElement parent, Dictionary<string, Variant> args)
		{
		}

		[ShouldClone]
		private bool ShouldClone(object context)
		{
			return false;
		}
	}
}
