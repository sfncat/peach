using System;
using System.Diagnostics;
using System.IO;
using Peach.Core.Cracker;
using Peach.Core.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using NLog;

namespace Peach.Core.Dom.Actions
{
	[Action("Input")]
	[Serializable]
	public class Input : Action
	{
		static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();

		protected BitStream _inputData;

		public ActionData data { get; set; }

		public override IEnumerable<ActionData> allData
		{
			get
			{
				yield return data;
			}
		}

		public override IEnumerable<BitwiseStream> inputData
		{
			get
			{
				if (_inputData != null)
					yield return _inputData;
			}
		}

		protected override void OnRun(Publisher pub, RunContext context)
		{
			_inputData = null;

			pub.start();
			pub.open();
			pub.input();

			var startPos = pub.Position;
			var endPos = startPos;

			try
			{
				var cracker = new DataCracker();
				cracker.CrackData(data.dataModel, new BitStream(pub));

				endPos = pub.Position;

				logger.Debug("Final pos: {0} length: {1} crack consumed: {2} bytes", endPos, pub.Length, endPos - startPos);
			}
			catch (CrackingFailure cex)
			{
				endPos = pub.Length;

				var ex = startPos == endPos
					? new TimeoutException("Timed out waiting for input from the publisher.", cex)
					: (Exception)cex;

				throw new SoftException(ex);
			}
			finally
			{
				// If data came from the publisher, save off a copy
				// since other actions can potentially close/stop the
				// publisher before inputData is enumerated.
				// For example, logging with a fault on a control iteration.

				if (startPos < endPos)
				{
					pub.Seek(startPos, SeekOrigin.Begin);

					var src = new BitStream(pub);

					// Create a view from startPos to endPos so we can use CopyTo()
					var slice = src.SliceBits((endPos - startPos) * 8);

					// Create a new input data each time so that if
					// the action runs multiple times we don't overwrite
					// previously collected input
					_inputData = new BitStream
					{
						Name = data.inputName
					};

					slice.CopyTo(_inputData);

					_inputData.Seek(0, SeekOrigin.Begin);

					Debug.Assert(endPos == pub.Position);
				}
			}
		}

		public override void WritePitBody(XmlWriter pit)
		{
			if (allData.Any() && dataModel != null)
			{
				pit.WriteStartElement("DataModel");
				pit.WriteAttributeString("ref", dataModel.Name);
				pit.WriteEndElement();
			}

		}

	}
}
