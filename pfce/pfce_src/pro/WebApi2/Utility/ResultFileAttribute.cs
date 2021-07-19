using System;
using System.Web;

namespace Peach.Pro.WebApi2.Utility
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	internal class ResultFileAttribute : Attribute
	{
		public string ContentType { get; private set; }

		public ResultFileAttribute(string extension)
		{
			ContentType = MimeMapping.GetMimeMapping(extension);
		}
	}
}
