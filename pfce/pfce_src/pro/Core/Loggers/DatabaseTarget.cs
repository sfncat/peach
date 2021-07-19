using System;
using NLog;
using NLog.Targets;
using Peach.Pro.Core.Storage;
using Peach.Pro.Core.WebServices.Models;

namespace Peach.Pro.Core.Loggers
{
	class DatabaseTarget : TargetWithLayout
	{
		NodeDatabase _db = new NodeDatabase();
		readonly string _jobId;

		public DatabaseTarget(Guid jobId)
		{
			Name = "DatabaseTarget";
			_jobId = jobId.ToString();
		}

		protected override void Write(LogEventInfo logEvent)
		{
			_db.InsertJobLog(new JobLog
			{
				JobId = _jobId,
				Message = Layout.Render(logEvent),
			});
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing && _db != null)
			{
				_db.Dispose();
				_db = null;
			}
		}
	}
}