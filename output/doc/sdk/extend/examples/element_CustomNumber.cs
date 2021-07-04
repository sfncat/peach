using System;
using System.IO;
using System.Xml;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.IO;
using ValueType = Peach.Core.Dom.ValueType;

namespace CustomNumber
{
	[DataElement("CustomNumber", DataElementTypes.NonDataElements)]
	[PitParsable("CustomNumber")]
	[Parameter("name", typeof(string), "Element name", "")]
	[Parameter("length", typeof(uint?), "Length in data element", "")]
	[Parameter("lengthType", typeof(LengthType), "Units of the length attribute", "bytes")]
	[Parameter("value", typeof(string), "Default value", "")]
	[Parameter("valueType", typeof(ValueType), "Format of value attribute", "string")]
	[Parameter("mutable", typeof(bool), "Is element mutable", "true")]
	[Parameter("constraint", typeof(string), "Scripting expression that evaluates to true or false", "")]
	[Parameter("minOccurs", typeof(int), "Minimum occurances", "1")]
	[Parameter("maxOccurs", typeof(int), "Maximum occurances", "1")]
	[Parameter("occurs", typeof(int), "Actual occurances", "1")]
	[Serializable]
	public class CustomNumber : Block
	{
		readonly Number _number;

		public CustomNumber()
		{
			_number = new Number();
			Initialize();
		}

		public CustomNumber(string name)
			: base(name)
		{
			_number = new Number(name);
		}

		private void Initialize()
		{
			_number.Signed = false;
			_number.lengthType = LengthType.Bits;
			_number.length = 64;
		}

		public static new DataElement PitParser(PitParser context, XmlNode node, DataElementContainer parent)
		{
			var ret = Generate<CustomNumber>(node, parent);

			context.handleCommonDataElementAttributes(node, ret);
			context.handleCommonDataElementChildren(node, ret);
			context.handleCommonDataElementValue(node, ret);

			return ret;
		}

		public override Variant DefaultValue
		{
			get { return _number.DefaultValue; }
			set { _number.DefaultValue = value; }
		}

		public override void Crack(DataCracker context, BitStream data, long? size)
		{
			var val = 0;
			// TODO: crack data into a number
			_number.DefaultValue = new Variant(val);
		}

		protected override BitwiseStream InternalValueToBitStream()
		{
			var val = (ulong)_number.InternalValue;
			var ret = new BitStream();
			// TODO: encode val as bytes and store in ret
			var buf = Endian.Big.GetBytes(val, 64);
			ret.Write(buf, 0, buf.Length);
			ret.Seek(0, SeekOrigin.Begin);
			return ret;
		}
	}
}
