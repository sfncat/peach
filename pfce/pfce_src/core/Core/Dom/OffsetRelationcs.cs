


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using NLog;

namespace Peach.Core.Dom
{

	/// <summary>
	/// Byte offset relation
	/// </summary>
	[Serializable]
	[Relation("offset", true)]
	[Description("Byte offset relation")]
	[Parameter("of", typeof(string), "Element used to generate relation value", "")]
	[Parameter("expressionGet", typeof(string), "Scripting expression that is run when getting the value", "")]
	[Parameter("expressionSet", typeof(string), "Scripting expression that is run when setting the value", "")]
	[Parameter("relative", typeof(bool), "Is the offset relative", "false")]
	[Parameter("relativeTo", typeof(string), "Element to compute value relative to", "")]
	public class OffsetRelation : Relation
	{
		[Serializable]
		private class RelativeBinding : Binding
		{
			private readonly OffsetRelation rel;

			public RelativeBinding(OffsetRelation rel, DataElement parent)
				: base(parent)
			{
				this.rel = rel;
			}

			protected override void OnResolve()
			{
				rel.OnRelativeToResolve();
			}
		}

		private static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();

		private readonly Binding commonAncestor;
		private readonly RelativeBinding relativeElement;

		// This is local only state, don't copy to prevent cloned objects
		// from getting into the wrong state if the clone happens
		// when a relation is being evaulated
		[NonSerialized]
		private bool _isRecursing;

		public bool isRelativeOffset
		{
			get;
			set;
		}

		public string relativeTo
		{
			get
			{
				return relativeElement.OfName;
			}
			set
			{
				relativeElement.OfName = value;
			}
		}

		public OffsetRelation(DataElement parent)
			: base(parent)
		{
			commonAncestor = new Binding(parent);
			relativeElement = new RelativeBinding(this, parent);
		}

		public override void WritePit(XmlWriter pit)
		{
			pit.WriteStartElement("Relation");
			pit.WriteAttributeString("type", "offset");
			pit.WriteAttributeString("of", OfName);

			if (ExpressionGet != null)
				pit.WriteAttributeString("expressionGet", ExpressionGet);
			if (ExpressionSet != null)
				pit.WriteAttributeString("expressionSet", ExpressionSet);

			if(isRelativeOffset)
				pit.WriteAttributeString("relative", "true");
			if (relativeTo != null)
				pit.WriteAttributeString("relativeTo", relativeTo);

			pit.WriteEndElement();
		}


		protected override void OnResolve()
		{
			if (!isRelativeOffset)
			{
				// Non-relative offsets are computed from the root
				commonAncestor.Of = From.getRoot();
			}
			else if (string.IsNullOrEmpty(relativeTo))
			{
				// If this is a relative offset but not relativeTo a specific item
				// the offset should be relative to 'From'
				FindCommonParent(From);
			}
		}

		private void FindCommonParent(DataElement from)
		{
			var parent = from.CommonParent(Of);
			if (parent == null)
				throw new PeachException("Error resolving offset relation on {0}, couldn't find common parent between {0} and {1}.".Fmt(From.debugName, from.debugName, Of.debugName));

			commonAncestor.Of = parent;
		}

		private void OnRelativeToResolve()
		{
			FindCommonParent(relativeElement.Of);
		}

		protected override void OnClear()
		{
			commonAncestor.Clear();
			relativeElement.Clear();
		}

		public override long GetValue()
		{
			if (_isRecursing)
				return 0;

			try
			{
				_isRecursing = true;

				var offset = From.DefaultValue;

				if (_expressionGet == null)
					return (long)offset;

				var state = new Dictionary<string, object>
				{
					{ "self", From }
				};

				if (offset.GetVariantType() == Variant.VariantType.ULong)
				{
					state["offset"] = (ulong)offset;
					state["value"] = (ulong)offset;
				}
				else
				{
					state["offset"] = (long)offset;
					state["value"] = (long)offset;
				}

				var value = From.EvalExpression(_expressionGet, state);

				return Convert.ToInt64(value, CultureInfo.InvariantCulture);
			}
			finally
			{
				_isRecursing = false;
			}
		}

		public override Variant CalculateFromValue()
		{
			if (_isRecursing)
				return new Variant(0);

			if (Of == null)
			{
				logger.Error("Error, Of returned null");
				return null;
			}

			_isRecursing = true;

			try
			{
				// calculateOffset can throw PeachException during mutations
				// we will catch and return null;
				var offset = calculateOffset() / 8;

				if (_expressionSet == null)
					return new Variant(offset);

				var state = new Dictionary<string, object>
				{
					{ "self", From },
					{ "offset", offset },
					{ "value", offset }
				};

				var value = From.EvalExpression(_expressionSet, state);

				return Scripting.ToVariant(value);
			}
			catch (PeachException ex)
			{
				logger.Error(ex.Message);
				return null;
			}
			finally
			{
				_isRecursing = false;
			}
		}

		/// <summary>
		/// Caluclate the offset in bytes between two data elements.
		/// </summary>
		/// <returns>Returns the offset in bits between two elements.  Return can be negative.</returns>
		private long calculateOffset()
		{
			Debug.Assert(_isRecursing);
			var where = commonAncestor.Of;
			if (where == null)
				Error("could not locate common ancestor");

			Debug.Assert(where != null);

			var stream = where.Value;

			long fromPosition = 0;
			long toPosition;

			if (isRelativeOffset)
			{
				if (relativeElement.OfName == null)
				{
					if (!stream.TryGetPosition(From.fullName, out fromPosition))
						Error("couldn't locate position of {0}".Fmt(From.debugName));
				}
				else if (relativeElement.Of != null)
				{
					if (relativeElement.Of != where && !stream.TryGetPosition(relativeElement.Of.fullName, out fromPosition))
						Error("could't locate position of {0}".Fmt(relativeElement.Of.debugName));
				}
				else
				{
					Error("could't locate element '{0}'".Fmt(relativeElement.OfName));
				}
			}

			if (!stream.TryGetPosition(Of.fullName, out toPosition))
				Error("could't locate position of {0}".Fmt(Of.debugName));

			return toPosition - fromPosition;
		}

		private void Error(string error)
		{
			var msg = string.Format(
				"Error, unable to calculate offset between {0} and {1}, {2}.",
				From.debugName,
				Of.debugName,
				error);

			throw new PeachException(msg);
		}
	}
}

// end
