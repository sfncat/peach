using System;

namespace Peach.Core.Runtime
{
	public class SyntaxException : Exception
	{
		public bool ShowUsage { get; private set; }

		public SyntaxException(bool showUsage = false)
			: base("")
		{
			ShowUsage = showUsage;
		}

		public SyntaxException(string message, bool showUsage = true)
			: base(message)
		{
			ShowUsage = showUsage;
		}
	}
}
