namespace Peach.Pro.Core.OS.Windows.Publishers.Com
{
	public interface IComContainer
	{
		/// <summary>
		/// Call method on COM control
		/// </summary>
		/// <param name="method">Name of method</param>
		/// <param name="args">Arguments to pass</param>
		/// <returns>Returns result if any</returns>
		object CallMethod(string method, object[] args);

		/// <summary>
		/// Get Property value
		/// </summary>
		/// <param name="property">Name of property</param>
		/// <returns>Returns property value or null.</returns>
		object GetProperty(string property);

		/// <summary>
		/// Set property value
		/// </summary>
		/// <param name="property">Name of property</param>
		/// <param name="value">Value to set</param>
		void SetProperty(string property, object value);
	}
}
