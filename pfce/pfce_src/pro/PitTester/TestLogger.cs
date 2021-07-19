using System;
using System.Collections.Generic;
using System.Linq;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Dom.XPath;

using Action = Peach.Core.Dom.Action;

namespace Peach.Pro.PitTester
{
	public class TestLogger : Logger
	{
		readonly TestData.Test testData;
		readonly List<string> xpathIgnore;
		int index;
		Action action;
		bool verify;
		List<Tuple<Action, DataElement>> ignores;
		RunContext context;

		public delegate void ErrorHandler(string msg);
		public event ErrorHandler Error;

		public bool VerifyDataSets { get { return testData.VerifyDataSets; } }
		public bool ExceptionOccurred { get { return !verify; } }
		public string ActionName { get; private set; }
		const string ErrorFormat = "{0}\n\tAction: {1}\n\tExpected: {2}\n\tActual: {3}";

		public TestLogger(TestData.Test testData, IEnumerable<string> xpathIgnore)
		{
			this.testData = testData;
			this.xpathIgnore = new List<string>(xpathIgnore);
		}

		protected override void Engine_TestStarting(RunContext ctx)
		{
			context = ctx;
		}

		private void FireError(string msg)
		{
			using (new ForegroundColor(ConsoleColor.Red))
				Console.WriteLine(msg);
			if (Error != null)
				Error(msg);
		}

		public T Verify<T>(string publisherName) where T : TestData.Action
		{
			if (!verify)
				return null;

			// Ignore implicit closes that are called at end of state model
			if (action == null)
				return null;

			try
			{
				var errors = new List<string>();

				if (!context.controlIteration)
				{
					return testData.Actions
						.OfType<T>()
						.Where(a => a.ActionName == ActionName && a.PublisherName == publisherName)
						.Skip((int)action.parent.runCount - 1)
						.FirstOrDefault();
				}

				if (index >= testData.Actions.Count)
					throw new SoftException(string.Format("Missing record in test data: {1}: {0}",
						ActionName,
						typeof(T).Name));

				var d = testData.Actions[index++];

				if (d.ActionName != ActionName)
					errors.Add(ErrorFormat.Fmt(
						"Action name mismatch.",
						ActionName,
						ActionName,
						d.ActionName
						)
					);

				if (d.PublisherName != publisherName)
					errors.Add(ErrorFormat.Fmt(
						"Publisher name mismatch.", 
						ActionName,
						publisherName,
						d.PublisherName 
						)
					);

				if (errors.Any())
					FireError(string.Join("\n", errors));

				// Wrong type means we can't cast and we can't keep going
				if (typeof(T) != d.GetType())
					throw new SoftException(ErrorFormat.Fmt(
						"Action type mismatch.",
						ActionName,
						typeof(T).Name,
						d.GetType().Name
						)
					);

				return (T)d;
			}
			catch
			{
				// don't perform anymore verification
				verify = false;

				throw;
			}
		}

		public IEnumerable<Tuple<Action, DataElement>> Ignores
		{
			get { return ignores; }
		}

		protected override void StateModelStarting(RunContext context, StateModel model)
		{
			// Re-evaluate ignores every only on control record iterations
			if (!context.controlRecordingIteration)
				return;

			ignores = new List<Tuple<Action, DataElement>>();

			var resolver = new PeachXmlNamespaceResolver();
			var navi = new PeachXPathNavigator(model);

			foreach (var item in xpathIgnore)
			{
				var iter = navi.Select(item, resolver);
				if (!iter.MoveNext())
					throw new PeachException("Error, ignore xpath returned no values. [" + item + "]");

				do
				{
					var valueElement = ((PeachXPathNavigator)iter.Current).CurrentNode as DataElement;
					if (valueElement == null)
						throw new PeachException("Error, ignore xpath did not return a Data Element. [" + item + "]");

					// Only track elements that are attached to actions, not free form data models
					var dm = valueElement.root as DataModel;
					if (dm.actionData != null)
						ignores.Add(new Tuple<Action, DataElement>(dm.actionData.action, valueElement));
				}
				while (iter.MoveNext());
			}
		}

		protected override void Engine_TestFinished(RunContext context)
		{
			ignores = null;
		}

		protected override void Engine_IterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
		{
			verify = true;
			index = 0;

			ThePitTester.OnIterationStarting(context, currentIteration, totalIterations);
		}

		protected override void Engine_IterationFinished(RunContext context, uint currentIteration)
		{
			// TODO: Assert we made it all the way through TestData.Actions
			//if (verify && index != testData.Actions.Count)
			//	throw new PeachException("Didn't make it all the way through the expected data");

			// Don't perform anymore verification
			// This prevents publisher stopping that happens
			// after the iteration from causing problems
			verify = false;
		}

		protected override void ActionStarting(RunContext context, Action action)
		{
			this.action = action;

			ActionName = string.Join(".", new[] { action.parent.parent.Name, action.parent.Name, action.Name });
		}

		protected override void ActionFinished(RunContext context, Action action)
		{
			// If the action errored, don't do anymore verification
			if (action.error)
				verify = false;

			this.action = null;

			ActionName = null;
		}
	}
}
