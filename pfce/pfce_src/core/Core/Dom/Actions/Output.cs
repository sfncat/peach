using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Peach.Core.Dom.Actions
{
	[Action("Output")]
	[Serializable]
	public class Output : Action
	{
		public ActionData data { get; set; }

		public override IEnumerable<ActionData> allData
		{
			get
			{
				yield return data;
			}
		}

		public override IEnumerable<ActionData> outputData
		{
			get
			{
				yield return data;
			}
		}

		protected override void OnRun(Publisher publisher, RunContext context)
		{
			publisher.start();
			publisher.open();

			publisher.output(data.dataModel);
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
