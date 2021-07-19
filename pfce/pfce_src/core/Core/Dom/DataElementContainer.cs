


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using Peach.Core.IO;
using Peach.Core.Cracker;

namespace Peach.Core.Dom
{
	/// <summary>
	/// Abstract base class for DataElements that contain other
	/// data elements.  Such as Block, Choice, or Flags.
	/// </summary>
	[Serializable]
	public abstract class DataElementContainer : DataElement, IList<DataElement>
	{
		protected List<DataElement> _childrenList = new List<DataElement>();
		protected Dictionary<string, DataElement> _childrenDict = new Dictionary<string, DataElement>();

		public DataElementContainer()
		{
		}

		public DataElementContainer(string name)
			: base(name)
		{
		}

		/// <summary>
		/// Called before the item is going to be added
		/// </summary>
		/// <param name="item"></param>
		protected virtual void OnInsertItem(DataElement item)
		{
			item.parent = this;

			Invalidate();
		}

		/// <summary>
		/// Called after the item has been removed
		/// </summary>
		/// <param name="item"></param>
		/// <param name="cleanup">Remove any bindings that could prevent garbage collection</param>
		protected virtual void OnRemoveItem(DataElement item, bool cleanup = true)
		{
			item.parent = null;

			if (cleanup)
			{
				// Clear any bindings this element has to other elements
				foreach (var elem in item.PreOrderTraverse())
				{
					foreach (var rel in elem.relations.ToArray())
					{
						rel.From.relations.Remove(rel);
						rel.Clear();
					}
				}
			}

			Invalidate();
		}

		/// <summary>
		/// Called before oldItem is going to be replaced with newItem
		/// </summary>
		/// <param name="oldItem"></param>
		/// <param name="newItem"></param>
		protected virtual void OnSetItem(DataElement oldItem, DataElement newItem)
		{
			oldItem.parent = null;

			newItem.UpdateBindings(oldItem);

			newItem.parent = this;

			Invalidate();
		}

		public override void Crack(DataCracker context, BitStream data, long? size)
		{
			BitStream sizedData = ReadSizedData(data, size);
			long startPosition = data.PositionBits;
			var prevCount = Count;

			// Handle children, iterate over a copy since cracking can modify the list
			for (var i = 0; i < Count; )
			{
				var child = this[i];
				context.CrackData(child, sizedData);

				// If we are unsized, cracking a child can cause our size
				// to be available.  If so, update and keep going.
				if (!size.HasValue)
				{
					size = context.GetElementSize(this);

					if (size.HasValue)
					{
						long read = data.PositionBits - startPosition;
						sizedData = ReadSizedData(data, size, read);
					}
				}

				// if Count < prevCount, the child was placed in a different
				// spot in the dom, so don't increment our index

				if (Count == prevCount)
				{
					// if the child's index is previous to our current index
					// the child was placed behind the current position.
					// it will get cracked on a subsequent pass so just
					// increment the index to go to the next element.
					// if the child index is ahead of the index, this means
					// it was placed and not cracked so keep the index the same
					var idx = IndexOf(child);

					System.Diagnostics.Debug.Assert(idx >= 0);

					if (idx <= i)
						++i;
				}
				else if (Count > prevCount)
				{
					// Cracking child caused new elements to be added to
					// this container, so set our next index to be one
					// after the index of the child
					i = IndexOf(child) + 1;
				}

				prevCount = Count;
			}

			if (size.HasValue && sizedData == data)
				data.SeekBits(startPosition + size.Value, System.IO.SeekOrigin.Begin);
		}

		public string UniqueName(string name)
		{
			string ret = name;

			for (int i = 1; _childrenDict.ContainsKey(ret); ++i)
			{
				ret = string.Format("{0}_{1}", name, i);
			}

			return ret;
		}

		public override BitStream  ReadSizedData(BitStream data, long? size, long read = 0)
		{
			if (!size.HasValue)
				return data;

			if (size.Value < read)
			{
				throw new CrackingFailure("Length is {0} bits but already read {1} bits."
					.Fmt(size.Value, read), this, data);
			}

			long needed = size.Value - read;
			data.WantBytes((needed + 7) / 8);
			long remain = data.LengthBits - data.PositionBits;

			if (needed > remain)
			{
				if (read == 0)
					throw new CrackingFailure("Length is {0} bits but buffer only has {1} bits left."
						.Fmt(size.Value, remain), this, data);

				throw new CrackingFailure("Read {0} of {1} bits but buffer only has {2} bits left."
					.Fmt(read, size.Value, remain), this, data);
			}

			// Always return a slice of data.  This way, if data
			// is a stream publisher, it will be presented as having a fixed length.

			var ret = (BitStream)data.SliceBits(needed);
			System.Diagnostics.Debug.Assert(ret != null);

			return ret;
		}

		public override bool CacheValue
		{
			get
			{
				if (!base.CacheValue)
					return false;

				return this.All(elem => elem.CacheValue);
			}
		}

		public override IEnumerable<DataElement> Children(bool forDisplay = false)
		{
			return this;
		}

		/// <summary>
		/// Returns a list of children for use in XPath navigation.
		/// Should not be called directly.
		/// </summary>
		/// <returns></returns>
		public override IList<DataElement> XPathChildren()
		{
			return this;
		}

		protected override DataElement GetChild(string name)
		{
			DataElement ret;
			TryGetValue(name, out ret);
			return ret;
		}

		/// <summary>
		/// Create a pretty string representation of model from here.
		/// </summary>
		/// <returns></returns>
		public string prettyPrint(StringBuilder sb = null, int indent = 0)
		{
			if(sb == null)
				sb = new StringBuilder();

			stringPrintLineWithIndent(sb, Name + ": " + GetType().Name, indent);

			foreach (DataElement child in this)
			{
				if (child is DataElementContainer)
					((DataElementContainer)child).prettyPrint(sb, indent + 1);
				else
					stringPrintLineWithIndent(sb, child.Name + ": " + child.GetType().Name, indent);
			}

			return sb.ToString();
		}

		void stringPrintLineWithIndent(StringBuilder sb, string line, int indent)
		{
			for (int i = 0; i < indent; i++)
				sb.Append(' ');

			sb.Append(line);
			sb.Append("\n");
		}

		/// <summary>
		/// Does container contain child element with name key?
		/// </summary>
		/// <param name="key">Name of child element to check</param>
		/// <param name="value">Gets the element named 'key'.</param>
		/// <returns>Returns true if child exits</returns>
		public virtual bool TryGetValue(string key, out DataElement value)
		{
			return _childrenDict.TryGetValue(key, out value);
		}

		public virtual DataElement this[int index]
		{
			get { return _childrenList[index]; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				if (index >= _childrenList.Count)
					throw new ArgumentOutOfRangeException("index");

				var oldElem = _childrenList[index];

				_childrenDict.Remove(oldElem.Name);
				_childrenList.RemoveAt(index);

				_childrenDict.Add(value.Name, value);
				_childrenList.Insert(index, value);

				OnSetItem(oldElem, value);
			}
		}

		public virtual DataElement this[string key]
		{
			get { return _childrenDict[key]; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				DataElement child;
				if (_childrenDict.TryGetValue(key, out child))
				{
					int index = _childrenList.IndexOf(child);
					this[index] = value;
				}
				else
				{
					if (key != value.Name)
						throw new ArgumentException("Key must be the same as the DataElement name.");

					Add(value);
				}
			}
		}

		public virtual void ApplyReference(DataElement newElem)
		{
			this[newElem.Name] = newElem;
		}

		public void SwapElements(int first, int second)
		{
			if (first >= _childrenList.Count || second >= _childrenList.Count)
				throw new ArgumentException();

			var tmp = _childrenList[first];
			_childrenList[first] = _childrenList[second];
			_childrenList[second] = tmp;

			Invalidate();
		}

		/// <summary>
		/// Takes the element at oldIndex and moves it to newIndex.
		/// </summary>
		/// <param name="oldIndex"></param>
		/// <param name="newIndex"></param>
		public void MoveChild(int oldIndex, int newIndex)
		{
			if (oldIndex >= _childrenList.Count)
				throw new ArgumentOutOfRangeException("oldIndex");
			if (newIndex >= _childrenList.Count)
				throw new ArgumentOutOfRangeException("newIndex");

			var elem = _childrenList[oldIndex];

			_childrenList.RemoveAt(oldIndex);
			_childrenList.Insert(newIndex, elem);

			Invalidate();
		}

		#region IEnumerable<Element> Members

		public IEnumerator<DataElement> GetEnumerator()
		{
			return _childrenList.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region IList<DataElement> Members

		public int IndexOf(DataElement item)
		{
			return _childrenList.IndexOf(item);
		}

		public void Insert(int index, DataElement item)
		{
			// Add throws if key already exists
			_childrenDict.Add(item.Name, item);
			_childrenList.Insert(index, item);

			OnInsertItem(item);
		}

		public void RemoveAt(int index)
		{
			RemoveAt(index, true);
		}

		/// <summary>
		/// Remove child element at specific index
		/// </summary>
		/// <remarks>
		/// Warning, once an item has been removed it is no longer in a useable state
		/// unless cleanup is set to false!  By default bindings that could prevent
		/// garbage collection such as relations are removed.
		/// </remarks>
		/// <param name="index"></param>
		/// <param name="cleanup">Remove any bindings that could prevent garbage collection</param>
		public virtual void RemoveAt(int index, bool cleanup)
		{
			// Index operator throws if index out of range
			var item = _childrenList[index];

			System.Diagnostics.Debug.Assert(item.parent == this);

			bool removed = _childrenDict.Remove(item.Name);
			_childrenList.RemoveAt(index);
			System.Diagnostics.Debug.Assert(removed);

			OnRemoveItem(item, cleanup);
		}

		#endregion

		#region ICollection<DataElement> Members

		public void Add(DataElement item)
		{
			try
			{
				// Add throws if key already exists
				_childrenDict.Add(item.Name, item);
				_childrenList.Add(item);
			}
			catch (System.ArgumentException ex)
			{
				if (ex.Message.Contains("same key"))
				{
					throw new PeachException(string.Format(
						"Error: Detected duplicate child name of '{0}' on element '{1}'.",
						item.Name, this.fullName));
				}

				throw;
			}

			OnInsertItem(item);
		}

		public void Clear()
		{
			_childrenList.Clear();
			_childrenDict.Clear();

			Invalidate();
		}

		public bool Contains(DataElement item)
		{
			return _childrenList.Contains(item);
		}

		public void CopyTo(DataElement[] array, int arrayIndex)
		{
			_childrenList.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return _childrenList.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(DataElement item)
		{
			return Remove(item, true);
		}

		/// <summary>
		/// Remove child element
		/// </summary>
		/// <remarks>
		/// Warning, once an item has been removed it is no longer in a useable state
		/// unless cleanup is set to false!  By default bindings that could prevent
		/// garbage collection such as relations are removed.
		/// </remarks>
		/// <param name="item"></param>
		/// <param name="cleanup">Remove any bindings that could prevent garbage collection</param>
		/// <returns></returns>
		public bool Remove(DataElement item, bool cleanup)
		{
			int index = IndexOf(item);

			if (index >= 0)
			{
				RemoveAt(index, cleanup);
				return true;
			}

			return false;
		}

		#endregion
	}
}

// end
