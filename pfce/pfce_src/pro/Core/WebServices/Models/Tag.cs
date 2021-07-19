using System;
using System.Collections.Generic;
using Peach.Pro.Core.Storage;

namespace Peach.Pro.Core.WebServices.Models
{
	/// <summary>
	/// An arbitrary tag that is included with various models.
	/// </summary>
	[Serializable]
	public class Tag
	{
		/// <summary>
		/// The name of this tag.
		/// </summary>
		/// <example>
		/// "Category.Network"
		/// </example>
		[Key]
		public string Name { get; set; }

		/// <summary>
		/// The values of this tag.
		/// </summary>
		/// <example>
		/// { "Category", "Network" }
		/// </example>
		public List<string> Values { get; set; }
	}
}
