using System.Collections.Generic;

namespace Peach.Pro.Core.WebServices.Models
{
	public class LibraryPit
	{
		public string Id { get; set; }
		public string PitUrl { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public List<Tag> Tags { get; set; }
		public bool Locked { get; set; }
	}
}
