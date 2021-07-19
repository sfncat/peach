using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Pro.Core.Loggers
{
	[Logger("Visualizer", true)]
	[Obsolete]
	public class VisualizerLogger : Logger
	{
		object mutext = new object();
		string json;
		public static string startUpPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + Path.DirectorySeparatorChar + "PeachView" + Path.DirectorySeparatorChar;

		uint currentIteration = 0;
		uint totalIterations = 0;

		List<ActionData> recentData = new List<ActionData>();
		List<Tuple<string, string>> fuzzedElements = new List<Tuple<string, string>>();
		List<ActionData> dataModelsFromActions = new List<ActionData>();

		public VisualizerLogger(Dictionary<string, Variant> args)
		{
		}

		protected override void DataMutating(RunContext context, ActionData actionData, DataElement element, Mutator mutator)
		{
			lock (mutext)
			{
				fuzzedElements.Add(new Tuple<string, string>(element.fullName, mutator.Name));
			}
		}

		protected override void Engine_IterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
		{
			lock (mutext)
			{
				if (totalIterations != null)
					this.totalIterations = (uint)totalIterations;

				// Remove any data models from last iteration
				dataModelsFromActions.Clear();
				fuzzedElements.Clear();
			}
		}

		/// <summary>
		/// Collection of data models from Action_Finished event.
		/// </summary>
		protected override void ActionFinished(RunContext context, Peach.Core.Dom.Action action)
		{
			// TODO - Handle parameters
			lock (mutext)
			{
				dataModelsFromActions.AddRange(action.allData);
			}
		}

		protected override void Engine_IterationFinished(RunContext context, uint currentIteration)
		{
			lock (mutext)
			{
				recentData = dataModelsFromActions;
				dataModelsFromActions = new List<ActionData>();
				json = null;

				this.currentIteration = currentIteration;
			}
		}

		public string getJson()
		{
			lock (mutext)
			{
				if (json == null)
					json = getJsonData();
				return json;
			}
		}

		static string Base64Encode(BitwiseStream bs)
		{
			bs.Seek(0, SeekOrigin.Begin);
			var writer = new MemoryStream();
			var cs = new CryptoStream(writer, new ToBase64Transform(), CryptoStreamMode.Write);
			bs.CopyTo(cs);
			cs.FlushFinalBlock();
			return System.Text.Encoding.ASCII.GetString(writer.ToArray());
		}

		public string getJsonData()
		{
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				StringWriter stringWriter = new StringWriter(stringBuilder);

				using (JsonWriter jsonWriter = new JsonTextWriter(stringWriter))
				{
					jsonWriter.WriteStartArray();
					jsonWriter.WriteStartObject();
					jsonWriter.WritePropertyName("IterationNumber");
					jsonWriter.WriteValue(Convert.ToString(currentIteration));
					jsonWriter.WritePropertyName("TotalIteration");
					jsonWriter.WriteValue(Convert.ToString(totalIterations));
					jsonWriter.WriteEndObject();

					//Adding all mutated elements to json string
					jsonWriter.WriteStartObject();
					jsonWriter.WritePropertyName("MutatedElements");
					jsonWriter.WriteStartArray();
					foreach (var item in fuzzedElements)
					{
						jsonWriter.WriteValue(item.Item1);
					}
					jsonWriter.WriteEndArray();
					jsonWriter.WriteEndObject();

					jsonWriter.WriteStartObject();
					jsonWriter.WritePropertyName("DataModels");
					jsonWriter.WriteStartArray();
					foreach (var item in recentData)
					{
						// StateModel.dataActions is now the serialized data model
						// in order to properly keep the data around when actions
						// have been re-entered.  This code will need to be updated
						// to hook ActionFinished event and serialize each action
						// to json when it runs.  This way our serialized json is correct
						// when actions have been re-entered.
						//throw new NotImplementedException("Needs fixing!");

						// EDDINGTON
						// Easy fix was to store datamodels during Action_Finished.

						//Add fuzzed data
						jsonWriter.WriteStartObject();
						jsonWriter.WritePropertyName("FuzzedDataModel");
						jsonWriter.WriteValue(Base64Encode(item.dataModel.Value));
						jsonWriter.WriteEndObject();

						//Add original data
						jsonWriter.WriteStartObject();
						jsonWriter.WritePropertyName("OriginalDataModel");
						jsonWriter.WriteValue(Base64Encode(item.originalDataModel.Value));
						jsonWriter.WriteEndObject();

						jsonWriter.WriteStartObject();
						DataModelToJson(item.dataModel.Name, item.dataModel, jsonWriter);
						jsonWriter.WriteEndObject();
					}

					jsonWriter.WriteEndArray();
					jsonWriter.WriteEndObject();
					jsonWriter.WriteEndArray();
				}

				return stringBuilder.ToString();
			}
			catch (Exception e)
			{
				throw new PeachException("Failure writing Peach JSON Model for Visualizer: {0}".Fmt(e.Message), e);
			}
		}

		private void DataModelToJson(string name, DataElementContainer model, JsonWriter writer)
		{
			writer.WritePropertyName("name");
			writer.WriteValue(name);
			writer.WritePropertyName("children");
			writer.WriteStartArray();
			foreach (var item in model)
			{
				writer.WriteStartObject();

				var cont = item as DataElementContainer;
				if (cont != null)
				{
					DataModelToJson(item.Name, cont, writer);
				}
				else
				{
					writer.WritePropertyName("name");
					writer.WriteValue(item.Name);
					writer.WritePropertyName("type");
					writer.WriteValue(item.elementType);

				}

				writer.WriteEndObject();
			}

			writer.WriteEndArray();
		}

		private string StateModelToJson(StateModel model)
		{
			return "StateModel";
		}

		private string AgentToJson(Peach.Core.Dom.Agent agent)
		{
			return "Agent";
		}
	}
}
