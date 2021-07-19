using System;
using Peach.Pro.Core.Storage;
using Newtonsoft.Json;

namespace Peach.Pro.Core.WebServices.Models
{
	public enum TestStatus
	{
		Active,
		Pass,
		Fail
	}

	public class TestEvent
	{
		public TestEvent() { }

		public TestEvent(
			long id, 
			Guid jobId,
			TestStatus status, 
			string short_, 
			string description, 
			string resolve)
		{
			Id = id;
			JobId = jobId.ToString();
			Status = status;
			Short = short_;
			Description = description;
			Resolve = resolve;
		}

		/// <summary>
		/// Identifier of event
		/// </summary>
		[Key]
		public long Id { get; set; }

		[Required]
		[ForeignKey(typeof(Job))]
		public string JobId { get; set; }

		/// <summary>
		/// Status of event
		/// </summary>
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
		public TestStatus Status { get; set; }

		/// <summary>
		/// Short description of event
		/// </summary>
		public string Short { get; set; }

		/// <summary>
		/// Long description of event
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// How to resolve the event if it is an issue
		/// </summary>
		public string Resolve { get; set; }

		public override string ToString()
		{
			return string.Format("{0}: {1}", Status, Short);
		}
	}
}
