using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace Peach.Core
{
	public abstract class Process : IDisposable
	{
		protected interface IProcess : IDisposable
		{
			int Id { get; }
			int ExitCode { get; }
			bool HasExited { get; }

			StreamWriter StandardInput { get; }
			StreamReader StandardOutput { get; }
			StreamReader StandardError { get; }

			ProcessInfo Snapshot();

			void Terminate();
			void Kill();
			bool WaitForExit(int timeout);
		}

		protected NLog.Logger _logger;
		protected IProcess _process;
		private Task _stdoutTask;
		private Task _stderrTask;

		protected Process(NLog.Logger logger)
		{
			_logger = logger;
		}

		protected abstract IProcess CreateProcess(
			string executable,
			string arguments,
			string workingDirectory,
			Dictionary<string, string> environment);

		protected abstract IProcess AttachProcess(int pid);

		protected ProcessRunResult Run(
			string executable,
			string arguments,
			Dictionary<string, string> environment,
			string workingDirectory,
			int timeout)
		{
			using (var process = CreateProcess(executable, arguments, workingDirectory, environment))
			{
				var prefix = "[{0}] {1}".Fmt(process.Id, Path.GetFileName(executable));
				var stdout = new StringWriter();
				var stderr = new StringWriter();

				// Close stdin so all reads return zero
				process.StandardInput.Close();

				_logger.Trace("[{0}] Run(): start stdout task".Fmt(process.Id));
				var stdoutTask = Task.Factory.StartNew(LoggerTask, new LoggerArgs
				{
					Prefix = prefix + " out",
					Source = process.StandardOutput,
					Sink = stdout,
				}, TaskCreationOptions.LongRunning);

				_logger.Trace("[{0}] Run(): start stderr task".Fmt(process.Id));
				var stderrTask = Task.Factory.StartNew(LoggerTask, new LoggerArgs
				{
					Prefix = prefix + " err",
					Source = process.StandardError,
					Sink = stderr,
				}, TaskCreationOptions.LongRunning);

				var clean = false;
				try
				{
					clean = process.WaitForExit(timeout);
				}
				catch (Exception ex)
				{
					_logger.Warn("[{0}] Run(): Exception in WaitForExit(): {1}", process.Id, ex.Message);
				}

				try
				{
					if (!clean)
					{
						process.Kill();
						process.WaitForExit(timeout);
					}
				}
				catch (Exception ex)
				{
					_logger.Warn("[{0}] Run(): Exception in Kill(): {1}", process.Id, ex.Message);
				}

				try
				{
					if (!Task.WaitAll(new[] { stdoutTask, stderrTask }, timeout))
						clean = false;
				}
				catch (AggregateException ex)
				{
					clean = false;
					_logger.Warn("[{0}] Run(): Exception in stdout/stderr task: {1}", process.Id, ex.InnerException.Message);
				}
				catch (Exception ex)
				{
					clean = false;
					_logger.Warn("[{0}] Run(): Exception in stdout/stderr task: {1}", process.Id, ex.Message);
				}

				var result = new ProcessRunResult
				{
					Pid = process.Id,
					Timeout = !clean,
					ExitCode = clean ? process.ExitCode : -1,
					StdOut = stdout.GetStringBuilder(),
					StdErr = stderr.GetStringBuilder(),
				};

				return result;
			}
		}

		public int Id
		{
			get
			{
				if (_process == null)
					throw new InvalidOperationException("Process has not been started");

				return _process.Id;
			}
		}

		public bool IsRunning
		{
			get { return _process != null && !_process.HasExited; }
		}

		public Action<string> StandardOutput
		{
			get;
			set;
		}

		public Action<string> StandardError
		{
			get;
			set;
		}

		public ProcessInfo Snapshot()
		{
			if (_process == null)
				throw new InvalidOperationException("Process has not been started");

			return _process.Snapshot();
		}

		public void Start(
			string executable, 
			string arguments, 
			Dictionary<string, string> environment,
			string logDir)
		{
			if (IsRunning)
				throw new InvalidOperationException("Process already started");

			_process = CreateProcess(executable, arguments, null, environment);

			TextWriter stdout = null;
			TextWriter stderr = null;
			if (!string.IsNullOrEmpty(logDir) && Directory.Exists(logDir))
			{
				stdout = new StreamWriter(Path.Combine(logDir, "stdout.log"));
				stderr = new StreamWriter(Path.Combine(logDir, "stderr.log"));
			}

			// Close stdin so all reads return zero
			_process.StandardInput.Close();

			_logger.Trace("[{0}] Start(): start stdout task".Fmt(_process.Id));
			_stdoutTask = Task.Factory.StartNew(LoggerTask, new LoggerArgs
			{
				Prefix = "[{0}:out]".Fmt(_process.Id),
				Source = _process.StandardOutput,
				Sink = stdout,
				Callback = StandardOutput
			}, TaskCreationOptions.LongRunning);

			_logger.Trace("[{0}] Start(): start stderr task".Fmt(_process.Id));
			_stderrTask = Task.Factory.StartNew(LoggerTask, new LoggerArgs
			{
				Prefix = "[{0}:err]".Fmt(_process.Id),
				Source = _process.StandardError,
				Sink = stderr,
				Callback = StandardError
			}, TaskCreationOptions.LongRunning);
		}

		public void Attach(int pid)
		{
			if (IsRunning)
				throw new InvalidOperationException("Process already started");

			_process = AttachProcess(pid);
		}

		/// <summary>
		/// Attempt to gracefully stop the process.
		/// If the process does not gracefully stop after timeout milliseconds,
		/// forcibly terminate the process.
		/// </summary>
		/// <param name="timeout"></param>
		public void Stop(int timeout)
		{
			// TODO: Windows doesn't differentiate between SIGTERM and SIGKILL so revisit this

			if (IsRunning)
			{
				_logger.Debug("[{0}] Stop(): SIGTERM", _process.Id);
				try
				{
					_process.Terminate();
				}
				catch (Exception ex)
				{
					_logger.Warn("[{0}] Stop(): Exception sending SIGTERM: {1}", _process.Id, ex.Message);
				}
			}

			if (_stdoutTask != null)
			{
				_logger.Debug("[{0}] Stop(): Wait for stdout/stderr to finish", _process.Id);
				try
				{
					Task.WaitAll(new [] { _stdoutTask, _stderrTask }, timeout);
				}
				catch (AggregateException ex)
				{
					_logger.Warn("[{0}] Stop(): Exception in stdout/stderr task: {1}", _process.Id, ex.InnerException.Message);
				}

				_stdoutTask = null;
				_stderrTask = null;
			}


			if (_process != null)
			{
				_logger.Debug("[{0}] Stop(): WaitForExit({1})", _process.Id, timeout);
				if (!_process.WaitForExit(timeout))
				{
					_logger.Debug("[{0}] Stop(): WaitForExit timed out, killing...", _process.Id);
					_process.Kill();
				}
			}

			Dispose();
		}

		/// <summary>
		/// Closes all open process resources.
		/// If the process was started by us, it will be killed.
		/// If the process was attached, it will continue to run.
		/// </summary>
		public void Dispose()
		{
			if (_process != null)
			{
				var pid = _process.Id;

				_logger.Debug("[{0}] Close(): Closing process", pid);
				_process.Dispose();
				_process = null;
				_logger.Debug("[{0}] Close(): Complete", pid);
			}
		}

		public void Shutdown()
		{
			if (!IsRunning)
				return;

			// TODO:There is a potential race here where the process could terminate after checking IsRunning and before calling Terminate.
			_process.Terminate();
		}

		public bool WaitForExit(int timeout)
		{
			var exited = true;

			if (IsRunning)
			{
				_logger.Debug("[{0}] WaitForExit({1})", _process.Id, timeout);
				exited = _process.WaitForExit(timeout);
			}

			Stop(timeout);

			return exited;
		}

		public void WaitForIdle(int timeout)
		{
			if (IsRunning)
			{
				const int pollInterval = 200;
				ulong lastTime = 0;

				var pid = _process.Id;

				try
				{
					int i;

					for (i = 0; i < timeout; i += pollInterval)
					{
						var pi = _process.Snapshot();

						_logger.Trace("[{0}] WaitForIdle(): OldTicks={1} NewTicks={2}", pid, lastTime, pi.TotalProcessorTicks);

						if (i != 0 && lastTime == pi.TotalProcessorTicks)
						{
							_logger.Debug("[{0}] WaitForIdle(): Cpu is idle, stopping process.", pid);
							break;
						}

						lastTime = pi.TotalProcessorTicks;
						Thread.Sleep(pollInterval);
					}

					if (i >= timeout)
						_logger.Debug("[{0}] WaitForIdle(): Timed out waiting for cpu idle, stopping process.", pid);
				}
				catch (Exception ex)
				{
					if (IsRunning)
						_logger.Debug("[{0}] WaitForIdle(): Error querying cpu time: {1}", pid, ex.Message);
				}
			}

			Stop(timeout);
		}

		struct LoggerArgs
		{
			public string Prefix;
			public StreamReader Source;
			public TextWriter Sink;
			public Action<string> Callback;
		}

		private void LoggerTask(object obj)
		{
			var args = (LoggerArgs)obj;

			try
			{
				using (var reader = args.Source)
				{
					while (!reader.EndOfStream)
					{
						var line = reader.ReadLine();
						if (!string.IsNullOrEmpty(line) && _logger.IsDebugEnabled)
							_logger.Debug("{0} {1}", args.Prefix, line);
						if (args.Sink != null)
							args.Sink.WriteLine(line);
						if (args.Callback != null)
							args.Callback(line);
					}
					_logger.Debug("{0} EOF", args.Prefix);
				}
			}
			finally
			{
				if (args.Sink != null)
				{
					args.Sink.Close();
					args.Sink.Dispose();
				}
			}
		}
	}
}
