

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using Peach.Core;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Pro.Core.Analyzers
{
    [Analyzer("Binary", true)]
    [Analyzer("BinaryAnalyzer")]
    [Parameter("Tokens", typeof(string), "List of character tokens to pass to the StringToken analyzer", StringTokenAnalyzer.TOKENS)]
    [Parameter("AnalyzeStrings", typeof(string), "Call the StringToken analyzer on string elements", "true")]
    [Serializable]
    public class Binary : Analyzer
    {
        static NLog.Logger logger = LogManager.GetCurrentClassLogger();

        protected Dictionary<string, Variant> args = null;
        protected bool analyzeStrings = true;
		protected long elementCount;

		public new static readonly bool supportParser = false;
		public new static readonly bool supportDataElement = true;
		public new static readonly bool supportCommandLine = false;
		public new static readonly bool supportTopLevel = false;

        public Binary()
        {
        }

        public Binary(Dictionary<string, Variant> args)
        {
            this.args = args;

            Variant val = null;
            if (args != null && args.TryGetValue("AnalyzeStrings", out val))
                analyzeStrings = ((string)val).ToLower() == "true";
        }

        const int MINCHARS = 5;

		public override void asDataElement(DataElement parent, Dictionary<DataElement, Position> positions)
		{
			var blob = parent as Blob;

			if (blob == null)
				throw new PeachException("Error, Binary analyzer only operates on Blob elements!");

			var data = blob.Value;

			if (data.Length == 0)
				return;

			var block = new Block(blob.Name);
			long pos = 0;
			long chars = 0;

			elementCount = 0;

			while (true)
			{
				int value = data.ReadByte();
				if (value == -1)
					break;

				if (isAsciiChar(value))
				{
					++chars;
				}
				else
				{
					// Only treat this as a string if MINCHARS were found
					if (chars >= MINCHARS)
					{
						var begin = pos;
						var end = data.PositionBits - 8;

						pos = end;
						chars *= 8;

						data.SeekBits(begin, SeekOrigin.Begin);

						var pre = data.SliceBits(end - chars - begin);
						var str = data.SliceBits(chars);

						data.SeekBits(8, SeekOrigin.Current);

						// Save off any data before the string 1st
						saveData(block, pre, positions, begin);

						// Save off the string 2nd
						var elem = new Peach.Core.Dom.String("Elem_{0}".Fmt(elementCount++))
						{
							DefaultValue = new Variant(str)
						};

						// Add the string 2nd
						block.Add(elem);

						if (positions != null)
							positions[elem] = new Position(end - chars, end);

						// Potentially analyze the string further
						if (analyzeStrings)
						{
							var other = positions != null ? new Dictionary<DataElement, Position>() : null;

							new StringTokenAnalyzer(args).asDataElement(elem, other);

							if (other != null)
								foreach (var kv in other)
									positions[kv.Key] = new Position(kv.Value.begin + begin, kv.Value.end + begin);
						}
					}

					chars = 0;
				}
			}

			// Save off any trailing data
			data.SeekBits(pos, SeekOrigin.Begin);

			var bs = data.SliceBits(data.LengthBits - pos);

			saveData(block, bs, positions, pos);

			if (logger.IsDebugEnabled)
			{
				var count = block.Walk().Count();
				logger.Debug("Created {0} data elements from binary data.", count);
			}

			parent.parent[parent.Name] = block;
			if (positions != null)
				positions[block] = new Position(0, data.LengthBits);
		}

        protected void saveData(Block block, BitwiseStream data, Dictionary<DataElement, Position> positions, long offset)
        {
            if (data.Length == 0)
                return;

            var elem = new Blob("Elem_{0}".Fmt(elementCount++))
            {
                DefaultValue = new Variant(data)
            };

            block.Add(elem);

            if (positions != null)
                positions[elem] = new Position(offset, offset + data.LengthBits);
        }

        protected bool isGzip(int b, BitStream data)
        {
            if (b == 0x1f)
            {
                if (data.ReadByte() == 0x8b)
                {
                    data.Seek(-1, SeekOrigin.Current);
                    return true;
                }

                data.Seek(-1, SeekOrigin.Current);
            }

            return false;
        }

        protected bool isAsciiChar(int b)
        {
            if (b == 0x09 || /* tab */
                b == 0x0a || b == 0x0d || /* crlf */
                (b >= 32 && b <= 126))
            {
                return true;
            }

            return false;
        }
    }
}

// end
