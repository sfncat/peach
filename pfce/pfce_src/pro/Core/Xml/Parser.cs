

using System.Collections.Generic;
using System.IO;

namespace Peach.Pro.Core.Xml
{
	/// <summary>
	/// Abstract base class for all XML schema parsers (DTD, Schema, relaxedng, etc)
	/// </summary>
	public abstract class Parser
	{
		public Dictionary<string, Element> elements = new Dictionary<string, Element>();
		public Dictionary<Element, string> elementData = new Dictionary<Element, string>();

		public abstract void parse(TextReader reader);
	}
}

// end
