


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Peach.Core.Dom;
using Peach.Core.IO;

using NLog;
using Array = Peach.Core.Dom.Array;

namespace Peach.Core.Cracker
{
	#region Event Delegates

	public delegate void EnterHandleNodeEventHandler(DataElement element, long position, BitStream data);
	public delegate void ExitHandleNodeEventHandler(DataElement element, long position, BitStream data);
	public delegate void ExceptionHandleNodeEventHandler(DataElement element, long position, BitStream data, Exception e);
	public delegate void PlacementEventHandler(DataElement oldElement, DataElement newElement, DataElementContainer oldParent);
	public delegate void AnalyzerEventHandler(DataElement element, BitStream data);

	#endregion

	/// <summary>
	/// Class for tracking the positions of elements.
	/// </summary>
	public class Position
	{
		public long begin { get; set; }
		public long end { get; set; }

		public Position()
		{
		}

		public Position(long begin, long end)
		{
			this.begin = begin;
			this.end = end;
		}

		public override string ToString()
		{
			return "Begin: {0}, End: {1}".Fmt(begin, end);
		}
	}

	/// <summary>
	/// Crack data into a DataModel.
	/// </summary>
	public class DataCracker
	{
		#region Private Members

		static readonly NLog.Logger Logger = LogManager.GetLogger("DataCracker");

		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		#region Position Class

		/// <summary>
		/// Helper class for tracking positions of cracked elements using
		/// optionally available sizing information.
		/// </summary>
		class SizedPosition : Position
		{
			public long? size { get; set; }

			public override string ToString()
			{
				return "Begin: {0}, Size: {1}, End: {2}".Fmt(
					begin,
					size.HasValue ? size.Value.ToString(CultureInfo.InvariantCulture) : "<null>",
					end);
			}
		}

		#endregion

		/// <summary>
		/// Collection of all elements that have been cracked so far.
		/// </summary>
		Dictionary<DataElement, SizedPosition> _sizedElements;

		/// <summary>
		/// Stack of all BitStream objects passed to CrackData().
		/// This is used for determining absolute locations from relative offsets.
		/// </summary>
		List<BitStream> _dataStack = new List<BitStream>();

		/// <summary>
		/// Elements that have analyzers attached.  We run them all post-crack.
		/// </summary>
		List<DataElement> _elementsWithAnalyzer;

		/// <summary>
		/// Placements to run after current cracking phase cracking complete
		/// </summary>
		List<DataElement> _deferredPlacement;

		/// <summary>
		/// Absolute placements based on offset relations
		/// </summary>
		SortedDictionary<long, DataElement> _absolutePlacement;

		/// <summary>
		/// The element we are cracking from
		/// </summary>
		DataElement _root;

		/// <summary>
		/// The string to prefix log messages with.
		/// </summary>
		readonly StringBuilder _logPrefix;

		/// <summary>
		/// The last error we logged
		/// </summary>
		Exception _lastError;

		#endregion

		#region Events

		public event EnterHandleNodeEventHandler EnterHandleNodeEvent;
		protected void OnEnterHandleNodeEvent(DataElement element, long position, BitStream data)
		{
			if(EnterHandleNodeEvent != null)
				EnterHandleNodeEvent(element, position, data);
		}
		
		public event ExitHandleNodeEventHandler ExitHandleNodeEvent;
		protected void OnExitHandleNodeEvent(DataElement element, long position, BitStream data)
		{
			if (ExitHandleNodeEvent != null)
				ExitHandleNodeEvent(element, position, data);
		}

		public event ExceptionHandleNodeEventHandler ExceptionHandleNodeEvent;
		protected void OnExceptionHandleNodeEvent(DataElement element, long position, BitStream data, Exception e)
		{
			if (ExceptionHandleNodeEvent != null)
				ExceptionHandleNodeEvent(element, position, data, e);
		}

		public event PlacementEventHandler PlacementEvent;
		protected void OnPlacementEvent(DataElement oldElement, DataElement newElement, DataElementContainer oldParent)
		{
			if (PlacementEvent != null)
				PlacementEvent(oldElement, newElement, oldParent);
		}

		public event AnalyzerEventHandler AnalyzerEvent;
		protected void OnAnalyzerEvent(DataElement element, BitStream data)
		{
			if (AnalyzerEvent != null)
				AnalyzerEvent(element, data);
		}

		#endregion

		#region Public Methods

		private DataCracker(string logPrefix)
		{
			_logPrefix = new StringBuilder(logPrefix);
		}

		/// <summary>
		/// Constructs a DataCracker
		/// </summary>
		public DataCracker()
		{
			_logPrefix = new StringBuilder();
		}

		/// <summary>
		/// Returns a new DataCracker with the log prefix maintained.
		/// </summary>
		/// <returns></returns>
		public DataCracker Clone()
		{
			return new DataCracker(_logPrefix.ToString());
		}

		/// <summary>
		/// Main entry method that will take a data stream and parse it into a data model.
		/// </summary>
		/// <remarks>
		/// Method will throw one of two exceptions on an error: CrackingFailure, or NotEnoughDataException.
		/// </remarks>
		/// <param name="element">DataElement to import data into</param>
		/// <param name="data">Data stream to read data from</param>
		public void CrackData(DataElement element, BitStream data)
		{
			if (element == null)
				throw new ArgumentNullException("element");
			if (data == null)
				throw new ArgumentNullException("data");

			try
			{
				_dataStack.Insert(0, data);

				if (_dataStack.Count == 1)
					handleRoot(element, data);
				else if (element.placement != null)
					handlePlacelemt(element, data);
				else
					handleNode(element, data);
			}
			finally
			{
				_dataStack.RemoveAt(0);
			}

		}

		public Position GetElementPos(DataElement elem)
		{
			SizedPosition ret;
			_sizedElements.TryGetValue(elem, out ret);
			return ret;
		}

		/// <summary>
		/// Get the size of an element that has already been cracked.
		/// The size only has a value if the element has a length attribute
		/// or the element has a size relation that has successfully resolved.
		/// </summary>
		/// <param name="elem">Element to query</param>
		/// <returns>size of the element</returns>
		public long? GetElementSize(DataElement elem)
		{
			return _sizedElements[elem].size;
		}

		/// <summary>
		/// Determines if the From half of a relation has been cracked.
		/// </summary>
		/// <param name="rel">The Relation to test.</param>
		/// <returns>True if the From half has been cracked, false otherwise.</returns>
		public bool HasCracked(Relation rel)
		{
			return _sizedElements.ContainsKey(rel.From);
		}

		/// <summary>
		/// Perform optimizations of data model for cracking
		/// </summary>
		/// <remarks>
		/// Optimization can be performed once on a data model and used
		/// for any clones made.  Optimizations will increase the speed
		/// of data cracking.
		/// </remarks>
		/// <param name="model">DataModel to optimize</param>
		public void OptimizeDataModel(DataModel model)
		{
			foreach (var element in model.EnumerateElementsUpTree())
			{
				if (element is Choice)
				{
					// TODO - Fast CACHE IT!
				}
			}
		}

		/// <summary>
		/// Elements can use this to log messages whe nthey are being cracked.
		/// </summary>
		/// <param name="msg"></param>
		/// <param name="fmt"></param>
		public void Log(string msg, params object[] fmt)
		{
			if (Logger.IsDebugEnabled)
				Logger.Debug("{0} {1}", _logPrefix, string.Format(msg, fmt));
		}

		/// <summary>
		/// Returns true if the logger is enabled.
		/// </summary>
		/// <returns></returns>
		public bool IsLogEnabled
		{
			get
			{
				return Logger.IsDebugEnabled;
			}
		}

		#endregion

		#region Private Helpers

		long getDataOffset()
		{
			long offset = 0;

			for (int i = _dataStack.Count - 2, prev = i + 1; i >= 0; --i)
			{
				if (_dataStack[i] != _dataStack[prev])
				{
					offset += _dataStack[prev].PositionBits - _dataStack[i].LengthBits;
					prev = i;
				}
			}

			System.Diagnostics.Debug.Assert(offset >= 0);
			return offset;
		}

		void addElements(DataElement de, BitStream data, Dictionary<DataElement, Position> positions, long offset)
		{
			Position pos;
			if (!positions.TryGetValue(de, out pos))
				pos = new Position() { begin = -offset, end = -offset };

			OnEnterHandleNodeEvent(de, offset + pos.begin, data);

			var cont = de as DataElementContainer;
			if (cont != null)
			{
				foreach (var child in cont)
					addElements(child, data, positions, offset);
			}

			OnExitHandleNodeEvent(de, offset + pos.end, data);
		}

		#endregion

		#region Handlers

		internal class ElementComparer : IComparer<DataElement>
		{
			public static DataElement FollowingElement(DataElement elem)
			{
				for (var curr = elem; curr != null; curr = curr.parent)
				{
					var next = curr.nextSibling();
					if (next != null)
						return next;
				}

				return null;
			}

			public static DataElement PreceedingElement(DataElement elem)
			{
				for (var curr = elem; curr != null; curr = curr.parent)
				{
					var prev = curr.previousSibling();
					if (prev != null)
						return prev;
				}

				return null;
			}


			public static int CompareTo(DataElement lhs, DataElement rhs)
			{
				if (lhs == rhs)
					return 0;

				var parents = new List<DataElement>();

				for (var p = lhs; p != null; p = p.parent)
					parents.Add(p);

				// rhs is a parent of lhs
				if (parents.Contains(rhs))
					return 1;

				for (var e = rhs; e != null; e = e.parent)
				{
					var idx = parents.IndexOf(e.parent);

					// no common parent
					if (idx == -1)
						continue;

					// lhs is a child of rhs
					if (idx == 0)
						return -1;

					// Found common parent, so compare the siblings
					var sibling = parents[idx - 1];

					Debug.Assert(sibling.parent == e.parent);

					if (e.parent.IndexOf(sibling) < e.parent.IndexOf(e))
						return -1;

					return 1;
				}

				throw new ArgumentException("No common parent could be found.");
			}

			public int Compare(DataElement lhs, DataElement rhs)
			{
				return CompareTo(lhs, rhs);
			}
		}

		#region Top Level Handlers

		void handleRoot(DataElement element, BitStream data)
		{
			_sizedElements = new Dictionary<DataElement, SizedPosition>();
			_elementsWithAnalyzer = new List<DataElement>();
			_deferredPlacement = new List<DataElement>();
			_absolutePlacement = new SortedDictionary<long, DataElement>();
			_root = element;

			// We want at least 1 byte before we begin
			data.WantBytes(1);

			// Crack the model
			handleNode(element, data);

			while (_absolutePlacement.Count > 0 || _deferredPlacement.Count > 0)
			{
				// Handle absolute placements first
				while (_absolutePlacement.Count > 0)
				{
					var first = _absolutePlacement.First();

					var elem = element
						.PreOrderTraverse(e => !_absolutePlacement.ContainsValue(e))
						.Skip(1) // Skip root
						.SkipWhile(e => _sizedElements[e].end < first.Key)
						.FirstOrDefault();

					if (elem == null)
					{
						var c = (DataElementContainer)element;
						elem = c[c.Count - 1];
					}

					_absolutePlacement.Remove(first.Key);

					elem = placeElement(first.Value, data, elem, true, false);

					data.Position = 0;
					handleNode(elem, data);
				}

				// Need to sort based on dom walking order
				_deferredPlacement.Sort(new ElementComparer());

				// Copy current list so it doesn't get modified when iterating
				var copy = _deferredPlacement;
				_deferredPlacement = new List<DataElement>();

				foreach (var elem in copy)
				{
					var prev = ElementComparer.PreceedingElement(elem);

					if (prev == null)
					{
						data.Position = 0;
					}
					else
					{
						SizedPosition pos;
						if (!_sizedElements.TryGetValue(prev, out pos))
							Debug.Assert(false, "Preceeding element should have been cracked");
						else
							data.PositionBits = pos.end;
					}

					handleNode(elem, data);
				}
			}

			// Handle any analyzers
			foreach (DataElement elem in _elementsWithAnalyzer)
			{
				OnAnalyzerEvent(elem, data);

				var positions = new Dictionary<DataElement, Position>();
				var parent = elem.parent;

				try
				{
					elem.analyzer.asDataElement(elem, positions);
				}
				catch (SoftException ex)
				{
					throw new CrackingFailure(ex.Message, elem, data, ex);
				}
				catch (Exception ex)
				{
					throw new CrackingFailure("The analyzer encountered an error. {0}".Fmt(ex.Message),
						elem, data, ex);
				}

				var de = parent[elem.Name];
				var pos = _sizedElements[elem];
				positions[elem] = new Position() { begin = 0, end = pos.end - pos.begin };
				addElements(de, data, positions, pos.begin);
			}
		}

		/// <summary>
		/// Called to crack a DataElement based on an input stream.  This method
		/// will hand cracking off to a more specific method after performing
		/// some common tasks.
		/// </summary>
		/// <param name="elem">DataElement to crack</param>
		/// <param name="data">Input stream to use for data</param>
		void handleNode(DataElement elem, BitStream data)
		{
			List<BitStream> oldStack = null;

			try
			{
				logger.Trace("------------------------------------");
				logger.Trace("{0} {1}", elem.debugName, data.Progress);

				var pos = handleNodeBegin(elem, data);

				if (elem.transformer != null)
				{
					long startPos = data.PositionBits;
					var sizedData = elem.ReadSizedData(data, pos.size);
					var decodedData = elem.transformer.decode(sizedData);

					// Make a new stack of data for the decoded data
					oldStack = _dataStack;
					_dataStack = new List<BitStream>();
					_dataStack.Add(decodedData);

					// Use the size of the transformed data as the new size of the element
					handleCrack(elem, decodedData, decodedData.LengthBits);

					// Make sure the non-decoded data is at the right place
					if (data == decodedData)
						data.SeekBits(startPos + decodedData.LengthBits, System.IO.SeekOrigin.Begin);
				}
				else
				{
					handleCrack(elem, data, pos.size);
				}

				if (elem.constraint != null)
					handleConstraint(elem, data);

				if (elem.analyzer != null)
					_elementsWithAnalyzer.Add(elem);

				handleNodeEnd(elem, data, pos);
			}
			catch (Exception e)
			{
				handleException(elem, data, e);
				throw;
			}
			finally
			{
				if (oldStack != null)
					_dataStack = oldStack;
			}
		}

		void handlePlacelemt(DataElement element, BitStream data)
		{
			if (element.placement.after != null)
			{
				var target = element.find(element.placement.after);
				if (target == null)
					throw new CrackingFailure("Couldn't place element after '{0}', target could not be found."
						.Fmt(element.placement.after), element, data);

				placeElement(element, data, target, true, true);
			}
			else if (element.placement.before != null)
			{
				var target = element.find(element.placement.before);
				if (target == null)
					throw new CrackingFailure("Couldn't place element before '{0}', target could not be found."
						.Fmt(element.placement.before), element, data);

				placeElement(element, data, target, false, true);
			}
			else
			{
				var pos = GetAbsoluteOffset(element, data);
				if (!pos.HasValue)
					throw new CrackingFailure("Placement requires before/after attribute or an offset relation.", element, data);

				// Element has an offset relation so use that when cracking

				if (Logger.IsDebugEnabled)
				{
					Logger.Debug("{0}-- {1} '{2}'", _logPrefix, element.elementType, element.Name);
					Logger.Debug("{0}   Placing At Offset: {1} bytes | {2} bits", _logPrefix, pos / 8, pos);
				}

				logger.Trace("handlePlacement: {0} -> Placing at offset {1} bits", element, pos);

				element.placement = null;

				try
				{
					_absolutePlacement.Add(pos.Value, element);
				}
				catch (ArgumentException ex)
				{
					// Have to trigger an enter event before we throw fail
					// so error event propigates properly
					OnEnterHandleNodeEvent(element, data.PositionBits, data);

					throw new CrackingFailure("Two elements exist at the same offset.", element, data, ex);
				}
			}
		}

		DataElement placeElement(DataElement element, BitStream data, DataElement target, bool after, bool defer)
		{
			var fixups = new List<Tuple<Fixup, string, string>>();

			// Locate relevant fixups that reference this element about to me moved
			// or any element that is a child of the element that is going to be moved
			// Store off fixup,ref,Full.Path.To.Element.We.Are.Placing
			// So we can update it to New.Path.To.Placed.Element

			foreach (var child in element.getRoot().Walk())
			{
				if (child.fixup == null)
					continue;

				foreach (var item in child.fixup.references)
				{
					var refElem = child.find(item.Item2);
					if (refElem == null)
						throw new CrackingFailure("Failed to resolve Fixup ref '{0}' during placement."
							.Fmt(item.Item2), element, data);

					if (refElem == element)
						fixups.Add(new Tuple<Fixup, string, string>(child.fixup, item.Item1, null));
					else if (!refElem.isChildOf(element))
						fixups.Add(new Tuple<Fixup, string, string>(child.fixup, item.Item1, refElem.fullName));
				}
			}

			// Update fixups
			foreach (var fixup in fixups.Where(f => f.Item3 != null))
			{
				fixup.Item1.updateRef(fixup.Item2, fixup.Item3);
			}

			var oldElem = element;
			var oldParent = element.parent;
			var next = element.nextSibling();


			if (after)
			{
				element = element.MoveAfter(target);
			}
			else
			{
				element = element.MoveBefore(target);
			}

			// Update fixups
			foreach (var fixup in fixups.Where(f => f.Item3 == null))
			{
				fixup.Item1.updateRef(fixup.Item2, element.fullName);
			}

			// Clear placement now that it has occured
			element.placement = null;

			if (Logger.IsDebugEnabled)
			{
				Logger.Debug("{0}-- {1} '{2}'", _logPrefix, oldElem.elementType, oldElem.Name);
				Logger.Debug("{0}   Placed As: {1}", _logPrefix, element.fullName);
			}

			// We placed behind the current position if:
			// 1) No next and newElem is a child of oldParent
			// 2) No next element and newElem < oldParent
			// 3) Next and newElem < next

			if (!defer)
			{
				logger.Trace("handlePlacement: {0} -> {1}", oldElem.debugName, element.fullName);
			}
			else if (next == null)
			{
				if (element.isChildOf(oldParent) || ElementComparer.CompareTo(element, oldParent) < 0)
				{
					_deferredPlacement.Add(element);
					logger.Trace("handlePlacement: {0} -> {1} (Deferring cracking, no next)", oldElem.debugName, element.fullName);
				}
				else
				{
					logger.Trace("handlePlacement: {0} -> {1} (Immediate cracking, no next)", oldElem.debugName, element.fullName);
				}
			}
			else
			{
				if (ElementComparer.CompareTo(element, next) < 0)
				{
					_deferredPlacement.Add(element);
					logger.Trace("handlePlacement: {0} -> {1} (Deferring cracking, yes next)", oldElem.debugName, element.fullName);
				}
				else
				{
					logger.Trace("handlePlacement: {0} -> {1} (Immediate cracking, yes next)", oldElem.debugName, element.fullName);
				}
			}

			OnPlacementEvent(oldElem, element, oldParent);

			return element;
		}

		#endregion

		#region Helpers

		void handleOffsetRelation(DataElement element, BitStream data)
		{
			long? offset = getRelativeOffset(element, data, 0);

			if (!offset.HasValue)
				return;

			offset += data.PositionBits;

			if (offset > data.LengthBits)
				data.WantBytes((offset.Value + 7 - data.LengthBits) / 8);

			if (offset > data.LengthBits)
				throw new CrackingFailure("Offset is {0} bits but buffer only has {1} bits."
					.Fmt(offset, data.LengthBits), element, data);

			data.SeekBits(offset.Value, System.IO.SeekOrigin.Begin);
		}

		void handleException(DataElement elem, BitStream data, Exception e)
		{
			if (Logger.IsDebugEnabled)
			{
				_logPrefix.Remove(_logPrefix.Length - 2, 2);

				var ex = e as CrackingFailure;
				var msg = ex != null ? ex.ShortMessage : e.Message;

				if (elem is DataElementContainer)
				{
					if (_lastError != e)
						Logger.Debug("{0} X ({1})", _logPrefix, msg);
					else
						Logger.Debug("{0} X", _logPrefix);
				}
				else if (_lastError != e)
				{
					Logger.Debug("{0}   Failed: {1}", _logPrefix, msg);
				}
			}

			if (_lastError == e)
			{
				// Already logged the exception
				logger.Trace("{0} failed to crack.", elem.debugName);
			}
			else if (e is CrackingFailure)
			{
				// Cracking failures include element name in message
				logger.Trace(e.Message);
			}
			else
			{
				logger.Trace("{0} failed to crack.", elem.debugName);
				logger.Trace("Exception occured: {0}", e.ToString());
			}

			_lastError = e;

			var items = _sizedElements.Where(x => x.Key.isChildOf(elem)).Select(x => x.Key).ToList();

			foreach (var item in items)
				_sizedElements.Remove(item);

			_sizedElements.Remove(elem);

			items = _elementsWithAnalyzer.Where(x => x.isChildOf(elem)).ToList();

			foreach (var item in items)
				_elementsWithAnalyzer.Remove(item);

			OnExceptionHandleNodeEvent(elem, data.PositionBits, data, e);
		}

		void handleConstraint(DataElement element, BitStream data)
		{
			var scope = new Dictionary<string, object>();
			scope["element"] = element;
			scope["self"] = element;

			// Use DefaultValue for constraint, it is the actual cracked value.
			// InternalValue will have relations/fixups applied

			var iv = element.DefaultValue;
			if (iv == null)
			{
				scope["value"] = null;
				logger.Trace("Running constraint [{0}], value=None.", element.constraint);
			}
			else if (iv.GetVariantType() == Variant.VariantType.ByteString || iv.GetVariantType() == Variant.VariantType.BitStream)
			{
				scope["value"] = (BitwiseStream)iv;
				logger.Trace("Running constraint [{0}], value={1}.", element.constraint, iv);
			}
			else
			{
				scope["value"] = (string)iv;
				logger.Trace("Running constraint [{0}], value={1}.", element.constraint, (string)iv);
			}

			object oReturn = element.EvalExpression(element.constraint, scope);

			if (!((bool)oReturn))
				throw new CrackingFailure("Constraint failed [{0}].".Fmt(element.constraint), element, data);
		}

		SizedPosition handleNodeBegin(DataElement elem, BitStream data)
		{
			try
			{
				handleOffsetRelation(elem, data);
			}
			finally
			{
				// Wait to log start element until we have updated data.Progress
				// to reflect any offset relations that might exist.
				// We always want to log so that if an exception is thrown
				// we will be at the right indentation level for logging
				// in handleException()

				if (Logger.IsDebugEnabled)
				{
					if (elem is DataElementContainer)
					{
						Logger.Debug("{0}-+ {1} '{2}', {3}", _logPrefix, elem.elementType, elem.Name, data.Progress);
						_logPrefix.Append(" |");
					}
					else
					{
						Logger.Debug("{0}-- {1} '{2}', {3}", _logPrefix, elem.elementType, elem.Name, data.Progress);
						_logPrefix.Append("  ");
					}
				}
			}

			System.Diagnostics.Debug.Assert(!_sizedElements.ContainsKey(elem));

			logger.Trace("getSize: -----> {0}", elem.debugName);

			var size = GetSize(elem, data);

			logger.Trace("getSize: <----- {0} {1}", elem.debugName, size);

			Log("{0}", size);

			var pos = new SizedPosition
			{
				begin = data.PositionBits + getDataOffset(),
				size = size.Unknown ? (long?)null : size.Size,
			};

			_sizedElements.Add(elem, pos);

			OnEnterHandleNodeEvent(elem, pos.begin, data);

			return pos;
		}

		void handleNodeEnd(DataElement elem, BitStream data, Position pos)
		{
			// Completing this element might allow us to evaluate
			// outstanding size reation computations.

			foreach (var rel in elem.relations.From<SizeRelation>())
			{
				SizedPosition other;

				if (_sizedElements.TryGetValue(rel.Of, out other))
				{
					long size = rel.GetValue();

					if (other.size.HasValue)
						logger.Trace("Size relation of {0} cracked again. Updating size from: {1} to: {2}",
							rel.Of.debugName, other.size, size);
					else
						logger.Trace("Size relation of {0} cracked. Updating size to: {1}",
							rel.Of.debugName, size);

					other.size = size;
				}
			}

			// Mark the end position of this element
			pos.end = data.PositionBits + getDataOffset();

			OnExitHandleNodeEvent(elem, pos.end, data);

			if (Logger.IsDebugEnabled)
			{
				_logPrefix.Remove(_logPrefix.Length - 2, 2);

				if (elem is DataElementContainer)
					Logger.Debug("{0} /", _logPrefix);
			}
		}

		void handleCrack(DataElement elem, BitStream data, long? size)
		{
			logger.Trace("Crack: {0} Size: {1}, {2}", elem.debugName,
				size.HasValue ? size.ToString() : "<null>", data.Progress);

			elem.Crack(this, data, size);
		}

		#endregion

		#endregion

		#region Calculate Element Size

		long? GetAbsoluteOffset(DataElement elem, BitStream data)
		{
			var relations = elem.relations.Of<OffsetRelation>().ToList();
			if (!relations.Any())
				return null;

			// Ensure we have cracked the from half of the relation
			var rel = relations.Where(HasCracked).FirstOrDefault();
			if (rel == null)
				return null;

			// Offset is in bytes
			var offset = rel.GetValue() * 8;

			if (rel.isRelativeOffset)
			{
				var from = rel.From;

				if (rel.relativeTo != null)
					from = from.find(rel.relativeTo);

				if (from == null)
					throw new CrackingFailure("Unable to resolve offset relation relative to '{0}'."
						.Fmt(rel.relativeTo), elem, data);

				// Get the position we are related to
				SizedPosition pos;
				if (!_sizedElements.TryGetValue(from, out pos))
					return null;

				// If relativeTo, offset is from beginning of relativeTo element
				// Otherwise, offset is after the From element
				offset += rel.relativeTo != null ? pos.begin : pos.end;
			}

			return offset;
		}

		long? getRelativeOffset(DataElement elem, BitStream data, long minOffset = 0)
		{
			var offset = GetAbsoluteOffset(elem, data);
			if (!offset.HasValue)
				return null;

			// Adjust offset to be relative to the current BitStream
			offset -= getDataOffset();

			// Ensure the offset is not before our current position
			if (offset < data.PositionBits)
				throw new CrackingFailure("Offset is {0} bits but already read {1} bits."
					.Fmt(offset, data.PositionBits), elem, data);

			// Make offset relative to current position
			offset -= data.PositionBits;

			// Ensure the offset satisfies the minimum
			if (offset < minOffset)
				throw new CrackingFailure("Offset is {0} bits but must be at least {1} bits."
					.Fmt(offset, minOffset), elem, data);

			return offset;
		}

		/// <summary>
		/// Searches data for the first occurance of token starting at offset.
		/// </summary>
		/// <param name="data">BitStream to search in.</param>
		/// <param name="token">BitStream to search for.</param>
		/// <param name="offset">How many bits after the current position of data to start searching.</param>
		/// <param name="optional">Is the token optional.</param>
		/// <returns>The location of the token in data from the current position or null.</returns>
		long? findToken(BitStream data, BitwiseStream token, long offset, bool optional)
		{
			while (true)
			{
				var pos = data.PositionBits;
				var start = pos + offset;

				if (start < data.LengthBits)
				{
					var end = data.IndexOf(token, start);

					if (end >= 0)
						return end - pos;
				}

				if (optional)
					return null;

				// The minimum to ask for is offset + tokenLength;
				// Ask for 1 more than actually needed
				// If no new data arrives, give up but as long as more
				// data keeps coming in we will keep scanning

				var len = data.Length;
				var minLen = ((offset + 7) / 8) + token.Length;
				var want = Math.Max(len - data.Position, minLen) + 1;

				data.WantBytes(want);
				if (len == data.Length)
					return null;
			}
		}

		bool? scanArray(Dom.Array array, ref long pos, List<Mark> tokens, Until until)
		{
			logger.Trace("scanArray: {0}", array.debugName);

			int tokenCount = tokens.Count;
			long arrayPos = 0;
			var ret = scan(array.OriginalElement, ref arrayPos, tokens, null, until);

			for (int i = tokenCount; i < tokens.Count; ++i)
			{
				if (array.Count >= array.minOccurs)
					tokens[i].Priority = 2;
				tokens[i].Position += pos;
			}

			if (ret.HasValue && ret.Value)
			{
				if (until == Until.FirstSized)
					ret = false;

				var relations = array.relations.Of<CountRelation>();
				if (relations.Any())
				{
					var rel = relations.Where(HasCracked).FirstOrDefault();
					if (rel != null)
					{
						arrayPos *= rel.GetValue();
						pos += arrayPos;
						logger.Trace("scanArray: {0} -> Count Relation: {1}, Size: {2}",
							array.debugName, rel.GetValue(), arrayPos);
						return ret;
					}
					else
					{
						logger.Trace("scanArray: {0} -> Count Relation: ???", array.debugName);
						return null;
					}
				}
				else if (array.minOccurs == 1 && array.maxOccurs == 1)
				{
					arrayPos *= array.occurs;
					pos += arrayPos;
					logger.Trace("scanArray: {0} -> Occurs: {1}, Size: {2}",
						array.debugName, array.occurs, arrayPos);
					return ret;
				}
				else
				{
					// If the count is unknown, treat the array unsized
					ret = null;

					// If no tokens were found in the array, we are done
					if (tokenCount == tokens.Count)
					{
						logger.Trace("scanArray: {0} -> Count Unknown", array.debugName);
						return ret;
					}
				}
			}

			// If we are looking for the first sized element, try cracking our first element
			if (until == Until.FirstSized)
			{
				logger.Trace("scanArray: {0} -> FirstSized", array.debugName);
				return false;
			}

			if (tokenCount == tokens.Count)
			{
				logger.Trace("scanArray: {0} -> No Tokens", array.debugName);
					//ret.HasValue ? "Deterministic" : "Unsized");
				return false;
			}

			// If we have tokens, keep scanning thru the dom.
			logger.Trace("scanArray: {0} -> Tokens", array.debugName);
			return true;
		}

		class Mark
		{
			public DataElement Element { get; set; }
			public long Position { get; set; }
			public int Priority { get; set; }
		}

		enum Until { FirstSized, FirstUnsized };

		/// <summary>
		/// Scan elem and all children looking for a target element.
		/// The target can either be the first sized element or the first unsized element.
		/// If an unsized element is found, keep track of the determinism of the element.
		/// An element is determinstic if its size is unknown, but can be determined by calling
		/// crack(). Examples are a container with sized children or a null terminated string.
		/// </summary>
		/// <param name="elem">Element to start scanning at.</param>
		/// <param name="pos">The position of the scanner when 'until' occurs.</param>
		/// <param name="tokens">List of tokens found when scanning.</param>
		/// <param name="end">If non-null and an element with an offset relation is detected,
		/// record the element's absolute position and stop scanning.</param>
		/// <param name="until">When to stop scanning.
		/// Either first sized element or first unsized element.</param>
		/// <returns>Null if an unsized element was found.
		/// False if a deterministic element was found.
		/// True if all elements are sized.</returns>
		bool? scan(DataElement elem, ref long pos, List<Mark> tokens, Mark end, Until until)
		{
			if (elem.isToken)
			{
				tokens.Add(new Mark() { Element = elem, Position = pos, Priority = 0 });
				logger.Trace("scan: {0} -> Pos: {1}, Saving Token", elem.debugName, pos);
			}

			if (end != null)
			{
				long? offRel = getRelativeOffset(elem, _dataStack.First(), pos);
				if (offRel.HasValue)
				{
					end.Element = elem;
					end.Position = offRel.Value;
					logger.Trace("scan: {0} -> Pos: {1}, Offset relation: {2}", elem.debugName, pos, end.Position);
					return true;
				}
			}

			// See if we have a size relation
			var relations = elem.relations.Of<SizeRelation>();
			if (relations.Any())
			{
				var sizeRel = relations.Where(HasCracked).FirstOrDefault();

				if (sizeRel != null)
				{
					pos += sizeRel.GetValue();
					logger.Trace("scan: {0} -> Pos: {1}, Size relation: {2}", elem.debugName, pos, sizeRel.GetValue());
					return true;
				}
				else
				{
					// If the size relation has not been resolved, keep cracking until it has
					logger.Trace("scan: {0} -> Pos: {1}, Size relation: ???", elem.debugName, pos);
					return false;
				}
			}

			// See if our length is defined
			if (elem.hasLength)
			{
				pos += elem.lengthAsBits;
				logger.Trace("scan: {0} -> Pos: {1}, Length: {2}", elem.debugName, pos, elem.lengthAsBits);
				return true;
			}

			// See if our length is determinstic, size is determined by cracking
			if (elem.isDeterministic)
			{
				logger.Trace("scan: {0} -> Pos: {1}, Determinstic", elem.debugName, pos);
				return false;
			}

			// If we are unsized, see if we are a container
			var cont = elem as DataElementContainer;
			if (cont == null)
			{
				logger.Trace("scan: {0} -> Offset: {1}, Unsized element", elem.debugName, pos);
				return null;
			}

			// Elements with transformers require a size
			if (cont.transformer != null)
			{
				logger.Trace("scan: {0} -> Offset: {1}, Unsized transformer", elem.debugName, pos);
				return null;
			}

			// Treat choices as unsized
			if (cont is Dom.Choice)
			{
				var choice = (Dom.Choice)cont;
				if (choice.choiceElements.Count == 1)
					return scan(choice.choiceElements[0], ref pos, tokens, end, until);

				logger.Trace("scan: {0} -> Offset: {1}, Unsized choice", elem.debugName, pos);

				if (until == Until.FirstSized)
					return false;

				return null;
			}

			if (cont is Dom.Array)
			{
				return scanArray((Dom.Array)cont, ref pos, tokens, until);
			}

			logger.Trace("scan: {0}", elem.debugName);

			foreach (var child in cont)
			{
				bool? ret = scan(child, ref pos, tokens, end, until);

				// An unsized element was found
				if (!ret.HasValue)
					return until == Until.FirstSized ? false : ret;

				// Aa unsized but deterministic element was found
				if (ret.Value == false)
					return ret;

				// If we are looking for the first sized element than this
				// element size is determined by cracking all the children
				if (until == Until.FirstSized)
					return false;
			}

			// All children are sized, so we are sized
			return true;
		}

		/// <summary>
		/// Helper class for representing the size of an element.
		/// </summary>
		class ElementSize
		{
			/// <summary>
			/// Is the size unknown.
			/// </summary>
			public bool Unknown { get; set; }

			/// <summary>
			/// If the size is known, the size of the element in bits.
			/// </summary>
			public long Size { get; set; }

			/// <summary>
			/// Why the element was determined to be of this size.
			/// Stored as a delegate so any styring formatting will
			/// not happen unless logging is enabled.
			/// </summary>
			public Func<string> Reason { private get; set; }

			/// <summary>
			/// Format size for logging.
			/// </summary>
			/// <returns>String representation of size.</returns>
			public override string ToString()
			{
				if (Unknown)
					return "Size: ??? ({0})".Fmt(Reason());

				return "Size: {0} bytes | {1} bits ({2})".Fmt(Size / 8, Size, Reason());
			}
		}

		/// <summary>
		/// Get the size of the data element.
		/// </summary>
		/// <param name="elem">Element to size</param>
		/// <param name="data">Bits to crack</param>
		/// <returns>Null if size is unknown or the size in bits.</returns>
		ElementSize GetSize(DataElement elem, BitStream data)
		{
			long pos = 0;

			var ret = scan(elem, ref pos, new List<Mark>(), null, Until.FirstSized);

			if (ret.HasValue)
			{
				if (ret.Value)
				{
					return new ElementSize
					{
						Size = pos,
						Reason = () => "Has Length"
					};
				}

				return new ElementSize
				{
					Unknown = true,
					Reason = () => "Deterministic"
				};
			}

			var tokens = new List<Mark>();
			var end = new Mark();

			ret = lookahead(elem, ref pos, tokens, end);

			// 1st priority, end placement
			if (end.Element != null)
			{
				return new ElementSize
				{
					Size = end.Position - pos,
					Reason = () => "End Placement"
				};
			}

			// 2rd priority, token scan
			long? closest = null;
			Mark winner = null;

			foreach (var token in tokens)
			{
				long? where = findToken(data, token.Element.Value, token.Position, token.Priority != 0);
				if (!where.HasValue && token.Priority == 0)
				{
					var val = token.Element.DefaultValue;

					return new ElementSize
					{
						Unknown = true,
						Reason = () => "Missing Required Token '{0}'".Fmt(val)
					};
				}

				if (!where.HasValue)
					continue;

				if (!closest.HasValue || closest.Value > where.Value)
				{
					closest = where.Value;
					winner = token;
				}

				if (token.Priority <= 1)
					break;
			}

			if (closest.HasValue)
			{
				var pri = winner.Priority;
				var val = winner.Element.DefaultValue;

				return new ElementSize
				{
					Size = closest.Value - winner.Position,
					Reason = () => "{0}Token '{1}'".Fmt(pri == 0 ? "" : "Optional ", val)
				};
			}

			if (tokens.Count > 0 && ret.HasValue && ret.Value)
			{
				return new ElementSize
				{
					Size = data.LengthBits - (data.PositionBits + pos),
					Reason = () => "Missing Optional Tokens '{0}'".Fmt(
						string.Join("', '", tokens.Select(t => t.Element.DefaultValue)))
				};
			}

			// 3nd priority, last unsized element
			if (ret.HasValue)
			{
				if (ret.Value && (pos != 0 || !(elem is DataElementContainer)))
				{
					return new ElementSize
					{
						Size = data.LengthBits - (data.PositionBits + pos),
						Reason = () => "Last Unsized"
					};
				}

				return new ElementSize
				{
					Unknown = true,
					Reason = () => "Last Unsized"
				};
			}

			if (elem is Array)
			{
				return new ElementSize
				{
					Unknown = true,
					Reason = () => "Array Not Last Unsized"
				};
			}

			if (elem is Choice)
			{
				return new ElementSize
				{
					Unknown = true,
					Reason = () => "Choice Not Last Unsized"
				};
			}

			return new ElementSize
			{
				Unknown = true,
				Reason = () => "Not Last Unsized"
			};
		}

		/// <summary>
		/// Scan all elements after elem looking for the first unsized element.
		/// If an unsized element is found, keep track of the determinism of the element.
		/// An element is determinstic if its size is unknown, but can be determined by calling
		/// crack(). Examples are a container with sized children or a null terminated string.
		/// </summary>
		/// <param name="elem">Start scanning at this element's next sibling.</param>
		/// <param name="pos">The position of the scanner when 'until' occurs.</param>
		/// <param name="tokens">List of tokens found when scanning.</param>
		/// <param name="end">If non-null and an element with an offset relation is detected,
		/// record the element's absolute position and stop scanning.
		/// Either first sized element or first unsized element.</param>
		/// <returns>Null if an unsized element was found.
		/// False if a deterministic element was found.
		/// True if all elements are sized.</returns>
		bool? lookahead(DataElement elem, ref long pos, List<Mark> tokens, Mark end)
		{
			logger.Trace("lookahead: {0}", elem.debugName);

			// Ensure all elements are sized until we reach either
			// 1) A token
			// 2) An offset relation we have cracked that can be satisfied
			// 3) The end of the data model

			DataElement prev = elem;
			bool? final = true;

			while (prev != _root)
			{
				// Get the next sibling
				var curr = prev.nextSibling();

				if (curr != null)
				{
					var ret = scan(curr, ref pos, tokens, end, Until.FirstUnsized);
					if (!ret.HasValue)
					{
						if (tokens.Count == 0)
							return ret;

						final = ret;
						curr = prev.parent;
					}
					else if (ret.Value == false)
						return ret;

					if (end.Element != null)
						return final;
				}
				else if (prev.parent == _root.parent)
				{
					// hit the top
					break;
				}
				else if (GetElementSize(prev.parent).HasValue)
				{
					// Parent is bound by size
					break;
				}
				else
				{
					if (!(elem is DataElementContainer) && (prev.parent is Dom.Array))
					{
						var array = (Dom.Array)prev.parent;
						if (array.maxOccurs == -1 || array.Count < array.maxOccurs)
						{
							if (elem.isChildOf(array))
							{
								// Since we are crossing an array boundary
								// reset our token position offset back to zero
								// in case we encounter any new tokens
								tokens.ForEach(t => t.Priority = 1);
								pos = 0;
							}

							long arrayPos = pos;
							var ret = scanArray(array, ref arrayPos, tokens, Until.FirstUnsized);

							// If the array isn't sized and we haven't met the minimum, propigate
							// the lack of size, otherwise keep scanning
							if ((!ret.HasValue || ret.Value == false) && array.Count < array.minOccurs)
									return ret;
						}
					}

					// no more siblings, ascend
					curr = prev.parent;
				}

				prev = curr;
			}

			return final;
		}

		#endregion
	}
}

// end
