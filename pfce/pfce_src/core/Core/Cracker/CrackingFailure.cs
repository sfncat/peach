


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Core.Cracker
{
	public class CrackingFailure : ApplicationException
	{
		public DataElement element;
		public BitStream data;
		public bool logged = false;

		public string ShortMessage { get; private set; }

		public CrackingFailure(string msg, DataElement element, BitStream data)
			: this(msg, element, data, null)
		{
		}

		public CrackingFailure(string msg, DataElement element, BitStream data, Exception innerException)
			: base("{0} failed to crack. {1}".Fmt(element.debugName, msg), innerException)
		{
			this.element = element;
			this.data = data;

			ShortMessage = msg;
		}
	}
}
