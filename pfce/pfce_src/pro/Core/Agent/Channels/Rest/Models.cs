//
// Copyright (c) Peach Fuzzer, LLC
//

using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Peach.Core;

namespace Peach.Pro.Core.Agent.Channels.Rest
{
	public class MonitorRequest
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("class")]
		public string Class { get; set; }

		[JsonProperty("args")]
		public Dictionary<string, string> Args { get; set; }
	}

	internal class ConnectRequest
	{
		[JsonProperty("monitors")]
		public List<MonitorRequest> Monitors { get; set; }
	}

	internal class IterationStartingRequest
	{
		[JsonProperty("isReproduction")]
		public bool IsReproduction { get; set; }

		[JsonProperty("lastWasFault")]
		public bool LastWasFault { get; set; }
	}

	internal class ConnectResponse
	{
		[JsonProperty("url")]
		public string Url { get; set; }

		[JsonProperty("messages")]
		public List<string> Messages { get; set; }
	}

	internal class BoolResponse
	{
		[JsonProperty("value")]
		public bool Value { get; set; }
	}

	internal class FaultResponse
	{
		public class Record
		{
			public class FaultDetail
			{
				[JsonProperty("description")]
				public string Description { get; set; }

				[JsonProperty("majorHash")]
				public string MajorHash { get; set; }

				[JsonProperty("minorHash")]
				public string MinorHash { get; set; }

				[JsonProperty("risk")]
				public string Risk { get; set; }

				[JsonProperty("mustStop")]
				public bool MustStop { get; set; }
			}

			public class FaultData
			{
				[JsonProperty("key")]
				public string Key { get; set; }

				[JsonProperty("size")]
				public long Size { get; set; }

				[JsonProperty("url")]
				public string Url { get; set; }
			}

			[JsonProperty("monitorName")]
			public string MonitorName { get; set; }

			[JsonProperty("detectionSource")]
			public string DetectionSource { get; set; }

			[JsonProperty("title")]
			public string Title { get; set; }

			[JsonProperty("fault")]
			public FaultDetail Fault { get; set; }

			[JsonProperty("data")]
			public List<FaultData> Data { get; set; }
		}

		[JsonProperty("faults")]
		public List<Record> Faults { get; set; }
	}

	internal class ExceptionResponse
	{
		[JsonProperty("message")]
		public string Message { get; set; }

		[JsonProperty("stackTrace")]
		public string StackTrace { get; set; }

		[JsonProperty("fault")]
		public FaultSummary Fault { get; set; }
	}

	internal class PublisherRequest
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("class")]
		public string Class { get; set; }

		[JsonProperty("args")]
		public Dictionary<string, string> Args { get; set; }
	}

	internal class PublisherResponse
	{
		[JsonProperty("url")]
		public string Url { get; set; }
	}

	internal class PublisherOpenRequest
	{
		[JsonProperty("iteration")]
		public uint Iteration { get; set; }

		[JsonProperty("isControlIteration")]
		public bool IsControlIteration { get; set; }

		[JsonProperty("isIterationAfterFault")]
		public bool IsIterationAfterFault { get; set; }

		[JsonProperty("isControlRecordingIteration")]
		public bool IsControlRecordingIteration { get; set; }
	}

	internal abstract class VariantMessage
	{
		public enum ValueType
		{
			[EnumMember(Value = "integer")]
			Integer,
			[EnumMember(Value = "string")]
			String,
			[EnumMember(Value = "bytes")]
			Bytes,
			[EnumMember(Value = "bool")]
			Bool,
			[EnumMember(Value = "double")]
			Double,
		}

		[JsonConverter(typeof(StringEnumConverter))]
		[JsonProperty("type")]
		public ValueType Type { get; set; }

		[JsonProperty("value")]
		public string Value { get; set; }
	}

	internal class SetPropertyRequest : VariantMessage
	{
		[JsonProperty("name")]
		public string Name { get; set; }
	}

	internal class GetPropertyResponse : VariantMessage
	{
	}

	internal class CallRequest
	{
		public class Param
		{
			[JsonProperty("name")]
			public string Name { get; set; }

			[JsonProperty("value")]
			public byte[] Value { get; set; }
		}

		[JsonProperty("method")]
		public string Method { get; set; }

		[JsonProperty("args")]
		public List<Param> Args { get; set; }
	}

	internal class CallResponse : VariantMessage
	{
	}
}
