using System;
using System.Collections.Generic;
using Peach.Core.Dom.XPath;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Linq;
using System.Xml;
using System.Xml.XPath;
using NLog;

namespace Peach.Core.Dom.Actions
{
	public class SlurpElement
	{
		public string State { get; set; }
		public string Action { get; set; }
		public string Element { get; set; }

		public SlurpElement(DataElement element)
		{
			var dataModel = element.root as DataModel;
			State = dataModel.actionData.action.parent.Name;
			Action = dataModel.actionData.action.Name;
			Element = element.fullName;
		}

		public override string ToString()
		{
			return string.Join(".", State, Action, Element);
		}
	}

	public class SlurpDiagnostic
	{
		public Slurp Slurp { get; set; }
		public string SourceXPath { get; set; }
		public string SinkXPath { get; set; }
		public SlurpElement Source { get; set; }
		public List<SlurpElement> Sinks { get; set; }
	}

	public delegate void SlurpDiagnosticHandler(object sender, SlurpDiagnostic data);

	[Action("Slurp")]
	[Serializable]
	public class Slurp : Action
	{
		static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public event SlurpDiagnosticHandler Diagnostic;

		/// <summary>
		/// xpath for selecting set targets during slurp.
		/// </summary>
		/// <remarks>
		/// Can return multiple elements.  All returned elements
		/// will be updated with a new value.
		/// </remarks>
		[XmlAttribute]
		[DefaultValue(null)]
		public string setXpath { get; set; }

		/// <summary>
		/// xpath for selecting value during slurp
		/// </summary>
		/// <remarks>
		/// Must return a single element.
		/// </remarks>
		[XmlAttribute]
		[DefaultValue(null)]
		public string valueXpath { get; set; }

		private readonly List<SlurpElement> _sinks = new List<SlurpElement>(); 

		protected override void OnRun(Publisher publisher, RunContext context)
		{
			var resolver = new PeachXmlNamespaceResolver();
			var navi = new PeachXPathNavigator(parent.parent);
			var iter = Select(navi, resolver, valueXpath, "valueXpath");

			var elems = new List<DataElement>();

			while (iter.MoveNext())
			{
				var valueElement = ((PeachXPathNavigator)iter.Current).CurrentNode as DataElement;
				if (valueElement == null)
					throw new SoftException("Error, slurp valueXpath did not return a Data Element. [" + valueXpath + "]");

				if (valueElement.InScope())
					elems.Add(valueElement);
			}

			if (elems.Count == 0)
				throw new SoftException("Error, slurp valueXpath returned no values. [" + valueXpath + "]");

			if (elems.Count != 1)
				throw new SoftException("Error, slurp valueXpath returned multiple values. [" + valueXpath + "]");


			if (context.controlRecordingIteration)
				EvaluateSinks(navi, resolver, elems[0]);
			else
				UseCachedSinks(elems[0]);
		}

		private void EvaluateSinks(PeachXPathNavigator navi, PeachXmlNamespaceResolver resolver, DataElement source)
		{
			//Console.WriteLine("EvaluateSinks");

			var diagnostic = new SlurpDiagnostic
			{
				Slurp = this,
				SourceXPath = valueXpath,
				SinkXPath = setXpath,
				Sinks = new List<SlurpElement>(),
				Source = new SlurpElement(source)
			};

			var iter = Select(navi, resolver, setXpath, "setXpath");

			if (!iter.MoveNext())
				throw new SoftException("Error, slurp setXpath returned no values. [" + setXpath + "]");

			do
			{
				var setElement = ((PeachXPathNavigator)iter.Current).CurrentNode as DataElement;
				if (setElement == null)
					continue;

				var sink = new SlurpElement(setElement);
				diagnostic.Sinks.Add(sink);
				_sinks.Add(sink);

				logger.Debug("Slurp, setting {0} from {1}", setElement.fullName, source.fullName);
				setElement.DefaultValue = source.DefaultValue;
			}
			while (iter.MoveNext());

			if (Diagnostic != null)
				Diagnostic(this, diagnostic);
		}

		private void UseCachedSinks(DataElement source)
		{
			foreach (var sink in _sinks)
			{
				//Console.WriteLine(sink);

				State state;
				if (!parent.parent.states.TryGetValue(sink.State, out state))
					throw new PeachException("Invalid state in sink: {0}".Fmt(sink));

				Action action;
				if (!state.actions.TryGetValue(sink.Action, out action))
					throw new PeachException("Invalid action in sink: {1}".Fmt(sink));

				var parts = sink.Element.Split('.');

				foreach (var data in action.allData)
				{
					var element = ResolveElement(data.dataModel, parts);
					if (element == null)
						continue;

					logger.Debug("Slurp, setting {0} from {1}", element.fullName, source.fullName);
					element.DefaultValue = source.DefaultValue;
				}
			}
		}

		private DataElement ResolveElement(DataModel model, string[] parts)
		{
			var first = parts.First();
			if (model.Name != first)
				return null;

			var container = model as DataElementContainer;
			var element = container as DataElement;
			foreach (var part in parts.Skip(1))
			{
				container = element as DataElementContainer;
				if (container == null || !container.TryGetValue(part, out element))
					return null;
			}

			return element;
		}

		private XPathNodeIterator Select(PeachXPathNavigator navi, PeachXmlNamespaceResolver resolver, string xpath, string kind)
		{
			try
			{
				return navi.Select(xpath, resolver);
			}
			catch (XPathException ex)
			{
				throw new PeachException("Error, slurp {0} is not a valid xpath selector. [{1}]".Fmt(kind, xpath), ex);
			}
		}

		public override void WritePitBody(XmlWriter pit)
		{
			pit.WriteAttributeString("setXpath", setXpath);
			pit.WriteAttributeString("valueXpath", valueXpath);
		}

	}
}
