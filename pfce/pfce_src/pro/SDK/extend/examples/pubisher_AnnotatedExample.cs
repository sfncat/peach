
//
// Example publisher that supports the following action types:
//
//  - start
//  - stop
//  - open
//  - close
//  - input
//  - output
//
//
// This example is annotated and can be used as a template for creating your own
// custom publisher implementations.
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
using System.IO;
using NLog;
using Peach.Core;
using Peach.Core.IO;
using Logger = NLog.Logger;

namespace MyCustomExtensions
{
	// This name is what you will reference in the Publisher class attribute from the XML
    [Publisher("Example")]
	// Zero or more parameters are supported, both required and options
	// Enum data types are also supported.
    [Parameter("Device", typeof(string), "Example required parameter")]
    [Parameter("DeviceOption", typeof(string), "Example optional parameter", "default")]
    [Parameter("Timeout", typeof(int), "How many milliseconds to wait for data (default 3000)", "3000")]
	public class ExamplePublisher : Publisher
	{
		// Logger instance
		private static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override Logger Logger { get { return logger; } }

		// Parameter properties.  Populated by the base class automatically.
		public string Device { get; set; }
		public string DeviceOption { get; set; }
		public int Timeout { get; set; }

		// Back Stream function defined at end of class
	    private MemoryStream _stream;

		protected ExamplePublisher(Dictionary<string, Variant> args)
			: base(args)
		{
			// Verify publisher parameters here

			if (Device.Length < 5)
			{
				Logger.Error("Error, Invalid parameter 'Device' for Example publisher.  Device name too short.");
				throw new PeachException("Error, Invalid parameter 'Device' for Example publisher.  Device name too short.");
			}
		}

		protected override void OnStart()
		{
			// This method is called once at the start of a fuzzing session.
			// Create any resources that will live across test cases.
		}

		protected override void OnStop()
		{
			// Called once at the end of a fuzzing session
			// Verify all resources allocated in OnStart or during the fuzzing run
			// are closed/freed.
		}

		protected override void OnOpen()
		{
			// Open is also connect.  For network protocols you would typically
			// connect to a remote service here or open the resource to operate on.

			// Called prior to input/output, typically at the start of a test case
			// Method can be called multiple time if the StateModel has two open
			// actions.

			// For this example, allocate our steam on each iteration.
			_stream = new MemoryStream();
		}

		protected override void OnClose()
		{
			// Close resources opened in OnOpen

			_stream.Dispose();
			_stream = null;
		}

		protected override void OnInput()
		{
			// Trigger reading data if your publisher has this concept.
			// For some network protocols data maybe received in a background
			// thread leading this method unused.

			// Example: For UDP you would place the receive call here.

			// Assume buff is our receive buffer
			var buff = Encoding.UTF8.GetBytes("Data read during OnInput\n");

			// If --debug, output a hex dump of received data.
			if (Logger.IsDebugEnabled)
				Logger.Debug("\n\n" + Utilities.HexDump(buff, 0, buff.Length));

			// Add received data to our stream buffer
			_stream.Write(buff, 0, buff.Length);
		}

		protected override void OnOutput(BitwiseStream data)
		{
			// Send data if your publisher has this concept

			// If --debug, output a hex dump of sent data.
			if (Logger.IsDebugEnabled)
				Logger.Debug("\n\n" + Utilities.HexDump(data));

			// Writing data to console as an example
			using (var reader = new StreamReader(data))
			{
				Console.Write(reader.ReadToEnd());
			}
		}

		// Peach treats publishers as Streams when reading data.
		// THe following functions are needed.  Read is called
		// when peach required incoming data.

		// This example will back the stream functions with a MemoryStream object

		#region Read Stream

		public override bool CanRead
		{
			get { return _stream.CanRead; }
		}

		public override bool CanSeek
		{
			get { return _stream.CanSeek; }
		}

		public override long Length
		{
			get { return _stream.Length; }
		}

		public override long Position
		{
			get { return _stream.Position; }
			set { _stream.Position = value; }
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return _stream.Seek(offset, origin);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _stream.Read(buffer, offset, count);
		}

		#endregion
	}
}
