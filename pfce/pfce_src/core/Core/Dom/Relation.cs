


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Xml;

namespace Peach.Core.Dom
{
	/// <summary>
	/// Base class for all data element relations
	/// </summary>
	[Serializable]
	public abstract class Relation : Binding, IPitSerializable
	{
		protected string _expressionGet = null;
		protected string _expressionSet = null;

		public Relation(DataElement parent)
			: base(parent)
		{
		}

		public abstract void WritePit(XmlWriter xml);

		/// <summary>
		/// Expression that is run when getting the value.
		/// </summary>
		/// <remarks>
		/// This expression is only run when the data cracker
		/// has identified a size relation exists and is getting
		/// the value from the "from" side of the relation.
		/// 
		/// The expressionGet will only get executed when direcly calling
		/// the Relation.GetValue() method directly.  It is not called from
		/// DataElement by design.
		/// </remarks>
		public string ExpressionGet
		{
			get { return _expressionGet; }
			set
			{
				if (string.Equals(_expressionGet, value))
					return;

				_expressionGet = value;
				if(From != null)
					From.Invalidate();
			}
		}

		/// <summary>
		/// Expression that is run when setting the value.
		/// </summary>
		/// <remarks>
		/// This expression can be called numerouse times.  It will be
		/// executed any time the attached data element re-generates it's
		/// value (internal or real).
		/// 
		/// The ExpressionSet is executed typically from DataElement.GenerateInteralValue() via
		/// Relation.CalculateFromValue().  As such this expression should limit the amount of
		/// time intensive tasks it performs.
		/// </remarks>
		public string ExpressionSet
		{
			get { return _expressionSet; }
			set
			{
				if (string.Equals(_expressionSet, value))
					return;

				_expressionSet = value;
				if (From != null)
					From.Invalidate();
			}
		}

		/// <summary>
		/// Calculate the new From value based on Of
		/// </summary>
		/// <remarks>
		/// This method is called every time our attached DataElement re-generates it's
		/// value by calling DataElement.GenerateInteralValue().
		/// </remarks>
		/// <returns></returns>
		public abstract Variant CalculateFromValue();

		/// <summary>
		/// Get value from our "from" side.
		/// </summary>
		/// <remarks>
		/// Gets the value from our "from" side and run it through expressionGet (if set).
		/// This method is only called by the DataCracker and never from DataElement.
		/// </remarks>
		public abstract long GetValue();
	}

	/// <summary>
	/// Used to indicate a class is a valid Relation and 
	/// provide it's invoking name used in the Pit XML file.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class RelationAttribute : PluginAttribute
	{
		public RelationAttribute(string name, bool isDefault = false)
			: base(typeof(Relation), name, isDefault)
		{
		}
	}
}

// end
