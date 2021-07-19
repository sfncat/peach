//
// Copyright (c) Peach Fuzzer, LLC
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Mutators
{
	[Mutator("StringUnicodeAbstractCharacters")]
	[Description("Produce string comprised of unicode abstract characters.")]
	public class StringUnicodeAbstractCharacters : Utility.StringMutator
	{
		static readonly int[][] codePoints;

		static StringUnicodeAbstractCharacters()
		{
			var items = new List<int[]>();

			using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Peach.Pro.Core.Resources.NamedSequences.txt"))
			{
				using (var rdr = new StreamReader(stream))
				{
					while (!rdr.EndOfStream)
					{
						var line = rdr.ReadLine();

						if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
							continue;

						var parts = line.Split(';');
						var codes = parts[1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
						var asInt = codes.Select(i => int.Parse(i, NumberStyles.HexNumber)).ToArray();

						items.Add(asInt);
					}
				}
			}

			codePoints = items.ToArray();
		}

		public StringUnicodeAbstractCharacters(DataElement obj)
			: base(obj)
		{
		}

		protected override int GetCodePoint()
		{
			return context.Random.Next(codePoints.Length);
		}

		protected override string GetChar()
		{
			var cp = GetCodePoint();
			var ch = new string(codePoints[cp].Select(i => (char)i).ToArray());
			return ch;
		}
	}
}
