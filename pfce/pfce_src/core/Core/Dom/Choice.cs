


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;

using Peach.Core.Analyzers;
using Peach.Core.IO;
using Peach.Core.Cracker;

using NLog;

namespace Peach.Core.Dom
{
	/// <summary>
	/// Choice allows the selection of a single
	/// data element based on the current data set.
	/// 
	/// The other options in the choice are available
	/// for mutation by the mutators.
	/// </summary>
	[DataElement("Choice")]
	[PitParsable("Choice")]
	[Parameter("name", typeof(string), "Element name", "")]
	[Parameter("fieldId", typeof(string), "Element field ID", "")]
	[Parameter("length", typeof(uint?), "Length in data element", "")]
	[Parameter("lengthType", typeof(LengthType), "Units of the length attribute", "bytes")]
	[Parameter("mutable", typeof(bool), "Is element mutable", "true")]
	[Parameter("constraint", typeof(string), "Scripting expression that evaluates to true or false", "")]
	[Parameter("minOccurs", typeof(int), "Minimum occurrences", "1")]
	[Parameter("maxOccurs", typeof(int), "Maximum occurrences", "1")]
	[Parameter("occurs", typeof(int), "Actual occurrences", "1")]
	[Serializable]
	public class Choice : DataElementContainer
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		public NamedCollection<DataElement> choiceElements = new NamedCollection<DataElement>();
		DataElement _selectedElement = null;
		readonly HashSet<string> maskedElements = new HashSet<string>();

		// Used to squirrel away choiceElements during cloning in order to make shallow copies
		[NonSerialized]
		private NamedCollection<DataElement> tmpChoiceElements;

		public Choice()
		{
		}

		public Choice(string name)
			: base(name)
		{
		}

		#region Choice Token Cache

		/// <summary>
		/// Container for cache entries.
		/// </summary>
		[Serializable]
		class ChoiceCache
		{
			/// <summary>
			/// Offset to Token in bits
			/// </summary>
			public long Offset;

			/// <summary>
			/// Token
			/// </summary>
			public BitwiseStream Token;
		}

		/// <summary>
		/// Cache of tokens for fast choice cracking
		/// </summary>
		Dictionary<string, ChoiceCache> _choiceCache = new Dictionary<string, ChoiceCache>();

		class NoChoiceCacheException : Exception { }

		public IEnumerable<DataElement> EnumerateAllElementsDown(DataElementContainer start)
		{
			foreach (var child in start)
			{
				if (child is Choice || child is Array)
					throw new NoChoiceCacheException();

				if (child is DataElementContainer)
				{
					if (child.isDeterministic)
						throw new NoChoiceCacheException();

					foreach (var cchild in EnumerateAllElementsDown(child as DataElementContainer))
						yield return cchild;
				}
				else
				{
					yield return child;
				}
			}
		}

		/// <summary>
		/// Build cache of tokens to speed up choice cracking
		/// </summary>
		public void BuildCache()
		{
			foreach (var elem in choiceElements)
			{
				try
				{
					var cont = elem as DataElementContainer;
					if (cont != null)
					{
						if (cont.isDeterministic)
							throw new NoChoiceCacheException();

						long offset = 0;
						foreach (var child in EnumerateAllElementsDown(cont))
						{
							if (child.isToken)
							{
								logger.Trace("BuildCache: Adding '{0}' as token offset: {1}", child.fullName, offset);
								_choiceCache[elem.Name] = new ChoiceCache() { Offset = offset, Token = child.Value };
								break;
							}

							else if (child.hasLength)
								offset += child.lengthAsBits;

							else
								throw new NoChoiceCacheException();
						}
					}
					else if (elem.isToken)
						_choiceCache[elem.Name] = new ChoiceCache() { Offset = 0, Token = elem.Value };
				}
				catch (NoChoiceCacheException)
				{
					// we end up here if we run info a choice element
				}
			}
		}

		/// <summary>
		/// Check of cached token is in our data stream.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="tok"></param>
		/// <param name="startPosition"></param>
		/// <returns></returns>
		bool TokenCheck(BitStream data, ChoiceCache tok, long startPosition)
		{
			// Enough data?
			if ((startPosition + tok.Offset + tok.Token.LengthBits) > data.LengthBits)
				return false;

			data.PositionBits = startPosition + tok.Offset;
			tok.Token.PositionBits = 0;

			for (int b = 0; (b = tok.Token.ReadByte()) > -1; )
			{
				var bb = data.ReadByte();
				if (bb != b)
					return false;
			}

			for (int b = 0; (b = tok.Token.ReadBit()) > -1; )
			{
				if (data.ReadBit() != b)
					return false;
			}

			return true;
		}

		#endregion

		public override void Crack(DataCracker context, BitStream data, long? size)
		{
			BitStream sizedData = ReadSizedData(data, size);
			long startPosition = sizedData.PositionBits;
			string isTryAfterFailure = null;

			Clear();
			_selectedElement = null;

			// Try our cache (if any) first.
			foreach (var item in _choiceCache)
			{
				if (TokenCheck(sizedData, item.Value, startPosition))
				{
					var child = choiceElements[item.Key].Clone();

					// Need to update the parent prior to cracking because
					// it could be an array's OriginalElement due to the shallow
					// copies of Arrays.
					child.parent = this;

					try
					{
						logger.Trace("handleChoice: Cache hit for child: {0}", child.debugName);
						context.Log("Cache hit: {0}", child.Name);

						sizedData.SeekBits(startPosition, System.IO.SeekOrigin.Begin);
						context.CrackData(child, sizedData);
						SelectedElement = child;

						logger.Trace("handleChoice: Keeping child: {0}", child.debugName);
						return;
					}
					catch (CrackingFailure)
					{
						// If we fail to crack the cached option, fall back to the slow method. It's possible
						// there are two tokens in a row and the first one is not deterministic.
						logger.Trace("handleChoice: Failed to crack child using cache. Retrying with slow method...: {0}", child.debugName);
						context.Log("Cache failed, falling back to slow method");
						isTryAfterFailure = item.Key;

						break;
					}
					catch (Exception ex)
					{
						logger.Trace("handleChoice: Child threw exception: {0}: {1}", child.debugName, ex.Message);
						throw;
					}
				}
			}

			// Now try it the slow way
			foreach (DataElement item in choiceElements)
			{
				// Skip any cache entries, already tried them
				// Except if our cache choice failed to parse. Then 
				// try all options except the one we already tried.
				if (isTryAfterFailure == null && _choiceCache.ContainsKey(item.Name))
					continue;

				if (isTryAfterFailure == item.Name)
					continue;

				// Create a copy to actually try and crack into
				var child = item.Clone();

				// Need to update the parent prior to cracking because
				// it could be an array's OriginalElement due to the shallow
				// copies of Arrays.
				child.parent = this;

				try
				{

					logger.Trace("handleChoice: Trying child: {0}", child.debugName);

					sizedData.SeekBits(startPosition, System.IO.SeekOrigin.Begin);
					context.CrackData(child, sizedData);
					SelectedElement = child;

					logger.Trace("handleChoice: Keeping child: {0}", child.debugName);
					return;
				}
				catch (CrackingFailure)
				{
					logger.Trace("handleChoice: Failed to crack child: {0}", child.debugName);
				}
				catch (Exception ex)
				{
					logger.Trace("handleChoice: Child threw exception: {0}: {1}", child.debugName, ex.Message);
					throw;
				}
			}

			throw new CrackingFailure("No valid children were found.", this, data);
		}

		public override void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement(elementType);

			if (referenceName != null)
				pit.WriteAttributeString("ref", referenceName);

			WritePitCommonAttributes(pit);
			WritePitCommonChildren(pit);

			foreach (var obj in this)
				obj.WritePit(pit);

			pit.WriteEndElement();
		}

		public void SelectDefault()
		{
			if (choiceElements.Count == 0)
				throw new InvalidOperationException();

			SelectElement(choiceElements[0]);
		}

		public static DataElement PitParser(PitParser context, XmlNode node, DataElementContainer parent)
		{
			if (node.Name != "Choice")
				return null;

			Choice choice = DataElement.Generate<Choice>(node, parent);
			choice.parent = parent;

			context.handleCommonDataElementAttributes(node, choice);
			context.handleCommonDataElementChildren(node, choice);
			context.handleDataElementContainer(node, choice);

			// Move children to choiceElements collection
			foreach (DataElement elem in choice)
			{
				choice.choiceElements.Add(elem);
				elem.parent = choice;
			}

			choice.Clear();
			choice.BuildCache();

			return choice;
		}

		public override void RemoveAt(int index, bool cleanup)
		{
			// Choices only have a single child, the chosen element
			if (index != 0 || Count != 1)
				throw new ArgumentOutOfRangeException("index");

			Debug.Assert(_selectedElement != null);
			_selectedElement = null;

			base.RemoveAt(0, cleanup);

			if (this.Count == 0)
				parent.Remove(this, cleanup);
		}

		public override void ApplyReference(DataElement newElem)
		{
			DataElement oldChoice;
			var idx = choiceElements.Count;

			if (choiceElements.TryGetValue(newElem.Name, out oldChoice))
			{
				idx = choiceElements.IndexOf(oldChoice);
				choiceElements.RemoveAt(idx);
				oldChoice.parent = null;
				newElem.UpdateBindings(oldChoice);
			}

			choiceElements.Insert(idx, newElem);
			newElem.parent = this;
		}

		public void SelectElement(DataElement elem)
		{
			Debug.Assert(choiceElements.ContainsKey(elem.Name));
			Debug.Assert(choiceElements[elem.Name] == elem);

			if (SelectedElement != null && SelectedElement.Name == elem.Name)
				return;

			SelectedElement = elem.Clone();
		}

		public DataElement SelectedElement
		{
			get
			{
				return _selectedElement;
			}
			private set
			{
				// The selected element should be never set to the same object instance
				// that is in choiceElements.  It needs to be a Clone() so we can share
				// the choiceElements collection across array elements.
				Debug.Assert(!choiceElements.Contains(value));

				while (Count > 0)
				{
					// Must use base here, our RemoveAt will
					// remove us from our parent!
					base.RemoveAt(0, true);
				}

				Add(value);
				_selectedElement = value;
				Invalidate();
			}
		}

		internal HashSet<string> MaskedElements { get { return maskedElements; } }

		protected override bool InScope(DataElement child)
		{
			return child == SelectedElement;
		}

		public override IEnumerable<DataElement> Children(bool forDisplay = false)
		{
			if (forDisplay)
			{
				if (maskedElements.Any())
					return choiceElements.Where(e => maskedElements.Contains(e.Name));
				return choiceElements;
			}

			// Return choices if we haven't chosen yet
			if (_selectedElement != null)
				return base.Children();
			return choiceElements;
		}

		/// <summary>
		/// Returns a list of children for use in XPath navigation.
		/// Should not be called directly.
		/// </summary>
		/// <returns></returns>
		public override IList<DataElement> XPathChildren()
		{
			if (SelectedElement == null)
				return choiceElements;

			return new List<DataElement>(new[] { SelectedElement }.Concat(choiceElements));
		}

		protected override DataElement GetChild(string name)
		{
			DataElement ret;
			if (_selectedElement == null)
				choiceElements.TryGetValue(name, out ret);
			else
				TryGetValue(name, out ret);
			return ret;
		}

		protected override Variant GenerateDefaultValue()
		{
			if (SelectedElement == null)
				SelectDefault();

			return new Variant(new BitStreamList(new[] { SelectedElement.Value }));
		}

		[OnCloning]
		private void OnCloning(object context)
		{
			var ctx = context as CloneContext;

			if (ctx == null || !ctx.Shallow)
				return;

			// Squirrel away choiceElements so it doesn't get copied
			Debug.Assert(choiceElements != null);
			Debug.Assert(tmpChoiceElements == null);

			tmpChoiceElements = choiceElements;
			choiceElements = null;
		}

		[OnCloned]
		private void OnCloned(DataElement original, object context)
		{
			var ctx = context as CloneContext;

			if (ctx == null || !ctx.Shallow)
				return;

			var orig = (Choice)original;

			Debug.Assert(orig.choiceElements == null);
			Debug.Assert(orig.tmpChoiceElements != null);
			Debug.Assert(choiceElements == null);
			Debug.Assert(tmpChoiceElements == null);

			// Restore choiceElements so that the clone uses
			// the same instance as the original.  At usage time
			// the individual choice elements will get cloned
			// from the single shared collection

			orig.choiceElements = orig.tmpChoiceElements;
			orig.tmpChoiceElements = null;
			choiceElements = orig.choiceElements;
		}

		public override DataElement this[int index]
		{
			get { return base[index]; }
			set
			{
				var update = IndexOf(SelectedElement) == index;

				base[index] = value;

				if (update)
					_selectedElement = value;
			}
		}
	}
}

// end
