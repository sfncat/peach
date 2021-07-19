
using System;

namespace Peach.Core.Dom
{
	/// <summary>
	/// Flags to define what child elements are supported by
	/// a DataElement.  Flags can be combined or used individually.
	/// </summary>
	/// <remarks>
	/// Used primarily by the schema builder
	/// </remarks>
	[Flags]
	public enum DataElementTypes
	{
		/// <summary>
		/// No child elements
		/// </summary>
		None            = 0x00,
		/// <summary>
		/// Can contain child data elements
		/// </summary>
		DataElements    = 0x01,
		/// <summary>
		/// Can contain Parameter elements
		/// </summary>
		Parameter       = 0x02,
		/// <summary>
		/// Can contain Relation elements
		/// </summary>
		Relation        = 0x04,
		/// <summary>
		/// Can contain Transformer element
		/// </summary>
		Transformer     = 0x08,
		/// <summary>
		/// Can contain Fixup element
		/// </summary>
		Fixup           = 0x10,
		/// <summary>
		/// Can contain Hint elements
		/// </summary>
		Hint            = 0x20,
		/// <summary>
		/// Can contain Analyzer element
		/// </summary>
		Analyzer        = 0x40,
		/// <summary>
		/// Any child element except DataElements
		/// </summary>
		NonDataElements = 0xfe,
		/// <summary>
		/// All child elements
		/// </summary>
		All             = 0xff,
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class DataElementAttribute : PluginAttribute
	{
		public string elementName;
		public DataElementTypes elementTypes;

		public DataElementAttribute(string elementName)
			: base(typeof(DataElement), elementName, true)
		{
			this.elementName = elementName;
			this.elementTypes = DataElementTypes.All;
		}

		public DataElementAttribute(string elementName, DataElementTypes elementTypes)
			: base(typeof(DataElement), elementName, true)
		{
			this.elementName = elementName;
			this.elementTypes = elementTypes;
		}
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class DataElementChildSupportedAttribute : Attribute
	{
		public string elementName;

		public DataElementChildSupportedAttribute(string elementName)
		{
			this.elementName = elementName;
		}
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class DataElementParentSupportedAttribute : Attribute
	{
		public string elementName;

		public DataElementParentSupportedAttribute(string elementName)
		{
			this.elementName = elementName;
		}
	}
}
