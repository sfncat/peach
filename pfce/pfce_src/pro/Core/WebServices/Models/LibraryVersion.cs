using System.Collections.Generic;

namespace Peach.Pro.Core.WebServices.Models
{
	public class LibraryVersion
	{
		public int Version { get; set; }

		public bool Locked { get; set; }

		public List<LibraryPit> Pits { get; set; }
	}
}
