


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Peach.Core.Dom;

namespace Peach.Core
{
	/// <summary>
	/// Mutation strategies drive the fuzzing
	/// that Peach performs.  Creating a fuzzing
	/// strategy allows one to fully control which elements
	/// are mutated, by which mutators, and when.
	/// </summary>
	public abstract class MutationStrategy
	{
		protected MutationStrategy(Dictionary<string, Variant> args)
		{
		}

		public virtual void Initialize(RunContext context, Engine engine)
		{
			Context = context;
		}

		public virtual void Finalize(RunContext context, Engine engine)
		{
			Context = null;
		}

		public RunContext Context
		{
			get;
			private set;
		}

		public abstract bool UsesRandomSeed
		{
			get;
		}

		public abstract bool IsDeterministic
		{
			get;
		}

		public abstract uint Count
		{
			get;
		}

		public abstract uint Iteration
		{
			get;
			set;
		}

		public Random Random
		{
			get;
			private set;
		}

		public uint Seed
		{
			get
			{
				return Context.config.randomSeed;
			}
		}

		protected void SeedRandom()
		{
			Random = new Random(Seed + Iteration);
		}

		/// <summary>
		/// Allows mutation strategy to affect state change.
		/// </summary>
		/// <param name="state"></param>
		/// <returns></returns>
		public virtual State MutateChangingState(State state)
		{
			return state;
		}

		/// <summary>
		/// Allows mutation strategy to affect state change.
		/// </summary>
		/// <param name="state"></param>
		/// <param name="lastAction"></param>
		/// <param name="nextAction"></param>
		/// <returns></returns>
		public virtual Dom.Action NextAction(State state, Dom.Action lastAction, Dom.Action nextAction)
		{
			return nextAction;
		}

		/// <summary>
		/// Call supportedDataElement method on Mutator type.
		/// </summary>
		/// <param name="mutator"></param>
		/// <param name="elem"></param>
		/// <returns>Returns true or false</returns>
		protected bool SupportedDataElement(Type mutator, DataElement elem)
		{
			MethodInfo supportedDataElement = mutator.GetMethod("supportedDataElement", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

			object[] args = new object[1];
			args[0] = elem;

			return (bool)supportedDataElement.Invoke(null, args);
		}

		/// <summary>
		/// Call supportedDataElement method on Mutator type.
		/// </summary>
		/// <param name="mutator"></param>
		/// <param name="elem"></param>
		/// <returns>Returns true or false</returns>
		protected bool SupportedState(Type mutator, State elem)
		{
			MethodInfo supportedState = mutator.GetMethod("supportedState", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
			if (supportedState == null)
				return false;

			object[] args = new object[1];
			args[0] = elem;

			return (bool)supportedState.Invoke(null, args);
		}

		protected Mutator GetMutatorInstance(Type t, DataElement obj)
		{
			try
			{
				Mutator mutator = (Mutator)t.GetConstructor(new Type[] { typeof(DataElement) }).Invoke(new object[] { obj });
				mutator.context = this;
				return mutator;
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

		protected Mutator GetMutatorInstance(Type t, StateModel obj)
		{
			try
			{
				Mutator mutator = (Mutator)t.GetConstructor(new Type[] { typeof(StateModel) }).Invoke(new object[] { obj });
				mutator.context = this;
				return mutator;
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

		private static int CompareMutator(Type lhs, Type rhs)
		{
			return string.Compare(lhs.Name, rhs.Name, StringComparison.Ordinal);
		}

		/// <summary>
		/// Enumerate mutators valid to use in this test.
		/// </summary>
		/// <remarks>
		/// Function checks against included/exluded mutators list.
		/// </remarks>
		/// <returns></returns>
		protected IEnumerable<Type> EnumerateValidMutators()
		{
			if (Context.test == null)
				throw new ArgumentException("Error, _context.test == null");

			Func<Type, MutatorAttribute, bool> predicate = (type, attr) =>
			{
				if (Context.test.includedMutators.Count > 0 && !Context.test.includedMutators.Contains(type.Name))
					return false;

				if (Context.test.excludedMutators.Count > 0 && Context.test.excludedMutators.Contains(type.Name))
					return false;

				return true;
			};

			// Different environments enumerate the mutators in different orders.
			// To ensure mutation strategies run mutators in the same order everywhere
			// we have to have a well defined order.
			var ret = ClassLoader.GetAllTypesByAttribute(predicate).ToList();
			ret.Sort(CompareMutator);
			return ret;
		}

		protected void RecursevlyGetElements(DataElementContainer d, List<DataElement> all)
		{
			all.Add(d);

			foreach (DataElement elem in d)
			{
				var cont = elem as DataElementContainer;

				if (cont != null)
					RecursevlyGetElements(cont, all);
				else
					all.Add(elem);
			}
		}
	}

	[AttributeUsage(AttributeTargets.Class, Inherited=false)]
	public class DefaultMutationStrategyAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class MutationStrategyAttribute : PluginAttribute
	{
		[Obsolete("This constructor is obsolete. Use the constructor without isDefault instead.")]
		public MutationStrategyAttribute(string name, bool isDefault)
			: base(typeof(MutationStrategy), name, true)
		{
		}

		public MutationStrategyAttribute(string name)
			: base(typeof(MutationStrategy), name, true)
		{
		}
	}
}

// end
