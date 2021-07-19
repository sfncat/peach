using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Core.Runtime;

namespace Peach.Pro.Core.Analyzers
{
	[Analyzer("Regex", true)]
	[Usage("<regex> <infile> <outfile>")]
	[Description("Break up a string using a regex. Each group will become strings. The group name will be used as the element name.")]
	[Parameter("Regex", typeof(string), "The regex to use")]
	[Serializable]
	public class RegexAnalyzer : Analyzer
	{
		public new static readonly bool supportParser = false;
		public new static readonly bool supportDataElement = true;
		public new static readonly bool supportCommandLine = true;
		public new static readonly bool supportTopLevel = false;

		public string Regex { get; set; }

		public RegexAnalyzer()
		{
		}

		public RegexAnalyzer(Dictionary<string, Variant> args)
		{
			ParameterParser.Parse(this, args);
		}

		public override void asCommandLine(List<string> args)
		{
			if (args.Count != 3)
				throw new SyntaxException("Missing required arguments.");

			Regex = args[0];
			var inFile = args[1];
			var outFile = args[2];
			var data = new BitStream(File.ReadAllBytes(inFile));
			var model = new DataModel(Path.GetFileName(inFile).Replace(".", "_"));

			model.Add(new Peach.Core.Dom.String());
			model[0].DefaultValue = new Variant(data);

			asDataElement(model[0], null);

			var settings = new XmlWriterSettings();
			settings.Encoding = System.Text.Encoding.UTF8;
			settings.Indent = true;

			using (var sout = new FileStream(outFile, FileMode.Create))
			using (var xml = XmlWriter.Create(sout, settings))
			{
				xml.WriteStartDocument();
				xml.WriteStartElement("Peach");

				model.WritePit(xml);

				xml.WriteEndElement();
				xml.WriteEndDocument();
			}
		}

		public override void asDataElement(DataElement parent, Dictionary<DataElement, Peach.Core.Cracker.Position> positions)
		{
			// Verify the parent type is a string.
			if (!(parent is Peach.Core.Dom.String))
				throw new SoftException("Error, Regex analyzer can only be used with String elements. Element '" + parent.fullName + "' is a '" + parent.elementType + "'.");

			var data = (string)parent.DefaultValue;
			if (string.IsNullOrEmpty(data) && positions == null)
				return;

			var regex = new Regex(Regex, RegexOptions.Singleline);

			var match = regex.Match(data);
			if (!match.Success)
				throw new SoftException("The Regex analyzer failed to match.");

			var sorted = new SortedDictionary<int, Peach.Core.Dom.String>();

			// Create the Block element that will contain the matched strings
			var block = new Block(parent.Name);

			// The order of groups does not always match order from string
			// we will add them into a sorted dictionary to order them correctly
			for (int i = 1; i < match.Groups.Count; i++)
			{
				var group = match.Groups[i];
				var str = new Peach.Core.Dom.String(regex.GroupNameFromNumber(i));
				var value = data.Substring(group.Index, group.Length);
				str.DefaultValue = new Variant(value);
				sorted[group.Index] = str;
			}

			// Add elements in order they appeared in string
			foreach (var item in sorted.Keys)
				block.Add(sorted[item]);

			// Replace our current element (String) with the Block of matched strings
			parent.parent[parent.Name] = block;
		}
	}
}
