


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Xml.Serialization;
using System.Xml;

namespace Peach.Core.Dom
{
	/// <summary>
	/// Hints are attached to data elements providing information
	/// for mutators.
	/// </summary>
	[Serializable]
	[Parameter("Name", typeof(string), "Name of hint")]
	[Parameter("Value", typeof(string), "Value of hint")]
	public class Hint: IPitSerializable
	{
		public Hint(string name, string value)
		{
			Name = name;
			Value = value;
		}

		[XmlAttribute("name")]
		public string Name
		{
			get;
			set;
		}

		[XmlAttribute("value")]
		public string Value
		{
			get;
			set;
		}

		public void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement("Hint");

			pit.WriteAttributeString("name", Name);
			pit.WriteAttributeString("value", Value);

			pit.WriteEndElement();
		}

	}

	/// <summary>
	/// Used to indicate a mutator supports a type of Hint
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class HintAttribute : Attribute
	{
		public string name;
		public string description;

		public HintAttribute(string name, string description)
		{
			this.name = name;
			this.description = description;
		}
	}
}
