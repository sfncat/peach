


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using Peach.Core;
using Peach.Core.Agent;
using Monitor = Peach.Core.Agent.Monitor2;
using System.ComponentModel;

namespace Peach.Pro.OS.OSX.Agent.Monitors
{
	/// <summary>
	/// Monitor will use OS X's built in CrashReporter (similar to watson)
	/// to detect and report crashes.
	/// </summary>
	[Monitor("CrashReporter")]
	[Alias("osx.CrashReporter")]
	[Description("Collect information from crashes detected by OS X System Crash Reporter")]
	[Parameter("ProcessName", typeof(string), "Process name to watch for (defaults to all)", "")]
	public class CrashReporter : Monitor
	{
		private static readonly Regex CrashRegex = new Regex("Saved crash report for (.*)\\[\\d+\\] version .*? to (.*)");
		private static readonly Regex ProcessRegex = new Regex("^Process:\\s+(.*)\\s+\\[\\d+\\]", RegexOptions.Multiline);
		private static readonly Regex ExceptionRegex = new Regex("^Exception Type:\\s+(.*)$", RegexOptions.Multiline);

		private string _lastTime;
		private IntPtr _asl;
		private string[] _crashLogs;

		public string ProcessName { get; set; }

		public CrashReporter(string name)
			: base(name)
		{
		}

		public override void StartMonitor(Dictionary<string, string> args)
		{
			base.StartMonitor(args);

			_asl = asl_new(asl_type.ASL_TYPE_QUERY);
			if (_asl == IntPtr.Zero)
				throw new PeachException("Couldn't open ASL handle.");
		}

		public override void StopMonitor()
		{
			if (_asl != IntPtr.Zero)
			{
				asl_free(_asl);
				_asl = IntPtr.Zero;
			}
		}

		private string[] GetCrashLogs()
		{
			var ret = new List<string>();

			var err = asl_set_query(_asl, ASL_KEY_SENDER, "ReportCrash", asl_query_op.ASL_QUERY_OP_EQUAL);
			if (err != 0)
				throw new Exception();
			
			err = asl_set_query(_asl, ASL_KEY_TIME, _lastTime, asl_query_op.ASL_QUERY_OP_GREATER);
			if (err != 0)
				throw new Exception();
			
			var response = asl_search(IntPtr.Zero, _asl);
			if (response != IntPtr.Zero)
			{
				IntPtr msg;
				
				while (IntPtr.Zero != (msg = aslresponse_next(response)))
				{
					var time = asl_get(msg, "Time");
					var message = asl_get(msg, "Message");
					
					if (time == IntPtr.Zero || message == IntPtr.Zero)
						continue;
					
					//Saved crash report for CrashingProgram\[22774\] version ??? (???) to /path/to/crash.crash
					
					_lastTime = Marshal.PtrToStringAnsi(time);
					var value = Marshal.PtrToStringAnsi(message);

					Debug.Assert(value != null);

					var match = CrashRegex.Match(value);

					if (match.Success)
					{
						if (ProcessName == null || match.Groups[1].Value == ProcessName)
							ret.Add(match.Groups[2].Value);
					}
				}
				
				aslresponse_free(response);
			}
			
			return ret.ToArray();
		}

		public override void IterationStarting(IterationStartingArgs args)
		{
			_crashLogs = null;
		}

		public override bool DetectedFault()
		{
			// Method will get called multiple times
			// we only want to pause the first time.
			if (_crashLogs == null)
			{
				// Wait for CrashReporter to report!
				Thread.Sleep(500);
				_crashLogs = GetCrashLogs();
			}

			return _crashLogs.Length > 0;
		}

		public override MonitorData GetMonitorData()
		{
			if (!DetectedFault())
				return null;

			var title = string.IsNullOrEmpty(ProcessName)
				? "Crash report."
				: "{0} crash report.".Fmt(ProcessName);

			var log = File.ReadAllText(_crashLogs.First());

			var reMajor = ProcessRegex.Match(log);
			var reMinor = ExceptionRegex.Match(log);

			var ret = new MonitorData
			{
				Title = title,
				Fault = new MonitorData.Info
				{
					Description = log,
					MajorHash = Hash(reMajor.Success ? reMajor.Groups[1].Value : title),
					MinorHash = Hash(reMinor.Success ? reMinor.Groups[1].Value : "UNKNOWN"),
				},
				Data = new Dictionary<string, Stream>()
			};

			foreach (var file in _crashLogs)
			{
				var key = Path.GetFileName(file);
				Debug.Assert(key != null);
				ret.Data.Add(key, new MemoryStream(File.ReadAllBytes(file)));
			}

			return ret;
		}

		public override void SessionStarting()
		{
			_lastTime = "0";

			// Skip past any old messages in the log
			GetCrashLogs();
		}

#region ASL P/Invokes
		// ReSharper disable InconsistentNaming
		// ReSharper disable UnusedMember.Local

		private enum asl_type : uint
		{
			ASL_TYPE_MSG = 0,
			ASL_TYPE_QUERY = 1
		};
		
		[Flags]
		private enum asl_query_op : uint
		{
			ASL_QUERY_OP_CASEFOLD      = 0x0010,
			ASL_QUERY_OP_PREFIX        = 0x0020,
			ASL_QUERY_OP_SUFFIX        = 0x0040,
			ASL_QUERY_OP_SUBSTRING     = 0x0060,
			ASL_QUERY_OP_NUMERIC       = 0x0080,
			ASL_QUERY_OP_REGEX         = 0x0100,
			
			ASL_QUERY_OP_EQUAL         = 0x0001,
			ASL_QUERY_OP_GREATER       = 0x0002,
			ASL_QUERY_OP_GREATER_EQUAL = 0x0003,
			ASL_QUERY_OP_LESS          = 0x0004,
			ASL_QUERY_OP_LESS_EQUAL    = 0x0005,
			ASL_QUERY_OP_NOT_EQUAL     = 0x0006,
			ASL_QUERY_OP_TRUE          = 0x0007,
		};
		
		private static string ASL_KEY_TIME        { get { return "Time"; } }          /* Timestamp.  Set automatically */
		private static string ASL_KEY_TIME_NSEC   { get { return "TimeNanoSec"; } }   /* Nanosecond time. */
		private static string ASL_KEY_HOST        { get { return "Host"; } }          /* Sender's address (set by the server). */
		private static string ASL_KEY_SENDER      { get { return "Sender"; } }        /* Sender's identification string.  Default is process name. */
		private static string ASL_KEY_FACILITY    { get { return "Facility"; } }      /* Sender's facility.  Default is "user". */
		private static string ASL_KEY_PID         { get { return "PID"; } }           /* Sending process ID encoded as a string.  Set automatically. */
		private static string ASL_KEY_UID         { get { return "UID"; } }           /* UID that sent the log message (set by the server). */
		private static string ASL_KEY_GID         { get { return "GID"; } }           /* GID that sent the log message (set by the server). */
		private static string ASL_KEY_LEVEL       { get { return "Level"; } }         /* Log level number encoded as a string.  See levels above. */
		private static string ASL_KEY_MSG         { get { return "Message"; } }       /* Message text. */
		private static string ASL_KEY_READ_UID    { get { return "ReadUID"; } }       /* User read access (-1 is any group). */
		private static string ASL_KEY_READ_GID    { get { return "ReadGID"; } }       /* Group read access (-1 is any group). */
		private static string ASL_KEY_EXPIRE_TIME { get { return "ASLExpireTime"; } } /* Expiration time for messages with long TTL. */
		private static string ASL_KEY_MSG_ID      { get { return "ASLMessageID"; } }  /* 64-bit message ID number (set by the server). */
		private static string ASL_KEY_SESSION     { get { return "Session"; } }       /* Session (set by the launchd). */
		private static string ASL_KEY_REF_PID     { get { return "RefPID"; } }        /* Reference PID for messages proxied by launchd */
		private static string ASL_KEY_REF_PROC    { get { return "RefProc"; } }       /* Reference process for messages proxied by launchd */
		
		[DllImport("libc")]
		// int asl_send(aslclient asl, aslmsg msg);
		private static extern IntPtr asl_new(asl_type type);
		
		[DllImport("libc")]
		// void asl_free(aslmsg msg);
		private static extern void asl_free(IntPtr msg);
		
		[DllImport("libc")]
		// int asl_set_query(aslmsg msg, const char *key, const char *value, uint32_t op);
		private static extern int asl_set_query(IntPtr msg, [MarshalAs(UnmanagedType.LPStr)] string key, [MarshalAs(UnmanagedType.LPStr)] string value, asl_query_op op);
		
		[DllImport("libc")]
		// aslresponse asl_search(aslclient asl, aslmsg msg);
		private static extern IntPtr asl_search(IntPtr asl, IntPtr msg);
		
		[DllImport("libc")]
		// void aslresponse_free(aslresponse r);
		private static extern void aslresponse_free(IntPtr r);
		
		[DllImport("libc")]
		// aslmsg aslresponse_next(aslresponse r);
		private static extern IntPtr aslresponse_next(IntPtr r);
		
		[DllImport("libc")]
		//const char *asl_key(aslmsg msg, uint32_t n);
		private static extern IntPtr asl_key(IntPtr msg, uint n);
		
		[DllImport("libc")]
		//const char *asl_get(aslmsg msg, const char *key);
		private static extern IntPtr asl_get(IntPtr msg, [MarshalAs(UnmanagedType.LPTStr)] string key);

		// ReSharper restore UnusedMember.Local
		// ReSharper restore InconsistentNaming
#endregion
	}
}
