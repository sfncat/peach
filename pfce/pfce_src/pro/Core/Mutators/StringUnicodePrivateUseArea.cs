//
// Copyright (c) Peach Fuzzer, LLC
//

using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Mutators
{
	/// <summary>
	/// Private Use Area: U+E000..U+F8FF (6,400 characters)
	/// </summary>
	[Mutator("StringUnicodePrivateUseArea")]
	[Description("Produce a random string from the Unicode private use area character set.")]
	public class StringUnicodePrivateUseArea : Utility.StringMutator
	{
		public StringUnicodePrivateUseArea(DataElement obj)
			: base(obj)
		{
		}

		protected override int GetCodePoint()
		{
			return context.Random.Next(0xE000, 0xF8FF + 1);
		}
	}
}
