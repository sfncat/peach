using System;
using System.Collections.Generic;

namespace Peach.Pro.Core.WebServices.Models
{
	public class Library
	{
		public string LibraryUrl { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }

		public bool Locked { get; set; }

		public List<LibraryVersion> Versions { get; set; }

		public List<Group> Groups { get; set; }

		public string User { get; set; }

		public DateTime Timestamp { get; set; }
	}
}
