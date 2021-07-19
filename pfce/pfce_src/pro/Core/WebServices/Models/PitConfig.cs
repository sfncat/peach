using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace Peach.Pro.Core.WebServices.Models
{
	[Serializable]
	public class PitConfig
	{
		public string Name { get; set; }

		public string Description { get; set; }

		[JsonConverter(typeof(JsonPathConverter))]
		public string OriginalPit { get; set; }

		public List<Param> Config { get; set; }

		public List<Agent> Agents { get; set; }

		public List<PitWeight> Weights { get; set; }
	}
}
