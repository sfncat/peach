using System;
using System.Collections.Generic;

namespace Peach.Pro.Core.WebServices.Models
{
	public class PitField 
	{
		public PitField()
		{
			Fields = new List<PitField>();
		}

		public string Id { get; set; }
		public List<PitField> Fields { get; set; }
	}

	[Serializable]
	public class PitWeight 
	{
		public string Id { get; set; }
		public int Weight { get; set; }
	}

	public class PitMetadata
	{
		public List<ParamDetail> Defines { get; set; }
		public List<string> Calls { get; set; }
		public List<ParamDetail> Monitors { get; set; }
		public List<PitField> Fields { get; set; }
	}

	public class Pit : LibraryPit
	{
		public List<PeachVersion> Peaches { get; set; }

		public string User { get; set; }

		public DateTime Timestamp { get; set; }

		public List<Param> Config { get; set; }

		public List<Agent> Agents { get; set; }

		public List<PitWeight> Weights { get; set; }

		public PitMetadata Metadata { get; set; }
	}
}
