using System.Collections.Generic;
using Newtonsoft.Json;

namespace Peach.Pro.Core.WebServices.Models
{
	public class TestResult
	{
		/// <summary>
		/// The overall status of the test result
		/// </summary>
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
		public TestStatus Status { get; set; }

		/// <summary>
		/// The events that make up the test reslt
		/// </summary>
		public IEnumerable<TestEvent> Events { get; set; }

		/// <summary>
		/// The debug log from the test run
		/// </summary>
		public string Log { get; set; }

		/// <summary>
		/// URL to the debug log from the test run
		/// </summary>
		public string LogUrl { get; set; }
	}
}
