using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using Ionic.Zip;
using Peach.Core;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.IO;
using Stream = Peach.Pro.Core.Dom.Stream;

namespace Peach.Pro.Core.Analyzers
{
	[Analyzer("Zip", true)]
	[Description("Converts Zip data in Blobs into the appropriate streams.")]
	[Parameter("Map", typeof(string), "List of file suffix to data model mappings", "")]
	[Serializable]
	public class ZipAnalyzer : Analyzer
	{
		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		protected Dictionary<string, string> mappings = new Dictionary<string, string>();

		public new static readonly bool supportParser = false;
		public new static readonly bool supportDataElement = true;
		public new static readonly bool supportCommandLine = false;
		public new static readonly bool supportTopLevel = false;

		public ZipAnalyzer()
		{
		}

		public ZipAnalyzer(Dictionary<string, Variant> args)
		{
			if (args != null)
			{
				Variant v;
				if (args.TryGetValue("Map", out v))
				{
					var arg = ((string)v).Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
					foreach (var item in arg)
					{
						var opts = item.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
						if (opts.Length != 2)
							throw new PeachException("TODO");

						mappings.Add(opts[0], opts[1]);
					}
				}
			}
		}

		public override void asDataElement(DataElement parent, Dictionary<DataElement, Position> positions)
		{
			if (parent == null)
				throw new ArgumentNullException("parent");

			var blob = parent as Blob;

			if (blob == null)
				throw new PeachException("Error, Zip analyzer only operates on Blob elements!");

			var data = blob.Value;

			if (data.Length == 0)
				return;

			var root = blob.parent as DataElementContainer;

			var model = parent.getRoot() as DataModel;

			var dom = model.dom;
			if (dom == null)
				dom = model.actionData.action.parent.parent.parent;

			data.Seek(0, SeekOrigin.Begin);

			try
			{
				var block = new Block(blob.Name);

				using (var zip = ZipFile.Read(data))
				{
					foreach (var entry in zip)
					{
						string entryName = entry.FileName;
						var entryData = new BitStream();

						logger.Debug("Attempting to parse: {0}", entryName);

						using (var rdr = entry.OpenReader())
						{
							rdr.CopyTo(entryData);
						}

						entryData.Seek(0, SeekOrigin.Begin);

						var content = GetContentModel(dom, entryName);
						var cracker = new DataCracker();
						cracker.CrackData(content, entryData);

						var elemName = root.UniqueName(block.UniqueName("Stream"));
						var stream = new Stream(elemName);

						stream.DefaultValue = new Variant(entryName);

						stream["Name"].DefaultValue = new Variant(entryName);
						stream["Content"] = content;

						block.Add(stream);

						logger.Debug("Successfully parsed: {0}", entryName);
					}
				}

				root[blob.Name] = block;
			}
			catch (PeachException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new PeachException("Zip analyzer failed.", ex);
			}
		}

		private DataElementContainer GetContentModel(Peach.Core.Dom.Dom dom, string fileName)
		{
			foreach (var item in mappings)
			{
				var re = new Regex(item.Key);
				if (re.IsMatch(fileName))
				{
					var other = dom.getRef<DataModel>(item.Value, d => d.dataModels);
					if (other == null)
						throw new PeachException("Error, Could not resolve ref'd data model.");

					logger.Debug("Resolved entry '{0}' to data model '{1}'.", fileName, other.Name);

					var ret = (DataModel)other.Clone("Content");
					ret.dom = dom;
					return ret;

				}
			}

			var block = new Peach.Core.Dom.Block("Content");
			var blob = new Peach.Core.Dom.Blob("Data");

			block.Add(blob);

			return block;
		}
	}
}
