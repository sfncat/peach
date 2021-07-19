
//
// Example implementing Publisher that uses the BufferedStreamPublisher
// base class.  This is used when the interface being wrapped exposes
// a Stream object.  The base class automatically handles I/O to Stream
// objects.

// Supports the following action types:
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
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.IO.Ports;
using NLog;
using Peach.Core;
using Peach.Core.Publishers;

namespace MyExtensions
{
	// THe following class attributes are used to identify Peach extensions and also 
	// produce the information shown by "peach --showenv".

	// "SerialPortExample" is the name used in the "class" attribute of Publisher XML element
    [Publisher("SerialPortExample")]
	// Zero or more parameters are supported.  Parameters without a default value
	// are considered required.  Those with a default value are optional.
    [Parameter("PortName", typeof(string), "Com interface for the device to connect to")]
    [Parameter("Baudrate", typeof(int), "The serial baud rate.")]
    [Parameter("Parity", typeof(Parity), "The parity-checking protocol.")]
    [Parameter("DataBits", typeof(int), "Standard length of data bits per byte.")]
    [Parameter("StopBits", typeof(StopBits), "The standard number of stopbits per byte.")]
    [Parameter("Handshake", typeof(Handshake), "The handshaking protocol for serial port transmission of data.", "None")]
    [Parameter("DtrEnable", typeof(bool), "Enables the Data Terminal Ready (DTR) signal during serial communication.", "false")]
    [Parameter("RtsEnable", typeof(bool), "Enables the Request To Transmit (RTS) signal during serial communication.", "false")]
    [Parameter("Timeout", typeof(int), "How many milliseconds to wait for data (default 3000)", "3000")]
	// Note we are extending from BUfferedStreamPublisher instead of Publisher.  This
	// base class will provide a stock implementation for Stream objects.
    public class SerialPortPublisher : BufferedStreamPublisher
    {
		// All publishers require a logger for debug/error messages
        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
        protected override NLog.Logger Logger { get { return logger; } }

		// The following properties will automatically be set
		// based on the parameters defined above.
        public string PortName { get; protected set; }
        public int Baudrate { get; protected set; }
        public Parity Parity { get; protected set; }
        public int DataBits { get; protected set; }
        public StopBits StopBits { get; protected set; }
        public Handshake Handshake { get; protected set; }
        public bool DtrEnable { get; protected set; }
        public bool RtsEnable { get; protected set; }

        protected SerialPort _serial;

        public SerialPortPublisher(Dictionary<string, Variant> args)
            : base(args)
        {
        }

		// For Stream publishers, only the OnOpen method is required. THis method
		// is called each test case to open our Stream interface for read/write
		// operations.
        protected override void OnOpen()
        {
			// Always call the base method first.
            base.OnOpen();

            try
            {
				// Create our interface
				_serial = new SerialPort(PortName, Baudrate, Parity, DataBits, StopBits);
                _serial.Handshake = Handshake;
                _serial.DtrEnable = DtrEnable;
                _serial.RtsEnable = RtsEnable;

				// Set timeout values
				_serial.ReadTimeout = (Timeout >= 0 ? Timeout : SerialPort.InfiniteTimeout);
				_serial.WriteTimeout = (Timeout >= 0 ? Timeout : SerialPort.InfiniteTimeout);

                _serial.Open();

				// The following two variables will allow the base class to
				// see our Stream instance.
                _clientName = _serial.PortName;
                _client = _serial.BaseStream;
            }
            catch (Exception ex)
            {
                string msg = "Unable to open Serial Port {0}. {1}.".Fmt(PortName, ex.Message);
                Logger.Error(msg);

				// Always throw SoftExceptions
                throw new SoftException(msg, ex);
            }
           
			// Once we have set _client, call StartClient() on the base class
			// to start reading data.
            StartClient();
        }

		// No other methods are required.
    }
}
