namespace Peach.Core.Agent
{
	/// <summary>
	/// Information about the current iteration that is about to be run.
	/// </summary>
	public class IterationStartingArgs
	{
		/// <summary>
		/// Is the current iteration a reproduction of a previously executed iteration.
		/// </summary>
		public bool IsReproduction { get; set; }

		/// <summary>
		/// Did a fault occur on the previous fuzzing iteration.
		/// </summary>
		public bool LastWasFault { get; set; }
	}	
}
