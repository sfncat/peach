//
// Copyright (c) Peach Fuzzer, LLC
//

using Peach.Core;
using Peach.Core.Dom;
using System.ComponentModel;

namespace Peach.Pro.Core.Mutators
{
	/// <summary>
	/// Uses StringStatic and injects UTF-8 BOM characters randomly into the strings.
	/// </summary>
	[Mutator("StringUtf32BomStatic")]
	[Description("Uses StringStatic and injects UTF-8 BOM characters randomly into the strings.")]
	public class StringUtf32BomStatic : Utility.StringBomStatic
	{
		static byte[][] bom = new byte[][]
		{
			Encoding.UTF32.ByteOrderMark,
			Encoding.BigEndianUTF32.ByteOrderMark,
		};


		public StringUtf32BomStatic(DataElement obj)
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

			// Attach to all unicode strings
			if (asStr.stringType != StringType.ascii)
				return true;

			return false;
		}
	}
}
