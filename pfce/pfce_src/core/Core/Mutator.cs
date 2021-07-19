


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using Peach.Core.Dom;

namespace Peach.Core
{
	/// <summary>
	/// Base class for Mutators.
	/// </summary>
	/// <remarks>
	/// <para>There are two modes of operation for mutators, state model and data model. State model mutators
	/// will affect the flow of the state model and actions in the state model. Data model mutators will
	/// affect the data produced during the current test case.</para>
	/// 
	/// <para>Your static constructor will correct set the <see cref="affectStateModel"/>
	/// and <see cref="affectDataModel"/> variables.
	/// These static variables determine in which modes your mutator will operate.</para>
	/// 
	/// <para>The operation of <see cref="affectDataModel"/> is as follows:</para>
	/// 
	/// <list type="number">
	/// <item>
	/// During record iterations your supportedDataElement static method
	/// will be called to check if your mutator supports a specific data element. If true 
	/// an instance of your mutator will be created with the original instance of the data element
	/// passed in. For every data element in the model you will be queried and asked and an
	/// instance created for that specific element.
	/// </item>
	/// <item>
	/// During mutation your sequentialMutation or randomMutation methods
	/// will get called with a cloned instance of the original data element. Each call to these
	/// methods will be passed a new cloned instance that can be modified in any way. In face the
	/// entire model could be modified in any way desired.
	/// </item>
	/// </list>
	/// 
	/// <para>The operation of <see cref="affectStateModel"/> is as follows:</para>
	/// 
	/// <list type="number">
	/// <item>
	/// A single instance of your mutator will be created and passed a 
	/// the original state model instance.
	/// </item>
	/// <item>
	/// During mutation your sequentialMutation or randomMutation methods
	/// will get called with a cloned instance of the original state model. Each call to these
	/// methods will be passed a new cloned instance that can be modified in any way.
	/// </item>
	/// <item>
	/// When a changeState action occurs the changeState method is called
	/// to provide an opportunity to modify the state that will be switched to.
	/// </item>
	/// </list>
	/// </remarks>
	public abstract class Mutator : IWeighted, INamed
	{
		#region Obsolete Functions

		[Obsolete("This property is obsolete and has been replaced by the Name property.")]
		public string name { get { return Name; } }

		#endregion

		/// <summary>
		/// Is this mutator able to affect the state model?
		/// </summary>
		/// <remarks>
		/// This static variable should be set through a static constructor in the sub-class.
		/// </remarks>
		public static readonly bool affectStateModel = false;

		/// <summary>
		/// Is this mutator able to affect the data model?
		/// </summary>
		/// <remarks>
		/// This static variable should be set through a static constructor in the sub-class.
		/// </remarks>
		public static readonly bool affectDataModel = true;

		/// <summary>
		/// Instance of current mutation strategy
		/// </summary>
		public MutationStrategy context = null;

		readonly bool hasRelation;
		readonly bool hasFixup;

		protected Mutator(DataElement obj)
		{
			hasRelation = obj.relations.HasOf();
			hasFixup = obj.fixup != null;
		}

		protected Mutator(StateModel obj)
		{
		}

		/// <summary>
		/// The name of the mutator
		/// </summary>
		/// <remarks>
		/// The default implementation of this method returns the class name.
		/// </remarks>
		public virtual string Name
		{
			get { return GetType().Name; }
		}

		/// <summary>
		/// Check to see if DataElement is supported by this mutator.
		/// </summary>
		/// <remarks>
		/// Custom mutators that mutate data models must implement this method. The mutator
		/// will only be used to mutate a specific element when it returns true.
		/// </remarks>
		/// <param name="obj">DataElement to check</param>
		/// <returns>True if object is supported, else False</returns>
		public static bool supportedDataElement(DataElement obj)
		{
			return false;
		}

		/// <summary>
		/// Returns the total number of mutations this mutator is able to perform.
		/// </summary>
		/// <returns>
		/// Returns number of mutations mutator can generate.
		/// </returns>
		public abstract int count
		{
			get;
		}

		/// <summary>
		/// Raw weight this mutator will get chosen in random mutation mode.
		/// The selection weight will also take into account if the element
		/// being mutated has a fixup or relation on it.
		/// </summary>
		/// <remarks>
		/// It is recommended that the base class implementation of this property be used.
		/// </remarks>
		public virtual int weight
		{
			get { return count; }
		}

		/// <summary>
		/// Called to set the mutation to return. Applies only to sequential mutation.
		/// </summary>
		/// <remarks>
		/// This value will range form 0 to count. The same mutation number must always
		/// produce the same mutation form sequencialMutation(). It does not apply to 
		/// randomMutation.
		/// </remarks>
		public abstract uint mutation
		{
			get;
			set;
		}

		/// <summary>
		/// Perform a sequential mutation on a data element
		/// </summary>
		/// <param name="obj">Instance of DataElement for this specific iteration. Each iteration a new instance
		/// of the same object is passed in to be modified.</param>
		/// <remarks>
		/// This method performs mutation algorithm on a specific
		/// data element.
		/// </remarks>
		public abstract void sequentialMutation(DataElement obj);

		/// <summary>
		/// Perform a random mutation on a data element
		/// </summary>
		/// <param name="obj">Instance of DataElement for this specific iteration. Each iteration a new instance
		/// of the same object is passed in to be modified.</param>
		public abstract void randomMutation(DataElement obj);

		/// <summary>
		/// Perform a sequential mutation on state model
		/// </summary>
		/// <seealso cref="!:Peach.Core.Mutator.randomMutation(Peach.Core.Dom.StateModel)"/>
		/// <seealso cref="nextAction"/>
		/// <seealso cref="changeState"/>
		/// <param name="model"></param>
		public virtual void sequentialMutation(Core.Dom.StateModel model)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Perform a random mutation on state model.
		/// </summary>
		/// <remarks>
		/// The StateModel object is NOT duplicated. Changes made to the StateModel will
		/// persist across iterations and affect the entire fuzzing run.
		/// 
		/// These methods are called to indicate the type of mutation type requested by
		/// the strategy. They should cause the mutator to produce this type of test case.
		/// The actual methods that perform the mutation are <c>changeState</c> and <c>changeAction</c>.
		/// </remarks>
		/// <seealso cref="!:Peach.Core.Mutator.sequentialMutation(Peach.Core.Dom.StateModel)"/>
		/// <seealso cref="nextAction"/>
		/// <seealso cref="changeState"/>
		/// <param name="model"></param>
		public virtual void randomMutation(Core.Dom.StateModel model)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Allow changing which state we change to.
		/// </summary>
		/// <param name="currentState">Currently executing state</param>
		/// <param name="currentAction">Currently executing action</param>
		/// <param name="nextState">State we are changing to</param>
		/// <seealso cref="nextAction"/>
		/// <returns>Returns state to start executing</returns>
		public virtual State changeState(State currentState, Dom.Action currentAction, State nextState)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Return the next action to execute.
		/// </summary>
		/// <remarks>
		/// Called to find the next action to execute by the strategy.
		/// </remarks>
		/// <param name="state">Currently executing state</param>
		/// <param name="lastAction">The last action executed or null if first action in state.</param>
		/// <param name="nextAction"></param>
		/// <seealso cref="changeState"/>
		/// <returns>Next action to execute or null if no more actions available.</returns>
		public virtual Dom.Action nextAction(State state, Dom.Action lastAction, Dom.Action nextAction)
		{
			return state.NextAction();
		}

		#region IWeighted Members

		public virtual int SelectionWeight
		{
			get
			{
				// TODO: How do we want to weight down elements with fixups & relations
				if (hasFixup || hasRelation)
					return (int)(Math.Sqrt(weight));

				return weight;
			}
		}

		public int TransformWeight(Func<int, int> how)
		{
			return how(SelectionWeight);
		}

		#endregion

		protected static string getHint(DataElement obj, string name)
		{
			Hint h;

			if (!obj.Hints.TryGetValue(name, out h))
				return null;

			return h.Value;
		}

		protected static bool getTypeTransformHint(DataElement obj)
		{
			var name = "Peach.TypeTransform";
			var value = getHint(obj, name);

			// Defaults to supporting type transforms
			if (string.IsNullOrEmpty(value))
				return true;

			bool ret;

			if (!bool.TryParse(value.ToLower(), out ret))
				throw new PeachException("{0} hint '{1}' has invalid value '{2}'.".Fmt(obj.debugName, name, value));
			
			return ret;
		}

		protected bool getN(DataElement obj, out uint n)
		{
			return getN(obj, GetType().Name, out n);
		}

		protected bool getN(DataElement obj, string prefix, out uint n)
		{
			var name = prefix + "-N";
			var value = getHint(obj, name);

			if (string.IsNullOrEmpty(value))
			{
				n = 0;
				return false;
			}

			if (!uint.TryParse(value, out n))
				throw new PeachException("{0} hint '{1}' has invalid value '{2}'.".Fmt(obj.debugName, name, value));

			return true;
		}

		protected uint getN(DataElement obj, uint defaultValue)
		{
			uint ret;
			if (!getN(obj, out ret))
				ret = defaultValue;
			return ret;
		}
	}
}
