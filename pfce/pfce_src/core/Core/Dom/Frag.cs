using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Xml;
using NLog;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.IO;
using Logger = NLog.Logger;

namespace Peach.Core.Dom
{
	[PitParsable("Frag")]
	[DataElement("Frag", DataElementTypes.All, Scope = PluginScope.Beta)]
	[Description("Fragmentation element")]
	[Parameter("name", typeof(string), "Element name", "")]
	[Parameter("fieldId", typeof(string), "Element field ID", "")]
	[Parameter("fragLength", typeof(int), "Fragment size in bytes", "")]
	[Parameter("class", typeof(string), "Fragment extension class", "ByLength")]
	[Parameter("constraint", typeof(string), "Scripting expression that evaluates to true or false", "")]
	[Parameter("payloadOptional", typeof(bool), "Protocol allows for null payload", "false")]
	[Parameter("totalLengthField", typeof(string), "Name of total length field in template model.", "")]
	[Parameter("fragmentLengthField", typeof(string), "Name of fragment length field in template model.", "")]
	[Parameter("fragmentOffsetField", typeof(string), "Name of fragment offset field in template model.", "")]
	[Parameter("fragmentIndexField", typeof(string), "Name of fragment index field in template model.", "")]
	[Parameter("reassembleDataSet", typeof(bool), "Should data set files be reassembled.", "false")]
	[Serializable]
	public class Frag : Block
	{
		protected static NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		public Frag()
			: base()
		{
			InputModel = false;
			Add(new FragSequence("Rendering"));
		}

		public Frag(string name)
			: base(name)
		{
			InputModel = false;
			Add(new FragSequence("Rendering"));
		}

		#region Parameter Properties

		public string Class { get; set; }
		public int FragLength { get; set; }
		public string TotalLengthField { get; set; }
		public string FragmentLengthField { get; set; }
		public string FragmentOffsetField { get; set; }
		public string FragmentIndexField { get; set; }

		#endregion

		/// <summary>
		/// Set by Payload.Invalidated event handler
		/// </summary>
		private bool _payloadInvalidated = false;

		/// <summary>
		/// Set after we have generated fragements at least once
		/// </summary>
		private bool _generatedFragments = false;

		/// <summary>
		/// Instance of our fragement algorithm class. Can be null.
		/// </summary>
		public FragmentAlgorithm FragmentAlg { get; set; }

		/// <summary>
		/// Template to use for fragments
		/// </summary>
		public DataElement Template { get; set; }

		/// <summary>
		/// Acknowledgement to use for fragments
		/// </summary>
		public DataElement Ack { get; set; }

		/// <summary>
		/// Acknowledgement to use for final fragment.
		/// LastAck allows sending a different value for final fragments.
		/// If not provided the Ack model is used.
		/// </summary>
		public DataElement LastAck { get; set; }

		/// <summary>
		/// Negative acknowledgement to use for fragments.
		/// Recieving a nack will trigger retransmission of last fragment a max of 3 times.
		/// </summary>
		public DataElement Nack { get; set; }

		public bool ReassembleDataSet { get; set; }

		public bool InputModel { get; set; }

		public bool PayloadOptional { get; set; }

		public FragSequence Rendering { get { return (FragSequence)this["Rendering"]; } }

		public bool viewPreRendering = true;

		public new static DataElement PitParser(PitParser context, XmlNode node, DataElementContainer parent)
		{
			if (node.Name != "Frag")
				return null;

			var block = Generate<Frag>(node, parent);
			block.parent = parent;

			block.Class = node.getAttr("class", "ByLength");
			block.FragLength = node.getAttr("fragLength", 0);
			block.PayloadOptional = node.getAttr("payloadOptional", false);
			block.TotalLengthField = node.getAttr("totalLengthField", "");
			block.FragmentLengthField = node.getAttr("fragmentLengthField", "");
			block.FragmentOffsetField= node.getAttr("fragmentOffsetField", "");
			block.FragmentIndexField = node.getAttr("fragmentIndexField", "");
			block.ReassembleDataSet = node.getAttr("reassembleDataSet", false);
			block.isMutable = false;

			var type = ClassLoader.FindTypeByAttribute<FragmentAlgorithmAttribute>((t, a) => 0 == string.Compare(a.Name, block.Class, true));
			if (type == null)
				throw new PeachException(
					"Error, Frag element '" + parent.Name + "' has an invalid class attribute '" + block.Class + "'.");

			block.FragmentAlg = (FragmentAlgorithm)Activator.CreateInstance(type);
			block.FragmentAlg.Parent = block;

			context.handleCommonDataElementAttributes(node, block);
			context.handleCommonDataElementChildren(node, block);
			context.handleDataElementContainer(node, block);

			if (!block._childrenDict.ContainsKey("Template"))
				throw new PeachException(string.Format(
					"Error: Frag '{0}' missing child element named 'Template'.",
					block.Name));

			if (!block._childrenDict.ContainsKey("Payload"))
				throw new PeachException(string.Format(
					"Error: Frag '{0}' missing child element named 'Payload'.",
					block.Name));

			var validNames = new List<string> { "Template", "Rendering", "Payload", "Ack", "LastAck", "Nack" };
			var badNames = block._childrenDict.Keys.Where(x => !validNames.Contains(x)).ToList();

			if (badNames.Count != 0)
				throw new PeachException(string.Format(
					"Error: Frag '{0}' element has invalid child element{1} '{2}'.",
					block.Name, block.Count == 1 ? "" : "s", string.Join("', '", badNames)));

			if (!string.IsNullOrEmpty(block.TotalLengthField))
				if (block["Template"].find(block.TotalLengthField) == null)
					throw new PeachException(string.Format(
						"Error, Frag '{0}' element, unable to find totalLengthField '{1}' in template model.",
						block.Name, block.TotalLengthField));

			if (!string.IsNullOrEmpty(block.FragmentLengthField))
				if (block["Template"].find(block.FragmentLengthField) == null)
					throw new PeachException(string.Format(
						"Error, Frag '{0}' element, unable to find fragmentLengthField '{1}' in template model.",
						block.Name, block.FragmentLengthField));

			if (!string.IsNullOrEmpty(block.FragmentOffsetField))
				if (block["Template"].find(block.FragmentOffsetField) == null)
					throw new PeachException(string.Format(
						"Error, Frag '{0}' element, unable to find fragmentOffsetField '{1}' in template model.",
						block.Name, block.FragmentOffsetField));

			if (!string.IsNullOrEmpty(block.FragmentIndexField))
				if (block["Template"].find(block.FragmentIndexField) == null)
					throw new PeachException(string.Format(
						"Error, Frag '{0}' element, unable to find fragmentIndexField '{1}' in template model.",
						block.Name, block.FragmentIndexField));

			return block;
		}

		public override void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement("Frag");

			//pit.WriteAttributeString("name", Name);

			pit.WriteAttributeString("class", Class);
			pit.WriteAttributeString("fragLength", FragLength.ToString(CultureInfo.InvariantCulture));

			if (!string.IsNullOrEmpty(TotalLengthField))
				pit.WriteAttributeString("totalLengthField", TotalLengthField);

			if (!string.IsNullOrEmpty(FragmentLengthField))
				pit.WriteAttributeString("fragmentLengthField", FragmentLengthField);

			if (!string.IsNullOrEmpty(FragmentOffsetField))
				pit.WriteAttributeString("fragmentOffsetField", FragmentOffsetField);

			if (!string.IsNullOrEmpty(FragmentIndexField))
				pit.WriteAttributeString("fragmentIndexField", FragmentIndexField);

			if (ReassembleDataSet)
				pit.WriteAttributeString("reassembleDataSet", ReassembleDataSet.ToString().ToLower());

			WritePitCommonAttributes(pit);
			WritePitCommonChildren(pit);

			var elem = Template ?? this["Template"];
			elem.WritePit(pit);

			this["Payload"].WritePit(pit);

			if (!TryGetValue("Ack", out elem))
				elem = Ack;

			if (elem != null)
				elem.WritePit(pit);

			if (!TryGetValue("LastAck", out elem))
				elem = LastAck;

			if (elem != null)
				elem.WritePit(pit);

			if (!TryGetValue("Nack", out elem))
				elem = Nack;

			if (elem != null)
				elem.WritePit(pit);

			pit.WriteEndElement();
		}

		public override void ApplyDataFile(DataElement model, BitStream bs)
		{
			base.ApplyDataFile(ReassembleDataSet ? model : this["Payload"], bs);
		}

		protected override string GetDisplaySuffix(DataElement child)
		{
			return "";
		}

		public override IEnumerable<DataElement> Children(bool forDisplay = false)
		{
			// Make sure we re-generate if needed
			if (!viewPreRendering)
				GenerateDefaultValue();
			
			return base.Children(forDisplay);
		}

		protected override DataElement GetChild(string name)
		{
			// Make sure we re-generate if needed
			if (!viewPreRendering)
				GenerateDefaultValue();

			return base.GetChild(name);
		}

		protected virtual void OnPayloadInvalidated(object o, EventArgs e)
		{
			_payloadInvalidated = true;
		}

		protected override void OnInsertItem(DataElement item)
		{
			if (item.Name == "Payload")
			{
				var payload = this["Payload"];
				if(payload != null)
					payload.Invalidated -= OnPayloadInvalidated;

				item.Invalidated += OnPayloadInvalidated;
			}

			base.OnInsertItem(item);
		}

		protected override void OnRemoveItem(DataElement item, bool cleanup = true)
		{
			if (item.Name == "Payload")
				item.Invalidated -= OnPayloadInvalidated;

			base.OnRemoveItem(item, cleanup);
		}

		protected override void OnSetItem(DataElement oldItem, DataElement newItem)
		{
			if (oldItem.Name == "Payload")
			{
				oldItem.Invalidated -= OnPayloadInvalidated;
				newItem.Invalidated += OnPayloadInvalidated;
			}

			base.OnSetItem(oldItem, newItem);
		}

		[OnCloned]
		private void OnCloned(Frag original, object context)
		{
			this["Payload"].Invalidated += OnPayloadInvalidated;
		}

		protected override Variant GenerateDefaultValue()
		{
			// On first call re-locate our template
			if (Template == null)
			{
				Template = this["Template"];

				Remove(Template, false);
				Template.parent = this;

				viewPreRendering = false;

				var value = Template.Value;
				Debug.Assert(value != null);

				// Also relocate our ack
				DataElement elem;
				if (TryGetValue("Ack", out elem))
				{
					Ack = elem;
					Remove(elem, false);
					Ack.parent = this;
				}

				// Also relocate our LastAck
				if (TryGetValue("LastAck", out elem))
				{
					LastAck = elem;
					Remove(elem, false);
					LastAck.parent = this;
				}

				// Also relocate our nack
				if (TryGetValue("Nack", out elem))
				{
					Nack = elem;
					Remove(elem, false);
					Nack.parent = this;
				}
			}

			if (!_childrenDict.ContainsKey("Payload") ||
			    !_childrenDict.ContainsKey("Rendering"))
			{
				return new Variant(new byte[] {});
			}

			// Only perform regeneration if payload is invalidated
			if (!InputModel && (_payloadInvalidated || !_generatedFragments))
			{
				_generatedFragments = true;
				_payloadInvalidated = false;

				Logger.Debug("Generating fragments: ", fullName);
				FragmentAlg.Fragment(Template, this["Payload"], this["Rendering"] as Sequence);
			}

			return new Variant(this["Rendering"].Value);
		}

		public override void Crack(DataCracker context, BitStream data, long? size)
		{
			if (Rendering.Count > 0)
			{
				context.Log("Cracking Payload");

				if (FragmentAlg.NeedFragment())
					throw new SoftException("Error, still waiting on fragments prior to reassembly.");

				var reassembledData = FragmentAlg.Reassemble();
				context.CrackData(this["Payload"], reassembledData);
			}
			else
			{
				context.Log("Cracking Fragments");

				var noPayload = false;
				var startPos = data.Position;
				var endPos = startPos;
				var template = Template ?? this["Template"];

				var fragment = template.Clone("Frag_0");
				Rendering.Add(fragment);

				var cracker = context.Clone();
				cracker.CrackData(fragment, data);

				endPos = data.Position;

				var fragDataElement = fragment.find("FragData");
				if (fragDataElement == null && PayloadOptional)
				{
					Logger.Trace("FragData not found, optional payload enabled.");
					noPayload = true;
				}
				else if (fragDataElement == null)
					throw new SoftException("Unable to locate FragData element during infrag action.");

				Logger.Trace("Fragment {3}: pos: {0} length: {1} crack consumed: {2} bytes",
					endPos, data.Length, endPos - startPos, 0);

				if (FragmentAlg.NeedFragment())
					throw new SoftException("Error, still waiting on fragments prior to reassembly.");

				if (noPayload)
					return;

				context.Log("Cracking Payload");
				var reassembledData = FragmentAlg.Reassemble();
				// Must use new data cracker so we treat Payload as the root
				new DataCracker().CrackData(this["Payload"], reassembledData);
			}
		}
	}
}
