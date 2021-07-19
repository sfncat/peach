//
// Copyright (c) Peach Fuzzer, LLC
//

using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Mutators
{
	/// <summary>
	/// Uses StringStatic and injects UTF-8 BOM characters randomly into the strings.
	/// </summary>
	[Mutator("StringUtf8BomStatic")]
	[Description("Uses StringStatic and injects UTF-8 BOM characters randomly into the strings.")]
	public class StringUtf8BomStatic : Utility.StringBomStatic
	{
		static byte[][] bom = new byte[][]
		{
			Encoding.UTF8.ByteOrderMark,
		};


		public StringUtf8BomStatic(DataElement obj)
			: base(obj)
		{
		}

		protected override byte[][] BOM
		{
			get
			{
				return bom;
			}
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			var asStr = obj as Peach.Core.Dom.String;

			// Make sure we are a mutable string and Peach.TypeTransform hint is not false
			if (asStr == null || !asStr.isMutable || !getTypeTransformHint(asStr))
				return false;

			// Attach to ascii and unicode, since most ascii parsers are utf8
			return true;
		}
	}
}
