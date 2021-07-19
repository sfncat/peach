using System.Xml;

namespace Peach.Core
{
	/// <summary>
	/// Implemented by items that can be serialized to a Peach Pit format.
	/// </summary>
	public interface IPitSerializable
	{
		/// <summary>
		/// Converts object into Pit XML
		/// </summary>
		/// <remarks>
		/// Objects that implement this interface must create their full
		/// xml representation. This includes calls to WriteElementString.
		/// </remarks>
		/// <param name="writer"></param>
		void WritePit(XmlWriter writer);
	}
}
