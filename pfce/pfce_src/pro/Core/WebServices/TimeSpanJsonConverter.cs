using System;
using Newtonsoft.Json;

namespace Peach.Pro.Core.WebServices
{
	internal class TimeSpanJsonConverter : JsonConverter
	{
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var val = ((TimeSpan)value).Ticks / TimeSpan.TicksPerSecond;
			serializer.Serialize(writer, val);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var val = serializer.Deserialize<long>(reader);
			return TimeSpan.FromTicks(val * TimeSpan.TicksPerSecond);
		}

		public override bool CanConvert(Type objectType)
		{
			return typeof(TimeSpan).IsAssignableFrom(objectType);
		}
	}
}
