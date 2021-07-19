using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Linq;
using System.Xml;

namespace Peach.Core.Dom
{
	[Serializable]
	public class ActionParameter : ActionData
	{
		public ActionParameter(string name)
		{
			Name = name;
		}

		/// <summary>
		/// Type of parameter used when calling a method.
		/// 'In' means output the data on call
		/// 'Out' means input the data after the call
		/// 'InOut' means the data is output on call and input afterwards
		/// </summary>
		public enum Type
		{
			[XmlEnum("in")]
			In,
			[XmlEnum("out")]
			Out,
			[XmlEnum("inout")]
			InOut
		};

		/// <summary>
		/// The type of this parameter.
		/// </summary>
		[XmlAttribute]
		[DefaultValue(Type.In)]
		public Type type { get; set; }

		/// <summary>
		/// Currently unused.  Exists for schema generation.
		/// </summary>
		[XmlElement]
		[DefaultValue(null)]
		public List<Xsd.Data> Data
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		/// <summary>
		/// Full input name of this parameter.
		/// 'Out' parameters are input
		/// </summary>
		public override string inputName { get { return base.inputName + ".Out"; } }

		/// <summary>
		/// Full output name of this parameter.
		/// 'In' parameters are input
		/// </summary>
		public override string outputName { get { return base.outputName + ".In"; } }

		public virtual void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement("Param");

			if(!string.IsNullOrEmpty(Name))
				pit.WriteAttributeString("name", Name);

			if(type != Type.In)
				pit.WriteAttributeString("type", type.ToString());

			pit.WriteStartElement("DataModel");
			pit.WriteAttributeString("ref", dataModel.Name);
			pit.WriteEndElement();

			foreach (var data in allData)
				data.WritePit(pit);

			pit.WriteEndElement();
		}
	}
}
