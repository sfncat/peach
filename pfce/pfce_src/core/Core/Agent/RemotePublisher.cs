using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Core.Agent
{
	public class RemotePublisher : Publisher
	{
		private static readonly NLog.Logger ClassLogger = LogManager.GetCurrentClassLogger();

		protected override NLog.Logger Logger { get { return ClassLogger; } }

		private IPublisher _publisher;

		public RemotePublisher()
			: base(new Dictionary<string, Variant>())
		{
		}

		public AgentManager AgentManager
		{
			get;
			set;
		}

		public string Agent
		{
			get;
			set;
		}

		public string Class
		{
			get;
			set;
		}

		public Dictionary<string, string> Args
		{
			get;
			set;
		}

		protected override void OnStart()
		{
			_publisher = AgentManager.CreatePublisher(Agent, Name, Class, Args);
		}

		protected override void OnStop()
		{
			if (_publisher != null)
			{
				_publisher.Dispose();
				_publisher = null;
			}
		}

		protected override void OnOpen()
		{
			_publisher.Open(Iteration, IsControlIteration, IsControlRecordingIteration, IsIterationAfterFault);
		}

		protected override void OnClose()
		{
			_publisher.Close();
		}

		protected override void OnAccept()
		{
			_publisher.Accept();
		}

		protected override Variant OnCall(string method, List<BitwiseStream> args)
		{
			return _publisher.Call(method, args);
		}

		protected override void OnSetProperty(string property, Variant value)
		{
			_publisher.SetProperty(property, value);
		}

		protected override Variant OnGetProperty(string property)
		{
			return _publisher.GetProperty(property);
		}

		protected override void OnOutput(BitwiseStream data)
		{
			_publisher.Output(data);
		}

		protected override void OnInput()
		{
			_publisher.Input();
		}

		public override void WantBytes(long count)
		{
			_publisher.WantBytes(count);
		}

		#region Input Stream

		public override bool CanRead
		{
			get
			{
				return _publisher.InputStream.CanRead;
			}
		}

		public override bool CanSeek
		{
			get
			{
				return _publisher.InputStream.CanSeek;
			}
		}

		public override long Length
		{
			get
			{
				return _publisher.InputStream.Length;
			}
		}

		public override long Position
		{
			get
			{
				return _publisher.InputStream.Position;
			}
			set
			{
				_publisher.InputStream.Position = value;
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _publisher.InputStream.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return _publisher.InputStream.Seek(offset, origin);
		}

		#endregion
	}
}
