using System;
using System.Collections.Generic;
using Peach.Core;

namespace Peach.Pro.Core.Publishers
{
	[Obsolete("This class is obsolete.  Use Peach.Core.Publishers.StreamPublisher instead.")]
	public abstract class StreamPublisher : Peach.Core.Publishers.StreamPublisher
	{
		protected StreamPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}
	}
}
