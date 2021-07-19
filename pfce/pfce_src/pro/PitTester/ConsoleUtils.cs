using System;
using System.Collections.Generic;

namespace Peach.Pro.PitTester
{
	class ForegroundColor : IDisposable
	{
		readonly ConsoleColor _fg;

		public ForegroundColor(ConsoleColor color)
		{
			_fg = Console.ForegroundColor;
			Console.ForegroundColor = color;
		}

		public void Dispose()
		{
			Console.ForegroundColor = _fg;
		}
	}

	class ConsoleRegion
	{
		public ConsoleColor ForegroundColor { get; set; }
		public ConsoleColor BackgroundColor { get; set; }
		public string String { get; set; }
	}

	class ConsoleBuffer
	{
		readonly List<ConsoleRegion> _regions = new List<ConsoleRegion>();

		public void Append(ConsoleColor fg, ConsoleColor bg, string str)
		{
			_regions.Add(new ConsoleRegion {
				ForegroundColor = fg,
				BackgroundColor = bg,
				String = str,
			});
		}

		public void Append(string str)
		{
			Append(ConsoleColor.Gray, ConsoleColor.Black, str);
		}

		public void Append(ConsoleBuffer cb)
		{
			_regions.AddRange(cb._regions);
		}

		public void Print()
		{
			var fg = Console.ForegroundColor;
			var bg = Console.BackgroundColor;
			try
			{
				foreach (var region in _regions)
				{
					Console.ForegroundColor = region.ForegroundColor;
					Console.BackgroundColor = region.BackgroundColor;
					Console.Write(region.String);
				}
			}
			finally
			{
				Console.ForegroundColor = fg;
				Console.BackgroundColor = bg;
			}
		}
	}
}