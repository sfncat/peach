using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Core.Xsd;

namespace Peach.Pro.Test.Core
{
	public class PitReader : XmlReader
	{
		private static readonly Regex MatchDefine = new Regex(@"##(.*?)##");

		private static XmlSchema PitSchema;

		private readonly XmlReader _reader;
		private readonly Dictionary<string, string> _defines;

		public StringBuilder Errors { get; private set; }

		public PitReader(string xml, Dictionary<string, string> defines)
			: this(new StringReader(xml), defines)
		{
			//			value = reEscapeSlash.Replace(value, new MatchEvaluator(ReplaceSlash));

		//static Regex reEscapeSlash = new Regex(@"\\\\|\\n|\\r|\\t");

		//static string ReplaceSlash(Match m)
		//{
		//	string s = m.ToString();

		//	switch (s)
		//	{
		//		case "\\\\": return "\\";
		//		case "\\n": return "\n";
		//		case "\\r": return "\r";
		//		case "\\t": return "\t";
		//	}

		//	throw new ArgumentOutOfRangeException("m");
		//}

		}

		public PitReader(TextReader reader, Dictionary<string, string> defines)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");
			if (defines == null)
				throw new ArgumentNullException("defines");

			if (PitSchema == null)
				PitSchema = new SchemaBuilder(typeof(Peach.Core.Xsd.Dom)).Compile();

			Errors = new StringBuilder();

			var set = new XmlSchemaSet();
			set.Add(PitSchema);

			var settings = new XmlReaderSettings
			{
				ValidationType = ValidationType.Schema,
				Schemas = set,
				NameTable = new NameTable()
			};

			settings.ValidationEventHandler += delegate(object sender, ValidationEventArgs e)
			{
				var ex = e.Exception;

				Errors.AppendFormat("Line: {0}, Position: {1} - ", ex.LineNumber, ex.LinePosition);
				Errors.Append(ex.Message);
				Errors.AppendLine();
			};

			// Default the namespace to peach
			var nsMgr = new XmlNamespaceManager(settings.NameTable);
			nsMgr.AddNamespace("", Peach.Core.Xsd.Dom.TargetNamespace);

			var parserCtx = new XmlParserContext(settings.NameTable, nsMgr, null, XmlSpace.Default);
			_reader = Create(reader, settings, parserCtx);
			_defines = defines;
		}

		private string FilterValue(string value)
		{
			var ret = MatchDefine.Replace(value, ReplaceDefine);
			Console.WriteLine("Filter: {0} -> {1}", value, ret);
			return ret;
		}

		private string ReplaceDefine(Match m)
		{
			string s = m.ToString();

			if (_defines.ContainsKey(s))
				return s;

			switch (s)
			{
				case "\\\\": return "\\";
				case "\\n": return "\n";
				case "\\r": return "\r";
				case "\\t": return "\t";
			}

			throw new ArgumentOutOfRangeException("m");
		}

		#region XmlReader Overrides

		protected override void Dispose(bool disposing)
		{
			if (_reader != null)
				_reader.Dispose();

			base.Dispose(disposing);
		}

		public override string GetAttribute(string name)
		{
			return _reader.GetAttribute(name);
		}

		public override string GetAttribute(string name, string namespaceURI)
		{
			return _reader.GetAttribute(name, namespaceURI);
		}

		public override string GetAttribute(int i)
		{
			return _reader.GetAttribute(i);
		}

		public override bool MoveToAttribute(string name)
		{
			return _reader.MoveToAttribute(name);
		}

		public override bool MoveToAttribute(string name, string ns)
		{
			return _reader.MoveToAttribute(name, ns);
		}

		public override bool MoveToFirstAttribute()
		{
			return _reader.MoveToFirstAttribute();
		}

		public override bool MoveToNextAttribute()
		{
			return _reader.MoveToNextAttribute();
		}

		public override bool MoveToElement()
		{
			return _reader.MoveToElement();
		}

		public override bool ReadAttributeValue()
		{
			return _reader.ReadAttributeValue();
		}

		public override bool Read()
		{
			return _reader.Read();
		}

		public override string LookupNamespace(string prefix)
		{
			return _reader.LookupNamespace(prefix);
		}

		public override void ResolveEntity()
		{
			_reader.ResolveEntity();
		}

		public override XmlNodeType NodeType
		{
			get { return _reader.NodeType; }
		}

		public override string LocalName
		{
			get { return _reader.LocalName; }
		}

		public override string NamespaceURI
		{
			get { return _reader.NamespaceURI; }
		}

		public override string Prefix
		{
			get { return _reader.Prefix; }
		}

		public override string Value
		{
			get { return NodeType == XmlNodeType.Text ? FilterValue(_reader.Value) : _reader.Value; }
		}

		public override int Depth
		{
			get { return _reader.Depth; }
		}

		public override string BaseURI
		{
			get { return _reader.BaseURI; }
		}

		public override bool IsEmptyElement
		{
			get { return _reader.IsEmptyElement; }
		}

		public override int AttributeCount
		{
			get { return _reader.AttributeCount; }
		}

		public override bool EOF
		{
			get { return _reader.EOF; }
		}

		public override ReadState ReadState
		{
			get { return _reader.ReadState; }
		}

		public override XmlNameTable NameTable
		{
			get { return _reader.NameTable; }
		}

		#endregion
	}

	[TestFixture]
	[Quick]
	[Peach]
	public class PitReaderTests
	{
		[Test]
		public void TestRead()
		{
			const string xml = @"
<Peach>
	<DataModel name='DM'>
		<String value='Hello World' />
		<Choice name='TheChoice'>
			<Block name='A'>
				<Choice name='InnerChoice'>
					<Blob name='AA' />
					<Blob name='AB' />
				</Choice>
			</Block>
			<Block name='B'>
				<Choice name='InnerChoice'>
					<Blob name='BA' />
					<Blob name='BB' />
					<Asn1Type name='ASN' tag='0'>
						<Block name='V' />
					</Asn1Type>
				</Choice>
			</Block>
		</Choice>
		<Block name='Array' occurs='10'>
			<Blob name='Item' />
		</Block>
	</DataModel>
</Peach>";

			var defines = new Dictionary<string, string>();

			var ret = new XmlDocument();

			using (var rdr = new PitReader(xml, defines))
			{
				ret.Load(rdr);
			}
		}
	}
}
