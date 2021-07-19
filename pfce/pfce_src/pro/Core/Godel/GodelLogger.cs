using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Godel
{
	public class GodelLogger : Logger
	{
		private NamedCollection<GodelContext> Expressions { get; set; }
		private StateModel OriginalStateModel { get; set; }

		GodelContext GetExpr(params string[] names)
		{
			var name = string.Join(".", names);
			GodelContext ret;
			if (Expressions != null && Expressions.TryGetValue(name, out ret))
				return ret;
			return null;
		}

		protected override void Engine_TestStarting(RunContext context)
		{
			var sm = context.test.stateModel as StateModel;
			if (sm == null || sm.godel.Count == 0)
				return;

			// Create a script scope
			var scope = context.test.parent.Python.CreateScope();

			// Pre-compile all the expressions
			foreach (var item in sm.godel)
				item.OnTestStarting(context, scope);

			Expressions = sm.godel;
		}

		protected override void Engine_TestFinished(RunContext context)
		{
			if (Expressions != null)
			{
				foreach (var item in Expressions)
					item.OnTestFinished();
			}

			OriginalStateModel = null;
			Expressions = null;
		}

		protected override void StateModelStarting(RunContext context, Peach.Core.Dom.StateModel model)
		{
			if (Expressions == null)
				return;

			// Keep a copy of the original for 'pre' variable in the post 
			OriginalStateModel = ObjectCopier.Clone((StateModel)model);

			var expr = GetExpr(model.Name);
			if (expr != null)
				expr.Pre(model);
		}

		protected override void StateModelFinished(RunContext context, Peach.Core.Dom.StateModel model)
		{
			var expr = GetExpr(model.Name);
			if (expr != null)
				expr.Post(model, OriginalStateModel);
		}

		protected override void StateStarting(RunContext context, State state)
		{
			var expr = GetExpr(state.parent.Name, state.Name);
			if (expr != null)
				expr.Pre(state);
		}

		protected override void StateFinished(RunContext context, State state)
		{
			var expr = GetExpr(state.parent.Name, state.Name);
			if (expr != null)
			{
				var pre = OriginalStateModel.states[state.Name];
				expr.Post(state, pre);
			}
		}

		protected override void ActionStarting(RunContext context, Action action)
		{
			var expr = GetExpr(action.parent.parent.Name, action.parent.Name, action.Name);
			if (expr != null)
				expr.Pre(action);
		}

		protected override void ActionFinished(RunContext context, Action action)
		{
			var expr = GetExpr(action.parent.parent.Name, action.parent.Name, action.Name);
			if (expr != null)
			{
				var pre = OriginalStateModel.states[action.parent.Name].actions[action.Name];
				expr.Post(action, pre);
			}
		}
	}
}
