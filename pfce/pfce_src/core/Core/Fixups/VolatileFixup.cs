using System;
using System.Collections.Generic;
using Peach.Core.Dom;

namespace Peach.Core.Fixups
{
	/// <summary>
	/// A helper class for fixups that are volatile and need to
	/// be computed every time an action is run.
	/// Any fixup that requires per-iteration state stored in the
	/// RunContext should derive from this class and override OnActionRun.
	/// </summary>
	[Serializable]
	public abstract class VolatileFixup : Fixup
	{
		DataModel _dataModel;
		Variant _defaultValue;
		Exception _lastError;

		protected VolatileFixup(DataElement parent, Dictionary<string, Variant> args, params string[] refs)
			: base(parent, args, refs)
		{
		}

		protected override Variant fixupImpl()
		{
			if (_lastError != null)
				throw new SoftException(_lastError);

			if (_dataModel == null)
			{
				_dataModel = (DataModel)parent.getRoot();
				_dataModel.ActionRun += OnActionRunEvent;
			}

			return _defaultValue ?? parent.DefaultValue;
		}

		/// <summary>
		/// Called before an output/call/setProperty action is run.
		/// Derivations can compute the result of the fixup
		/// from state stored on the run context.
		/// </summary>
		/// <param name="ctx"></param>
		/// <returns></returns>
		protected abstract Variant OnActionRun(RunContext ctx);

		void OnActionRunEvent(RunContext ctx)
		{
			_lastError = null;

			try
			{
				_defaultValue = DoActionRun(ctx);
			}
			catch (Exception ex)
			{
				_lastError = ex;
			}

			parent.Invalidate();
		}

		private Variant DoActionRun(RunContext ctx)
		{
			if (ctx.controlRecordingIteration)
			{
				var dm = parent.root as DataModel;
				if (dm != null && dm.actionData != null)
				{
					// Allow value to be overridden via the stateStore using key:
					// Peach.VolatileOverride.StateName.ActionName.ModelName.Path.To.Element
					var key = "Peach.VolatileOverride.{0}.{1}".Fmt(dm.actionData.outputName, parent.fullName);

					object obj;
					if (ctx.stateStore.TryGetValue(key, out obj))
						return (Variant)obj;
				}
			}

			return OnActionRun(ctx);
		}

		// ReSharper disable once UnusedMember.Local
		// ReSharper disable UnusedParameter.Local

		/// <summary>
		/// This is needed for cloning with the object copier.
		/// </summary>
		/// <param name="original">The original the copy is made from</param>
		/// <param name="context">The clone context</param>
		[OnCloned]
		void OnCloned(Fixup original, object context)
		{
			// The event is not serialized so we need to
			// resubscribe when we are cloned.
			if (_dataModel != null)
				_dataModel.ActionRun += OnActionRunEvent;
		}

		// ReSharper restore UnusedParameter.Local
	}
}
