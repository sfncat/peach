//
// Copyright (c) Peach Fuzzer, LLC
//

using System.ComponentModel;
using System.IO;
using NLog;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Pro.Core.Storage;

namespace Peach.Pro.Core.Mutators
{
	[Mutator("SampleNinja")]
	[Description("Will use existing samples to generate mutated files.")]
	public class SampleNinja : Mutator
	{
		static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();

		readonly ElementQuery _element;
		readonly string _dbPath;

		public SampleNinja(DataElement obj)
			: base(obj)
		{
			var pitFile = GetPitFile(obj);

			_dbPath = Path.ChangeExtension(Path.GetFullPath(pitFile), ".ninja");

			using (var db = new SampleNinjaDatabase(_dbPath))
			{
				_element = db.GetElement(NormalizeName(obj));
				if (_element == null)
					throw new PeachException("Error, failed to find element in sample ninja db: [{0}]".Fmt(
						obj.fullName
					));
			}
		}

		public override uint mutation
		{
			get { return Pos; }
			set { Pos = value; }
		}

		public override int count
		{
			get { return _element.Count; }
		}

		public uint Pos { get; set; }

		public new static bool supportedDataElement(DataElement obj)
		{
			if (!obj.isMutable)
				return false;

			var pitFile = GetPitFile(obj);
			if (pitFile == null)
			{
				logger.Trace("no pit file specified in run configuration, disabing mutator.");
				return false;
			}

			var dbPath = Path.ChangeExtension(Path.GetFullPath(pitFile), ".ninja");
			if (!File.Exists(dbPath))
			{
				logger.Trace("ninja database not found, disabling mutator. \"{0}\".", dbPath);
				return false;
			}

			using (var db = new SampleNinjaDatabase(dbPath))
			{
				var element = db.GetElement(NormalizeName(obj));
				if (element != null && element.Count > 0)
					return true;
			
				logger.Trace("Element \"{0}\" not found in ninja db, not enabling.", obj.fullName);
				return false;
			}
		}

		public byte[] GetAt(uint index)
		{
			using (var db = new SampleNinjaDatabase(_dbPath))
			{
				var data = db.GetDataAt(_element, index);
				if (data == null)
					throw new PeachException("SampleNinjaMutator error getting back row. Position: {0} Count: {1}".Fmt(
						Pos,
						count
					));
				return data;
			}
		}

		public override void sequentialMutation(DataElement obj)
		{
			obj.MutatedValue = new Variant(GetAt(Pos));
			obj.mutationFlags = MutateOverride.Default;
			obj.mutationFlags |= MutateOverride.TypeTransform;
		}

		public override void randomMutation(DataElement obj)
		{
			var index = context.Random.Next(count);
			obj.MutatedValue = new Variant(GetAt((uint)index));
			obj.mutationFlags = MutateOverride.Default;
			obj.mutationFlags |= MutateOverride.TypeTransform;
		}

		static string GetPitFile(DataElement elem)
		{
			var root = elem.getRoot() as DataModel;
			if (root == null)
				return null;

			if (root.actionData == null)
				return null;

			var dom = root.actionData.action.parent.parent.parent;
			return dom.context.config.pitFile;
		}

		// For arrays, normalize to wrapper name
		static string NormalizeName(DataElement element)
		{
			if (element.parent is Array)
				return element.parent.Name;
			return element.Name;
		}
	}
}
