
//
// Example analyzer implementation.  This analyzer works via the command line and
// also at runtime via the Analyzer XML element.
//
// This example is annotated and can be used as a template for creating your own
// custom analyzer implementations.
//
// To use this example:
//   1. Create a .NET CLass Library project in Visual Studio or Mono Develop
//   2. Add a refernce to Peach.Core.dll
//   3. Add this source file
//   4. Modify and compile
//   5. Place compiled DLL into the Peach\Plugins folder
//   6. Verify Peach picks up your extension by checking "peach --showenv" output
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.ComponentModel;

namespace MyExtensions
{
	// The following class attributes are used to identify Peach extensions and also 
	// produce the information shown by "peach --showenv".
	//
	// "RegexExample" is the name used in the "class" attribute of Analyzer XML element
	// and also for command line as value for --analyzer=XXX option
	[Analyzer("RegexExample", true)]
	[Description("Break up a string using a regex. Each group will become strings. The group name will be used as the element name.")]
	// Zero or more parameters are supported. Parameters without a default value are
	// considered required.
	[Parameter("Regex", typeof(string), "The regex to use")]
	// All analyzers must be marked Serializable
	[Serializable]
	public class RegexAnalyzer : Analyzer
	{
		// All analyzers are required to define these static
		// values to define which operational methods are 
		// supported.
		public new static readonly bool supportParser = false;
		public new static readonly bool supportDataElement = true;
		public new static readonly bool supportCommandLine = true;
		public new static readonly bool supportTopLevel = false;

		// These properties will receive the parameter values
		// defined above.
		public string Regex { get; set; }

		public RegexAnalyzer()
		{
		}

		public RegexAnalyzer(Dictionary<string, Variant> args)
		{
			// Parse parameters into properties
			ParameterParser.Parse(this, args);
		}

		// This method is only required when supporting command line operation
		// via the --analyzer command line switch.
		//
		// The output from this method should be a file on disk containing
		// valid Peach PIT XML.
		public override void asCommandLine(List<string> args)
		{
			// Handle any arguments.  TYpically analyzers take in two
			// arguments.  An input file and output file.  This analzer
			// uses three arguments.

			if (args.Count < 3)
			{
				Console.WriteLine("Syntax: <regex> <infile> <outfile>");
				return;
			}

			Regex = args[0];
			var inFile = args[1];
			var outFile = args[2];

			// Re-use the logic from asDataElement by creating a data model
			// containing the data

			var data = new BitStream(File.ReadAllBytes(inFile));
			var model = new DataModel(Path.GetFileName(inFile).Replace(".", "_"))
			{
				new Peach.Core.Dom.String()
			};

			model[0].DefaultValue = new Variant(data);

			asDataElement(model[0], null);

			// Write the generated elements to XML using the "WritePit" methods.

			var settings = new XmlWriterSettings
			{
				Encoding = System.Text.Encoding.UTF8,
				Indent = true
			};

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

		// This method is only required when supporting runtime operation via
		// the Analyzer XML element.
		//
		// Analyzer should replace or expand the parent element.
		public override void asDataElement(DataElement parent, Dictionary<DataElement, Peach.Core.Cracker.Position> positions)
		{
			// THis analyzer only works with String parents
			if (!(parent is Peach.Core.Dom.String))
				throw new SoftException("Error, Regex analyzer can only be used with String elements. Element '" + parent.fullName + "' is a '" + parent.elementType + "'.");

			// Implement the logic

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
			for (var i = 1; i < match.Groups.Count; i++)
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
