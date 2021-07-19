//
// Copyright (c) Peach Fuzzer, LLC
//

using System.Collections.Generic;
using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Mutators
{
	/// <summary>
	/// Certain noncharacter code points are guaranteed never to be used for
	/// encoding characters, although applications may make use of these
	/// code points internally if they wish.
	/// There are sixty-six noncharacters: U+FDD0..U+FDEF and any code point
	/// ending in the value FFFE or FFFF (i.e. U+FFFE, U+FFFF, U+1FFFE,
	/// U+1FFFF, ... U+10FFFE, U+10FFFF). The set of noncharacters is stable,
	/// and no new noncharacters will ever be defined.[14]
	/// </summary>
	[Mutator("StringUnicodeNonCharacters")]
	[Description("Produce string comprised of unicode noncharacters.")]
	public class StringUnicodeNonCharacters : Utility.StringMutator
	{
		// TODO: Populate this with something
		static readonly int[] codePoints = GetCodePoints();

		static int[] GetCodePoints()
		{
			var ret = new List<int>();

			for (int i = 0xFDD0; i <= 0xFDEF; ++i)
				ret.Add(i);

			for (int i = 0xFFFE; i <= 0x10FFFE; i += 0x10000)
				ret.AddRange(new int[] { i, i + 1 });

			// Supposed to be 66 noncharacter codepoints
			System.Diagnostics.Debug.Assert(ret.Count == 66);

			return ret.ToArray();
		}

		public StringUnicodeNonCharacters(DataElement obj)
			: base(obj)
		{
		}

		protected override int GetCodePoint()
		{
			var idx = context.Random.Next(codePoints.Length);
			return codePoints[idx];
		}
	}
}
