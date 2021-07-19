


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Xml;

using Peach.Core.IO;
using Peach.Core.Cracker;
using System.Diagnostics;
using System.Globalization;
using System.IO;

using NLog;
using System.Xml.Serialization;

namespace Peach.Core.Dom
{
	#region Enumerations

	/// <summary>
	/// Length types
	/// </summary>
	/// <remarks>
	/// The "length" property defaults to Bytes.  Not all
	/// implementations of DataElement will support all LengthTypes.
	/// </remarks>
	public enum LengthType
	{
		/// <summary>
		/// Indicates the length is specified in units of bytes.
		/// </summary>
		[XmlEnum("bytes")]
		Bytes,

		/// <summary>
		/// Indicates the length is specified in units of bits.
		/// </summary>
		[XmlEnum("bits")]
		Bits,

		/// <summary>
		/// Indicates the length is specified in units of characters.
		/// </summary>
		[XmlEnum("chars")]
		Chars,
	}

	public enum ValueType
	{
		/// <summary>
		/// Regular string. C style "\" escaping can be used such as: \\r, \\n, \\t, and \\\\\.
		/// </summary>
		[XmlEnum("string")]
		String,

		/// <summary>
		/// Hex string. Allows specifying binary data.
		/// </summary>
		[XmlEnum("hex")]
		Hex,

		/// <summary>
		/// Treated as a python literal string.
		/// An example is "[1,2,3,4]" which would evaluate to a python list.
		/// </summary>
		[XmlEnum("literal")]
		Literal,

		/// <summary>
		/// An IPv4 string address that is converted to an array of bytes.
		/// An example is "127.0.0.1" which would evaluate to the bytes: 0x7f, 0x00, 0x00, 0x01.
		/// </summary>
		[XmlEnum("ipv4")]
		IPv4,

		/// <summary>
		/// An IPv6 string address that is converted to an array of bytes.
		/// </summary>
		[XmlEnum("ipv6")]
		IPv6,
	}

	public enum EndianType
	{
		/// <summary>
		/// Big endian encoding.
		/// </summary>
		[XmlEnum("big")]
		Big,

		/// <summary>
		/// Little endian encoding.
		/// </summary>
		[XmlEnum("little")]
		Little,

		/// <summary>
		/// Big endian encoding.
		/// </summary>
		[XmlEnum("network")]
		Network,
	}

	/// <summary>
	/// Mutated value override's fixupImpl
	///
	///  - Default Value
	///  - Relation
	///  - Fixup
	///  - Type contraints
	///  - Transformer
	/// </summary>
	[Flags]
	public enum MutateOverride : uint
	{
		/// <summary>
		/// No overrides have occured
		/// </summary>
		None = 0x00,
		/// <summary>
		/// Mutated value overrides fixups
		/// </summary>
		Fixup = 0x01,
		/// <summary>
		/// Mutated value overrides transformers
		/// </summary>
		Transformer = 0x02,
		/// <summary>
		/// Mutated value overrides type constraints (e.g. string length, null terminated, etc.)
		/// </summary>
		TypeConstraints = 0x04,
		/// <summary>
		/// Mutated value overrides relations.
		/// </summary>
		Relations = 0x08,
		/// <summary>
		/// Mutated value overrides type transforms.
		/// </summary>
		TypeTransform = 0x20,
		/// <summary>
		/// Default mutate value
		/// </summary>
		Default = Fixup,
	}

	public enum ElementWeight
	{
		Off = 0,
		Lowest,
		BelowNormal,
		Normal,
		AboveNormal,
		Highest
	}

	#endregion

	public delegate void InvalidatedEventHandler(object sender, EventArgs e);

	/// <summary>
	/// Base class for all data elements.
	/// </summary>
	[Serializable]
	[DebuggerDisplay("{debugName}")]
	public abstract class DataElement : INamed, IOwned<DataElementContainer>, IPitSerializable
	{
		#region Obsolete Functions

		[Obsolete("This property is obsolete and has been replaced by the Name property.")]
		public string name { get { return Name; } }

		#endregion

		/// <summary>
		/// This exists so the ParameterParser can parse 'ref' parameters.
		/// </summary>
		public static DataElement Parse(string str)
		{
			return null;
		}

		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		#region Clone

		public class CloneContext
		{
			public CloneContext(DataElement root, DataElementContainer parent, string name, bool shallow)
			{
				this.root = root;
				this.name = name;

				Parent = parent;
				Shallow = shallow;

				rename = new List<DataElement>();

				if (root.Name != name)
					rename.Add(root);
			}

			public DataElement root
			{
				get;
				private set;
			}

			public string name
			{
				get;
				private set;
			}

			public List<DataElement> rename
			{
				get;
				private set;
			}

			public bool Shallow
			{
				get;
				private set;
			}

			public DataElementContainer Parent
			{
				get;
				private set;
			}

			public string UpdateRefName(DataElement parent, DataElement elem, string name)
			{
				if (parent == null || name == null)
					return name;

				// Expect parent and element to be in the source object graph
				Debug.Assert(InSourceGraph(parent));

				if (elem == null)
					elem = parent.find(name);
				else
					Debug.Assert(InSourceGraph(elem));

				return rename.Contains(elem) ? this.name : name;
			}

			private bool InSourceGraph(DataElement elem)
			{
				var top = root.getRoot();
				return elem == top || elem.isChildOf(top);
			}
		}

		/// <summary>
		/// Creates a deep copy of the DataElement, and updates the appropriate Relations.
		/// </summary>
		/// <returns>Returns a copy of the DataElement.</returns>
		public DataElement Clone()
		{
			// If we have a parent, we need a CloneContext
			if (parent != null)
				return Clone(Name);

			// Slight optimization for cloning. No CloneContext is needed since
			// we are cloning the whole dom w/o renaming the root.  This means
			// fixups & relations will not try and update any name ref's
			return ObjectCopier.Clone(this, null);
		}

		/// <summary>
		/// Creates a deep copy of the DataElement, and updates the appropriate Relations.
		/// </summary>
		/// <param name="newParent">The new parent of the cloned element</param>
		/// <returns>Returns a copy of the DataElement.</returns>
		public DataElement ShallowClone(DataElementContainer newParent)
		{
			return DoClone(newParent, Name, true);
		}

		/// <summary>
		/// Creates a deep copy of the DataElement, and updates the appropriate Relations.
		/// </summary>
		/// <param name="newName">What name to set on the cloned DataElement</param>
		/// <returns>Returns a copy of the DataElement.</returns>
		public DataElement Clone(string newName)
		{
			return DoClone(parent, newName, false);
		}

		/// <summary>
		/// Creates a deep copy of the DataElement, and updates the appropriate Relations.
		/// </summary>
		/// <param name="newParent">The new parent of the cloned element</param>
		/// <param name="newName">What name to set on the cloned DataElement</param>
		/// <returns>Returns a copy of the DataElement.</returns>
		public DataElement ShallowClone(DataElementContainer newParent, string newName)
		{
			return DoClone(newParent, newName, true);
		}

		private DataElement DoClone(DataElementContainer newParent, string newName, bool shallow)
		{
			var ret = ObjectCopier.Clone(this, new CloneContext(this, newParent, newName, shallow));

			if (Name != newName)
			{
				if (ret.parent == null)
					ret.fullName = ret.Name;
				else
					ret.fullName = ret.parent.fullName + "." + ret.Name;

				foreach (var item in ret.PreOrderTraverse().Skip(1))
					item.fullName = item.parent.fullName + "." + item.Name;
			}

			ret.parent = newParent;

			return ret;
		}

		#endregion

		#region Walking Functions

		/// <summary>
		/// Performs pre-order traversal starting with this node.
		/// If forDisplay is true, returns only children we want to display to the user.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<KeyValuePair<string, DataElement>> TuningTraverse(bool useFieldIds, bool forDisplay)
		{
			var key = useFieldIds ? FullFieldId : fullName;
			var toVisit = new List<KeyValuePair<string, DataElement>>
			{
				new KeyValuePair<string, DataElement>(key, this)
			};

			while (toVisit.Any())
			{
				var index = toVisit.Count - 1;
				var node = toVisit[index];
				toVisit.RemoveAt(index);

				yield return node;

				index = toVisit.Count;
				foreach (var child in node.Value.Children(forDisplay))
				{
					key = useFieldIds ? 
						child.FullFieldId : 
						node.Key + node.Value.GetDisplaySuffix(child);
					var next = new KeyValuePair<string, DataElement>(key, child);
					toVisit.Insert(index, next);
				}
			}
		}

		/// <summary>
		/// Performs pre-order traversal starting with this node.
		/// </summary>
		/// <param name="filter">Only traverse elements that pass the predacate</param>
		/// <returns></returns>
		public IEnumerable<DataElement> PreOrderTraverse(Func<DataElement, bool> filter)
		{
			var toVisit = new List<DataElement> { null };

			var elem = this;

			while (elem != null)
			{
				yield return elem;

				var index = toVisit.Count;
				foreach (var item in elem.Children().Where(filter))
					toVisit.Insert(index, item);

				index = toVisit.Count - 1;
				elem = toVisit[index];
				toVisit.RemoveAt(index);
			}
		}

		/// <summary>
		/// Performs pre-order traversal starting with this node.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<DataElement> PreOrderTraverse()
		{
			var toVisit = new List<DataElement> { null };

			var elem = this;

			while (elem != null)
			{
				yield return elem;

				var index = toVisit.Count;
				foreach (var item in elem.Children())
					toVisit.Insert(index, item);

				index = toVisit.Count - 1;
				elem = toVisit[index];
				toVisit.RemoveAt(index);
			}
		}

		/// <summary>
		/// Performs breadth-first traversal starting with this node.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<DataElement> BreadthFirstTraverse()
		{
			var toVisit = new Queue<DataElement>();
			toVisit.Enqueue(this);

			while (toVisit.Count > 0)
			{
				var elem = toVisit.Dequeue();

				foreach (var item in elem.Children())
					toVisit.Enqueue(item);

				yield return elem;
			}
		}

		/// <summary>
		/// Walk the DOM by performing pre-order traversal starting
		/// at this node.  Then perform pre-order traversal of our
		/// parent ignoring the branch containing ourselves. Keep
		/// iterating up the tree until we reach the root.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<DataElement> Walk()
		{
			// Preform pre-order traversal starting with ourselves
			foreach (var item in PreOrderTraverse())
				yield return item;

			// Walk up the dom, performing pre-order traversal of each of
			// our parents but skip children that have already been traversed.
			var skip = this;
			var next = parent;

			while (next != null)
			{
				yield return next;

				foreach (var child in next.Children().Where(x => x != skip))
				{
					foreach (var item in child.PreOrderTraverse())
						yield return item;
				}

				skip = next;
				next = next.parent;
			}
		}

		public IEnumerable<DataElement> EnumerateAll()
		{
			var toVisit = new List<IEnumerable<DataElement>>();
			toVisit.Add(Children());

			while (toVisit.Count > 0)
			{
				var index = 0;
				var elems = toVisit[0];
				toVisit.RemoveAt(0);

				foreach (var item in elems)
				{
					toVisit.Insert(index++, item.Children());
					yield return item;
				}
			}
		}

		public IEnumerable<DataElement> EnumerateUpTree()
		{
			// Traverse, will not return ourselves
			foreach (var item in EnumerateAll())
				yield return item;

			var skip = this;
			var next = parent;

			while (next != null)
			{
				// Return parent's children first
				foreach (var child in next.Children())
					yield return child;

				// Traverse on children we have not already traversed
				foreach (var child in next.Children().Where(x => x != skip).SelectMany(x => x.EnumerateAll()))
					yield return child;

				skip = next;
				next = next.parent;
			}

			yield return skip;
		}

		public virtual IEnumerable<DataElement> Children(bool forDisplay = false)
		{
			return new DataElement[0];
		}

		/// <summary>
		/// Returns an enumeration of children that are diplayed to the user.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<DataElement> DisplayChildren()
		{
			return new DataElement[0];
		}

		/// <summary>
		/// Returns a list of children for use in XPath navigation.
		/// Should not be called directly.
		/// </summary>
		/// <returns></returns>
		public virtual IList<DataElement> XPathChildren()
		{
			return new DataElement[0];
		}

		/// <summary>
		/// Is this element in scope in the data model.
		/// </summary>
		/// <returns></returns>
		public bool InScope()
		{
			var e = this;
			var p = e.parent;

			while (p != null && p.InScope(e))
			{
				e = p;
				p = e.parent;
			}

			return p == null;
		}

		/// <summary>
		/// Is a child in the selected element scope for a given parent.
		/// Default behavior is true: all cchildren are in scope.
		/// </summary>
		/// <param name="child"></param>
		/// <returns></returns>
		protected virtual bool InScope(DataElement child)
		{
			return true;
		}

		#region Find Element By Name

		/// <summary>
		/// Find data element with specific name.
		/// </summary>
		/// <remarks>
		/// We will search starting at our level in the tree, then moving
		/// to children from our level, then walk up node by node to the
		/// root of the tree.
		/// </remarks>
		/// <param name="name">Name to search for</param>
		/// <returns>Returns found data element or null.</returns>
		public DataElement find(string name)
		{
			// TODO: Investigate to see if pre-order is any better
			// return FindByWalk(name);
			return FindByEnumerateUpTree(name);
		}

		private DataElement FindByWalk(string name)
		{
			var parts = name.Split(new[] { '.' });

			foreach (var item in Walk())
			{
				var index = 0;

				if (item.Name != parts[index++])
					continue;

				var candidate = item;

				do
				{
					if (index == parts.Length)
						return candidate;

					candidate = candidate.GetChild(parts[index++]);
					if (candidate == null)
						break;
				}
				while (true);
			}

			return null;
		}

		private DataElement FindByEnumerateUpTree(string name)
		{
			var parts = name.Split(new[] { '.' });

			// EnumerateUpTree doesn't check this first, so do that
			if (parts.Length == 1 && parts[0] == Name)
				return this;

			foreach (var item in EnumerateUpTree())
			{
				var index = 0;

				if (item.Name != parts[index++])
					continue;

				var candidate = item;

				do
				{
					if (index == parts.Length)
						return candidate;

					candidate = candidate.GetChild(parts[index++]);
					if (candidate == null)
						break;
				}
				while (true);
			}

			return null;
		}

		protected virtual DataElement GetChild(string name)
		{
			return null;
		}

		#endregion

		/// <summary>
		/// Enumerate all items in tree starting with our current position
		/// then moving up towards the root.
		/// </summary>
		/// <remarks>
		/// This method uses yields to allow for efficient use even if the
		/// quired node is found quickely.
		/// 
		/// The method in which we return elements should match a human
		/// search pattern of a tree.  We start with our current position and
		/// return all children then start walking up the tree towards the root.
		/// At each parent node we return all children (excluding already returned
		/// nodes).
		/// 
		/// This method is ideal for locating objects in the tree in a way indented
		/// a human user.
		/// </remarks>
		/// <returns></returns>
		public IEnumerable<DataElement> EnumerateElementsUpTree()
		{
			return EnumerateUpTree();
		}

		/// <summary>
		/// Enumerate all child elements recursevely.
		/// </summary>
		/// <remarks>
		/// This method will return this objects direct children
		/// and finally recursevely return children's children.
		/// </remarks>
		/// <returns></returns>
		public IEnumerable<DataElement> EnumerateAllElements()
		{
			return EnumerateAll();
		}

		#endregion

		private string _name;
		private string _fieldId;
		private string _fullFieldId;

		public string Name
		{
			get { return _name; }
		}

		public ElementWeight Weight { get; set; }

		public bool isMutable = true;
		public MutateOverride mutationFlags = MutateOverride.None;
		public bool isToken = false;

		public Analyzer analyzer = null;

		protected Dictionary<string, Hint> hints = new Dictionary<string, Hint>();

		protected bool _isReference = false;

		protected Variant _defaultValue;
		protected Variant _mutatedValue;

		protected RelationContainer _relations = null;
		protected Fixup _fixup = null;
		protected Transformer _transformer = null;
		protected Placement _placement = null;

		private uint _rootRecursion = 0;
		private uint _recursionDepth = 0;
		private uint _intRecursionDepth = 0;
		private bool _readValueCache = true;
		private bool _writeValueCache = true;
		private Variant _internalValue;
		private BitwiseStream _value;

		private DataElementContainer _parent;
		private string _fullName;

		private bool _invalidated = false;

		/// <summary>
		/// Does this element have a defined length?
		/// </summary>
		protected bool _hasLength = false;

		/// <summary>
		/// Length in bits
		/// </summary>
		protected long _length = 0;

		/// <summary>
		/// Determines how the length property works.
		/// </summary>
		protected LengthType _lengthType = LengthType.Bytes;

		protected string _constraint = null;

		#region Events

		[NonSerialized]
		private InvalidatedEventHandler _invalidatedEvent;

		public event InvalidatedEventHandler Invalidated
		{
			add { _invalidatedEvent += value; }
			remove { _invalidatedEvent -= value; }
		}

		protected virtual Variant GetDefaultValue(BitStream data, long? size)
		{
			if (size.HasValue && size.Value == 0)
				return new Variant(new BitStream());

			var sizedData = ReadSizedData(data, size);
			return new Variant(sizedData);
		}

		public virtual void Crack(DataCracker context, BitStream data, long? size)
		{
			var oldDefalut = DefaultValue;

			try
			{
				DefaultValue = GetDefaultValue(data, size);
			}
			catch (PeachException pe)
			{
				throw new CrackingFailure(pe.Message, this, data, pe);
			}

			if (context.IsLogEnabled)
			{
				var vt = DefaultValue.GetVariantType();

				if (vt == Variant.VariantType.Int || vt == Variant.VariantType.Long)
					context.Log("Value: {0} (0x{1:X})", DefaultValue, (long)DefaultValue);
				else if (vt == Variant.VariantType.ULong)
					context.Log("Value: {0} (0x{1:X})", DefaultValue, (ulong)DefaultValue);
				else
					context.Log("Value: {0}", DefaultValue);
			}

			logger.Trace("{0} value is: {1}", debugName, DefaultValue);

			if (isToken && oldDefalut != DefaultValue)
			{
				var newDefault = DefaultValue;
				DefaultValue = oldDefalut;

				throw new CrackingFailure("Token did not match '{0}' vs. '{1}'."
					.Fmt(newDefault, oldDefalut), this, data);
			}
		}

		public abstract void WritePit(XmlWriter pit);

		public void WritePitCommonValue(XmlWriter pit)
		{
			if (DefaultValue.GetVariantType() == Variant.VariantType.ByteString)
			{
				var sb = new StringBuilder();
				foreach (var b in (byte[])DefaultValue)
					sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));

				pit.WriteAttributeString("valueType", "hex");
				pit.WriteAttributeString("value", sb.ToString());
			}
			else if (DefaultValue.GetVariantType() == Variant.VariantType.BitStream)
			{
				var stream = (BitStream)DefaultValue;
				var sb = new StringBuilder();
				var pos = stream.Position;

				stream.Seek(0, SeekOrigin.Begin);

				for (var i = 0; i < stream.Length; i++)
					sb.Append(stream.ReadByte().ToString("x2", CultureInfo.InvariantCulture));

				stream.Position = pos;
				pit.WriteAttributeString("valueType", "hex");
				pit.WriteAttributeString("value", sb.ToString());
			}
			else if (DefaultValue.GetVariantType() == Variant.VariantType.String)
			{
				pit.WriteAttributeString("value", (string)DefaultValue);
			}
			else
			{
				pit.WriteAttributeString("value", DefaultValue.ToString());
			}
		}

		/// <summary>
		/// Write out common data element children such as relations
		/// </summary>
		/// <param name="pit"></param>
		/// <param name="excludeTypeTransformHint"></param>
		public void WritePitCommonChildren(XmlWriter pit, bool excludeTypeTransformHint = false)
		{
			foreach (var obj in relations.From<Relation>())
				obj.WritePit(pit);

			if (fixup != null)
				fixup.WritePit(pit);

			if (transformer != null)
				transformer.WritePit(pit);

			foreach (var obj in hints.Values.Where(c => (c.Name != "NumericalString" && (!excludeTypeTransformHint || c.Name != "Peach.TypeTransform"))))
				obj.WritePit(pit);

			if (analyzer != null)
				analyzer.WritePit(pit);

			if (placement != null)
				placement.WritePit(pit);

		}

		/// <summary>
		/// Write out common data element attributes.
		/// </summary>
		/// <param name="pit"></param>
		public void WritePitCommonAttributes(XmlWriter pit)
		{
			if (!Name.StartsWith("DataElement_"))
				pit.WriteAttributeString("name", Name);

			if (FieldId != null)
				pit.WriteAttributeString("fieldId", FieldId);

			if (isToken)
				pit.WriteAttributeString("token", "true");

			if (!isMutable)
				pit.WriteAttributeString("mutable", "false");

			if (constraint != null)
				pit.WriteAttributeString("constraint", constraint);

			if (hasLength && !(this is Number) && !(this is Padding) && !(this is Flags) && !(this is Flag) && !(this is Double))
			{
				pit.WriteAttributeString("lengthType", lengthType.ToString().ToLower());
				pit.WriteAttributeString("length", lengthType == LengthType.Bits ? lengthAsBits.ToString(CultureInfo.InvariantCulture) : length.ToString(CultureInfo.InvariantCulture));
			}

			var array = parent as Array;
			if (array != null)
			{
				if (array.occurs != 1)
					pit.WriteAttributeString("occurs", array.occurs.ToString(CultureInfo.InvariantCulture));
				else
				{
					if (array.minOccurs != 1)
						pit.WriteAttributeString("minOccurs", array.minOccurs.ToString(CultureInfo.InvariantCulture));
					if (array.maxOccurs != 1)
						pit.WriteAttributeString("maxOccurs", array.maxOccurs.ToString(CultureInfo.InvariantCulture));
				}
			}
		}

		protected void OnInvalidated(EventArgs e)
		{
			// This spews a lot, only turn it on when needed
			//logger.Trace("OnInvalidated: {0}", Name);

			// Prevent infinite loops
			if (_invalidated)
				return;

			try
			{
				_invalidated = true;

				// Cause values to be regenerated next time they are
				// requested.  We don't want todo this now as there could
				// be a series of invalidations that occur.
				_internalValue = null;
				_value = null;

				// Bubble this up the chain
				if (parent != null)
					parent.Invalidate();

				if (_invalidatedEvent != null)
					_invalidatedEvent(this, e);
			}
			finally
			{
				_invalidated = false;
			}
		}

		#endregion

		/// <summary>
		/// Dynamic properties
		/// </summary>
		/// <remarks>
		/// Any objects added to properties must be serializable!
		/// </remarks>
		public Dictionary<string, object> Properties { get; set; }

		protected static uint _uniqueName = 0;

		public DataElement()
			: this("DataElement_" + (_uniqueName++).ToString(CultureInfo.InvariantCulture))
		{
		}

		public DataElement(string name)
		{
			if (name == null)
				name = "DataElement_" + (_uniqueName++);

			if (name.IndexOf('.') > -1)
				throw new PeachException("Error, DataElements cannot contain a period in their name. \"" + name + "\"");

			var attr = GetType().GetAttributes<DataElementAttribute>().FirstOrDefault();

			elementType = attr != null ? attr.elementName : GetType().Name;

			_relations = new RelationContainer(this);
			_name = name;
			fullName = name;
			root = this;
			Weight = ElementWeight.Normal;
		}

		public static T Generate<T>(XmlNode node, DataElementContainer parent) where T : DataElement, new()
		{
			T ret;

			string name = null;
			if (node.hasAttr("name"))
				name = node.getAttrString("name");

			if (string.IsNullOrEmpty(name))
			{
				ret = new T();
			}
			else
			{
				try
				{
					ret = (T)Activator.CreateInstance(typeof(T), name);
				}
				catch (TargetInvocationException ex)
				{
					var baseEx = ex.GetBaseException();
					if (baseEx is ThreadAbortException)
						throw baseEx;

					var inner = ex.InnerException;
					if (inner == null)
						throw;

					var outer = (Exception)Activator.CreateInstance(inner.GetType(), inner.Message, inner);
					throw outer;
				}
			}

			ret.parent = parent;
			return ret;
		}

		public string elementType { get; private set; }

		public string debugName { get; internal set; }

		/// <summary>
		/// Fully qualified name of DataElement to
		/// root DataElement.
		/// </summary>
		public string fullName
		{
			get { return _fullName; }
			private set
			{
				_fullName = value;
				debugName = "{0} '{1}'".Fmt(elementType, value);
			}
		}

		public string FieldId
		{
			get { return _fieldId; }
			set
			{
				_fieldId = value;

				var newFullFieldId = parent == null ? FieldId : FieldIdConcat(parent.FullFieldId, FieldId);

				if (FullFieldId == newFullFieldId)
					return;

				FullFieldId = newFullFieldId;

				foreach (var item in PreOrderTraverse().Skip(1))
				{
					item.FullFieldId = FieldIdConcat(item.parent.FullFieldId, item.FieldId);
				}
			}
		}

		protected virtual string GetDisplaySuffix(DataElement child)
		{
			return "." + child.Name;
		}

		public string FullFieldId
		{
			get { return _fullFieldId; }
			private set { _fullFieldId = value; }
		}

		public DataElement root { get; private set; }

		/// <summary>
		/// Recursively execute analyzers
		/// </summary>
		public void evaulateAnalyzers()
		{
			foreach (var item in PreOrderTraverse())
				if (item.analyzer != null)
					item.analyzer.asDataElement(item, null);
		}

		public Dictionary<string, Hint> Hints
		{
			get { return hints; }
			set { hints = value; }
		}

		public object EvalExpression(string code, Dictionary<string, object> localScope, Dom context = null)
		{
			if (context == null)
			{
				var dm = (DataModel) root;
				context = dm.dom ?? dm.actionData.action.parent.parent.parent;
			}

			return context.Python.Eval(code, localScope);
		}

		/// <summary>
		/// Constraint on value of data element.
		/// </summary>
		/// <remarks>
		/// This
		/// constraint is only enforced when loading data into
		/// the object.  It will not affect values that are
		/// produced during fuzzing.
		/// </remarks>
		public string constraint
		{
			get { return _constraint; }
			set { _constraint = value; }
		}

		/// <summary>
		/// Is this DataElement created by a 
		/// reference to another DataElement?
		/// </summary>
		public bool isReference
		{
			get { return _isReference; }
			set { _isReference = value; }
		}

		string _referenceName;

		/// <summary>
		/// If created by reference, has the reference name
		/// </summary>
		public string referenceName
		{
			get { return _referenceName; }
			set { _referenceName = value; }
		}

		public DataElementContainer parent
		{
			get { return _parent; }
			set
			{
				if (value == parent)
					return;

				_parent = value;

				string newFieldId;

				if (parent == null)
				{
					root = this;
					fullName = Name;
					newFieldId = FieldId;
				}
				else
				{
					root = parent.root;
					fullName = parent.fullName + "." + Name;
					newFieldId = FieldIdConcat(_parent.FullFieldId, FieldId);
				}

				if (FullFieldId != newFieldId)
				{
					FullFieldId = newFieldId;

					foreach (var item in PreOrderTraverse().Skip(1))
					{
						item.root = root;
						item.fullName = item.parent.fullName + "." + item.Name;
						item.FullFieldId = FieldIdConcat(item.parent.FullFieldId, item.FieldId);
					}
				}
				else
				{
					foreach (var item in PreOrderTraverse().Skip(1))
					{
						item.root = root;
						item.fullName = item.parent.fullName + "." + item.Name;
					}
				}

#if DEBUG && DISABLED
				var toVisit = new List<DataElement> { null };
				var elem = this;

				while (elem != null)
				{
					if (elem.root != root)
						throw new PeachException("Bad root on {0}. Expected {1} but was {2}.".Fmt(elem.debugName, root.debugName, elem.root.debugName));

					var index = toVisit.Count;
					foreach (var item in elem.XPathChildren())
						toVisit.Insert(index, item);

					index = toVisit.Count - 1;
					elem = toVisit[index];
					toVisit.RemoveAt(index);
				}
#endif
			}
		}

		public static string FieldIdConcat(string lhs, string rhs)
		{
			if (string.IsNullOrEmpty(lhs))
				return rhs;

			if (string.IsNullOrEmpty(rhs))
				return lhs;

			return lhs + "." + rhs;
		}

		public DataElement getRoot()
		{
			return root;
		}

		/// <summary>
		/// Find our next sibling.
		/// </summary>
		/// <returns>Returns sibling or null.</returns>
		public DataElement nextSibling()
		{
			if (parent == null)
				return null;

			var nextIndex = parent.IndexOf(this) + 1;
			if (nextIndex >= parent.Count)
				return null;

			return parent[nextIndex];
		}

		/// <summary>
		/// Find our previous sibling.
		/// </summary>
		/// <returns>Returns sibling or null.</returns>
		public DataElement previousSibling()
		{
			if (parent == null)
				return null;

			var priorIndex = parent.IndexOf(this) - 1;
			if (priorIndex < 0)
				return null;

			return parent[priorIndex];
		}

		public void BeginUpdate()
		{
			// Prevent calls to invalidate from propigating
			_invalidated = true;
		}

		public void EndUpdate()
		{
			_invalidated = false;

			Invalidate();
		}

		/// <summary>
		/// Call to invalidate current element and cause rebuilding
		/// of data elements dependent on this element.
		/// </summary>
		public void Invalidate()
		{
			//_invalidated = true;

			OnInvalidated(null);
		}

		/// <summary>
		/// Does element have a length?  This is
		/// separate from Relations.
		/// </summary>
		public virtual bool hasLength
		{
			get
			{
				if (isToken && DefaultValue != null)
					return true;

				return _hasLength;
			}
		}

		/// <summary>
		/// Is the length of the element deterministic.
		/// This is the case if the element hasLength or
		/// if the element has a specific end. For example,
		/// a null terminated string.
		/// </summary>
		public virtual bool isDeterministic
		{
			get
			{
				return hasLength;
			}
		}

		/// <summary>
		/// Length of element in lengthType units.
		/// </summary>
		/// <remarks>
		/// In the case that LengthType == "Calc" we will evaluate the
		/// expression.
		/// </remarks>
		public virtual long length
		{
			get
			{
				if (_hasLength)
				{
					switch (_lengthType)
					{
						case LengthType.Bytes:
							return _length;
						case LengthType.Bits:
							return _length;
						case LengthType.Chars:
							throw new NotSupportedException("Length type of Chars not supported by DataElement.");
					}
				}
				else if (isToken && DefaultValue != null)
				{
					switch (_lengthType)
					{
						case LengthType.Bytes:
							return Value.Length;
						case LengthType.Bits:
							return Value.LengthBits;
						case LengthType.Chars:
							throw new NotSupportedException("Length type of Chars not supported by DataElement.");
					}
				}

				throw new NotSupportedException("Error calculating length.");
			}
			set
			{
				switch (_lengthType)
				{
					case LengthType.Bytes:
						_length = value;
						break;
					case LengthType.Bits:
						_length = value;
						break;
					case LengthType.Chars:
						throw new NotSupportedException("Length type of Chars not supported by DataElement.");
					default:
						throw new NotSupportedException("Error setting length.");
				}

				_hasLength = true;
			}
		}

		/// <summary>
		/// Returns length as bits.
		/// </summary>
		public virtual long lengthAsBits
		{
			get
			{
				switch (_lengthType)
				{
					case LengthType.Bytes:
						return length * 8;
					case LengthType.Bits:
						return length;
					case LengthType.Chars:
						throw new NotSupportedException("Length type of Chars not supported by DataElement.");
					default:
						throw new NotSupportedException("Error calculating lengthAsBits.");
				}
			}
		}

		/// <summary>
		/// Type of length.
		/// </summary>
		/// <remarks>
		/// Not all DataElement implementations support "Chars".
		/// </remarks>
		public virtual LengthType lengthType
		{
			get { return _lengthType; }
			set { _lengthType = value; }
		}

		/// <summary>
		/// Default value for this data element.
		/// 
		/// Changing the default value will invalidate
		/// the model.
		/// </summary>
		public virtual Variant DefaultValue
		{
			get { return _defaultValue; }
			set
			{
				_defaultValue = value;
				Invalidate();
			}
		}

		/// <summary>
		/// Current mutated value (if any) for this data element.
		/// 
		/// Changing the MutatedValue will invalidate the model.
		/// </summary>
		public virtual Variant MutatedValue
		{
			get { return _mutatedValue; }
			set
			{
				_mutatedValue = value;
				Invalidate();
			}
		}

        /// <summary>
        /// Get the Internal Value of this data element
        /// </summary>
		[DebuggerDisplay("{InternalValueDebugName}")]
		public Variant InternalValue
		{
			get
			{
				if (_internalValue == null || _invalidated || !_readValueCache)
				{
					// If this is not invoked by calling .Value
					// on the root, then mark all elements up to the
					// root so they don't cache any values computed
					// if recursion occurs.
					if (root._rootRecursion++ == 0)
					{
						var e = parent;
						while (e != null)
						{
							e._recursionDepth++;
							e = e.parent;
						}
					}

					_intRecursionDepth++;

					var internalValue = GenerateInternalValue();

					_intRecursionDepth--;

					if (CacheValue)
						_internalValue = internalValue;

					if (--root._rootRecursion == 0)
					{
						var e = parent;
						while (e != null)
						{
							e._recursionDepth--;
							e = e.parent;
						}
					}

					return internalValue;
				}

				return _internalValue;
			}
		}

		private string InternalValueDebugName
		{
			get
			{
				return _internalValue != null ? _internalValue.ToString() : null;
			}
		}

		/// <summary>
		/// Returns the final value without any transformers being applied
		/// </summary>
		public BitwiseStream PreTransformedValue
		{
			get
			{
				// TODO: Should this be cached?
				// Alternatively, transformers could be be a different
				// type of data element so InternalValue is pre-transformed
				// and Value is post-transformed
				if (_transformer != null)
					return Value;
				else if (_mutatedValue != null && mutationFlags.HasFlag(MutateOverride.TypeTransform))
					return (BitwiseStream)_mutatedValue;
				else
					return InternalValueToBitStream();
			}
		}

        /// <summary>
        /// Get the final Value of this data element
        /// </summary>
		[DebuggerDisplay("{ValueDebugName}")]
		public BitwiseStream Value
		{
			get
			{
				// If cache reads have not been disabled, inherit value from parent
				var oldReadCache = _readValueCache;
				if (_readValueCache && parent != null)
					_readValueCache = parent._readValueCache;

				// If cache writes have not been disabled, inherit value from parent
				var oldWriteCache = _writeValueCache;
				if (_writeValueCache && parent != null)
					_writeValueCache = parent._writeValueCache;

				try
				{
					if (_value == null || _invalidated || !_readValueCache)
					{
						_recursionDepth++;

						var value = GenerateValue();
						_invalidated = false;

						if (CacheValue)
							_value = value;

						_recursionDepth--;

						return value;
					}

					return _value;
				}
				finally
				{
					// Restore values
					_writeValueCache = oldWriteCache;
					_readValueCache = oldReadCache;
				}
			}
		}

		private string ValueDebugName
		{
			get
			{
				return _value != null ? _value.ToString() : null;
			}
		}

		public virtual bool CacheValue
		{
			get
			{
				if (!_writeValueCache || _recursionDepth > 1 || _intRecursionDepth > 0)
					return false;

				if (_fixup != null)
				{
					// The root can't have a fixup!
					Debug.Assert(parent != null);

					// We can only have a valid fixup value when the parent
					// has not recursed onto itself
					foreach (var elem in _fixup.dependents)
					{
						// If elem is in our parent heirarchy, we are invalid any
						// element in the heirarchy has a _recustionDepth > 1
						// Otherwise, we are invalid if the _recursionDepth > 0

						if (isChildOf(elem))
						{
							var p = this;

							do
							{
								p = p.parent;

								if (p._recursionDepth > 1)
									return false;
							}
							while (p != elem);
						}
						else if (elem._recursionDepth > 0)
						{
							return false;
						}
					}
				}

				return true;
			}
		}

		public long CalcLengthBits()
		{
			// Turn off read and write caching of 'Value'
			var oldReadCache = _readValueCache;
			_readValueCache = false;
			var oldWriteCache = _writeValueCache;
			_writeValueCache = false;

			var ret = Value.LengthBits;

			_writeValueCache = oldWriteCache;
			_readValueCache = oldReadCache;

			return ret;
		}

		protected virtual Variant GenerateDefaultValue()
		{
			return DefaultValue;
		}

		/// <summary>
		/// Generate the internal value of this data element
		/// </summary>
		/// <returns>Internal value in .NET form</returns>
		protected virtual Variant GenerateInternalValue()
		{
			// 1. Default value
			var value = MutatedValue ?? GenerateDefaultValue();

			// 2. Check for type transformations
			if (MutatedValue != null && mutationFlags.HasFlag(MutateOverride.TypeTransform))
				return MutatedValue;

			// 3. Relations
			if (MutatedValue != null && mutationFlags.HasFlag(MutateOverride.Relations))
				return MutatedValue;

			for (var i = 0; i < relations.Count; ++i)
			{
				// Only interested in "From" relations
				var r = relations[i] as Relation;
				if (r == null || r.From != this)
					continue;

				// CalculateFromValue can return null sometimes
				// when mutations mess up the relation.
				// In that case use the exsiting value for this element.

				var relationValue = r.CalculateFromValue();
				if (relationValue != null)
					value = relationValue;
			}

			// 4. Fixup
			if (MutatedValue != null && mutationFlags.HasFlag(MutateOverride.Fixup))
				return MutatedValue;

			if (_fixup != null)
				value = _fixup.fixup(this);

			return value;
		}

		protected virtual BitwiseStream InternalValueToBitStream()
		{
			var ret = InternalValue;
			if (ret == null)
				return new BitStream();
			return (BitwiseStream)ret;
		}

		/// <summary>
		/// How many times GenerateValue has been called on this element
		/// </summary>
		/// <returns></returns>
		public uint GenerateCount { get; private set; }

		/// <summary>
		/// Generate the final value of this data element
		/// </summary>
		/// <returns></returns>
		protected BitwiseStream GenerateValue()
		{
			++GenerateCount;

			BitwiseStream value = null;

			if (_mutatedValue != null && mutationFlags.HasFlag(MutateOverride.TypeTransform))
			{
				value = (BitwiseStream)_mutatedValue;
			}
			else
			{
				value = InternalValueToBitStream();
			}

			if (_mutatedValue == null || !mutationFlags.HasFlag(MutateOverride.Transformer))
				if (_transformer != null)
					value = _transformer.encode(value);

			value.Name = fullName;

			return value;
		}

		/// <summary>
		/// Fixup for this data element.  Can be null.
		/// </summary>
		public Fixup fixup
		{
			get { return _fixup; }
			set { _fixup = value; }
		}

		/// <summary>
		/// Placement for this data element. Can be null.
		/// </summary>
		public Placement placement
		{
			get { return _placement; }
			set { _placement = value; }
		}

		/// <summary>
		/// Transformer for this data element.  Can be null.
		/// </summary>
		public Transformer transformer
		{
			get { return _transformer; }
			set { _transformer = value; }
		}

		/// <summary>
		/// Relations for this data element.
		/// </summary>
		public RelationContainer relations
		{
			get { return _relations; }
		}

		/// <summary>
		/// Helper fucntion to obtain a bitstream sized for this element
		/// </summary>
		/// <param name="data">Source BitStream</param>
		/// <param name="size">Length of this element</param>
		/// <param name="read">Length of bits already read of this element</param>
		/// <returns>BitStream of length 'size - read'</returns>
		public virtual BitStream ReadSizedData(BitStream data, long? size, long read = 0)
		{
			if (!size.HasValue)
				throw new CrackingFailure("Element is unsized.", this, data);

			if (size.Value < read)
				throw new CrackingFailure("Length is {0} bits but already read {1} bits."
					.Fmt(size.Value, read), this, data);

			var needed = size.Value - read;
			data.WantBytes((needed + 7) / 8);
			var remain = data.LengthBits - data.PositionBits;

			if (needed > remain)
			{
				if (read == 0)
					throw new CrackingFailure("Length is {0} bits but buffer only has {1} bits left."
						.Fmt(size.Value, remain), this, data);

				throw new CrackingFailure("Read {0} of {1} bits but buffer only has {2} bits left."
					.Fmt(read, size.Value, remain), this, data);
			}

			var slice = data.SliceBits(needed);
			Debug.Assert(slice != null);

			var ret = new BitStream();
			slice.CopyTo(ret);
			ret.Seek(0, SeekOrigin.Begin);

			return ret;
		}

		public virtual void ApplyDataFile(DataElement model, BitStream bs)
		{
			var cracker = new DataCracker();
			cracker.CrackData(model, bs);
		}

		#region Parent/Child Helpers

		/// <summary>
		/// Determines whether or not a DataElement is a child of this DataElement.
		/// </summary>
		/// <param name="dataElement">The DataElement to test for a child relationship.</param>
		/// <returns>Returns true if 'dataElement' is a child, false otherwise.</returns>
		public bool isChildOf(DataElement dataElement)
		{
			for (var obj = parent; obj != null; obj = obj.parent)
			{
				if (obj == dataElement)
					return true;
			}

			return false;
		}

		/// <summary>
		/// Check if we are a parent of an element.  This is
		/// true even if we are not the direct parent, but several
		/// layers up.
		/// </summary>
		/// <param name="element">Element to check</param>
		/// <returns>Returns true if we are a parent of element.</returns>
		public bool isParentOf(DataElement element)
		{
			for (var obj = element.parent; obj != null; obj = obj.parent)
			{
				if (obj == this)
					return true;
			}

			return false;
		}

		/// <summary>
		/// Finds the common parent between this elemet
		/// and the target element.
		/// </summary>
		/// <param name="elem">Element to check</param>
		/// <returns>The common parent or null of none exists.</returns>
		public DataElement CommonParent(DataElement elem)
		{
			var parents = new List<DataElement>();

			parents.Add(this);

			var parent = this.parent;
			while (parent != null)
			{
				parents.Add(parent);
				parent = parent.parent;
			}

			if (parents.Contains(elem))
				return elem;

			parent = elem.parent;
			while (parent != null)
			{
				if (parents.Contains(parent))
					return parent;

				parent = parent.parent;
			}

			return null;
		}

		#endregion

		public void UpdateBindings(DataElement oldElem)
		{
			var oldParent = oldElem.parent;
			var newParent = parent;

			oldElem.parent = null;
			parent = null;

			foreach (var elem in oldElem.PreOrderTraverse())
				UpdateBindings(oldElem, elem);

			oldElem.parent = oldParent;
			parent = newParent;
		}

		private void UpdateBindings(DataElement oldElem, DataElement child)
		{
			// Make a copy since we will be modifying relations
			foreach (var rel in child.relations.ToArray())
			{
				// If the child element owns this relation, just remove the binding
				if (rel.From == child)
				{
					rel.Clear();
				}
				else if (!rel.From.isChildOf(oldElem))
				{
					// The other half of the binding is not a child of oldChild, so attempt fixing

					var other = find(child.fullName);

					if (child == other)
						continue;

					if (other == null)
					{
						// If the other half no longer exists under newChild, reset the relation
						rel.Clear();
					}
					else
					{
						// Fix up the relation to be in the newChild branch of the DOM
						rel.Of = other;
					}
				}
			}
		}

		private DataElement MoveTo(DataElementContainer newParent, int index)
		{
			var oldIndex = parent.IndexOf(this);

			if (parent == newParent)
			{
				if (oldIndex < index)
					index--;

				parent.MoveChild(oldIndex, index);

				return this;
			}

			var newName = newParent.UniqueName(Name);
			var newElem = Clone(newName);

			parent.RemoveAt(oldIndex);
			newParent.Insert(index, newElem);

			return newElem;
		}

		public DataElement MoveBefore(DataElement target)
		{
			var parent = target.parent;
			var offset = parent.IndexOf(target);
			return MoveTo(parent, offset);
		}

		public DataElement MoveAfter(DataElement target)
		{
			var parent = target.parent;
			var offset = parent.IndexOf(target) + 1;
			return MoveTo(parent, offset);
		}

		[ShouldClone]
		private bool ShouldClone(object context)
		{
			var ctx = context as CloneContext;

			// If this element is under the root, clone it.
			return ctx == null || (ctx.root == this || isChildOf(ctx.root));
		}

		[OnCloned]
		private void OnCloned(DataElement original, object context)
		{
			var ctx = context as CloneContext;

			if (ctx != null && ctx.rename.Contains(original))
				_name = ctx.name;
		}
	}
}

// end
