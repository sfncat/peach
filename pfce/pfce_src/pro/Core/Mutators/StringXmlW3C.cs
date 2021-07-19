//
// Copyright (c) Peach Fuzzer, LLC
//

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Ionic.Zip;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.ComponentModel;

namespace Peach.Pro.Core.Mutators
{
	[Mutator("StringXmlW3C")]
	[Description("Performs the W3C parser tests. Only works on <String> elements with a <Hint name=\"type\" value=\"xml\">")]
	[Hint("XML", "Enable XML W2C test cases")]
	public class StringXmlW3C : Mutator
	{
		static string[] values;
		static Stream stream;
		static ZipFile zip;

		static StringXmlW3C()
		{
			stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Peach.Pro.Core.Resources.xmltests.zip");
			zip = ZipFile.Read(stream);

			var list = new List<string>();

			list.AddRange(ReadLines("xmltests/error.txt"));
			list.AddRange(ReadLines("xmltests/invalid.txt"));
			list.AddRange(ReadLines("xmltests/nonwf.txt"));
			list.AddRange(ReadLines("xmltests/valid.txt"));

			values = list.ToArray();
		}

		static IEnumerable<string> ReadLines(string fileName)
		{
			var rdr = new StreamReader(zip[fileName].OpenReader());

			while (!rdr.EndOfStream)
				yield return rdr.ReadLine();
		}

		public StringXmlW3C(DataElement obj)
			: base(obj)
		{
		}

		public new static bool supportedDataElement(DataElement obj)
		{
			if (obj is Peach.Core.Dom.String && obj.isMutable)
			{
				Hint h = null;
				if (obj.Hints.TryGetValue("XML", out h))
				{
					if (h.Value == "xml")
						return true;
				}
			}

			return false;
		}

		public override int count
		{
			get
			{
				return values.Length;
			}
		}

		public override uint mutation
		{
			get;
			set;
		}

		public override void sequentialMutation(DataElement obj)
		{
			performMutation(obj, (int)mutation);
		}

		public override void randomMutation(DataElement obj)
		{
			performMutation(obj, context.Random.Next(values.Length));
		}

		void performMutation(DataElement obj, int index)
		{
			var path = "xmltests/" + values[index];
			var entry = zip[path];
			var data = entry.OpenReader();
			var bs = new BitStream();

			data.CopyTo(bs);
			bs.Seek(0, SeekOrigin.Begin);

			obj.MutatedValue = new Variant(bs);
			obj.mutationFlags = MutateOverride.Default | MutateOverride.TypeTransform;
		}
	}
}
