using System;
using System.Collections.Generic;
using Peach.Core;
using Peach.Core.Runtime;

namespace Peach.Pro.Core
{
	public class Command : INamed, IComparable<Command>
	{
		public string Name { get; set; }
		public string Usage { get; set; }
		public string Description { get; set; }
		public OptionSet Options { get; set; }
		public Func<Command, List<string>, int> Action { get; set; }
		public Func<Command, List<string>, int> Help { get; set; }

		public string name
		{
			get { return Name; }
		}

		public int CompareTo(Command other)
		{
			return string.Compare(Name, other.name, StringComparison.Ordinal);
		}
	}
}
