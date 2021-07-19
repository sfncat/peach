//
// Copyright (c) Peach Fuzzer, LLC
//

using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Mutators
{
	[Mutator("StringAsciiRandom")]
	[Description("Produce random strings using the ascii character set.")]
	public class StringAsciiRandom : Utility.StringMutator
	{
		public StringAsciiRandom(DataElement obj)
			: base(obj)
		{
		}

		protected override int GetCodePoint()
		{
			return context.Random.Next(0x00, 0x7F + 1);
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			// Override so we attach to all strings, not just unicode
			if (obj is Peach.Core.Dom.String && obj.isMutable)
				return true;

			return false;
		}
	}
}
