
// Copyright (c) Peach Fuzzer, LLC

// Example stub code for a custom Peach Fixup

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

using Peach.Core;
using Peach.Core.Dom;

namespace VisualStudio2012
{
	// Use the [Fixup] attribute to indicate the class can be used as a Fixup and
	// provide the name used in the XML. (<Fixup class="Custom" />)
	[Fixup("Custom", true)]
	// Define zero or more parameters with name, type, descriptiom, and optional default value.
	// parameters w/o a default value will be required.
	[Parameter("ref", typeof(string), "Reference to data element")]
	// An example optional integer parameter that defaults to 42.
	[Parameter("intArg", typeof(int), "Integer fixup argument", "42")]
	// Optional description of fixup to display via --showenv
	[Description("Example custom fixup from Peach SDK.")]
	// Fixups must have the Serializable attribute
	[Serializable]
	public class CustomFixup : Fixup
	{
		// Needed by the ParameterParser to bind [Parameter("ref")] to a property
		// Because 'ref' is a C# keyword, ParameterParser will look for '_ref'
		protected string _ref { get; set; }

		// ParameterParser will populate this, needs to be same name and type as
		// used by the Parameter Attribute on the class.
		protected int intArg { get; set; }

		public CustomFixup(DataElement parent, Dictionary<string, Variant> args)
			// For parameters that are references to data elements, we need to
			// pass the name of the parameter to the base constructor so the
			// fixup will properly track when the target element's value changes
			: base(parent, args, "ref")
		{
			// Automatically resolve parameters
			ParameterParser.Parse(this, args);
		}

		protected override Variant fixupImpl()
		{
			// Locate the target element in the dictionary this.elements
			// using the name of the parameter as the key.
			var elem = elements["ref"];
			var data = elem.Value;

			// Make sure we are at the start of our data stream
			data.Seek(0, System.IO.SeekOrigin.Begin);

			// TODO - Implement fixup logic here

			return new Variant(intArg /* place output here */);
		}
	}
}

// end
