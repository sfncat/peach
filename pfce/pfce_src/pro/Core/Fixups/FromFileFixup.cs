using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Pro.Core.Fixups
{
	public enum FromFileEncoding
	{
		Raw,
		Pem
	}

	[Description("Load data from a file into data element.")]
	[Fixup("FromFile", true)]
	[Parameter("Filename", typeof(string), "Filename to load data from")]
	[Parameter("Encoding", typeof(FromFileEncoding), "File encoding [Raw, Pem]", "Raw")]
	[Parameter("Required", typeof(bool), "Is file required?", "true")]
	[Serializable]
	public class FromFileFixup : Fixup
	{
		public string Filename { get; set; }
		public FromFileEncoding Encoding { get; set; }
		public bool Required { get; set; }

		public FromFileFixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
			ParameterParser.Parse(this, args);
		}

		protected BitwiseStream DecodePem(byte[] bytes)
		{
			var _pemContentRegex = new Regex(@"-----BEGIN [A-Za-z0-9 ]*-----(.*)-----END [A-Za-z0-9 ]*-----", RegexOptions.Singleline);

			var pem = Peach.Core.Encoding.UTF8.GetString(bytes);
			var match = _pemContentRegex.Match(pem);

			if (!match.Success)
				throw new PeachException(string.Format("Error, invalid PEM file supplied to FromFile fixup. File '{0}'.",
					Filename));

			var b64data = pem.Substring(match.Groups[1].Index, match.Groups[1].Length);

			return new BitStream(Convert.FromBase64String(b64data));
		}

		protected override Variant fixupImpl()
		{
			if (!System.IO.File.Exists(Filename))
			{
				if(Required)
					throw new PeachException(string.Format("Error, FromFile fixup cannot find file '{0}'.",
						Filename));

				return new Variant(new byte[0]);
			}

			var bytes = System.IO.File.ReadAllBytes(Filename);

			switch (Encoding)
			{
				case FromFileEncoding.Raw:
					break;

				case FromFileEncoding.Pem:
					return new Variant(DecodePem(bytes));
			}

			return new Variant(bytes);
		}
	}
}
