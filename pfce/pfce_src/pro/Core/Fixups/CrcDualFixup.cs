


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)
//   Mikhail Davidov (sirus@haxsys.net)

// $Id$

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Pro.Core.Fixups.Libraries;

namespace Peach.Pro.Core.Fixups
{
	[Description("Standard CRC32 as defined by ISO 3309 applied to two elements.")]
	[Fixup("Crc32Dual", true)]
	[Fixup("CrcDualFixup")]
	[Fixup("Crc32DualFixup")]
	[Fixup("checksums.CrcDualFixup")]
	[Fixup("checksums.Crc32DualFixup")]
	[Parameter("ref1", typeof(DataElement), "Reference to first data element")]
	[Parameter("ref2", typeof(DataElement), "Reference to second data element")]
	[Parameter("type", typeof(CRCTool.CRCCode), "Type of CRC to run [CRC32, CRC16, CRC_CCITT]", "CRC32")]
	[Serializable]
	public class CrcDualFixup : Fixup
	{
		public DataElement ref1 { get; protected set; }
		public DataElement ref2 { get; protected set; }
		public CRCTool.CRCCode type { get; protected set; }

		public CrcDualFixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args, "ref1", "ref2")
		{
			ParameterParser.Parse(this, args);
		}

		protected override Variant fixupImpl()
		{
			var ref1 = elements["ref1"];
			var ref2 = elements["ref2"];

			var data = new BitStreamList();
			data.Add(ref1.Value);
			data.Add(ref2.Value);
			data.Seek(0, System.IO.SeekOrigin.Begin);

			CRCTool crcTool = new CRCTool();
			crcTool.Init(type);

			return new Variant((uint)crcTool.crctablefast(data));
		}

		protected override Variant GetDefaultValue(DataElement obj)
		{
			return new Variant(0);
		}
	}
}

// end
