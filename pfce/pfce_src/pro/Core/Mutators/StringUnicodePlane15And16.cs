//
// Copyright (c) Peach Fuzzer, LLC
//

using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Mutators
{
	/// <summary>
	/// Generate string with random unicode characters in them from plane 15 and 16 (0xf0000 - 0x10ffff).
	/// </summary>
	[Mutator("StringUnicodePlane15And16")]
	[Description("Produce a random string from the Unicode Plane 15 and 16 character set.")]
	public class StringUnicodePlane15And16 : Utility.StringMutator
	{
		public StringUnicodePlane15And16(DataElement obj)
			: base(obj)
		{
		}

		protected override int GetCodePoint()
		{
			return context.Random.Next(0xF0000, 0x10FFFD + 1);
		}
	}
}
