


// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Linq;

using Peach.Core.Agent;
using Peach.Core.Dom;

using NLog;
using Monitor = System.Threading.Monitor;

namespace Peach.Core
{
	/// <summary>
	/// The main Peach fuzzing engine!
	/// </summary>
	public class Engine
	{
		#region Obsolete Functions

		[Obsolete("This property is obsolete and should not be used.")]
		public RunContext context { get { return _context; } }

		#endregion

		static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();

		private readonly Watcher _watcher;
		private readonly RunContext _context;
		private readonly Thread _currentThread;
		private readonly Timer _timer;
		private int _timerCount;

		private object _timerSync = new object();
		private object _canAbortSync = new object();
		private object _hasAbortedSync = new object();

		//public Dom.Dom dom { get { return runContext.dom; } }
		//public Test test  { get { return runContext.test; } }

		#region Events

		public delegate void TestStartingEventHandler(RunContext context);
		public delegate void IterationStartingEventHandler(RunContext context, uint currentIteration, uint? totalIterations);
		public delegate void IterationFinishedEventHandler(RunContext context, uint currentIteration);
		public delegate void FaultEventHandler(RunContext context, uint currentIteration, StateModel stateModel, Fault[] faultData);
		public delegate void ReproFaultEventHandler(RunContext context, uint currentIteration, StateModel stateModel, Fault[] faultData);
		public delegate void ReproFailedEventHandler(RunContext context, uint currentIteration);
		public delegate void TestFinishedEventHandler(RunContext context);
		public delegate void TestWarningEventHandler(RunContext context, string msg);
		public delegate void TestErrorEventHandler(RunContext context, Exception e);
		public delegate void HaveCountEventHandler(RunContext context, uint totalIterations);
		public delegate void HaveParallelEventHandler(RunContext context, uint startIteration, uint stopIteration);

		/// <summary>
		/// Fired when a Test is starting.  This could be fired
		/// multiple times after the RunStarting event if the Run
		/// contains multiple Tests.
		/// </summary>
		public event TestStartingEventHandler TestStarting;

		/// <summary>
		/// Fired at the start of each iteration.  This event will
		/// be fired often.
		/// </summary>
		public event IterationStartingEventHandler IterationStarting;

		/// <summary>
		/// Fired at end of each iteration.  This event will be fired often.
		/// </summary>
		public event IterationFinishedEventHandler IterationFinished;

		/// <summary>
		/// Fired when a Fault is detected and the engine starts retrying to reproduce it.
		/// </summary>
		public event ReproFaultEventHandler ReproFault;

		/// <summary>
		/// Fired when a Fault is is unable to be reproduced
		/// </summary>
		public event ReproFailedEventHandler ReproFailed;

		/// <summary>
		/// Fired when a Fault is detected.
		/// </summary>
		public event FaultEventHandler Fault;

		/// <summary>
		/// Fired when a Test is finished.
		/// </summary>
		public event TestFinishedEventHandler TestFinished;

		/// <summary>
		/// Fired when a recoverable warning occurs during a Test.
		/// </summary>
		public event TestWarningEventHandler TestWarning;

		/// <summary>
		/// Fired when we know the count of iterations the Test will take.
		/// </summary>
		public event TestErrorEventHandler TestError;

		/// <summary>
		/// Fired when we know the count of iterations the Test will take.
		/// </summary>
		public event HaveCountEventHandler HaveCount;

		/// <summary>
		/// Fired when we know the range of iterations the parallel Test will take.
		/// </summary>
		public event HaveParallelEventHandler HaveParallel;

		private void OnTestStarting()
		{
			if (TestStarting != null)
				TestStarting(_context);
		}

		private void OnIterationStarting(uint currentIteration, uint? totalIterations)
		{
			if (IterationStarting != null)
				IterationStarting(_context, currentIteration, totalIterations);
		}

		private void OnIterationFinished(uint currentIteration)
		{
			if (IterationFinished != null)
				IterationFinished(_context, currentIteration);
		}

		private void OnFault(uint currentIteration, StateModel stateModel, Fault[] faultData)
		{
			logger.Debug(">> OnFault");

			if (Fault != null)
				Fault(_context, currentIteration, stateModel, faultData);

			logger.Debug("<< OnFault");
		}

		private void OnReproFault(uint currentIteration, StateModel stateModel, Fault[] faultData)
		{
			if (ReproFault != null)
				ReproFault(_context, currentIteration, stateModel, faultData);
		}

		private void OnReproFailed(uint currentIteration)
		{
			if (ReproFailed != null)
				ReproFailed(_context, currentIteration);
		}

		private void OnTestFinished()
		{
			if (TestFinished != null)
				TestFinished(_context);
		}

		private void OnTestError(Exception e)
		{
			if (TestError != null)
				TestError(_context, e);
		}

		private void OnTestWarning(string msg)
		{
			if (TestWarning != null)
				TestWarning(_context, msg);
		}

		private void OnHaveCount(uint totalIterations)
		{
			if (HaveCount != null)
				HaveCount(_context, totalIterations);
		}

		private void OnHaveParallel(uint startIteration, uint stopIteration)
		{
			if (HaveParallel != null)
				HaveParallel(_context, startIteration, stopIteration);
		}

		#endregion

		public Engine(Watcher watcher)
		{
			_currentThread = Thread.CurrentThread;
			_watcher = watcher;
			_context = new RunContext
			{
#pragma warning disable 612
				engine = this
#pragma warning restore 612
			};
			_timer = new Timer(OnTimer);
			_timerCount = 0;
		}

		/// <summary>
		/// Run the default fuzzing run in the specified dom.
		/// </summary>
		/// <param name="dom"></param>
		/// <param name="config"></param>
		public void startFuzzing(Dom.Dom dom, RunConfiguration config)
		{
			if (dom == null)
				throw new ArgumentNullException("dom");
			if (config == null)
				throw new ArgumentNullException("config");

			Test test;

			if (!dom.tests.TryGetValue(config.runName, out test))
				throw new PeachException("Unable to locate test named '" + config.runName + "'.");

			startFuzzing(dom, test, config);
		}

		protected void startFuzzing(Dom.Dom dom, Test test, RunConfiguration config)
		{
			if (dom == null)
				throw new ArgumentNullException("dom");
			if (test == null)
				throw new ArgumentNullException("test");
			if (config == null)
				throw new ArgumentNullException("config");

			_context.config = config;
			_context.test = test;
			dom.context = _context;

			try
			{
				// Initialize any watchers and loggers
				if (_watcher != null)
					_watcher.Initialize(this, _context);

				foreach (var item in test.loggers)
					item.Initialize(this, _context);

				try
				{
					try
					{
						StartTest();

						RunTest();

						if (!_context.continueFuzzing)
							logger.Debug("Stop command received, stopping engine.");
						else
							logger.Debug("All test cases executed, stopping engine.");
					}
					finally
					{
						logger.Trace("finally Enter");
						Monitor.Enter(_hasAbortedSync);
						logger.Trace("finally Lock Acquired");
					}
				}
				catch (Exception ex)
				{
					if (ex.GetBaseException() is ThreadAbortException)
					{
						logger.Debug("Kill command received, stopping engine.");
						logger.Trace("ResetAbort()");
						Thread.ResetAbort();
					}
					else
					{
						logger.Debug("Stopping engine due to {0}.", ex.GetType().Name);
						OnTestError(ex);
						throw;
					}
				}
				finally
				{
					EndTest();
				}
			}
			finally
			{
				logger.Trace("Finalize loggers");
				foreach (var item in test.loggers)
					item.Finalize(this, _context);

				logger.Trace("Finalize watcher");
				if (_watcher != null)
					_watcher.Finalize(this, _context);

				using (var evt = new AutoResetEvent(false))
					_timer.Dispose(evt);

				dom.context = null;
				_context.test = null;
				_context.config = null;
			}
		}

		public void Abort()
		{
			logger.Trace(">>> Abort");

			lock (_canAbortSync)
			{
				if (Monitor.TryEnter(_hasAbortedSync))
				{
					logger.Trace("Abort> Acquired Lock");
					_currentThread.Abort();

					logger.Trace("Abort> Release Lock");
					Monitor.Exit(_hasAbortedSync);
				}

				logger.Trace("Join");
				_currentThread.Join();
			}

			logger.Trace("<<< Abort");
		}

		protected void OnTimer(object arg)
		{
			int cnt;

			lock (_timerSync)
			{
				cnt = ++_timerCount;
			}

			logger.Trace(">> Timer Expired (#{0})", cnt);

			if (cnt == 1)
			{
				logger.Debug("Requested fuzzing duration reached, stopping after this iteration completes");
				_context.continueFuzzing = false;
			}
			else if (cnt == 2)
			{
				logger.Debug("Failed to gracefully stop after {0} seconds, aborting the job", _context.config.AbortTimeout.TotalSeconds);
				Abort();
			}

			logger.Trace("<< Timer Expired");
		}

		protected void StartTest()
		{
		}

		protected void EndTest()
		{
			try
			{
				foreach (var pub in _context.test.publishers)
				{
					try
					{
						pub.stop();
					}
					catch (Exception ex)
					{
						logger.Trace("EndTest: Ignoring exception stopping publisher '{0}': {1}", pub.Name, ex.Message);
					}

					var asRemote = pub as RemotePublisher;
					if (asRemote != null)
						asRemote.AgentManager = null;
				}
			}
			finally
			{
				// This finally clause needs to be here so that agent shutdown
				// happens when the engine is stopped...

				logger.Debug("EndTest: Stopping all agents and monitors");

				_context.agentManager.Dispose();
				_context.agentManager = null;

				_context.test.strategy.Finalize(_context, this);

				OnTestFinished();
			}
		}

		/// <summary>
		/// Run a test case.  Contains main fuzzing loop.
		/// </summary>
		protected void RunTest()
		{
			var test = _context.test;
			var context = _context;

			context.agentManager = new AgentManager(context);
			context.reproducingFault = false;
			context.reproducingIterationJumpCount = 0;

			if (context.config.userDefinedSeed && !test.strategy.UsesRandomSeed)
			{
				var attr = test.strategy.GetType().GetDefaultAttr<MutationStrategyAttribute>();
				var name = attr != null ? attr.Name : test.strategy.GetType().Name;
				var msg = "The '{0}' mutation strategy does not allow setting the random seed.".Fmt(name);
				OnTestWarning(msg);
			}

			// Get mutation strategy
			MutationStrategy mutationStrategy = test.strategy;
			mutationStrategy.Initialize(context, this);

			uint iterationStart = 1;
			uint iterationStop = uint.MaxValue;
			uint? iterationTotal = null;
			uint lastControlIteration = 0;

			if (!mutationStrategy.IsDeterministic)
			{
				if (context.config.parallel)
					throw new PeachException("parallel is not supported when a non-deterministic mutation strategy is used");
				if (context.config.countOnly)
					throw new PeachException("count is not supported when a non-deterministic mutation strategy is used");
			}

			if (context.config.range)
			{
				if (context.config.parallel)
					throw new PeachException("range is not supported when parallel is used");

				logger.Debug("runTest: context.config.range == true, start: {0}, stop: {1}",
					context.config.rangeStart, context.config.rangeStop);

				iterationStart = context.config.rangeStart;
				iterationStop = context.config.rangeStop;
			}
			else if (context.config.skipToIteration > 1)
			{
				logger.Debug("runTest: context.config.skipToIteration == {0}",
					context.config.skipToIteration);

				iterationStart = context.config.skipToIteration;
			}

			if (context.config.Duration < TimeSpan.MaxValue)
			{
				logger.Debug("runTest: context.config.Duration == {0} ({1} seconds)",
					context.config.Duration, context.config.Duration.TotalSeconds);

				// Fire in Duration Time, and disable periodic signaling
				_timer.Change(context.config.Duration, context.config.AbortTimeout);
			}

			iterationStart = Math.Max(1, iterationStart);

			uint lastReproFault = iterationStart - 1;
			uint iterationCount = iterationStart;
			bool firstRun = true;
			bool controlAfterRepro = false;
			bool reproducedFault = false;
			bool isReplayJump = false;

			// First iteration is always a control/recording iteration
			context.controlIteration = true;
			context.controlRecordingIteration = true;

			// Initialize the current iteration prior to the TestStarting event
			context.currentIteration = iterationStart;

			lock (_canAbortSync)
			{
				OnTestStarting();
			}

			StartAgents();

			while ((firstRun || iterationCount <= iterationStop) && context.continueFuzzing)
			{
				var isFirst = firstRun;

				context.currentIteration = iterationCount;

				firstRun = false;

				// Clear out or iteration based state store
				context.iterationStateStore.Clear();

				// Should we perform a control iteration?
				if (test.controlIteration > 0 && !context.reproducingFault)
				{
					if ((test.controlIteration == 1 || iterationCount % test.controlIteration == 1) && lastControlIteration != iterationCount)
						context.controlIteration = true;
				}

				try
				{
					// Must set iteration 1st as strategy could enable control/record bools
					mutationStrategy.Iteration = iterationCount;

					if (context.controlIteration && context.controlRecordingIteration)
					{
						context.controlRecordingActionsExecuted.Clear();
						context.controlRecordingStatesExecuted.Clear();
					}

					context.controlActionsExecuted.Clear();
					context.controlStatesExecuted.Clear();


					if (context.config.singleIteration && !context.controlIteration)
					{
						logger.Debug("runTest: context.config.singleIteration == true");
						break;
					}

					// Record if the last iteration had a fault
					// Or if we are starting a new replay window and we want the
					// monitors to restart the target environment
					context.FaultOnPreviousIteration = context.faults.Count > 0 || isReplayJump;

					// Make sure we are not hanging on to old faults.
					context.faults.Clear();

					isReplayJump = false;

					// For session lifetime targets, if we faulted on the previous
					// iteration we expect to be running a control iteration to
					// verify the monitors properly restarted the target
					if (context.test.TargetLifetime == Test.Lifetime.Session && context.FaultOnPreviousIteration)
						Debug.Assert(context.controlIteration);

					try
					{
						logger.Debug("runTest: Iteration Starting: {0}, {1} =============================",
							iterationCount, iterationTotal);

						OnIterationStarting(iterationCount, iterationTotal.HasValue ? iterationStop : iterationTotal);

						if (context.controlIteration)
						{
							if (context.controlRecordingIteration)
								logger.Debug("runTest: Performing control recording iteration.");
							else
								logger.Debug("runTest: Performing control iteration.");
						}

						context.agentManager.IterationStarting(context.reproducingFault, context.FaultOnPreviousIteration);

						test.stateModel.Run(context);
					}
					catch (FaultException ex)
					{
						var fe = ex.Fault;

						logger.Debug("runTest: Creating fault from FaultException: {0}", fe.Title);
						var fault = new Fault
						{
							title = fe.Title,
							description = fe.Description,
							detectionSource = fe.DetectionSource ?? "Unknown",
							monitorName = fe.DetectionName ?? "Unknown",
							majorHash = fe.MajorHash,
							minorHash = fe.MinorHash,
							exploitability = fe.Exploitablity ?? "Unknown",
							agentName =  fe.AgentName ?? "Internal",
							type = FaultType.Fault
						};

						context.faults.Add(fault);
					}
					catch (SoftException se)
					{
						// We should just eat SoftExceptions.
						// They indicate we should move to the next
						// iteration.

						if (isFirst)
						{
							logger.Debug("runTest: SoftException on control recording iteration");
							if (se.InnerException != null && string.IsNullOrEmpty(se.Message))
								throw new PeachException(se.InnerException.Message, se);
							throw new PeachException(se.Message, se);
						}
						else if (context.controlRecordingIteration)
						{
							logger.Debug("runTest: SoftException on control recording iteration, saving as fault");
							OnControlFault(se);
						}
						else if (context.controlIteration)
						{
							logger.Debug("runTest: SoftException on control iteration, saving as fault");
							OnControlFault(se);
						}
						else
						{
							logger.Debug("runTest: SoftException, skipping to next iteration");
							logger.Trace(se);
						}
					}
					catch (OutOfMemoryException ex)
					{
						logger.Debug(ex.Message);
						logger.Debug(ex.StackTrace);
						logger.Debug("runTest: Warning: Iteration ended due to out of memory exception.  Continuing to next iteration.");

						throw new SoftException("Out of memory", ex);
					}
					finally
					{
						context.agentManager.IterationFinished();

						OnIterationFinished(iterationCount);
					}

					CollectControlFaults();

					// User can specify a time to wait between iterations
					// we can use that time to better detect faults
					if (context.test.waitTime > 0)
						Thread.Sleep(TimeSpan.FromSeconds(context.test.waitTime));

					if (context.reproducingFault && context.test.faultWaitTime > 0)
					{
						// User can specify a time to wait between iterations
						// when reproducing faults.  We only wait if we are at the end of
						// a reproduction sequence or it is the initial search of the previous
						// ten iterations.
						if (context.reproducingInitialIteration == iterationCount || context.reproducingIterationJumpCount <= 10)
							Thread.Sleep(TimeSpan.FromSeconds(context.test.faultWaitTime));
					}

					var engineFaults = context.faults.Count;

					// Collect any faults that were found
					context.agentManager.CollectFaults();

					// Ensure engine faults are prioritized second to agent faults
					context.faults = context.faults.Skip(engineFaults).Concat(context.faults.Take(engineFaults)).ToList();

					if (context.faults.Count > 0)
					{
						logger.Debug("runTest: detected fault on iteration {0}", iterationCount);

						if (!context.reproducingFault)
						{
							// Set these on the context now so that OnFault handlers
							// can properly reference them.
							context.reproducingInitialIteration = iterationCount;
							context.reproducingIterationJumpCount = 0;
							context.reproducingControlIteration = context.controlIteration;
							context.reproducingControlRecordingIteration = context.controlRecordingIteration;
						}

						foreach (Fault fault in context.faults)
						{
							fault.iteration = iterationCount;
							fault.iterationStart = context.reproducingInitialIteration - context.reproducingIterationJumpCount;
							fault.iterationStop = context.reproducingInitialIteration;
							fault.controlIteration = context.controlIteration;
							fault.controlRecordingIteration = context.controlRecordingIteration;
						}

						if (context.reproducingFault || context.disableReproduction)
						{
							// Notify loggers first
							OnFault(iterationCount, test.stateModel, context.faults.ToArray());

							if (context.controlRecordingIteration && test.TargetLifetime == Test.Lifetime.Iteration)
							{
								logger.Debug("runTest: Fault detected on control record iteration");
								throw new PeachException("Fault detected on control record iteration.");
							}

							// If the lifetime is Iteration and it reproduced on a control iteration
							// we need to stop fuzzing.
							if (context.controlIteration && test.TargetLifetime == Test.Lifetime.Iteration)
							{
								logger.Debug("runTest: Fault detected on control iteration");
								throw new PeachException("Fault detected on control iteration.");
							}

							// If the lifetime is Session the fault was detected on a control iteration
							// and we reproduced it on the very first try, we need to stop fuzzing
							if (context.controlRecordingIteration && test.TargetLifetime == Test.Lifetime.Session && context.reproducingControlIteration && context.reproducingIterationJumpCount == 0)
							{
								logger.Debug("runTest: Fault detected on control recording iteration");
								throw new PeachException("Fault detected on control recording iteration.");
							}


							// If the lifetime is Session the fault was detected on a control iteration
							// and we reproduced it on the very first try, we need to stop fuzzing
							if (context.controlIteration && test.TargetLifetime == Test.Lifetime.Session && context.reproducingControlIteration && context.reproducingIterationJumpCount == 0)
							{
								logger.Debug("runTest: Fault detected on control iteration");
								throw new PeachException("Fault detected on control iteration.");
							}

							// A control iteration after a fault produced a fault so automation
							// did not correctly restart the target
							if (context.controlIterationAfterFault)
							{
								logger.Debug("runTest: Fault detected on control iteration");
								throw new PeachException("Fault detected on control iteration.");
							}

							// Fault reproduced, so skip forward to were we left off.
							lastReproFault = context.reproducingInitialIteration;

							if (context.reproducingControlIteration)
								--lastReproFault;

							if (context.reproducingControlRecordingIteration)
								reproducedFault = true;

							controlAfterRepro = false;
							iterationCount = context.reproducingInitialIteration;
							context.controlIteration = context.reproducingControlIteration;
							context.controlRecordingIteration = context.reproducingControlRecordingIteration;

							context.reproducingFault = false;
							context.reproducingIterationJumpCount = 0;

							if (test.TargetLifetime == Test.Lifetime.Session)
							{
								context.controlIterationAfterFault = true;
							}

							logger.Debug("runTest: Reproduced fault, continuing fuzzing at iteration {0}", iterationCount);
						}
						else
						{
							// Notify loggers first
							OnReproFault(iterationCount, test.stateModel, context.faults.ToArray());

							logger.Debug("runTest: Attempting to reproduce fault.");

							// If target lifetime is session, and the fault did not occur
							// on a control iteration, we need to run a control iteration
							// to ensure automation has properly restarted the target
							if (test.TargetLifetime == Test.Lifetime.Session)
							{
								context.controlIterationAfterFault = !context.controlIteration;
								context.controlIteration = true;
							}

							context.reproducingFault = true;
							controlAfterRepro = false;

							logger.Debug("runTest: replaying iteration " + iterationCount);
						}
					}
					else if (context.reproducingFault)
					{
						if (test.TargetLifetime == Test.Lifetime.Iteration)
						{
							logger.Debug("runTest: Could not reproducing fault.");

							context.reproducingFault = false;
							iterationCount = context.reproducingInitialIteration;

							OnReproFailed(iterationCount);

							context.controlIteration = context.reproducingControlIteration;
							context.controlRecordingIteration = context.reproducingControlRecordingIteration;
							context.reproducingControlIteration = false;
							context.reproducingControlRecordingIteration = false;
						}
						else if (test.TargetLifetime == Test.Lifetime.Session)
						{
							if (context.controlIterationAfterFault)
							{
								// Repro failed on 1st control iteration following fault
								// This means automation worked so now try and reproduce
								// the last test case
								context.controlIterationAfterFault = false;
								context.controlIteration = false;
								--iterationCount;
							}
							else
							if ((context.test.controlIteration == 0 && iterationCount == context.reproducingInitialIteration) ||
								(
									context.controlIteration && iterationCount == context.reproducingInitialIteration && 
								    (context.reproducingControlIteration ^ controlAfterRepro)
								 )
							    )
							{
								var maxJump = Math.Min(test.MaxBackSearch, context.reproducingInitialIteration - lastReproFault - 1);

								if (context.reproducingIterationJumpCount >= maxJump)
								{
									logger.Debug("runTest: Giving up reproducing fault, reached max backsearch.");

									// Even if fault did not reproduce, don't search past that point
									// in the future when reproducing new faults.
									lastReproFault = context.reproducingInitialIteration;

									if (context.reproducingControlIteration)
										--lastReproFault;

									context.reproducingFault = false;
									iterationCount = context.reproducingInitialIteration;
									controlAfterRepro = false;

									OnReproFailed(iterationCount);

									context.controlIteration = context.reproducingControlIteration;
									context.controlRecordingIteration = context.reproducingControlRecordingIteration;
									context.reproducingInitialIteration = 0;
									context.reproducingIterationJumpCount = 0;
									context.reproducingControlIteration = false;
									context.reproducingControlRecordingIteration = false;

								}
								else
								{
									if (context.reproducingIterationJumpCount == 0)
									{
										context.reproducingIterationJumpCount = 10;
										isReplayJump = !context.reproducingControlIteration;
									}
									else
									{
										context.reproducingIterationJumpCount *= 2;
										isReplayJump = true;
									}

									var delta = Math.Min(maxJump, context.reproducingIterationJumpCount);
									context.reproducingIterationJumpCount = delta;
									iterationCount = context.reproducingInitialIteration - delta - 1;
									controlAfterRepro = false;

									if (isReplayJump)
										++iterationCount;

									context.controlIteration = isReplayJump;
									context.controlRecordingIteration = false;

									logger.Debug("runTest: Moving backwards {0} iterations to reproduce fault.", delta);
								}
							}
							else if (context.test.controlIteration > 0 || context.reproducingControlRecordingIteration)
							{
								controlAfterRepro = false;
								context.controlRecordingIteration = false;

								if (context.reproducingIterationJumpCount <= 10)
								{
									// Do control after every one when we are jumping <= 10
									if (!context.controlIteration)
									{
										context.controlIteration = true;

										if (iterationCount == context.reproducingInitialIteration)
											controlAfterRepro = true;
										else
											++iterationCount;
									}
									else
									{
										context.controlIteration = false;
										--iterationCount;
									}
								}
								else if (context.reproducingControlIteration && (iterationCount + 1) == context.reproducingInitialIteration)
								{
									context.controlIteration = true;
									++iterationCount;
								}
								else if (!context.reproducingControlIteration && iterationCount == context.reproducingInitialIteration)
								{
									context.controlIteration = true;
									controlAfterRepro = true;
								}
								else if (context.controlIteration)
								{
									context.controlIteration = false;
									--iterationCount;
								}
							}
							else
							{
								if (context.controlIteration)
									--iterationCount;

								context.controlIteration = false;
								context.controlRecordingIteration = false;
							}
						}
					}

					if (context.faults.Any(f => f.mustStop))
					{
						logger.Debug("runTest: agents say we must stop!");

						throw new PeachException("Error, agent monitor stopped run!");
					}

					if (context.faults.Count > 0 && context.reproducingFault)
						continue;

					// Update our totals and stop based on new count
					if (context.controlIteration && context.controlRecordingIteration && !iterationTotal.HasValue)
					{
						if (context.config.countOnly)
						{
							OnHaveCount(mutationStrategy.Count);
							break;
						}

						iterationTotal = mutationStrategy.Count;
						if (iterationTotal < iterationStop)
							iterationStop = iterationTotal.Value;

						if (context.config.parallel)
						{
							if (iterationTotal < context.config.parallelTotal)
								throw new PeachException(string.Format("Error, {1} parallel machines is greater than the {0} total iterations.", iterationTotal, context.config.parallelTotal));

							var range = Utilities.SliceRange(1, iterationStop, context.config.parallelNum, context.config.parallelTotal);

							iterationStart = range.Item1;
							iterationStop = range.Item2;

							OnHaveParallel(iterationStart, iterationStop);

							if (context.config.skipToIteration > iterationStart)
								iterationStart = context.config.skipToIteration;

							iterationCount = iterationStart;
						}
					}

					// Don't increment the iteration count if we are on a 
					// control iteration
					if (!context.controlIteration)
						++iterationCount;
				}
				finally
				{
					if (!context.reproducingFault)
					{
						if (context.controlIteration)
							lastControlIteration = iterationCount;

						if (reproducedFault)
						{
							reproducedFault = false;
						}
						if (context.controlIterationAfterFault)
						{
							// Inject a control iteration to verify automation
							// resets the target after a fault is detected
							context.controlIterationAfterFault = false;
							context.controlIteration = true;
						}
						else
						{
							context.controlIteration = false;
							context.controlRecordingIteration = false;
						}
					}
				}
			}
		}

		/// <summary>
		/// Start up the agents required for the current test
		/// </summary>
		private void StartAgents()
		{
			var ctx = _context;

			foreach (var agent in ctx.test.agents)
			{
				// Only use agent if on correct platform
				if ((agent.platform & Platform.GetOS()) != Platform.OS.None)
				{
					try
					{
						// AgentConnect will do three things:
						// 1) Connect to the agent
						// 2) Call StartMonitor on each monitor
						// 3) Call SessionStarting on each monitor
						ctx.agentManager.Connect(agent);
					}
					catch (SoftException se)
					{
						throw new PeachException(se.Message, se);
					}
					catch (PeachException)
					{
						throw;
					}
					catch (AgentException ae)
					{
						throw new PeachException("Agent Failure: " + ae.Message, ae);
					}
					catch (Exception ex)
					{
						if (ex.GetBaseException() is ThreadAbortException)
							throw;

						throw new PeachException("General Agent Failure: " + ex.Message, ex);
					}
				}
			}

			foreach (var pub in ctx.test.publishers.OfType<RemotePublisher>())
			{
				pub.AgentManager = ctx.agentManager;
			}
		}

		/// <summary>
		/// If this was a control iteration, verify it againt our origional recording.
		/// </summary>
		private void CollectControlFaults()
		{
			var ctx = _context;

			if (!ctx.controlIteration)
				return;

			if (ctx.controlRecordingIteration)
				return;

			if (ctx.test.nonDeterministicActions)
				return;

			if (ctx.faults.Count > 0)
				return;

			var expected = ctx.controlRecordingActionsExecuted;
			var actual = ctx.controlActionsExecuted;

			int i;

			for (i = 0; i < expected.Count && i < actual.Count; ++i)
			{
				if (expected[i] != actual[i])
					break;
			}

			string diff;
			string major;
			string minor;

			if (i < expected.Count)
			{
				if (i < actual.Count)
				{
					diff = "The {0}{1} executed action is different from what was expexted."
						.Fmt(i + 1, i == 0 ? "st" : (i == 1 ? "nd" : (i == 2 ? "rd" : "th")));

					major = expected[i].parent.Name + ":" + actual[i].parent.Name;
					minor = expected[i].Name + ":" + actual[i].Name;
				}
				else
				{
					diff = "The state model should have executed action '{0}.{1}' but finished instead."
						.Fmt(expected[i].parent.Name, expected[i].Name);

					major = expected[i].parent.Name + ":";
					minor = expected[i].Name + ":";
				}
			}
			else if (i < actual.Count)
			{
				diff = "The state model should have finished but executed action '{0}.{1}' instead."
					.Fmt(actual[i].parent.Name, actual[i].Name);

				major = ":" + actual[i].parent.Name;
				minor = ":" + actual[i].Name;
			}
			else
			{
				return;
			}

			var sb = new StringBuilder();
			sb.AppendLine("The Peach control iteration performed failed to execute same as initial control.");
			sb.AppendLine();
			sb.AppendLine(diff);

			FormatActions(sb, "Expected", ctx.controlRecordingActionsExecuted);
			FormatActions(sb, "Actual", ctx.controlActionsExecuted);

			minor = Monitor2.Hash(major + "," + minor);
			major = Monitor2.Hash(major);

			var desc = sb.ToString();

			logger.Debug(desc);
			OnControlFault(desc, major, minor);
		}

		private static void FormatActions(StringBuilder sb, string heading, IEnumerable<Dom.Action> actions)
		{
			sb.AppendLine();
			sb.AppendLine(heading);
			sb.AppendLine("----------------------------------------");

			// 'ChangeState' is longest action name at 11 characters
			foreach (var action in actions)
			{
				sb.AppendFormat("{0,-11} | {1}.{2}", action.type, action.parent.Name, action.Name);
				sb.AppendLine();
			}
		}

		private void OnControlFault(SoftException se)
		{
			var ex = se.InnerException ?? se;

			const string template =
@"Peach intermittently sends non-fuzzed values to ensure the test target 
is still responding correctly. During one of these check points, Peach 
detected an error.

This usually means the device/software under test:

 1. Crashed or exited during testing
 2. Overwhelmed and could not respond correctly 
 3. In an invalid state and non responsive 
 4. Had just restarted and was unable to process the request 

This can happen during testing when a series of test cases cause the 
target service to misbehave or even crash.

Extended error information:

{0}
";

			var msg = string.Format(template, ex.Message);

			OnControlFault(msg);
		}

		private void OnControlFault(string description, string majorHash = null, string minorHash = null)
		{
			// Don't tell the engine to stop, let the replay logic determine what to do
			// If a fault is detected or reproduced on a control iteration the engine
			// will automatically stop.

			var fault = new Fault
			{
				detectionSource = "ControlIteration",
				iteration = _context.currentIteration,
				controlIteration = _context.controlIteration,
				controlRecordingIteration = _context.controlRecordingIteration,
				title = "Peach Control Iteration Failed",
				description = description,
				folderName = "ControlIteration",
				majorHash = majorHash,
				minorHash = minorHash,
				type = FaultType.Fault
			};

			_context.faults.Add(fault);
		}
	}
}

// end
