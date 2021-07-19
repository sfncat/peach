//
// Copyright (c) Peach Fuzzer, LLC
//

using System.ComponentModel;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Mutators
{
	/// <summary>
	/// Uses StringLengthVariance and injects BOM characters randomly into the strings.
	/// </summary>
	[Mutator("StringUtf32BomLength")]
	[Description("Uses StringLengthVariance and injects BOM characters randomly into the strings.")]
	public class StringUtf32BomLength : Utility.StringBomLength
	{
		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		static byte[][] bom = new byte[][]
		{
			Encoding.UTF32.ByteOrderMark,
			Encoding.BigEndianUTF32.ByteOrderMark,
		};

		public StringUtf32BomLength(DataElement obj)
			: base(obj)
		{
		}

		protected override NLog.Logger Logger
		{
			get
			{
				return logger;
			}
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
