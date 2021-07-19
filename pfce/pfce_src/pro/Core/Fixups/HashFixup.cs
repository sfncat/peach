


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)
//   Ross Salpino (rsal42@gmail.com)
//   Mikhail Davidov (sirus@haxsys.net)

// $Id$

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Pro.Core.Fixups
{
	[Serializable]
	public class HashFixup<T> : Fixup where T: HashAlgorithm, new()
	{
		public HexString DefaultValue { get; protected set; }
		public DataElement _ref { get; protected set; }

		public HashFixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args, "ref")
		{
			ParameterParser.Parse(this, args);
		}

		protected override Variant fixupImpl()
		{
			var from = elements["ref"];
			var data = from.Value;
			T hashTool = new T();

			data.Seek(0, System.IO.SeekOrigin.Begin);

			var hash = hashTool.ComputeHash(data);
			return new Variant(new BitStream(hash));
		}

		protected override Variant GetDefaultValue(DataElement obj)
		{
			return DefaultValue != null ? new Variant(DefaultValue.Value) : base.GetDefaultValue(obj);
		}
	}
}

// end
