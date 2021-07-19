


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Linq;
using Peach.Core.Dom;
using System.Xml;

namespace Peach.Core
{
	[Serializable]
	public abstract class Fixup : IPitSerializable
	{
		protected Dictionary<string, Variant> args;
		protected bool isRecursing = false;
		protected DataElement parent = null;

		protected Dictionary<string, DataElement> elements = null;

		private Dictionary<string, string> refs = new Dictionary<string,string>();

		public void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement("Fixup");

			var fixup = this.GetType()
				.GetAttributes<FixupAttribute>(null)
				.Where(a => a.IsDefault)
				.FirstOrDefault();

			pit.WriteAttributeString("class", fixup.Name);

			foreach(var param in this.GetType().GetAttributes<ParameterAttribute>(null))
			{
				var name = param.name;
				string value = null;

				if (refs.ContainsKey(param.name))
					value = refs[param.name];
				else
				{
					var prop = this.GetType().GetProperty(param.name);
					if (prop == null)
						continue;

					var objValue = prop.GetValue(this, null);

					if (objValue == null)
						continue;

					value = objValue.ToString();
				}

				pit.WriteStartElement("Param");
				pit.WriteAttributeString("name", name);
				pit.WriteAttributeString("value", value);
				pit.WriteEndElement();
			}

			pit.WriteEndElement();
		}

		/// <summary>
		/// Returns mapping of ref key to ref value, eg: ("ref1", "DataModel.Emenent_0")
		/// </summary>
		public IEnumerable<Tuple<string, string>> references
		{
			get
			{
				foreach (var item in refs)
				{
					yield return new Tuple<string, string>(item.Key, item.Value);
				}
			}
		}

		public IEnumerable<DataElement> dependents
		{
			get
			{
				if (elements != null)
				{
					foreach (var kv in elements)
					{
						yield return kv.Value;
					}
				}
			}
		}

		public Fixup(DataElement parent, Dictionary<string, Variant> args, params string[] refs)
		{
			this.parent = parent;
			this.args = args;

			if (!refs.SequenceEqual(refs.Intersect(args.Keys)))
			{
				string msg = string.Format("Error, {0} requires a '{1}' argument!",
					this.GetType().Name,
					string.Join("' AND '", refs));

				throw new PeachException(msg);
			}

			foreach (var item in refs)
				this.refs.Add(item, (string)args[item]);
		}

		public void updateRef(string refKey, string refValue)
		{
			refs[refKey] = refValue;

			if (elements != null)
			{
				DataElement elem;
				if (elements.TryGetValue(refKey, out elem))
					elem.Invalidated -= OnInvalidated;

				elem = parent.find(refValue);
				if (elem == null)
					throw new SoftException(string.Format("{0} could not find ref element '{1}'", this.GetType().Name, refValue));

				elem.Invalidated += new InvalidatedEventHandler(OnInvalidated);
				elements[refKey] = elem;
			}
		}

		/// <summary>
		/// Perform fixup operation
		/// </summary>
		/// <param name="obj">Parent data element</param>
		/// <returns></returns>
		public Variant fixup(DataElement obj)
		{
			if (isRecursing)
				return GetDefaultValue(obj);

			try
			{
				isRecursing = true;
				return doFixupImpl(obj);
			}
			finally
			{
				isRecursing = false;
			}
		}

		/// <summary>
		/// This is the value to use for the data element if the
		/// fixup has been recursivley called.  This can happen when
		/// the fixup references a parent of the element being fixed up.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		protected virtual Variant GetDefaultValue(DataElement obj)
		{
			return obj.DefaultValue;
		}

		private Variant doFixupImpl(DataElement obj)
		{
			System.Diagnostics.Debug.Assert(parent != null);

			if (elements == null)
			{
				elements = new Dictionary<string, DataElement>();

				foreach (var kv in refs)
				{
					var elem = obj.find(kv.Value);
					if (elem == null)
						throw new SoftException(string.Format("{0} could not find ref element '{1}'", this.GetType().Name, kv.Value));

					elem.Invalidated += new InvalidatedEventHandler(OnInvalidated);
					elements.Add(kv.Key, elem);
				}
			}

			return fixupImpl();
		}

		private void OnInvalidated(object sender, EventArgs e)
		{
			parent.Invalidate();
		}

		[OnCloned]
		private void OnCloned(Fixup original, object context)
		{
			if (elements != null)
			{
				foreach (var kv in elements)
				{
					// DataElement.Invalidated is not serialized, so register for a re-subscribe to the event
					kv.Value.Invalidated += new InvalidatedEventHandler(OnInvalidated);
				}
			}

			DataElement.CloneContext ctx = context as DataElement.CloneContext;
			if (ctx != null)
			{
				var toUpdate = new Dictionary<string, string>();

				// Find all ref='xxx' values where the name should be changed to ref='yyy'
				foreach (var kv in original.refs)
				{
					DataElement elem;
					if (original.elements == null || !original.elements.TryGetValue(kv.Key, out elem))
						elem = null;
					else if (elem != ctx.root.getRoot() && !elem.isChildOf(ctx.root.getRoot()))
						continue; // ref'd element was removed by a mutator

					string name = ctx.UpdateRefName(original.parent, elem, kv.Value);
					if (name != kv.Value)
						toUpdate.Add(kv.Key, name);
				}

				foreach (var kv in toUpdate)
				{
					updateRef(kv.Key, kv.Value);
				}
			}
		}

		protected abstract Variant fixupImpl();
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class FixupAttribute : PluginAttribute
	{
		public FixupAttribute(string name, bool isDefault = false)
			: base(typeof(Fixup), name, isDefault)
		{
		}
	}
}

// end
