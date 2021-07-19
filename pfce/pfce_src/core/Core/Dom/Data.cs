

// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;
using Peach.Core.Cracker;
using Peach.Core.Dom.XPath;
using Peach.Core.IO;

namespace Peach.Core.Dom
{
	/// <summary>
	/// Interface for Data
	/// </summary>
	public interface Data : INamed
	{
		/// <summary>
		/// Applies the Data to the specified data model
		/// </summary>
		/// <param name="action"></param>
		/// <param name="model"></param>
		void Apply(Action action, DataModel model);

		/// <summary>
		/// Will this data set be ignored by the engine when
		/// looking for a new data set to switch to.
		/// </summary>
		bool Ignore { get; set; }

		string FieldId { get; }

		void WritePit(XmlWriter pit);
	}

	[Serializable]
	public class DataFieldMask : Data
	{
		public DataFieldMask(string selector)
		{
			Name = selector;
			FieldId = null;
			Ignore = true;
		}

		#region Obsolete Functions

		[Obsolete("This property is obsolete and has been replaced by the Name property.")]
		public string name { get { return Name; } }

		#endregion

		public string Name { get; private set; }

		public void Apply(Action action, DataModel model)
		{
			DataElement parent = model;
			foreach (var elem in DataField.EnumerateElements(action, model, Name, true, false))
			{
				var choice = parent as Choice;
				if (choice != null)
					choice.MaskedElements.Add(elem.Name);

				parent = elem;
			}
		}

		public bool Ignore { get; set; }
		public string FieldId { get; private set; }

		public void WritePit(XmlWriter pit)
		{
			throw new NotImplementedException();
		}
	}

	/// <summary>
	/// Data that comes from a file
	/// </summary>
	[Serializable]
	public class DataFile : DataField
	{
		public DataFile(DataSet dataSet, string fileName)
			: base(dataSet)
		{
			Name = "{0}/{1}".Fmt(dataSet.Name, Path.GetFileName(fileName));
			FileName = fileName;

			// If fieldId is omitted, default it to the filename
			if (string.IsNullOrEmpty(FieldId))
				FieldId = Path.GetFileName(FileName);
		}

		public override void Apply(Action action, DataModel model)
		{
			try
			{
				using (var fs = File.OpenRead(FileName))
				{
					Stream strm = fs;

					// If the sample file is < 16Mb, copy it to
					// a MemoryStream to speedup seeking during
					// cracking of lots of choices.
					if (fs.Length < 1024 * 1024 * 16)
					{
						var ms = new MemoryStream();
						fs.CopyTo(ms);
						ms.Seek(0, SeekOrigin.Begin);
						strm = ms;
					}

					model.ApplyDataFile(model, new BitStream(strm));
				}
			}
			catch (CrackingFailure ex)
			{
				throw new PeachException("Error, failed to crack \"{0}\" into \"{1}\": {2}".Fmt(
					FileName, model.fullName, ex.Message
				), ex);
			}
			
			// Apply field values
			base.Apply(action, model);
		}

		public string FileName { get; private set; }

		public override void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement("Data");
			pit.WriteAttributeString("fileName", FileName);
			WritePitInternals(pit);
			pit.WriteEndElement();
		}
	}

	/// <summary>
	/// Data that comes from fields
	/// </summary>
	[Serializable]
	public class DataField : Data
	{
		#region Obsolete Functions

		[Obsolete("This property is obsolete and has been replaced by the Name property.")]
		public string name { get { return Name; } }

		#endregion

		[Serializable]
		public class Field
		{
			public string Name { get; set; }
			public bool IsXpath { get; set; }
			public Variant Value { get; set; }
		}

		[Serializable]
		public class FieldCollection : KeyedCollection<string, Field>
		{
			protected override string GetKeyForItem(Field item)
			{
				return item.Name;
			}
		}

		public DataField(DataSet dataSet)
		{
			Name = dataSet.Name;
			FieldId = dataSet.FieldId;
			Fields = new FieldCollection();
		}

		[NonSerialized]
		private PeachXmlNamespaceResolver _resolver;

		[NonSerialized]
		private PeachXPathNavigator _navigator;

		public string Name { get; protected set; }

		public string FieldId { get; protected set; }

		public FieldCollection Fields { get; protected set; }

		public bool Ignore { get; set; }

		public virtual void Apply(Action action, DataModel model)
		{
			// Examples of valid field names:
			//
			//  1. foo
			//  2. foo.bar
			//  3. foo[N].bar[N].foo
			//

			foreach (var kv in Fields)
			{
				if (kv.IsXpath)
					ApplyXpath(action, model, kv.Name, kv.Value);
				else
					ApplyField(action, model, kv.Name, kv.Value);
			}

			model.evaulateAnalyzers();
		}

		public virtual void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement("Data");

			WritePitInternals(pit);

			pit.WriteEndElement();
		}

		protected void WritePitInternals(XmlWriter pit)
		{
			//if (!string.IsNullOrEmpty(Name))
			//	pit.WriteAttributeString("name", Name);

			if (!string.IsNullOrEmpty(FieldId))
				pit.WriteAttributeString("fieldId", FieldId);

			foreach (var field in Fields)
			{
				pit.WriteStartElement("Field");
				pit.WriteAttributeString("name", field.Name);

				if (field.Value != null)
				{
					switch (field.Value.GetVariantType())
					{
						case Variant.VariantType.BitStream:
						case Variant.VariantType.ByteString:
							pit.WriteAttributeString("valueType", "hex");
							pit.WriteAttributeString("value", field.Value.ToString());
							break;

						default:
							pit.WriteAttributeString("value", field.Value.ToString());
							break;
					}
				}
				else
				{
					pit.WriteAttributeString("value", "");
				}

				pit.WriteEndElement();
			}
		}


		protected void ApplyXpath(Action action, DataModel model, string xpath, Variant value)
		{
			if (_navigator == null)
			{
				_resolver = new PeachXmlNamespaceResolver();
				_navigator = new PeachXPathNavigator(model);
			}

			XPathNodeIterator iter;

			try
			{
				iter = _navigator.Select(xpath, _resolver);
			}
			catch (XPathException ex)
			{
				throw new PeachException("Error, Field contains invalid xpath selector. [{0}]".Fmt(xpath), ex);
			}

			if (!iter.MoveNext())
				throw new SoftException("Error, Field xpath returned no values. [" + xpath + "]");

			do
			{
				var setElement = ((PeachXPathNavigator)iter.Current).CurrentNode as DataElement;
				if (setElement == null)
					continue;

				setElement.DefaultValue = value;
			}
			while (iter.MoveNext());

		}

		protected static void ApplyField(Action action, DataElementContainer model, string field, Variant value)
		{
			var elem = EnumerateElements(action, model, field, false, true).Last();
			if (elem == null)
				return;

			if (elem.parent is Choice && string.IsNullOrEmpty(value.ToString()))
				return;

			if (!(elem is DataElementContainer))
			{
				if (value.GetVariantType() == Variant.VariantType.BitStream)
					((BitwiseStream)value).Seek(0, SeekOrigin.Begin);

				elem.DefaultValue = value;
			}
		}

		internal static IEnumerable<DataElement> EnumerateElements(
			Action action,
			DataElementContainer model, 
			string field,
			bool skipArray,
			bool selectChoice)
		{
			var parts = field.Split('.');
			var container = model;

			foreach (var part in parts)
			{
				var name = part;
				var m = Regex.Match(name, @"(.*)\[(-?\d+)\]$");

				var bestContainer = container ?? model;
				var resolutionError = (
					"Error, action \"{0}.{1}\" unable to resolve field \"{2}\" of \"{3}\" against \"{4}\" ({5}).".Fmt(
						action.parent.Name,
						action.Name,
						part,
						field,
						bestContainer.fullName,
						bestContainer.GetType().Name
					)
				);

				DataElement elem;
				if (m.Success)
				{
					name = m.Groups[1].Value;
					var index = int.Parse(m.Groups[2].Value);

					if (container == null || !container.TryGetValue(name, out elem))
						throw new PeachException(resolutionError);

					var seq = elem as Sequence;
					if (seq == null)
						throw new PeachException(
							"Error, cannot use array index syntax on field name unless target element is an array. Field: {0}".Fmt(field)
						);

					var array = elem as Array;
					if (array != null)
					{
						// Are we disabling this array?
						if (index == -1)
						{
							if (array.minOccurs > 0)
								throw new PeachException(
									"Error, cannot set array to zero elements when minOccurs > 0. Field: {0} Element: {1}".Fmt(field, array.fullName)
								);

							// Mark array as expanded
							array.ExpandTo(0);

							// The field should be applied to a template data model so
							// the array should have never had any elements in it.
							// Only the original element should be set.
							System.Diagnostics.Debug.Assert(array.Count == 0);

							yield return null;
							yield break;
						}

						if (array.maxOccurs != -1 && index > array.maxOccurs)
							throw new PeachException(
								"Error, index larger that maxOccurs.  Field: {0} Element: {1}".Fmt(field, array.fullName)
							);

						// Add elements up to our index
						array.ExpandTo(index + 1);
					}
					else
					{
						if (index < 0)
							throw new PeachException(
								"Error, index must be equal to or greater than 0"
							);
						if (index > seq.Count - 1)
							throw new PeachException(
								"Error, array index greater than the number of elements in sequence"
							);
					}

					elem = seq[index];
					container = elem as DataElementContainer;
				}
				else if (container is Choice)
				{
					var choice = container as Choice;
					if (!choice.choiceElements.TryGetValue(name, out elem))
						throw new PeachException(resolutionError);

					if (selectChoice)
					{
						choice.SelectElement(elem);

						// Selecting a choice element gives us a new element instance
						// to descend from
						elem = choice.SelectedElement;
					}

					container = elem as DataElementContainer;
				}
				else
				{
					if (container == null || !container.TryGetValue(name, out elem))
						throw new PeachException(resolutionError);

					var array = elem as Array;
					if (skipArray && array != null)
					{
						elem = array.OriginalElement;
					}

					container = elem as DataElementContainer;
				}

				yield return elem;
			}
		}
	}
}
