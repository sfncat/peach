using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using NLog;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Core.Publishers;
using Logger = NLog.Logger;
using String = System.String;

namespace Peach.Pro.PitTester
{
	public class TestPublisher : StreamPublisher
	{
		static readonly Logger ClassLogger = LogManager.GetCurrentClassLogger();

		bool _singleIteration;
		bool _datagram;
		readonly TestLogger _logger;
		public delegate void ErrorHandler(string msg);
		public event ErrorHandler Error;

		public TestPublisher(TestLogger logger, bool singleIteration)
			: base(new Dictionary<string, Variant>())
		{
			_logger = logger;
			stream = new MemoryStream();
			_singleIteration = singleIteration;
		}

		private void FireError(string msg)
		{
			if (Error != null)
				Error(msg);
		}

		private void Log(string action)
		{
			if (_singleIteration)
				Console.WriteLine("{0,-15} {1}".Fmt(action, _logger.ActionName));
		}

		protected override Logger Logger
		{
			get { return ClassLogger; }
		}

		protected override void OnStart()
		{
			//testLogger.Verify<TestData.Start>(name);
		}

		protected override void OnStop()
		{
			//testLogger.Verify<TestData.Stop>(name);
		}

		protected override void OnOpen()
		{
			Log("Open");
			_logger.Verify<TestData.Open>(Name);
		}

		protected override void OnClose()
		{
			Log("Close");

			_logger.Verify<TestData.Close>(Name);

			// Don't verify stream positions if previous error occurred
			if (_logger.ExceptionOccurred)
				return;

			if (!_datagram && stream.Position != stream.Length)
			{
				var msg = string.Format(
					"Error, input stream has {0} unconsumed bytes from last input action.",
					stream.Length - stream.Position
				);
				FireError(msg);
			}
		}

		protected override void OnAccept()
		{
			Log("Accept");
			_logger.Verify<TestData.Accept>(Name);
		}

		protected override Variant OnCall(string method, List<ActionParameter> args)
		{
			throw new NotImplementedException();
		}

		protected override Variant OnCall(string method, List<BitwiseStream> args)
		{
			// Handled with the override for output()
			throw new NotSupportedException();
		}

		protected override void OnSetProperty(string property, Variant value)
		{
			Log("SetProperty");
			_logger.Verify<TestData.SetProperty>(Name);
		}

		protected override Variant OnGetProperty(string property)
		{
			Log("GetProperty");
			var data = _logger.Verify<TestData.GetProperty>(Name);
			return new Variant(new BitStream(data.Payload));
		}

		protected override void OnInput()
		{
			Log("Input");

			var data = _logger.Verify<TestData.Input>(Name);

			if (data == null)
				throw new SoftException("No data available, emulating timeout.");

			_datagram = data.IsDatagram;

			if (data.IsDatagram)
			{
				// This is the 'Datagram' publisher behavior
				stream.Seek(0, SeekOrigin.Begin);
				stream.Write(data.Payload, 0, data.Payload.Length);
				stream.SetLength(data.Payload.Length);
				stream.Seek(0, SeekOrigin.Begin);
			}
			else
			{
				if (stream.Position != stream.Length)
				{
					var msg = string.Format(
						"Error, input stream has {0} unconsumed bytes from last input action.",
						stream.Length - stream.Position
					);
					FireError(msg);
				}

				// This is the 'Stream' publisher behavior
				var pos = stream.Position;
				stream.Seek(0, SeekOrigin.End);
				stream.Write(data.Payload, 0, data.Payload.Length);
				stream.Seek(pos, SeekOrigin.Begin);

				// TODO: For stream publishers, defer putting all of the
				// payload into this.stream and use 'WantBytes' to
				// deliver more bytes
			}

			if (Logger.IsDebugEnabled)
				Logger.Debug("\n\n" + Utilities.HexDump(data.Payload, 0, data.Payload.Length));
		}

		protected override void OnOutput(DataModel dataModel)
		{
			Log("Output");
			var data = _logger.Verify<TestData.Output>(Name);

			if (data == null)
				return;

			// Only check outputs on non-fuzzing iterations
			if (!IsControlIteration)
				return;

			// Ensure we end on a byte boundary
			var bs = dataModel.Value.PadBits();
			bs.Seek(0, SeekOrigin.Begin);
			var actual = new BitReader(bs).ReadBytes((int)bs.Length);

			// Determine expected value
			var expected = new byte[] {};
			var dataSet = dataModel.actionData.selectedData as DataFile;
			var cdataAvailable = !(data.Payload == null || data.Payload.Length == 0);
			switch (data.VerifyAgainst) {
				case TestData.ExpectedOutputSource.DataFile:
					if (cdataAvailable && !data.Ignore)
					{
						var msg = string.Format(
							"Unexpected CDATA set for '{0}' output action in pit test when `verifyAgainst='dataFile'`!", 
							data.ActionName);
						FireError(msg);
					}

					if (dataSet != null)
					{
						expected = File.ReadAllBytes(dataSet.FileName);
					}
					else
					{
						var msg = string.Format(
							"No data set available for the '{0}' output action, which was configured to `verifyAgainst='dataFile'`.",
							data.ActionName);
						throw new PeachException(msg);
					}
					break;
				case TestData.ExpectedOutputSource.CData:
					if (!cdataAvailable && !data.Ignore)
					{
						var msg = string.Format(
							"CDATA missing from '{0}' output action in pit test!", 
							data.ActionName);
						FireError(msg);
					}

					expected = data.Payload;
					break;
				default:
					break;
			}

			if (Logger.IsDebugEnabled)
				Logger.Debug("\n\n" + Utilities.HexDump(actual, 0, actual.Length));

			var skipList = new List<Tuple<string, long, long>>();
			foreach (var ignore in _logger.Ignores)
			{
				var act = ignore.Item1;
				var elem = ignore.Item2;

				if (act != dataModel.actionData.action)
					continue;

				var tgt = dataModel.find(elem.fullName);
				if (tgt == null)
				{
					// Can happen when we ignore non-selected choice elements
					Logger.Debug("Couldn't locate {0} in model on action {1} for ignoring.", elem.debugName, _logger.ActionName);
					continue;
				}

				// If we found the data element in the model, we expect to find its position

				long pos;
				var lst = (BitStreamList)dataModel.Value;
				if (!lst.TryGetPosition(elem.fullName, out pos))
					throw new PeachException("Error, Couldn't locate position of {0} in model on action {1} for ignoring.".Fmt(elem,
						_logger.ActionName));

				var skip = new Tuple<string, long, long>(elem.fullName, pos / 8, (pos / 8) + (tgt.Value.LengthBits + 7) / 8);
				Logger.Debug("Ignoring {0} from index {1} to {2}", elem.fullName, skip.Item1, skip.Item2);
				skipList.Add(skip);
			}

			var cb = new ConsoleBuffer();

			var different = data.ValueType == TestData.ValueType.Xml
				? XmlDiff(expected, actual, cb)
				: BinDiff(dataModel, expected, actual, skipList, cb);

			if (different)
			{
				string msg;
				if (data.Ignore)
					msg = "Ignoring action: {0}".Fmt(_logger.ActionName);
				else
					msg = "Test failed on action: {0}".Fmt(_logger.ActionName);

				using (new ForegroundColor(ConsoleColor.Red))
					Console.WriteLine(msg);
				if (cb != null)
					cb.Print();

				if (!data.Ignore)
					FireError(msg);
			}
		}

		protected override void OnOutput(BitwiseStream actualStream)
		{
			// most of the time output(datamodel) should be called instead of this.
			// however, with outfrag this is not possible.

			Log("Output");
			var data = _logger.Verify<TestData.Output>(Name);

			if (data == null)
				return;

			// Only check outputs on non-fuzzing iterations
			if (!IsControlIteration)
				return;

			// Ensure we end on a byte boundary
			var bs = actualStream.PadBits();
			bs.Seek(0, SeekOrigin.Begin);
			var actual = new BitReader(bs).ReadBytes((int)bs.Length);

			var expected = new byte[] { };
			var cdataAvailable = !(data.Payload == null || data.Payload.Length == 0);
			switch (data.VerifyAgainst)
			{
				case TestData.ExpectedOutputSource.DataFile:
					throw new PeachException("Can't verifyAgainst=\"dataFile\" on frag outputs.");
				case TestData.ExpectedOutputSource.CData:
					if (!cdataAvailable && !data.Ignore)
					{
						var msg = string.Format(
							"CDATA missing from '{0}' output action in pit test!",
							data.ActionName);
						FireError(msg);
					}

					expected = data.Payload;
					break;
			}

			if (Logger.IsDebugEnabled)
				Logger.Debug("\n\n" + Utilities.HexDump(actual, 0, actual.Length));


			var cb = new ConsoleBuffer();

			var different = data.ValueType == TestData.ValueType.Xml
				? XmlDiff(expected, actual, cb)
				: BinDiff(new DataModel(), expected, actual, new List<Tuple<string, long, long>>(), cb);

			if (different)
			{
				string msg;
				if (data.Ignore)
					msg = "Ignoring action: {0}".Fmt(_logger.ActionName);
				else
					msg = "Test failed on action: {0}".Fmt(_logger.ActionName);

				using (new ForegroundColor(ConsoleColor.Red))
					Console.WriteLine(msg);
				
				cb.Print();

				if (!data.Ignore)
					FireError(msg);
			}
		}

		static string NormalizeXml(byte[] bytes)
		{
			var ms = new MemoryStream(bytes);
			var rdr = XmlReader.Create(ms);

			var doc = new XmlDocument();
			doc.Load(rdr);

			var sb = new StringBuilder();

			var writer = XmlWriter.Create(sb, new XmlWriterSettings
			{
				Indent = true,
				IndentChars = "    ",
				NewLineChars = "\n",
				Encoding = System.Text.Encoding.UTF8,
				OmitXmlDeclaration = true,
				NewLineHandling = NewLineHandling.Replace
			});

			doc.WriteTo(writer);
			writer.Flush();

			sb.Append('\n');

			return sb.ToString();
		}

		bool XmlDiff(byte[] expected, byte[] actual, ConsoleBuffer cb)
		{
			var expectedText = NormalizeXml(expected);
			var actualText = NormalizeXml(actual);

			var diffBuilder = new InlineDiffBuilder(new Differ());
			var diff = diffBuilder.BuildDiffModel(expectedText, actualText);

			var different = false;

			foreach (var line in diff.Lines)
			{
				switch (line.Type)
				{
					case ChangeType.Inserted:
						cb.Append(ConsoleColor.Red, ConsoleColor.Black, "+ " + line.Text);
						different = true;
						break;
					case ChangeType.Deleted:
						cb.Append(ConsoleColor.Green, ConsoleColor.Black, "- " + line.Text);
						different = true;
						break;
					default:
						cb.Append(ConsoleColor.White, ConsoleColor.Black, "  " + line.Text);
						break;
				}

				cb.Append("\n");
			}

			return different;
		}

		bool BinDiff(DataModel dataModel, byte[] expected, byte[] actual, List<Tuple<string, long,long>> skipList, ConsoleBuffer cb)
		{
			var lst = (BitStreamList)dataModel.Value;

			var ms1 = new MemoryStream(expected);
			var ms2 = new MemoryStream(actual);
			
			const int bytesPerLine = 16;

			var diff = false;
			var bytes1 = new byte[bytesPerLine];
			var bytes2 = new byte[bytesPerLine];

			for (var i = 0;; i += bytesPerLine)
			{
				var readLen1 = ms1.Read(bytes1, 0, bytesPerLine);
				var readLen2 = ms2.Read(bytes2, 0, bytesPerLine);
				if (readLen1 == 0 && readLen2 == 0)
					break;

				var hex1 = new ConsoleBuffer();
				var hex2 = new ConsoleBuffer();

				var ascii1 = new ConsoleBuffer();
				var ascii2 = new ConsoleBuffer();

				var elements = new Dictionary<string, int>();

				var lineDiff = false;
				for (var j = 0; j < bytesPerLine; j++)
				{
					var offset = i + j;
					var skip = skipList.Any(p => p.Item2 <= offset && p.Item3 > offset);
					var bg = skip ? ConsoleColor.Blue : ConsoleColor.Black;

					string elementName;
					if (j < readLen1 && j < readLen2)
					{
						if (bytes1[j] == bytes2[j])
						{
							hex1.Append(ConsoleColor.Gray, bg, "{0:X2}".Fmt(bytes1[j]));
							hex1.Append(" ");
							ascii1.Append(ConsoleColor.Gray, bg, "{0}".Fmt(ByteToAscii(bytes1[j])));

							hex2.Append(ConsoleColor.Gray, bg, "{0:X2}".Fmt(bytes2[j]));
							hex2.Append(" ");
							ascii2.Append(ConsoleColor.Gray, bg, "{0}".Fmt(ByteToAscii(bytes2[j])));
						}
						else
						{
							if (!skip) lineDiff = true;

							hex1.Append(ConsoleColor.Green, bg, "{0:X2}".Fmt(bytes1[j]));
							hex1.Append(" ");
							ascii1.Append(ConsoleColor.Green, bg, "{0}".Fmt(ByteToAscii(bytes1[j])));

							hex2.Append(ConsoleColor.Red, bg, "{0:X2}".Fmt(bytes2[j]));
							hex2.Append(" ");
							ascii2.Append(ConsoleColor.Red, bg, "{0}".Fmt(ByteToAscii(bytes2[j])));

							if (lst.TryGetName(offset * 8, out elementName))
								if (!elements.ContainsKey(elementName))
									elements.Add(elementName, j);
						}
					}
					else if (j < readLen1)
					{
						if (!skip) lineDiff = true;

						hex1.Append(ConsoleColor.Green, bg, "{0:X2}".Fmt(bytes1[j]));
						hex1.Append(" ");
						ascii1.Append(ConsoleColor.Green, bg, "{0}".Fmt(ByteToAscii(bytes1[j])));

						hex2.Append("   ");
						ascii2.Append(" ");

						if (lst.TryGetName(offset * 8, out elementName))
							if (!elements.ContainsKey(elementName))
								elements.Add(elementName, j);
					}
					else if (j < readLen2)
					{
						if (!skip) lineDiff = true;

						hex1.Append("   ");
						ascii1.Append(" ");

						hex2.Append(ConsoleColor.Red, bg, "{0:X2}".Fmt(bytes2[j]));
						hex2.Append(" ");
						ascii2.Append(ConsoleColor.Red, bg, "{0}".Fmt(ByteToAscii(bytes2[j])));

						if (lst.TryGetName(offset * 8, out elementName))
							if (!elements.ContainsKey(elementName))
								elements.Add(elementName, j);
					}
					else
					{
						hex1.Append("   ");
						ascii1.Append(" ");

						hex2.Append("   ");
						ascii2.Append(" ");
					}
				}

				cb.Append("{0:X8}   ".Fmt(i));
				cb.Append(hex1);
				cb.Append("  ");
				cb.Append(ascii1);
				cb.Append(Environment.NewLine);

				if (lineDiff)
				{
					diff = true;

					cb.Append("           ");
					cb.Append(hex2);
					cb.Append("  ");
					cb.Append(ascii2);
					cb.Append(Environment.NewLine);
					foreach (var element in elements)
					{
						cb.Append("           ");
						cb.Append(new String(' ', element.Value * 3));
						cb.Append(element.Key);
						cb.Append(Environment.NewLine);
					}
				}
			}

			return diff;
		}

		char ByteToAscii(byte b)
		{
			return ((b < 32 || b > 126) ? '.' : (char)b);
		}
	}
}
