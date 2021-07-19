namespace Peach.Pro.Core.WebServices.Models
{
	public class Error
	{
		/// <summary>
		/// The error message.
		/// </summary>
		public string ErrorMessage { get; set; }

		/// <summary>
		/// Full stacktrace of the error.
		/// </summary>
		public string FullException { get; set; }
	}
}
