//
// Copyright (c) Peach Fuzzer, LLC
//

using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Mutators
{
	/// <summary>
	/// Generate string with random unicode characters in them from plane 14 (0xe0000 - 0xefffd).
	/// </summary>
	[Mutator("StringUnicodePlane14")]
	[Description("Produce a random string from the Unicode Plane 14 character set.")]
	public class StringUnicodePlane14 : Utility.StringMutator
	{
		public StringUnicodePlane14(DataElement obj)
			: base(obj)
		{
		}

		protected override int GetCodePoint()
		{
			return context.Random.Next(0xE0000, 0xEFFFD + 1);
		}
	}
}
