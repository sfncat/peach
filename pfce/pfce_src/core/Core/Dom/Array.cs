


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;

using Peach.Core.Analyzers;
using Peach.Core.IO;
using Peach.Core.Cracker;

using NLog;

namespace Peach.Core.Dom
{

	/// <summary>
	/// Array of zero or more DataElements. When a user marks an element with the attributes of
	/// occurs, minOcccurs, or maxOccurs, this element is used.
	/// </summary>
	/// <remarks>
	/// Array elements can be in one of two states, pre and post expansion. Initially an Array
	/// will have a single element called the OrigionalElement. This is the pre-expansion state. Once
	/// data is loaded into the Array, the array will have zero or more copies of OrigionalElement, each
	/// with different data. This is the post-expansion state.
	/// </remarks>
	[Serializable]
	public class Array : Sequence
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// Minimum number of elements this Array can contain
		/// </summary>
		public int minOccurs = 1;
		/// <summary>
		/// Maximum number of elements this Array can contain
		/// </summary>
		public int maxOccurs = 1;
		/// <summary>
		/// Number of occurrence this array should have
		/// </summary>
		public int occurs = 1;

		//private BitwiseStream expandedValue;
		//private int? countOverride;

		public override void SetCountOverride(int count, BitwiseStream value, int valueIndex)
		{
			if (value == null)
				base.SetCountOverride(count, OriginalElement.Value, 0);
			else
				base.SetCountOverride(count, value, valueIndex);
		}

		public override int GetCountOverride()
		{
			// Called from CountRelation to get our size.
			// Ensure we have expanded before checking this.Count
			if (!Expanded)
				ExpandTo(occurs);

			return CountOverride.GetValueOrDefault(Count);
		}

		private DataElement originalElement;

		/// <summary>
		/// The original elements that was marked with the occurs, minOccurs, or maxOccurs
		/// attributes.
		/// </summary>
		public DataElement OriginalElement
		{
			get { return originalElement; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				originalElement = value;
				originalElement.parent = this;
			}
		}

		public Array()
		{
		}

		public Array(string name)
			: base(name)
		{
		}

		public override void WritePit(XmlWriter pit)
		{
			originalElement.WritePit(pit);
		}

		protected override string GetDisplaySuffix(DataElement child)
		{
			return "";
		}

		protected override bool InScope(DataElement child)
		{
			return child != OriginalElement;
		}

		public override IEnumerable<DataElement> Children(bool forDisplay = false)
		{
			// If we have entries, just return them
			if (Expanded)
				return this;

			// If we don't have entries, just return our original element
			if (OriginalElement != null)
				return new DataElement[1] { OriginalElement };

			// Mutation might have removed our original element
			return new DataElement[0];
		}

		/// <summary>
		/// Returns a list of children for use in XPath navigation.
		/// Should not be called directly.
		/// </summary>
		/// <returns></returns>
		public override IList<DataElement> XPathChildren()
		{
			if (OriginalElement == null)
				return this;

			return new[] { OriginalElement }.Concat(this).ToList();
		}

		protected override DataElement GetChild(string name)
		{
			// If we already expanded, just search our children
			if (Expanded)
				return base.GetChild(name);

			// If we haven't expanded, just check our original element
			if (OriginalElement.Name == name)
				return OriginalElement;

			return null;
		}

		public override void Crack(DataCracker context, BitStream data, long? size)
		{
			long startPos = data.PositionBits;
			BitStream sizedData = ReadSizedData(data, size);

			if (OriginalElement == null)
				throw new CrackingFailure("No original element was found.", this, data);

			//Clear();

			// Use remove to undo any relation bindings on array elements
			BeginUpdate();

			while (Count > 0)
				RemoveAt(0);

			EndUpdate();

			// Mark that we have expanded since cracking will create our children
			Expanded = true;

			long min = minOccurs;
			long max = maxOccurs;

			var rel = relations.Of<CountRelation>().Where(context.HasCracked).FirstOrDefault();
			if (rel != null)
				min = max = rel.GetValue();
			else if (minOccurs == 1 && maxOccurs == 1)
				min = max = occurs;

			if (min > maxOccurs && maxOccurs != -1 && min != occurs)
				throw new CrackingFailure("Count of {0} is greater than the maximum of {1}."
					.Fmt(min, maxOccurs), this, data);

			if (min < minOccurs && min != occurs)
				throw new CrackingFailure("Count of {0} is less than the minimum of {1}."
					.Fmt(min, minOccurs), this, data);

			if (context.IsLogEnabled)
			{
				if (min == max)
					context.Log("Occurs: {0}{1}", min, rel == null ? "" : " (Count Relation)");
				else if (min == -1)
					context.Log("Max: {0}", max);
				else if (max == -1)
					context.Log("Min: {0}", min);
				else
					context.Log("Min: {0}, Max: {1}", min, max);
			}

			for (int i = 0; max == -1 || i < max; ++i)
			{
				logger.Trace("Crack: ======================");
				logger.Trace("Crack: {0} Trying #{1}", OriginalElement.debugName, i + 1);

				long pos = sizedData.PositionBits;
				if (pos == sizedData.LengthBits)
				{
					logger.Trace("Crack: Consumed all bytes. {0}", sizedData.Progress);
					break;
				}

				var clone = MakeElement(i);
				Add(clone);

				try
				{
					context.CrackData(clone, sizedData);

					// If we used 0 bytes and met the minimum, we are done
					if (pos == sizedData.PositionBits && i == min)
					{
						RemoveAt(clone.parent.IndexOf(clone));
						break;
					}
				}
				catch (CrackingFailure ex)
				{
					logger.Trace("Crack: {0} Failed on #{1}", debugName, i + 1);

					// If we couldn't satisfy the minimum propigate failure
					if (i < min)
						throw new CrackingFailure("Only cracked {0} of {1} array entries."
							.Fmt(i, min), this, data, ex);

					RemoveAt(clone.parent.IndexOf(clone));
					sizedData.SeekBits(pos, System.IO.SeekOrigin.Begin);
					break;
				}
			}

			if (Count < min)
				throw new CrackingFailure("Only cracked {0} of {1} array entries."
					.Fmt(Count, min), this, data);

			if (size.HasValue && data != sizedData)
				data.SeekBits(startPos + sizedData.PositionBits, System.IO.SeekOrigin.Begin);
		}

		protected override Variant GenerateDefaultValue()
		{
			if (!Expanded)
				ExpandTo(occurs);

			int remain = CountOverride.GetValueOrDefault(Count);

			var stream = new BitStreamList() { Name = fullName };

			for (int i = 0; remain > 0 && i < Count; ++i, --remain)
				stream.Add(this[i].Value);

			if (remain == 0)
				return new Variant(stream);

			// If we are here, it is because of CountOverride being set!
			System.Diagnostics.Debug.Assert(CountOverride.HasValue);
			System.Diagnostics.Debug.Assert(ExpandedValue != null);

			var halves = new Stack<Tuple<long, bool>>();
			halves.Push(null);

			while (remain > 1)
			{
				bool carry = remain % 2 == 1;
				remain /= 2;
				halves.Push(new Tuple<long, bool>(remain, carry));
			}

			var value = ExpandedValue;
			var toAdd = value;

			var item = halves.Pop();

			while (item != null)
			{
				var lst = new BitStreamList();
				lst.Add(toAdd);
				lst.Add(toAdd);
				if (item.Item2)
					lst.Add(value);

				toAdd = lst;
				item = halves.Pop();
			}

			stream.Add(toAdd);

			return new Variant(stream);
		}

		public new static DataElement PitParser(PitParser context, XmlNode node, DataElementContainer parent)
		{
			var array = Generate<Array>(node, parent);

			if (node.hasAttr("minOccurs"))
			{
				array.minOccurs = node.getAttrInt("minOccurs");
				array.maxOccurs = -1;
				array.occurs = array.minOccurs;
			}

			if (node.hasAttr("maxOccurs"))
				array.maxOccurs = node.getAttrInt("maxOccurs");

			if (node.hasAttr("occurs"))
				array.occurs = node.getAttrInt("occurs");

			if (node.hasAttr("mutable"))
				array.isMutable = node.getAttrBool("mutable");

			return array;
		}

		private bool CanShallowCopy()
		{
			if (OriginalElement is Choice)
				return true;

			var block = OriginalElement as Block;

			return block != null && block.Count == 1 && block[0] is Choice;
		}

		private DataElement MakeElement(int index)
		{
			var clone = OriginalElement;

			clone = CanShallowCopy()
				? clone.ShallowClone(this, "{0}_{1}".Fmt(clone.Name, index))
				: clone.Clone("{0}_{1}".Fmt(clone.Name, index));

			return clone;
		}

		[OnCloning]
		private void OnCloning(object context)
		{
			CloneContext ctx = context as CloneContext;

			if (ctx != null)
			{
				// If we are being renamed and our original element has the same name
				// as us, it needs to be renamed as well
				if (ctx.rename.Contains(this) && OriginalElement != null)
					ctx.rename.Add(OriginalElement);
			}
		}

		/// <summary>
		/// Expands the size of the array to be 'count' long.
		/// Does this by adding the same instance of the first
		/// item in the array until the Count is count.
		/// </summary>
		/// <param name="count">The total size the array should be.</param>
		public void ExpandTo(int count)
		{
			System.Diagnostics.Debug.Assert(OriginalElement != null);

			// Once this has been called mark the array as having been expanded
			Expanded = true;

			BeginUpdate();

			for (int i = Count; i < count; ++i)
				Add(MakeElement(i));

			EndUpdate();
		}
	}
}

// end
