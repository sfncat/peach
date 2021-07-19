//
// Copyright (c) Peach Fuzzer, LLC
//

using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Mutators
{
	/// <summary>
	/// Generate string with random invalid unicode characters (0xd800 - 0xdbff).
	/// </summary>
	[Mutator("StringUnicodeInvalid")]
	[Description("Produce a random string using invalid unicode characters.")]
	public class StringUnicodeInvalid : Utility.StringMutator
	{
		public StringUnicodeInvalid(DataElement obj)
			: base(obj)
		{
		}

		protected override int GetCodePoint()
		{
			return context.Random.Next(0xD800, 0xDBFF + 1);
		}

		protected override string GetChar()
		{
			var cp = GetCodePoint();
			var ch = new string((char)cp, 1);

			return ch;
		}
	}
}
