using System;
using Newtonsoft.Json;
using Peach.Pro.Core.Storage;

namespace Peach.Pro.Core.WebServices.Models
{
	[Flags]
	public enum GroupAccess
	{
		None  = 0x0,
		Read  = 0x1,
		Write = 0x2,
	}

	public class Group
	{
		[Key]
		public string GroupUrl { get; set; }

		public GroupAccess Access { get; set; }
	}
}
