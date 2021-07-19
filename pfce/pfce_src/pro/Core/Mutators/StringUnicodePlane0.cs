//
// Copyright (c) Peach Fuzzer, LLC
//

using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Mutators
{
	/// <summary>
	/// Generate string with random unicode characters in them from plane 0 (0 - 0xffff).
	/// </summary>
	[Mutator("StringUnicodePlane0")]
	[Description("Produce a random string from the Unicode Plane 0 character set.")]
	public class StringUnicodePlane0 : Utility.StringMutator
	{
		public StringUnicodePlane0(DataElement obj)
			: base(obj)
		{
		}

		protected override int GetCodePoint()
		{
			while (true)
			{
				var cp = context.Random.Next(0x0000, 0xFFFD + 1);

				// Ignore low and high surrogate code points
				if (cp >= 0xD800 && cp <= 0xDFFF)
					continue;

				// Ignore noncharacter code points
				if (cp >= 0xFDD0 && cp <= 0xFDFE)
					continue;

				// Ignore private use
				if (cp >= 0xE000 && cp <= 0xF8FF)
					continue;

				return cp;
			}
		}
	}
}
