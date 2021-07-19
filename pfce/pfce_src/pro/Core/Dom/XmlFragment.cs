
using System;
using System.IO;
using System.Xml;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.IO;
using ValueType = Peach.Core.Dom.ValueType;

namespace Peach.Pro.Core.Dom
{
	[DataElement("XmlFragment", DataElementTypes.NonDataElements)]
	[PitParsable("XmlFragment")]
	[DataElementChildSupported("Placement")]
	[Parameter("name", typeof(string), "Element name", "")]
	[Parameter("fieldId", typeof(string), "Element field ID", "")]
	[Parameter("type", typeof(StringType), "Type of string (encoding)", "utf8")]
	[Parameter("value", typeof(string), "Default value", "")]
	[Parameter("valueType", typeof(ValueType), "Format of value attribute", "string")]
	[Parameter("token", typeof(bool), "Is element a token", "false")]
	[Parameter("mutable", typeof(bool), "Is element mutable", "true")]
	[Parameter("constraint", typeof(string), "Scripting expression that evaluates to true or false", "")]
	[Parameter("minOccurs", typeof(int), "Minimum occurances", "1")]
	[Parameter("maxOccurs", typeof(int), "Maximum occurances", "1")]
	[Parameter("occurs", typeof(int), "Actual occurances", "1")]
	[Serializable]
	public class XmlFragment : Peach.Core.Dom.String
	{
		public XmlFragment()
		{
		}

		public XmlFragment(string name)
			: base(name)
		{
		}

		public override bool hasLength
		{
			get
			{
				return false;
			}
		}

		public override bool isDeterministic
		{
			get
			{
				return true;
			}
		}

		public new static DataElement PitParser(PitParser context, XmlNode node, DataElementContainer parent)
		{
			if (node.Name != "XmlFragment")
				return null;

			var doc  = Generate<XmlFragment>(node, parent);
			doc.parent = parent;

			context.handleCommonDataElementAttributes(node, doc);
			context.handleCommonDataElementChildren(node, doc);

			return doc;
		}

		public override void WritePit(XmlWriter pit)
		{
			throw new NotImplementedException();
		}

		public override void Crack(DataCracker context, BitStream data, long? size)
		{
			var adapter = new StreamAdapter(data);

			var pos = data.PositionBits;

			var rdr = XmlReader.Create(adapter, new XmlReaderSettings
			{
				ConformanceLevel = ConformanceLevel.Fragment
			});

			try
			{
				while (rdr.Read())
				{
					if (rdr.NodeType == XmlNodeType.EndElement && rdr.Depth == 0)
						break;
				}
			}
			catch (XmlException ex)
			{
				throw new CrackingFailure("Value is not valid XML", this, data, ex);
			}

			var endPos = data.PositionBits;

			data.PositionBits = pos;

			base.Crack(context, data, endPos - pos);
		}

		private class StreamAdapter : System.IO.Stream
		{
			private readonly BitStream _bs;

			public StreamAdapter(BitStream bs)
			{
				_bs = bs;
			}

			public override void Flush()
			{
				throw new NotImplementedException();
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				throw new NotImplementedException();
			}

			public override void SetLength(long value)
			{
				throw new NotImplementedException();
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				// Only read 1 byte at a time so we don't go past the end of the doc
				_bs.WantBytes(1);

				return _bs.Read(buffer, offset, 1);
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				throw new NotImplementedException();
			}

			public override bool CanRead { get { return true;  } }
			public override bool CanSeek { get { return false; } }
			public override bool CanWrite { get { return false; } }
			public override long Length { get { return _bs.Length; } }

			public override long Position
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}
		}
	}
}
