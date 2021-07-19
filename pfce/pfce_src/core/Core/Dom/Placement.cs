
using System;
using System.Collections.Generic;
using System.Xml;

namespace Peach.Core.Dom
{
	/// <summary>
	/// Hints are attached to data elements providing information
	/// for mutators.
	/// </summary>
	[Serializable]
	[DataElement("Placement", DataElementTypes.None)]
	[Parameter("after", typeof(string), "Place after this element", "")]
	[Parameter("before", typeof(string), "Place before this element", "")]
	public class Placement: IPitSerializable
	{
		public Placement(Dictionary<string, Variant> args)
		{
			if(args.ContainsKey("after"))
				after = (string)args["after"];
			if (args.ContainsKey("before"))
				before = (string)args["before"];
		}

		public string after
		{
			get;
			set;
		}

		public string before
		{
			get;
			set;
		}

		public void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement("Placement");

			if(after != null)
				pit.WriteAttributeString("after", after);
			if(before != null)
				pit.WriteAttributeString("before", before);

			pit.WriteEndElement();
		}
	}
}
