using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Peach.Pro.Core.Storage;

namespace Peach.Pro.Core.WebServices.Models
{
	[Flags]
	public enum IterationFlags
	{
		None = 0x0,
		Control = 0x1,
		Record = 0x2,
	}

	[Table("FaultDetail")]
	public class FaultSummary
	{
		/// <summary>
		/// Unique ID for this fault.
		/// </summary>
		[Key]
		public long Id { get; set; }

		/// <summary>
		/// The URL to the FaultDetail for this fault.
		/// </summary>
		/// <example>
		/// "/p/faults/{id}"
		/// </example>
		[NotMapped]
		public string FaultUrl { get; set; }

		/// <summary>
		/// The URL to download a zip archive of the entire fault data
		/// </summary>
		[NotMapped]
		public string ArchiveUrl { get; set; }

		/// <summary>
		/// Was this fault reproducible.
		/// </summary>
		public bool Reproducible { get; set; }

		/// <summary>
		/// The iteration this fault was detected on.
		/// </summary>
		public long Iteration { get; set; }

		/// <summary>
		/// The type of iteration this fault was detected on.
		/// </summary>
		public IterationFlags Flags { get; set; }

		/// <summary>
		/// The time this fault was recorded at.
		/// </summary>
		public DateTime TimeStamp { get; set; }

		/// <summary>
		/// The monitor that generated this fault.
		/// </summary>
		public string Source { get; set; }

		/// <summary>
		/// An exploitablilty rating of this fault.
		/// </summary>
		public string Exploitability { get; set; }

		/// <summary>
		/// The major hash for this fault.
		/// </summary>
		public string MajorHash { get; set; }

		/// <summary>
		/// The minor hash for this fault.
		/// </summary>
		public string MinorHash { get; set; }
	}

	public class FaultDetail : FaultSummary
	{
		/// <summary>
		/// The URL to the node that reported this fault.
		/// </summary>
		/// <example>
		/// "/p/nodes/{id}"
		/// </example>
		[NotMapped]
		public string NodeUrl { get; set; }

		/// <summary>
		/// The URL of the target that this fault was detected against.
		/// </summary>
		/// <example>
		/// "/p/targets/{id}"
		/// </example>
		[NotMapped]
		public string TargetUrl { get; set; }

		/// <summary>
		/// The URL of the target configuration that this fault was detected against.
		/// </summary>
		/// <example>
		/// "/p/targets/{target_id}/config/{config_id}"
		/// </example>
		[NotMapped]
		public string TargetConfigUrl { get; set; }

		/// <summary>
		/// The URL of the specific version of the pit that this fault was detected against.
		/// TODO: Include version in the URL
		/// </summary>
		/// <example>
		/// "/p/pits/{id}"
		/// </example>
		[NotMapped]
		public string PitUrl { get; set; }

		/// <summary>
		/// The URL of the specific version of peach that this fault was detected against.
		/// TODO: Include version in the URL
		/// </summary>
		/// <example>
		/// "/p/peaches/{id}"
		/// </example>
		[NotMapped]
		public string PeachUrl { get; set; }

		/// <summary>
		/// The title of the fault.
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// The description of the fault.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// The seed used by peach when this fault was detected.
		/// </summary>
		public long Seed { get; set; }

		/// <summary>
		/// The start iteration used for reproducing this fault.
		/// </summary>
		public long IterationStart { get; set; }

		/// <summary>
		/// The end iteration used for reproducing this fault.
		/// </summary>
		public long IterationStop { get; set; }

		/// <summary>
		/// The list of files that are part of this fault.
		/// </summary>
		[NotMapped]
		public ICollection<FaultFile> Files { get; set; }

		[JsonIgnore]
		public string FaultPath { get; set; }

		[NotMapped]
		public IEnumerable<FaultMutation> Mutations { get; set; }
	}

	public class FaultFile
	{
		/// <summary>
		/// Unique ID of the file.
		/// </summary>
		[Key]
		public long Id { get; set; }

		/// <summary>
		/// Foreign key to FaultDetail table
		/// </summary>
		[JsonIgnore]
		[ForeignKey(typeof(FaultDetail))]
		public long FaultDetailId { get; set; }

		/// <summary>
		///  The display name of the file.
		/// </summary>
		/// <example>
		/// "description.txt"
		/// </example>
		public string Name { get; set; }

		/// <summary>
		///  The actual name of the file including path from root of fault directory.
		/// </summary>
		/// <example>
		/// "WinAgent.Monitor.WindowsDebugEngine.description.txt"
		/// "Initial\\5\\WinAgent.Monitor.WindowsDebugEngine.description.txt"
		/// </example>
		public string FullName { get; set; }

		/// <summary>
		/// The location to download the contents of the file.
		/// </summary>
		/// <example>
		/// "/p/files/{guid}"
		/// </example>
		[NotMapped]
		public string FileUrl { get; set; }

		/// <summary>
		/// The size of the contents of the file.
		/// </summary>
		/// <example>
		/// 1024
		/// </example>
		public long Size { get; set; }

		/// <summary>
		/// Is the file part of the initial fault or reproduction
		/// </summary>
		public bool Initial { get; set; }

		/// <summary>
		/// What type of file asset is it: Monitor, OutputData, InputData?
		/// </summary>
		public FaultFileType Type { get; set; }

		/// <summary>
		/// Name of agent that file came from.
		/// </summary>
		/// /<remarks>
		/// If grouping information is available then AgentName/MonitorName/MonitorClass are all provided.
		/// If no grouping information is available, the value is null.
		/// </remarks>
		public string AgentName { get; set; }

		/// <summary>
		/// Name of monitor that file came from.
		/// </summary>
		/// /<remarks>
		/// If grouping information is available then AgentName/MonitorName/MonitorClass are all populated.
		/// If no grouping information is available, the value is null.
		/// </remarks>
		public string MonitorName { get; set; }

		/// <summary>
		/// Type of monitor that file came from.
		/// </summary>
		/// /<remarks>
		/// If grouping information is available then AgentName/MonitorName/MonitorClass are all populated.
		/// If no grouping information is available, the value is null.
		/// </remarks>
		public string MonitorClass { get; set; }
	}

	public enum FaultFileType
	{
		Asset,
		Ouput,
		Input
	}

	[Table("ViewFaults")]
	public class FaultMutation
	{
		[JsonIgnore]
		public NameKind Kind { get; set; }

		public long Iteration { get; set; }
		public string State { get; set; }
		public string Action { get; set; }
		public string Element { get; set; }
		public string Mutator { get; set; }
		public string Dataset { get; set; }

		public FaultMutation() { }
		public FaultMutation(
			long iteration,
			string state,
			string action,
			string element,
			string mutator,
			string dataset)
		{
			Iteration = iteration;
			State = state;
			Action = action;
			Element = element;
			Mutator = mutator;
			Dataset = dataset;
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
}
