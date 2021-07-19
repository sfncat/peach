
using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceProcess;
using NLog;
using Peach.Core;
using Peach.Core.Agent;
using Monitor = Peach.Core.Agent.Monitor2;
using System.ComponentModel;

namespace Peach.Pro.OS.Windows.Agent.Monitors
{
	[Monitor("WindowsService")]
	[Description("Controls a Windows service")]
	[Parameter("Service", typeof(string), "The name that identifies the service to the system. This can also be the display name for the service.")]
	[Parameter("MachineName", typeof(string), "The computer on which the service resides. (optional, defaults to local machine)", "")]
	[Parameter("FaultOnEarlyExit", typeof(bool), "Fault if service exists early. (defaults to false)", "false")]
	[Parameter("Restart", typeof(bool), "Restart service on every iteration. (defaults to false)", "false")]
	[Parameter("StartTimeout", typeof(int), "Time in minutes to wait for service start. (defaults to 1 minute)", "1")]
	public class WindowsService : Monitor
	{
		static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		public string Service { get; set; }
		public string MachineName { get; set; }
		public bool FaultOnEarlyExit { get; set; }
		public bool Restart { get; set; }
		public int StartTimeout { get; set; }

		ServiceController _sc;
		MonitorData _data;
		TimeSpan _timeout;
		bool _firstRun;

		public WindowsService(string name)
			: base(name)
		{
		}

		public override void StartMonitor(Dictionary<string, string> args)
		{
			string val;
			if (args.TryGetValue("StartTimout", out val) && !args.ContainsKey("StartTimeout"))
			{
				Logger.Info("The parameter 'StartTimout' on the monitor 'WindowsService' is deprecated.  Use the parameter 'StartTimeout' instead.");
				args["StartTimeout"] = val;
				args.Remove("StartTimout");
			}

			base.StartMonitor(args);

			_timeout = TimeSpan.FromMinutes(StartTimeout);

			_sc = string.IsNullOrEmpty(MachineName)
				? new ServiceController(Service)
				: new ServiceController(Service, MachineName);

			try
			{
				_sc.Refresh();
			}
			catch (Exception ex)
			{
				if (string.IsNullOrEmpty(MachineName))
					throw new PeachException("WindowsService monitor was unable to connect to service '{0}'.".Fmt(Service), ex);

				throw new PeachException("WindowsService monitor was unable to connect to service '{0}' on computer '{1}'.".Fmt(Service, MachineName), ex);
			}
		}

		public override void StopMonitor()
		{
			if (_sc != null)
			{
				_sc.Close();
				_sc = null;
			}
		}

		public override void SessionStarting()
		{
			if (Restart)
			{
				_firstRun = false;
			}
			else
			{
				_firstRun = true;
				StartService();
			}
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			_data = null;

			if (_firstRun)
			{
				_firstRun = false;
				return;
			}

			if (Restart)
				StopService();

			StartService();
		}

		public override bool DetectedFault()
		{
			if (_data == null)
			{
				_sc.Refresh();

				if (FaultOnEarlyExit && _sc.Status != ServiceControllerStatus.Running)
				{
					Logger.Info("DetectedFault() - Fault detected, process exited early");

					_data = new MonitorData
					{
						Title = MachineName == null
							? "The windows service '{0}' stopped early.".Fmt(Service)
							: "The windows service '{0}' on machine '{1}' stopped early.".Fmt(Service, MachineName),
						Data = new Dictionary<string, Stream>(),
						Fault = new MonitorData.Info
						{
							MajorHash = Hash(_sc.MachineName + "\\\\" + _sc.ServiceName),
							MinorHash = Hash("ExitedEarly")
						}
					};
				}
			}

			return _data != null;
		}

		public override MonitorData GetMonitorData()
		{
			// Wil be called if a different monitor records a fault
			// so don't assume the service has stopped early.
			return _data;
		}

		private void ControlService(string what, Action action)
		{
			if (MachineName == null)
				Logger.Debug("Attempting to {0} service {1}", what, Service);
			else
				Logger.Debug("Attempting to {0} service {1} on machine {2}", what, Service, MachineName);

			try
			{
				_sc.Refresh();
				action();
			}
			catch (System.ServiceProcess.TimeoutException ex)
			{
				var pe = new PeachException(
					"WindowsService monitor was unable to {0} service '{1}'{2}.".Fmt(
						what,
						Service,
						MachineName == null ? "" : " on machine '{0}'".Fmt(MachineName)),
					ex);

				Logger.Debug(pe.Message);
				throw pe;
			}
		}

		private void StartService()
		{
			ControlService("start", () =>
			{
				switch (_sc.Status)
				{
					case ServiceControllerStatus.ContinuePending:
						break;
					case ServiceControllerStatus.Paused:
						_sc.Continue();
						break;
					case ServiceControllerStatus.PausePending:
						_sc.WaitForStatus(ServiceControllerStatus.Paused, _timeout);
						_sc.Continue();
						break;
					case ServiceControllerStatus.Running:
						return;
					case ServiceControllerStatus.StartPending:
						break;
					case ServiceControllerStatus.Stopped:
						_sc.Start();
						break;
					case ServiceControllerStatus.StopPending:
						_sc.WaitForStatus(ServiceControllerStatus.Stopped, _timeout);
						_sc.Start();
						break;
				}

				_sc.WaitForStatus(ServiceControllerStatus.Running, _timeout);
				OnInternalEvent(new ToggleEventArgs(true));
			});
		}

		protected void StopService()
		{
			ControlService("stop", () =>
			{
				switch (_sc.Status)
				{
					case ServiceControllerStatus.ContinuePending:
						_sc.WaitForStatus(ServiceControllerStatus.Running, _timeout);
						_sc.Stop();
						break;
					case ServiceControllerStatus.Paused:
						_sc.Stop();
						break;
					case ServiceControllerStatus.PausePending:
						_sc.WaitForStatus(ServiceControllerStatus.Paused, _timeout);
						_sc.Stop();
						break;
					case ServiceControllerStatus.Running:
						_sc.Stop();
						break;
					case ServiceControllerStatus.StartPending:
						_sc.WaitForStatus(ServiceControllerStatus.Running, _timeout);
						_sc.Stop();
						break;
					case ServiceControllerStatus.Stopped:
						return;
					case ServiceControllerStatus.StopPending:
						break;
				}

				_sc.WaitForStatus(ServiceControllerStatus.Stopped, _timeout);
				OnInternalEvent(new ToggleEventArgs(false));
			});
		}
	}
}
