using System;
using System.Collections.Generic;
using Peach.Core;

namespace Peach.Pro.Core.Publishers
{
	[Obsolete("This class is obsolete.  Use Peach.Core.Publishers.BufferedStreamPublisher instead.")]
	public abstract class BufferedStreamPublisher : Peach.Core.Publishers.BufferedStreamPublisher
	{
		protected BufferedStreamPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}
	}
}
