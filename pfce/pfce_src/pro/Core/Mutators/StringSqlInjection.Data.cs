//
// Copyright (c) Peach Fuzzer, LLC
//

namespace Peach.Pro.Core.Mutators
{
	public partial class StringSqlInjection
	{
		static readonly string[] values = new string[]
		{
			"\'",
			"\"",
			" -- ",
			" /* ",
			"%%"
		};
	}
}
