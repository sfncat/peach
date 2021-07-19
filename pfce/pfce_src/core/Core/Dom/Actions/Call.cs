using System;
using System.IO;
using Peach.Core.IO;
using Peach.Core.Cracker;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Xml;

namespace Peach.Core.Dom.Actions
{
	[Action("Call")]
	[Serializable]
	public class Call : Action
	{
		private BitStream _result;

		public Call()
		{
			parameters = new NamedCollection<ActionParameter>("Param");
		}

		/// <summary>
		/// Method to call
		/// </summary>
		[XmlAttribute]
		[DefaultValue(null)]
		public string method { get; set; }

		/// <summary>
		/// Array of parameters for a method call
		/// </summary>
		[XmlElement("Param")]
		[DefaultValue(null)]
		public NamedCollection<ActionParameter> parameters { get; private set; }

		/// <summary>
		/// Action result for a method call
		/// </summary>
		[XmlElement("Result")]
		[DefaultValue(null)]
		public ActionData result { get; set; }

		public override IEnumerable<ActionData> allData
		{
			get
			{
				foreach (var item in parameters)
					yield return item;

				if (result != null)
					yield return result;
			}
		}

		public override IEnumerable<BitwiseStream> inputData
		{
			get
			{
				// inputData is used for cracking
				// Out and InOut params to a method
				// are data inputs

				foreach (var item in parameters.Where(item => item.type != ActionParameter.Type.In))
				{
					yield return new BitStream(item.dataModel.Value) { Name = item.inputName };
				}

				if (result != null && _result != null)
					yield return _result;
			}
		}

		public override IEnumerable<ActionData> outputData
		{
			get
			{
				// outputData is used for fuzzing
				// In and InOut params to a method
				// are data outputs
				return parameters.Where(p => p.type != ActionParameter.Type.Out);
			}
		}

		protected override void OnRun(Publisher pub, RunContext context)
		{
			_result = null;

			Variant ret = null;

			// Are we sending to Agents?
			if (publisher == "Peach.Agent")
			{
				context.agentManager.Message(method);
			}
			else
			{
				pub.start();
				ret = pub.call(method, parameters.ToList());
			}

			if (result == null || ret == null)
				return;

			try
			{
				_result = (BitStream)ret;
				_result.Name = result.inputName;
			}
			catch (NotSupportedException)
			{
				throw new PeachException("Error, unable to convert result from method '" + method + "' to a BitStream");
			}

			try
			{
				var cracker = new DataCracker();
				cracker.CrackData(result.dataModel, _result);
			}
			catch (CrackingFailure ex)
			{
				throw new SoftException(ex);
			}
			finally
			{
				_result.Seek(0, SeekOrigin.Begin);
			}
		}

		public override void WritePitBody(XmlWriter pit)
		{
			pit.WriteAttributeString("method", method);

			foreach (var param in parameters)
				param.WritePit(pit);

			if (result != null)
			{
				pit.WriteStartElement("Result");
					pit.WriteStartElement("DataModel");
						pit.WriteAttributeString("ref", result.dataModel.Name);
					pit.WriteEndElement();
				pit.WriteEndElement();
			}
		}
	}
}
