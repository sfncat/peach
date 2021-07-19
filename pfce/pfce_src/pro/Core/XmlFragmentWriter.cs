using System.Xml;

namespace Peach.Pro.Core
{
	/// <summary>
	/// Filter object that doesn't pass WriteStartDocument/WriteEndDocument to the underlying XmlWriter.
	/// </summary>
	public class XmlFragmentWriter : XmlWriter
	{
		private readonly XmlWriter _writer;
		private bool _skip;

		public XmlFragmentWriter(XmlWriter writer)
		{
			_writer = writer;
		}

		public override void Close()
		{
			_writer.Close();
		}

		public override void Flush()
		{
			_writer.Flush();
		}

		public override string LookupPrefix(string ns)
		{
			return _writer.LookupPrefix(ns);
		}

		public override void WriteBase64(byte[] buffer, int index, int count)
		{
			_writer.WriteBase64(buffer, index, count);
		}

		public override void WriteCData(string text)
		{
			_writer.WriteCData(text);
		}

		public override void WriteCharEntity(char ch)
		{
			_writer.WriteCharEntity(ch);
		}

		public override void WriteChars(char[] buffer, int index, int count)
		{
			_writer.WriteChars(buffer, index, count);
		}

		public override void WriteComment(string text)
		{
			_writer.WriteComment(text);
		}

		public override void WriteDocType(string name, string pubid, string sysid, string subset)
		{
			_writer.WriteDocType(name, pubid, sysid, subset);
		}

		public override void WriteEndAttribute()
		{
			if (_skip)
				_skip = false;
			else
				_writer.WriteEndAttribute();
		}

		public override void WriteEndDocument()
		{
			// Don't pass thru to _writer
		}

		public override void WriteEndElement()
		{
			_writer.WriteEndElement();
		}

		public override void WriteEntityRef(string name)
		{
			_writer.WriteEntityRef(name);
		}

		public override void WriteFullEndElement()
		{
			_writer.WriteFullEndElement();
		}

		public override void WriteProcessingInstruction(string name, string text)
		{
			_writer.WriteProcessingInstruction(name, text);
		}

		public override void WriteRaw(string data)
		{
			_writer.WriteRaw(data);
		}

		public override void WriteRaw(char[] buffer, int index, int count)
		{
			_writer.WriteRaw(buffer, index, count);
		}

		public override void WriteStartAttribute(string prefix, string localName, string ns)
		{
			if (prefix == "xmlns" && (localName == "xsd" || localName == "xsi"))
			{
				_skip = true;
				return;
			}

			_writer.WriteStartAttribute(prefix, localName, ns);
		}

		public override void WriteStartDocument(bool standalone)
		{
			// Don't pass thru to _writer
		}

		public override void WriteStartDocument()
		{
			// Don't pass thru to _writer
		}

		public override void WriteStartElement(string prefix, string localName, string ns)
		{
			_writer.WriteStartElement(prefix, localName, ns);
		}

		public override WriteState WriteState
		{
			get { return _writer.WriteState; }
		}

		public override void WriteString(string text)
		{
			if (!_skip)
				_writer.WriteString(text);
		}

		public override void WriteSurrogateCharEntity(char lowChar, char highChar)
		{
			_writer.WriteSurrogateCharEntity(lowChar, highChar);
		}

		public override void WriteWhitespace(string ws)
		{
			_writer.WriteWhitespace(ws);
		}
	}
}
