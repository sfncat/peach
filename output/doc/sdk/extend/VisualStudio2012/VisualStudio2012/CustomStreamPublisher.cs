
// Copyright (c) Peach Fuzzer, LLC

// Example stub code for a custom Peach Publisher

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

using NLog;

using Peach.Core;
using Peach.Core.IO;
using Peach.Core.Dom;
using Peach.Core.Publishers;

namespace VisualStudio2012
{
	// Use the [Publisher] attribute to indicate the class can be used as a Publisher and
	// provide the name used in the XML. (<Publisher class="CustomStream" />)
	[Publisher("CustomStream", true)]
	// Define zero or more parameters with name, type, descriptiom, and optional default value.
	// parameters w/o a default value will be required.
	[Parameter("Param1", typeof(string), "Example of required string parameter.")]
	[Parameter("Param2", typeof(bool), "Example of optional bool parameter.", "true")]
	// Optional description of fixup to display via --showenv
	[Description("Example of a custom stream publisher from the Peach SDK.")]
	// Notice use of StreamPublisher base class. Several base classes are available with Publisher being
	// the top level parent. The StreamPublisher base class is used when a Stream object instance 
	// can be provided.
	public class CustomStreamPublisher : StreamPublisher
	{
		// Create an instance of a logger to be used for debug/trace/errors

		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		// Create properties for any defined parameters
		// they will automatically get populated

		public string Param1 { get; set; }
		public bool Param2 { get; set; }

		public CustomStreamPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
			// Automatically populate properties from parameter args
			ParameterParser.Parse(this, args);

			// TODO - Validate parameters here
		}

		protected override void OnOpen()
		{
			System.Diagnostics.Debug.Assert(stream == null);

			// TODO - Create and open a Stream object instance and place
			//        it into this.stream.
		}

		protected override void OnClose()
		{
			System.Diagnostics.Debug.Assert(stream != null);

			// TODO - Place any custom close logic here

			try
			{
				stream.Close();
			}
			catch (Exception ex)
			{
				logger.Error(ex.Message);
			}

			stream = null;
		}

		protected override void OnOutput(BitwiseStream data)
		{
			// TODO - Place any custom output logic here

			data.CopyTo(stream, BitwiseStream.BlockCopySize);
		}

		// Other methods exist that can be overwritten if needed.
	}
}

// end
