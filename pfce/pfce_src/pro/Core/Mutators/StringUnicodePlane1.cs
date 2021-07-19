//
// Copyright (c) Peach Fuzzer, LLC
//

using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Mutators
{
	/// <summary>
	/// Generate string with random unicode characters in them from plane 1 (0x10000 - 0x1fffd).
	/// </summary>
	[Mutator("StringUnicodePlane1")]
	[Description("Produce a random string from the Unicode Plane 1 character set.")]
	public class StringUnicodePlane1 : Utility.StringMutator
	{
		public StringUnicodePlane1(DataElement obj)
			: base(obj)
		{
		}

		protected override int GetCodePoint()
		{
			return context.Random.Next(0x10000, 0x1FFFD + 1);
		}
	}
}
