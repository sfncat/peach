using System.IO;
using System.Net.Http;
using Newtonsoft.Json;
using Peach.Pro.Core;

namespace Peach.Pro.Test.WebApi
{
	static class Extensions
	{
		public static TModel DeserializeJson<TModel>(this HttpResponseMessage resp)
		{
			var serializer = JsonUtilities.CreateSerializer();
			var stream = resp.Content.ReadAsStreamAsync().Result;
			using (var reader = new StreamReader(stream))
			using (var jsonReader = new JsonTextReader(reader))
			{
				return serializer.Deserialize<TModel>(jsonReader);
			}
		}
	}
}
