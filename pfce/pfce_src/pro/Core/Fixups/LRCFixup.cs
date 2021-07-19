


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)
//   Ross Salpino (rsal42@gmail.com)
//   Mikhail Davidov (sirus@haxsys.net)

// $Id$

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Pro.Core.Fixups
{
	[Description("XOR bytes of data.")]
	[Fixup("Lrc", true)]
	[Fixup("LRCFixup")]
	[Fixup("checksums.LRCFixup")]
	[Parameter("ref", typeof(DataElement), "Reference to data element")]
	[Serializable]
	public class LRCFixup : Fixup
	{
		public LRCFixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args, "ref")
		{
		}

		protected override Variant fixupImpl()
		{
			var from = elements["ref"];
			var data = from.Value;
			byte lrc = 0;
			int b = 0;

			data.Seek(0, System.IO.SeekOrigin.Begin);

			while ((b = data.ReadByte()) != -1)
				lrc = (byte)((lrc + b) & 0xff);

			lrc = (byte)(((lrc ^ 0xff) + 1) & 0xff);

			if (parent is Peach.Core.Dom.String)
				return new Variant(lrc.ToString());

			if (parent is Peach.Core.Dom.Number)
				return new Variant((uint)lrc);

			return new Variant(new BitStream(new byte[] { lrc }));
		}

		protected override Variant GetDefaultValue(DataElement obj)
		{
			return new Variant(0);
		}
	}
}

// end
