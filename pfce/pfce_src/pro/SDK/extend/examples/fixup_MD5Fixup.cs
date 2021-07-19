
// Example fixup class.  This file can be used as a template or starting point
// for your own custom fixup.
//
// To use this example:
//   1. Create a .NET CLass Library project in Visual Studio or Mono Develop
//   2. Add a refernce to Peach.Core.dll
//   3. Add this source file
//   4. Modify and compile
//   5. Place compiled DLL into the Peach folder
//   6. Verify Peach picks up your extension by checking "peach --showenv" output
//

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using System.ComponentModel;

namespace MyExtentions
{
	// THe following class attributes are used to find your extension
	// and produce the output from "peach --showenv".
	[Description("Standard MD5 checksum.")]
	// The name "Md5Example" is the fixup name used in the "<Fixup class="xxx"" attribute
	[Fixup("Md5Example", true)]
	// Zero or more paramters can be specified with or without a default value
	// Parameters w/o a default value are required.
	[Parameter("ref", typeof(DataElement), "Reference to data element")]
	[Parameter("DefaultValue", typeof(HexString), "Default value to use when recursing (default is parent's DefaultValue)", "")]
	[Serializable]
	public class MD5Fixup : Fixup
	{
		// The following properties will receive the parameter values
		public HexString DefaultValue { get; protected set; }
		public DataElement _ref { get; protected set; }

		public MD5Fixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args, "ref")
		{
			// Handle assising the arguments into the parameter properties
			ParameterParser.Parse(this, args);
		}

		// Code to implement fixup goes in this method
		// The return value becomes the elements new value
		protected override Variant fixupImpl()
		{
			// all references are accessable via the elements collection
			var from = elements["ref"];

			// The rest of this code is the MD5 fixup logic

			var data = from.Value;
			var hashTool = new MD5CryptoServiceProvider();

			data.Seek(0, System.IO.SeekOrigin.Begin);

			var hash = hashTool.ComputeHash(data);
			return new Variant(new BitStream(hash));
		}

		// Optionally provide a default value to use if the 
		// fixup will recurse.  For checksums this is typically 
		// required to be 0.
		protected override Variant GetDefaultValue(DataElement obj)
		{
			return DefaultValue != null ? new Variant(DefaultValue.Value) : base.GetDefaultValue(obj);
		}
	}
}
