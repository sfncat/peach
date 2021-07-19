


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Xml;
using Peach.Core.Dom.XPath;

namespace Peach.Core.Dom
{
	/// <summary>
	/// Mark state model/data models as mutable at runtime.
	/// </summary>
	public class WeightMutable : MarkMutable
	{
		/// <summary>
		/// Name of element to mark as mutable/non-mutable.
		/// </summary>
		[XmlAttribute("weight")]
		public ElementWeight Weight { get; set; }

		public override void Apply(DataElement elem)
		{
			elem.Weight = Weight;
		}
	}

	/// <summary>
	/// Mark state model/data models as mutable at runtime.
	/// </summary>
	public class IncludeMutable : MarkMutable
	{
		public override void Apply(DataElement elem)
		{
			elem.isMutable = true;
		}
	}

	/// <summary>
	/// Mark state model/data models as non-mutable at runtime.
	/// </summary>
	public class ExcludeMutable : MarkMutable
	{
		public override void Apply(DataElement elem)
		{
			elem.isMutable = false;
		}
	}

	/// <summary>
	/// Mark state model/data models as mutable true/false at runtime.
	/// </summary>
	public abstract class MarkMutable
	{
		public abstract void Apply(DataElement elem);

		/// <summary>
		/// Name of element to mark as mutable/non-mutable.
		/// </summary>
		[XmlAttribute("ref")]
		[DefaultValue(null)]
		public string refName { get; set; }

		/// <summary>
		/// Xpath to elements to mark as mutable/non-mutable.
		/// </summary>
		[XmlAttribute("xpath")]
		[DefaultValue(null)]
		public string xpath { get; set; }
	}

	public class SelectWeight : INamed
	{
		[Obsolete("This property is obsolete and has been replaced by the Name property.")]
		public string name { get { return Name; } }

		public string Name { get; set; }

		public ElementWeight Weight { get; set; }
	}

	public class MutatorFilter
	{
		public enum Mode
		{
			[XmlEnum("include")]
			Include,

			[XmlEnum("exclude")]
			Exclude,
		}

		[XmlAttribute]
		public Mode mode { get; set; }

		[PluginElement("class", typeof(Mutator))]
		public List<Mutator> Mutators { get; set; }
	}

	public class AgentRef
	{
		[XmlAttribute("ref")]
		public string refName { get; set; }

		[XmlAttribute("platform")]
		[DefaultValue(Platform.OS.All)]
		public Platform.OS platform { get; set; }
	}

	public interface IStateModelRef
	{
		void WritePit(XmlWriter pit);
	}

	[StateModelRef("StateModel")]
	public class StateModelRef : IStateModelRef
	{
		[XmlAttribute("ref")]
		public string refName { get; set; }

		public virtual void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement("StateModel");
			pit.WriteAttributeString("ref", refName);
			pit.WriteEndElement();
		}
	}

	public class StateModelRefAttribute : PluginAttribute
	{
		public StateModelRefAttribute(string name)
			: base(typeof(IStateModelRef), name, true)
		{
			Scope = PluginScope.Internal;
		}
	}

	/// <summary>
	/// Define a test to run. Currently a test is defined as a combination of a
	/// Template and optionally a Data set. In the future this will expand to include a state model,
	/// defaults for generation, etc.
	/// </summary>
	public class Test : INamed, IOwned<Dom>
	{
		static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		#region Obsolete Functions

		[Obsolete("This property is obsolete and has been replaced by the Name property.")]
		public string name { get { return Name; } }

		#endregion

		/// <summary>
		/// Defines the lifetime of the fuzzing target.
		/// </summary>
		public enum Lifetime
		{
			/// <summary>
			/// The fuzzing target is restarted once per fuzzing session.
			/// </summary>
			[XmlEnum("session")]
			Session,

			/// <summary>
			/// The fuzzing target is restarted once per fuzzing iteration.
			/// </summary>
			[XmlEnum("iteration")]
			Iteration,
		}

		#region Attributes

		/// <summary>
		/// Name of test case.
		/// </summary>
		[XmlAttribute("name")]
		public string Name { get; set; }

		/// <summary>
		/// Description of test case.
		/// </summary>
		[XmlAttribute]
		[DefaultValue(null)]
		public string description { get; set; }

		/// <summary>
		/// Time to wait in seconds between each test case. Value can be fractional
		/// (0.25). Defaults to zero (0).
		/// </summary>
		[XmlAttribute]
		[DefaultValue(0.0)]
		public double waitTime { get; set; }

		/// <summary>
		/// Time to wait in seconds between each iteration when in fault reproduction mode.
		/// This occurs when a fault has been detected and is being verified. Value can
		/// be fractional (0.25). Defaults to two (2) seconds.
		/// </summary>
		/// <remarks>
		/// This value should be large enough to make sure a fault is detected at the correct
		/// iteration.  We only wait this time when verifying a fault was detected.
		/// </remarks>
		[XmlAttribute]
		[DefaultValue(2.0)]
		public double faultWaitTime { get; set; }

		/// <summary>
		/// How often we should perform a control iteration.
		/// </summary>
		[XmlAttribute]
		[DefaultValue(0)]
		public int controlIteration { get; set; }

		/// <summary>
		/// Are action run counts non-deterministic.
		/// </summary>
		[XmlAttribute]
		[DefaultValue(false)]
		public bool nonDeterministicActions { get; set; }

		/// <summary>
		/// The maximum data size to generate for output actions.
		/// </summary>
		[XmlAttribute]
		[DefaultValue(500000000)]
		public ulong maxOutputSize { get; set; }

		/// <summary>
		/// Defines the lifetime of the fuzzing target.
		/// </summary>
		[XmlAttribute("targetLifetime")]
		[DefaultValue("session")]
		public Lifetime TargetLifetime { get; set; }

		/// <summary>
		/// Number of iteration to search backwards trying to reproduce a fault.
		/// </summary>
		/// <remarks>
		/// Many times, especially with network fuzzing, the iteration we detect a fault on is not the
		/// correct iteration, or the fault requires multiple iterations to reproduce.
		/// 
		/// Peach will start reproducing at the current iteration count then start moving backwards
		/// until we locate the iteration causing the crash, or reach our max back search value.
		/// </remarks>
		[XmlAttribute("maxBackSearch")]
		[DefaultValue(80)]
		public uint MaxBackSearch { get; set; }

		#endregion

		[ShouldClone]
		// ReSharper disable once UnusedMember.Local
		// ReSharper disable once UnusedParameter.Local
		private bool ShouldClone(object context)
		{
			// We should not ever get here.  This means
			// some other object is being cloned and has a member
			// that should be marked NonSerialized
			throw new NotSupportedException();
		}

		#region Elements

		[PluginElement("class", typeof(Logger))]
		[DefaultValue(null)]
		public List<Logger> loggers { get; set; }

		[XmlElement("Include", typeof(IncludeMutable))]
		[XmlElement("Exclude", typeof(ExcludeMutable))]
		[XmlElement("Weight", typeof(WeightMutable))]
		[DefaultValue(null)]
		public List<MarkMutable> mutables { get; set; }

		[PluginElement("Strategy", "class", typeof(MutationStrategy))]
		[DefaultValue(null)]
		public MutationStrategy strategy { get; set; }

		#endregion

		#region Schema Elements

		/// <summary>
		/// Currently unused.  Exists for schema generation.
		/// </summary>
		[XmlElement("Mutators")]
		[DefaultValue(null)]
		public MutatorFilter mutators { get; set; }

		/// <summary>
		/// Currently unused.  Exists for schema generation.
		/// </summary>
		[XmlElement("Agent")]
		[DefaultValue(null)]
		public List<AgentRef> agentRef { get; set; }

		/// <summary>
		/// Currently unused.  Exists for schema generation.
		/// </summary>
		[PluginElement(typeof(IStateModelRef))]
		public IStateModelRef stateModelRef { get; set; }

		/// <summary>
		/// Currently unused.  Exists for schema generation.
		/// </summary>
		[PluginElement("class", typeof(Publisher), Named = true)]
		public List<Publisher> pubs { get; set; }

		#endregion

		public Dom parent { get; set; }

		public StateModel stateModel = null;

		[NonSerialized]
		public NamedCollection<Publisher> publishers = new NamedCollection<Publisher>("Pub");

		[NonSerialized]
		public NamedCollection<Agent> agents = new NamedCollection<Agent>();

		[NonSerialized]
		public NamedCollection<SelectWeight> weights = new NamedCollection<SelectWeight>();

		/// <summary>
		/// List of mutators to include in run
		/// </summary>
		/// <remarks>
		/// If exclude is empty, and this collection contains values, then remove all mutators and only
		/// include these.
		/// </remarks>
		public List<string> includedMutators = new List<string>();

		/// <summary>
		/// List of mutators to exclude from run
		/// </summary>
		/// <remarks>
		/// If include is empty then use all mutators excluding those in this list.
		/// </remarks>
		public List<string> excludedMutators = new List<string>();

		public Test()
		{
			waitTime = 0;
			faultWaitTime = 2;
			maxOutputSize = 1073741824; // 1024 * 1024 * 1024 (1Gb)
			TargetLifetime = Lifetime.Session;
			MaxBackSearch = 80; // 10 * 2 * 2 * 2

			loggers = new List<Logger>();
			mutables = new List<MarkMutable>();
			agentRef = new List<AgentRef>();
			pubs = new List<Publisher>();
		}

		public void markMutableElements()
		{
			var nav = new PeachXPathNavigator(stateModel);
			foreach (var item in mutables)
			{
				var nodeIter = nav.Select(item.xpath);
				while (nodeIter.MoveNext())
				{
					var dataElement = ((PeachXPathNavigator)nodeIter.Current).CurrentNode as DataElement;
					if (dataElement == null)
						continue;

					foreach (var elem in dataElement.PreOrderTraverse())
						item.Apply(elem);
				}
			}

			if (weights.Count > 0)
			{
				foreach (var element in stateModel.TuningTraverse())
				{
					SelectWeight item;
					if (weights.TryGetValue(element.Key, out item))
					{
						element.Value.Weight = item.Weight;
					}
					else
					{
						Logger.Trace("Missing weight specification for: {0}", element.Key);
					}
				}
			}

			// disable mutations for elements in a final state
			if (stateModel.finalState != null)
			{
				foreach (var item in 
					from action in stateModel.finalState.actions 
					from actionData in action.outputData 
					from item in actionData.dataModel.PreOrderTraverse() 
					select item)
				{
					item.isMutable = false;
				}
			}
		}

		public void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement("Test");

			pit.WriteAttributeString("name", Name);

			if(!string.IsNullOrEmpty(description))
				pit.WriteAttributeString("description", description);

			if(waitTime != 0.0)
				pit.WriteAttributeString("waitTime", waitTime.ToString(CultureInfo.InvariantCulture));

			if(faultWaitTime != 2.0)
				pit.WriteAttributeString("faultWaitTime", faultWaitTime.ToString(CultureInfo.InvariantCulture));

			if(controlIteration != 0)
				pit.WriteAttributeString("controlIteration", controlIteration.ToString(CultureInfo.InvariantCulture));

			if(nonDeterministicActions)
				pit.WriteAttributeString("nonDeterministicActions", nonDeterministicActions.ToString().ToLower());

			if (maxOutputSize != 500000000)
				pit.WriteAttributeString("maxOutputSize", maxOutputSize.ToString(CultureInfo.InvariantCulture));

			if(TargetLifetime != Lifetime.Session)
				pit.WriteAttributeString("targetLifetime", TargetLifetime.ToString());

			if(MaxBackSearch != 80)
				pit.WriteAttributeString("maxBackSearch", MaxBackSearch.ToString(CultureInfo.InvariantCulture));

			// TODO - Make this work for real
			// Quick hack to make Swagger/Postman analyzers work better
			foreach (var obj in publishers)
			{
				if (obj.GetType().Name != "WebApiPublisher") continue;

				pit.WriteStartElement("Publisher");
				pit.WriteAttributeString("class", "WebApi");
				pit.WriteEndElement();
			}

			//foreach (var obj in weights)
			//	obj.WritePit(pit);

			//foreach (var obj in includedMutators)
			//	obj.WritePit(pit);

			if (agentRef != null)
			{
				foreach (var agent in agentRef)
				{
					pit.WriteStartElement("Agent");
					pit.WriteAttributeString("ref", agent.refName);
					pit.WriteEndElement();
				}
			}

			if (stateModelRef != null)
				stateModelRef.WritePit(pit);

			pit.WriteEndElement();
		}

	}
}
