using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Description;
using Peach.Pro.Core.WebServices;
using Peach.Pro.Core.WebServices.Models;
using Peach.Pro.WebApi2.Utility;
using Swashbuckle.Swagger.Annotations;

namespace Peach.Pro.WebApi2.Controllers
{
	[NoCache]
	[RestrictedApi]
	[RoutePrefix(Prefix)]
	public class NodesController : ApiController
	{
		public const string Prefix = "p/nodes";

		private IWebContext _context;
		private IJobMonitor _jobMonitor;

		public static string MakeUrl(params string[] args)
		{
			return string.Join("/", "", Prefix, string.Join("/", args));
		}

		public NodesController(IWebContext context, IJobMonitor jobMonitor)
		{
			_context = context;
			_jobMonitor = jobMonitor;
		}

		[Route("")]
		public IEnumerable<Node> Get()
		{
			return new[] { MakeNode() };
		}

		[Route("{id}")]
		[ResponseType(typeof(Node))]
		[SwaggerResponse(HttpStatusCode.NotFound, Description = "Specified node does not exist")]
		public IHttpActionResult Get(string id)
		{
			if (id != _context.NodeGuid)
				return NotFound();

			return Ok(MakeNode());
		}

		Node MakeNode()
		{
			var job = _jobMonitor.GetJob();

			return new Node
			{
				NodeUrl = MakeUrl(_context.NodeGuid),
				Name = Environment.MachineName,
				Mac = "00:00:00:00:00:00",
				Ip = "0.0.0.0",
				Tags = new List<Tag>(),
				Status = NodeStatus.Alive,
				Version = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
				Timestamp = DateTime.Now,
				JobUrl = job != null ? job.JobUrl : ""
			};
		}
	}
}
