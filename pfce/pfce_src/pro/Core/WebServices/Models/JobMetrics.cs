using System;
using System.Linq;
using Newtonsoft.Json;
using Peach.Pro.Core.Storage;

namespace Peach.Pro.Core.WebServices.Models
{
	[Table("ViewFaultTimeline")]
	public class FaultTimelineMetric
	{
		public DateTime Date { get; set; }
		public long FaultCount { get; set; }

		public FaultTimelineMetric() { }
		public FaultTimelineMetric(
			DateTime date,
			long faultCount)
		{
			Date = date;
			FaultCount = faultCount;
		}
	}

	[Table("ViewBucketTimeline")]
	public class BucketTimelineMetric
	{
		public string Label { get; set; }
		public long Iteration { get; set; }
		public DateTime Time { get; set; }
		public long FaultCount { get; set; }

		public BucketTimelineMetric() { }
		public BucketTimelineMetric(
			string label,
			long iteration,
			DateTime time,
			long faultCount)
		{
			Label = label;
			Iteration = iteration;
			Time = time;
			FaultCount = faultCount;
		}
	}

	[Table("ViewMutators")]
	public class MutatorMetric
	{
		public string Mutator { get; set; }
		public long ElementCount { get; set; }
		public long IterationCount { get; set; }
		public long BucketCount { get; set; }
		public long FaultCount { get; set; }

		public MutatorMetric() { }
		public MutatorMetric(
			string mutator,
			long elementCount,
			long iterationCount,
			long bucketCount,
			long faultCount)
		{
			Mutator = mutator;
			ElementCount = elementCount;
			IterationCount = iterationCount;
			BucketCount = bucketCount;
			FaultCount = faultCount;
		}
	}

	[Table("ViewElements")]
	public class ElementMetric
	{
		[JsonIgnore]
		public NameKind Kind { get; set; }

		public string State { get; set; }
		public string Action { get; set; }
		public string Element { get; set; }
		public long IterationCount { get; set; }
		public long BucketCount { get; set; }
		public long FaultCount { get; set; }

		public ElementMetric() { }
		public ElementMetric(
			string state,
			string action,
			string element,
			long iterationCount,
			long bucketCount,
			long faultCount)
		{
			State = state;
			Action = action;
			Element = element;
			IterationCount = iterationCount;
			BucketCount = bucketCount;
			FaultCount = faultCount;
		}

		/// <summary>
		/// Used by reporting.  Is the concatentation of
		/// State, Action, Element with empty strings omitted
		/// </summary>
		[JsonIgnore]
		public string FullElementName
		{
			get
			{
				var ret = string.Join(".", new[]
				{
					State,
					Action,
					Element
				}.Where(s => !string.IsNullOrEmpty(s)));

				return !string.IsNullOrEmpty(ret) ? ret : "Other";
			}
		}
	}

	[Table("ViewStates")]
	public class StateMetric
	{
		[JsonIgnore]
		public NameKind Kind { get; set; }

		public string State { get; set; }
		public long ExecutionCount { get; set; }

		public StateMetric() { }
		public StateMetric(
			string state,
			long executionCount)
		{
			State = state;
			ExecutionCount = executionCount;
		}
	}

	[Table("ViewDatasets")]
	public class DatasetMetric
	{
		[JsonIgnore]
		public NameKind Kind { get; set; }

		public string Dataset { get; set; }
		public long IterationCount { get; set; }
		public long BucketCount { get; set; }
		public long FaultCount { get; set; }

		public DatasetMetric() { }
		public DatasetMetric(
			string dataset,
			long iterationCount,
			long bucketCount,
			long faultCount)
		{
			Dataset = dataset;
			IterationCount = iterationCount;
			BucketCount = bucketCount;
			FaultCount = faultCount;
		}
	}

	[Table("ViewBuckets")]
	public class BucketMetric
	{
		[JsonIgnore]
		public NameKind Kind { get; set; }

		public string Bucket { get; set; }
		public string Mutator { get; set; }
		public string Element { get; set; }
		public long IterationCount { get; set; }
		public long FaultCount { get; set; }

		public BucketMetric() { }
		public BucketMetric(
			string bucket,
			string mutator,
			string element,
			long iterationCount,
			long faultCount)
		{
			Bucket = bucket;
			Mutator = mutator;
			Element = element;
			IterationCount = iterationCount;
			FaultCount = faultCount;
		}

		/// <summary>
		/// Used by reporting.
		/// </summary>
		[JsonIgnore]
		public string FullElementName
		{
			get
			{
				return !string.IsNullOrEmpty(Element) ? Element : "Other";
			}
		}
	}

	[Table("ViewIterations")]
	public class IterationMetric
	{
		[JsonIgnore]
		public NameKind Kind { get; set; }

		public string State { get; set; }
		public string Action { get; set; }
		public string Parameter { get; set; }
		public string Element { get; set; }
		public string Mutator { get; set; }
		public string Dataset { get; set; }
		public long IterationCount { get; set; }

		public IterationMetric() { }
		public IterationMetric(
			string state,
			string action,
			string parameter,
			string element,
			string mutator,
			string dataset,
			long iterationCount)
		{
			State = state;
			Action = action;
			Parameter = parameter;
			Element = element;
			Mutator = mutator;
			Dataset = dataset;
			IterationCount = iterationCount;
		}
	}

	[Table("ViewBucketDetails")]
	public class BucketDetail : FaultDetail
	{
		public long FaultCount { get; set; }
	}
}
