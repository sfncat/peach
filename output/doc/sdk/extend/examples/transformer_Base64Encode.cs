
//
// Example transformer implementation that performs both an encode and decode
// transformation.
//
// This example is annotated and can be used as a template for creating your own
// custom transformer implementations.
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

namespace MyExtensions
{
	// THe following class attributes are used to identify Peach extensions and also 
	// produce the information shown by "peach --showenv".
	[Description("Encode on output as Base64.")]
	// "Base64EncodeExample" is the name used in the "class" attribute of Transformer XML element
	[Transformer("Base64EncodeExample", true)]
	// Zero or more parameters can be defined.  Parameters without a default are considered
	// required.
	[Parameter("RequiredString", typeof(string), "This parameter is required")]
	[Parameter("OptionalInt", typeof(int), "This parameter is optional", "3000")]
	// Transformers must be marked as Serializable
	[Serializable]
	public class Base64Encode : Transformer
	{
		// Properties automatically get populated with parameters defined above
		public string RequiredString { get; set; }
		public int OptionalInt { get; set; }

		public Base64Encode(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
		}

		// Implement this method to perform an encoding transformeration
		protected override BitwiseStream internalEncode(BitwiseStream data)
		{
			return CryptoStream(data, new ToBase64Transform(), CryptoStreamMode.Write);
		}

		// Optionally implement this method to perform a decode transformation.  Not all
		// transformers can perform both encode and decode operations.
		protected override BitStream internalDecode(BitStream data)
		{
			return CryptoStream(data, new FromBase64Transform(), CryptoStreamMode.Read);
		}
	}
}

