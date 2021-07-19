using System.Collections.Generic;
using Peach.Core;
using Peach.Core.IO;

namespace Peach.Pro.Core.Publishers
{
	[Publisher("ConsoleHex")]
	[Alias("StdoutHex")]
	[Alias("stdout.StdoutHex")]
	[Parameter("BytesPerLine", typeof(int), "How many bytes per row of text", "16")]
	public class ConsoleHexPublisher : ConsolePublisher
	{
		public int BytesPerLine { get; protected set; }

		public ConsoleHexPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		protected override void OnOutput(BitwiseStream data)
		{
			Utilities.HexDump(data, stream, BytesPerLine);
		}
	}
}
