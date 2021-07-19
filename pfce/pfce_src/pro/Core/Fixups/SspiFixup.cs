using System;
using System.Collections.Generic;
using System.IO;
using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Pro.Core.Fixups.Ntlm;

namespace Peach.Pro.Core.Fixups
{
	[Fixup("Sspi", true)]
	[System.ComponentModel.Description("Perform Microsoft SSPI authentication")]
	[Parameter("User", typeof(string), "User name to authenticate with")]
	[Parameter("Domain", typeof(string), "Domain to authenticate with", "")]
	[Parameter("Password", typeof(string), "Password to authenticate with")]
	[Parameter("ContinueNeeded", typeof(string), "Name of field in state bag indicating if authentication is continuing.",
		"Peach.SspiContinueNeeded")]
	[Serializable]
	public class SspiFixup : Peach.Core.Fixups.VolatileFixup
	{
		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

		public string User { get; protected set; }
		public string Domain { get; protected set; }
		public string Password { get; protected set; }
		public string ContinueNeeded { get; protected set; }

		public SspiFixup(DataElement parent, Dictionary<string, Variant> args)
			: base(parent, args)
		{
			ParameterParser.Parse(this, args);
		}

		protected override Variant OnActionRun(RunContext ctx)
		{
			object obj;
			byte[] serverToken = null;
			BitwiseStream data = null;

			if (!ctx.iterationStateStore.TryGetValue("Peach.SspiSecurityBuffer", out obj))
			{
				ctx.iterationStateStore[ContinueNeeded] = true;

				var msg = new Type1Message();
				return new Variant(msg.GetBytes());
			}

			logger.Debug("Found 'Peach.SspiSecurityBuffer', applying last server message.");
			
			data = obj as BitwiseStream;

			if (data == null)
				throw new SoftException("Peach.SspiSecurityBuffer was not a BitwiseStream");

			data.Seek(0, SeekOrigin.Begin);

			var serverTokenStream = new MemoryStream((int)data.Length);
			data.CopyTo(serverTokenStream);
			serverToken = serverTokenStream.ToArray();

			var serverMsg = new Type2Message(serverToken);
			var responseMsg = new Type3Message(serverMsg)
			{
				Username = User,
				Domain = Domain,
				Password = Password
			};

			ctx.iterationStateStore[ContinueNeeded] = false;

			return new Variant(responseMsg.GetBytes());
		}
	}
}

// end
