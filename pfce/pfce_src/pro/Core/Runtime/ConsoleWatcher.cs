

using System;
using System.Diagnostics;
using Peach.Core;
using Peach.Core.Dom;

namespace Peach.Pro.Core.Runtime
{
	public class ConsoleWatcher : Watcher
	{
		private readonly Stopwatch timer = new Stopwatch();
		private uint startIteration;
		private bool reproducing;

		protected override void Engine_ReproFault(RunContext context, uint currentIteration, StateModel stateModel, Fault[] faultData)
		{
			var color = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("\n -- Caught fault at iteration {0}, trying to reproduce --\n", currentIteration);
			Console.ForegroundColor = color;
			reproducing = true;
		}

		protected override void Engine_ReproFailed(RunContext context, uint currentIteration)
		{
			var color = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("\n -- Could not reproduce fault at iteration {0} --\n", currentIteration);
			Console.ForegroundColor = color;
			reproducing = false;
		}

		protected override void Engine_Fault(RunContext context, uint currentIteration, StateModel stateModel, Fault[] faultData)
		{
			var color = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("\n -- {1} fault at iteration {0} --\n", currentIteration, reproducing ? "Reproduced" : "Caught");
			Console.ForegroundColor = color;
			reproducing = false;
		}

		protected override void Engine_HaveCount(RunContext context, uint totalIterations)
		{
			var color = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("\n -- A total of " + totalIterations + " iterations will be performed --\n");
			Console.ForegroundColor = color;
		}

		protected override void Engine_HaveParallel(RunContext context, uint startIteration, uint stopIteration)
		{
			var color = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("\n -- Machine {0} of {1} will run iterations {2} to {3} --\n",
				context.config.parallelNum, context.config.parallelTotal, startIteration, stopIteration);
			Console.ForegroundColor = color;
		}

		protected override void Engine_IterationFinished(RunContext context, uint currentIteration)
		{
		}

		protected override void Engine_IterationStarting(RunContext context, uint currentIteration, uint? totalIterations)
		{
			var controlIteration = "";
			if (context.controlIteration && context.controlRecordingIteration)
				controlIteration = "R";
			else if (context.controlIteration)
				controlIteration = "C";

			var strTotal = "-";
			var strEta = "-";


			if (!timer.IsRunning)
			{
				timer.Start();
				startIteration = currentIteration;
			}

			if (totalIterations != null && totalIterations < uint.MaxValue)
			{
				strTotal = totalIterations.ToString();

				var done = currentIteration - startIteration;
				var total = totalIterations.Value - startIteration + 1;
				var elapsed = timer.ElapsedMilliseconds;
				TimeSpan remain;

				if (done == 0)
				{
					remain = TimeSpan.FromMilliseconds(elapsed * total);
				}
				else
				{
					remain = TimeSpan.FromMilliseconds((total * elapsed / done) - elapsed);
				}

				strEta = remain.ToString("g");
			}


			var color = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write("\n[");
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.Write("{0}{1},{2},{3}", controlIteration, currentIteration, strTotal, strEta);
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write("] ");
			Console.ForegroundColor = ConsoleColor.DarkGreen;
			Console.WriteLine("Performing iteration");
			Console.ForegroundColor = color;
		}

		protected override void Engine_TestError(RunContext context, Exception e)
		{
			Console.Write("\n");
			WriteErrorMark();
			Console.WriteLine("Test '" + context.test.Name + "' error: " + e.Message);
		}

		protected override void Engine_TestWarning(RunContext context, string msg)
		{
			Console.Write("\n");
			WriteErrorMark();
			Console.WriteLine(msg);
		}

		protected override void Engine_TestFinished(RunContext context)
		{
			Console.Write("\n");
			WriteInfoMark();
			Console.WriteLine("Test '" + context.test.Name + "' finished.");
		}

		protected override void Engine_TestStarting(RunContext context)
		{
			Console.Write("\n");

			if (context.config.countOnly)
			{
				WriteInfoMark();
				Console.WriteLine("Calculating total iterations by running single iteration.");
			}

			WriteInfoMark();
			Console.WriteLine("Test '" + context.test.Name + "' starting with random seed " + context.config.randomSeed + ".");
		}

		protected override void DataMutating(RunContext context, ActionData actionData, DataElement element, Mutator mutator)
		{
			WriteInfoMark();
			Console.WriteLine("Fuzzing: {0}", element.fullName);
			WriteInfoMark();
			Console.WriteLine("Mutator: {0}", mutator.Name);
		}

		protected override void StateMutating(RunContext context, State state, Mutator mutator)
		{
			WriteInfoMark();
			Console.WriteLine("Fuzzing State: {0}", state.Name);
			WriteInfoMark();
			Console.WriteLine("Mutator: {0}", mutator.Name);
		}

		public static void WriteInfoMark()
		{
			var foregroundColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write("[");
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write("*");
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write("] ");
			Console.ForegroundColor = foregroundColor;
		}

		public static void WriteErrorMark()
		{
			var foregroundColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write("[");
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write("!");
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write("] ");
			Console.ForegroundColor = foregroundColor;
		}
	}
}

// end