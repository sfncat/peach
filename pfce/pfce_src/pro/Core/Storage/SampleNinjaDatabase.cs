using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Dapper;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Pro.Core.Storage
{
	public class Sample
	{
		[Key]
		public ulong SampleId { get; set; }
		public string File { get; set; }
		public byte[] Hash { get; set; }

		public Sample()
		{
		}

		public Sample(ulong sampleId, string file, byte[] hash)
		{
			SampleId = sampleId;
			File = file;
			Hash = hash;
		}
	}

	public class Element
	{
		[Key]
		public ulong ElementId { get; set; }

		[Unique]
		public string Name { get; set; }
	}

	public class SampleElement
	{
		[Key]
		public ulong SampleElementId { get; set; }
		public ulong SampleId { get; set; }
		public ulong ElementId { get; set; }
		public byte[] Data { get; set; }
	}

	public class ElementQuery
	{
		public int Count { get; set; }
		public ulong ElementId { get; set; }
	}

	public class SampleNinjaDatabase : Database
	{
		static readonly IEnumerable<Type> StaticSchema = new[]
		{
			typeof(Sample),
			typeof(Element),
			typeof(SampleElement),
		};

		public SampleNinjaDatabase(string path)
			: base(path, true)
		{
		}

		protected override IEnumerable<Type> Schema
		{
			get { return StaticSchema; }
		}

		protected override IEnumerable<string> Scripts
		{
			get { return new string[0]; }
		}

		public ElementQuery GetElement(string name)
		{
			return Connection.Query<ElementQuery>(Sql.SelectNinjaElementCount, new { Name = name })
				.SingleOrDefault();
		}

		public byte[] GetDataAt(ElementQuery element, uint offset)
		{
			var args = new
			{
				element.ElementId,
				Offset = offset,
			};
			return Connection.ExecuteScalar(Sql.SelectNinjaData, args) as byte[];
		}

		public static string Create(string pitLibraryPath, string pitFile, string dataModelRef, string samplesPath)
		{
			if (!File.Exists(pitFile))
				throw new FileNotFoundException("Unable to find pit file: {0}".Fmt(pitFile));

			var defs = PitDefines.ParseFile(pitFile + ".config", pitLibraryPath);
			var defsWithDefaults = defs.Evaluate().Select(PitDefines.PopulateRequiredDefine);

			var parser = new PitParser();
			var args = new Dictionary<string, object> {
				{ PitParser.DEFINED_VALUES, defsWithDefaults }
			};

			var dom = parser.asParser(args, pitFile);

			var dataModel = dom.getRef(dataModelRef, x => x.dataModels);
			if (dataModel == null)
				PitCompiler.RaiseMissingDataModel(dom, dataModelRef, pitFile);

			var dbPath = System.IO.Path.ChangeExtension(pitFile, ".ninja");
			using (var db = new SampleNinjaDatabase(dbPath))
			{
				if (samplesPath.Contains("*"))
				{
					var dir = System.IO.Path.GetDirectoryName(samplesPath);
					foreach (var file in Directory.GetFiles(dir, System.IO.Path.GetFileName(samplesPath)))
					{
						var path = System.IO.Path.Combine(dir, System.IO.Path.GetFileName(file));
						db.ProcessSample(dataModel, path);
					}
				}
				else if (Directory.Exists(samplesPath))
				{
					foreach (var file in Directory.EnumerateFiles(samplesPath))
					{
						var path = System.IO.Path.Combine(samplesPath, System.IO.Path.GetFileName(file));
						db.ProcessSample(dataModel, path);
					}
				}
				else if (File.Exists(samplesPath))
				{
					db.ProcessSample(dataModel, samplesPath);
				}
				else
				{
					throw new FileNotFoundException("Invalid samples path: {0}".Fmt(samplesPath));
				}
			}

			return dbPath;
		}

		public void ProcessSample(DataModel dataModel, string path)
		{
			using (var stream = File.OpenRead(path))
			{
				var hash = Hash(stream);

				var sample = GetSample(path);
				if (sample != null)
				{
					if (!hash.SequenceEqual(sample.Hash))
					{
						Console.WriteLine("Updating: " + path);
						DeleteSample(sample);
					}
					else
					{
						Console.WriteLine("Skipping: {0}", path);
						return;
					}
				}
				else
					Console.WriteLine("Processing: {0}", path);

				try
				{
					stream.Seek(0, SeekOrigin.Begin);
					var data = new BitStream(stream);
					var cracker = new DataCracker();
					var crackedModel = ObjectCopier.Clone(dataModel);

					// Update the DOM so scripting can execute
					crackedModel.dom = dataModel.dom;

					cracker.CrackData(crackedModel, data);

					sample = new Sample { File = path, Hash = hash };
					InsertSample(sample);

					ProcessElement(sample, crackedModel);
				}
				catch (CrackingFailure ex)
				{
					Console.WriteLine("Error cracking \"{0}\".", ex.element.fullName);
				}
			}
		}

		void ProcessElement(Sample sample, DataElement dataElement)
		{
			var name = dataElement.Name;
			if (dataElement.parent is Peach.Core.Dom.Array)
				name = dataElement.parent.Name;

			var element = new Element { Name = name };
			InsertElement(element);

			InsertSampleElement(new SampleElement
			{
				SampleId = sample.SampleId,
				ElementId = element.ElementId,
				Data = ToByteArray(dataElement.Value)
			});

			var container = dataElement as DataElementContainer;
			if (container != null)
			{
				foreach (var child in container)
				{
					ProcessElement(sample, child);
				}
			}
		}

		void InsertSample(Sample sample)
		{
			sample.SampleId = Connection.ExecuteScalar<ulong>(Sql.InsertNinjaSample, sample);
		}

		void InsertElement(Element element)
		{
			var elementId = Connection.ExecuteScalar<ulong?>(Sql.SelectNinjaElement, new { element.Name });
			if (elementId.HasValue)
				element.ElementId = elementId.Value;
			else
				element.ElementId = Connection.ExecuteScalar<ulong>(Sql.InsertNinjaElement, element);
		}

		void InsertSampleElement(SampleElement se)
		{
			se.SampleElementId = Connection.ExecuteScalar<ulong>(Sql.InsertNinjaSampleElement, se);
		}

		public Sample GetSample(string fileName)
		{
			return Connection.Query<Sample>(Sql.SelectNinjaSample, new { File = fileName })
				.SingleOrDefault();
		}

		void DeleteSample(Sample sample)
		{
			Connection.Execute(Sql.DeleteNinjaSample, sample);
		}

		byte[] Hash(Stream stream)
		{
			using (var sha1 = new SHA1Managed())
			{
				return sha1.ComputeHash(stream);
			}
		}

		byte[] ToByteArray(BitwiseStream data)
		{
			var ms = new MemoryStream();
			data.Seek(0, SeekOrigin.Begin);
			data.CopyTo(ms);
			return ms.ToArray();
		}
	}
}
