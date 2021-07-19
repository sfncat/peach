using System;
using System.Collections.Generic;
using Peach.Core.Agent;

namespace Peach.Core
{
	/// <summary>
	/// Type of fault
	/// </summary>
	public enum FaultType
	{
		Unknown,
		// Actual fault
		Fault,
		// Data collection
		Data
	}

	public class FaultSummary
	{
		/// <summary>
		/// Compute the hash of a value for use as either
		/// the MajorHash or MinorHash.
		/// </summary>
		/// <param name="value">String value to hash</param>
		/// <returns>The first 4 bytes of the md5 has as a hex string</returns>
		public static string Hash(string value)
		{
			return Monitor2.Hash(value);
		}

		/// <summary>
		/// One line title of fault
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// Description field of fault
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Major hash of fault.
		/// </summary>
		public string MajorHash { get; set; }

		/// <summary>
		/// Minor hash of fault
		/// </summary>
		public string MinorHash { get; set; }

		/// <summary>
		/// Exploitability of fault
		/// </summary>
		public string Exploitablity { get; set; }

		/// <summary>
		/// Detection source for fault, typically the class name
		/// </summary>
		/// For the Rest publisher the detection name is the name attribute while
		/// detection source is the Publisher attribute.
		public string DetectionSource { get; set; }

		/// <summary>
		/// Name of detection source
		/// </summary>
		/// <remarks>
		/// For the Rest publisher the detection name is the name attribute while
		/// detection source is the Publisher attribute.
		/// </remarks>
		public string DetectionName { get; set; }

		/// <summary>
		/// Name of agent fault was reported by.
		/// </summary>
		/// <remarks>
		/// Only used when fault generated via agent, otherwise null.
		/// </remarks>
		public string AgentName { get; set; }
	}

	/// <summary>
	/// Fault detected during fuzzing run
	/// </summary>
	[Serializable]
	public class Fault
	{
		/// <summary>
		/// Data contained in fault.
		/// </summary>
		[Serializable]
		public class Data
		{
			public Data()
			{
			}

			public Data(string key, byte[] value)
			{
				Key = key;
				Value = value;
			}

			public string Key { get; set; }
			public byte[] Value { get; set; }

			/// <summary>
			/// Set by FileLogger with the location on disk
			/// of this file.
			/// </summary>
			public string Path { get; set; }
		}

		[Serializable]
		public class Mutation
		{
			public string element { get; set; }
			public string mutator { get; set; }
		}

		[Serializable]
		public class Model
		{
			public string dataSet { get; set; }
			public string parameter { get; set; }
			public string name { get; set; }
			public List<Mutation> mutations { get; set; }
		}

		[Serializable]
		public class Action
		{
			public string name;
			public string type;
			public List<Model> models { get; set; }
		}

		[Serializable]
		public class State
		{
			public string name { get; set; }
			public List<Action> actions { get; set; }
		}

		public bool mustStop = false;

		/// <summary>
		/// Iteration fault was detected on
		/// </summary>
		public uint iteration = 0;

		/// <summary>
		/// Start iteration of search when fault was detected
		/// </summary>
		public uint iterationStart = 0;

		/// <summary>
		/// End iteration of search when fault was detected
		/// </summary>
		public uint iterationStop = 0;

		/// <summary>
		/// Is this a control iteration.
		/// </summary>
		public bool controlIteration = false;

		/// <summary>
		/// Is this control operation also a recording iteration?
		/// </summary>
		public bool controlRecordingIteration = false;

		/// <summary>
		/// Type of fault
		/// </summary>
		public FaultType type = FaultType.Unknown;

		/// <summary>
		/// Who detected this fault?
		/// </summary>
		/// <remarks>
		/// Example: "PageHeap Monitor"
		/// Example: "Name (PageHeap Monitor)"
		/// </remarks>
		public string detectionSource = null;

		/// <summary>
		/// Name of monitor instance that created this fault
		/// </summary>
		/// <remarks>
		/// Set by the agent
		/// </remarks>
		public string monitorName = null;

		/// <summary>
		/// Agent this fault came from
		/// </summary>
		/// <remarks>
		/// Set by the AgentManager
		/// </remarks>
		public string agentName = null;

		/// <summary>
		/// Title of finding
		/// </summary>
		public string title = null;

		/// <summary>
		/// Multiline description and collection of information.
		/// </summary>
		public string description = null;

		/// <summary>
		/// Major hash of fault used for bucketting.
		/// </summary>
		public string majorHash = null;

		/// <summary>
		/// Minor hash of fault used for bucketting.
		/// </summary>
		public string minorHash = null;

		/// <summary>
		/// Exploitability of fault, used for bucketting.
		/// </summary>
		public string exploitability = null;

		/// <summary>
		/// Folder for fault to be collected under.  Only used when
		/// major/minor hashes and exploitability are not defined.
		/// </summary>
		public string folderName = null;

		/// <summary>
		/// Binary data collected about fault.  Key is filename, value is content.
		/// </summary>
		public List<Data> collectedData = new List<Data>();
		// Note: We can't use a Dictionary<> since it won't remote between mono and .net correctly

		/// <summary>
		/// List of all states run when fault was detected.
		/// </summary>
		public ICollection<State> states = new List<State>();
	}
}
