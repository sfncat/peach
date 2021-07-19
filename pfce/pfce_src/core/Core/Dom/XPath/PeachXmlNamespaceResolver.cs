using System.Collections.Generic;
using System.Xml;

namespace Peach.Core.Dom.XPath
{
	public class PeachXmlNamespaceResolver : IXmlNamespaceResolver
	{
		public IDictionary<string, string> GetNamespacesInScope(XmlNamespaceScope scope)
		{
			return new Dictionary<string, string>();
		}

		public string LookupNamespace(string prefix)
		{
			return prefix;
		}

		public string LookupPrefix(string namespaceName)
		{
			return namespaceName;
		}
	}
}
