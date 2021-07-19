

using System.Collections.Generic;
using System.IO;
using NLog;
using Peach.Core;
using Peach.Core.IO;

namespace Peach.Pro.Core.Publishers
{
	[Publisher("Console")]
	[Alias("Stdout")]
	[Alias("stdout.Stdout")]
	public class ConsolePublisher : Publisher
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		protected Stream stream = null;

		public ConsolePublisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		protected override void OnOpen()
		{
			System.Diagnostics.Debug.Assert(stream == null);
			stream = System.Console.OpenStandardOutput();
		}

		protected override void OnClose()
		{
			System.Diagnostics.Debug.Assert(stream != null);
			stream.Close();
			stream = null;
		}

		protected override void OnOutput(BitwiseStream data)
		{
		    try
		    {
		        data.CopyTo(stream);
		    }
            catch(IOException ioException)
            {
                throw new SoftException("Error, Console Output Too Large",ioException);
            }
		}
	}
}

// END
