using System;
using Peach.Pro.Core.WebServices;

namespace Peach.Pro.WebApi2
{
	public interface IWebContext
	{
		string PitLibraryPath { get; }
		string NodeGuid { get; }
	}
	
	/// <summary>
	/// The context that is passed to each ApiController instance.
	/// This is where state between requests is maintained.
	/// </summary>
	public class WebContext : IWebContext
	{
		public WebContext(string pitLibraryPath)
		{
			PitLibraryPath = pitLibraryPath;
			NodeGuid = Guid.NewGuid().ToString().ToLower();
		}

		public string PitLibraryPath { get; private set; }
		public string NodeGuid { get; private set; }
	}
}
