using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Linq;
using System.Xml;

namespace Peach.Core.Dom.Actions
{
	[Action("SetProperty")]
	[Serializable]
	public class SetProperty : Action
	{
		/// <summary>
		/// Property to operate on
		/// </summary>
		[XmlAttribute]
		[DefaultValue(null)]
		public string property { get; set; }

		/// <summary>
		/// Data model containing value of property
		/// </summary>
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

			var value = data.dataModel.InternalValue;
			publisher.setProperty(property, value);
		}

		public override void WritePitBody(XmlWriter pit)
		{
			pit.WriteAttributeString("property", property);

			if (allData.Any() && dataModel != null)
			{
				pit.WriteStartElement("DataModel");
				pit.WriteAttributeString("ref", dataModel.Name);
				pit.WriteEndElement();
			}

		}
	}
}
