using System.Collections.Generic;

namespace Peach.Pro.Core.WebServices.Models
{
	public class Visualizer
	{
		public class Element
		{
			public string Name { get; set; }
			public string Type { get; set; }
			public List<Element> Children { get; set; }
		}

		public class Model : Element
		{
			public byte[] Original { get; set; }
			public byte[] Fuzzed { get; set; }
		}

		public uint Iteration { get; set; }

		public List<string> MutatedElements { get; set; }

		public List<Model> Models { get; set; }
	}
}
