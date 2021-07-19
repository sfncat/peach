

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using NLog;
using Peach.Core;
using Peach.Core.IO;
using Encoding = Peach.Core.Encoding;
using Logger = NLog.Logger;
using TimeoutException = System.TimeoutException;

namespace Peach.Pro.Core.Publishers
{
	[Publisher("Http")]
	[Parameter("Method", typeof(string), "Method type")]
	[Parameter("Url", typeof(string), "Url")]
	[Parameter("BaseUrl", typeof(string), "Optional BaseUrl for authentication", "")]
	[Parameter("Username", typeof(string), "Optional username for authentication", "")]
	[Parameter("Password", typeof(string), "Optional password for authentication", "")]
	[Parameter("Domain", typeof(string), "Optional domain for authentication", "")]
	[Parameter("ClientCertificate", typeof(string), "Optional client certificate filename for authentication", "")]
	[Parameter("Cookies", typeof(bool), "Track cookies (defaults to true)", "true")]
	[Parameter("CookiesAcrossIterations", typeof(bool), "Track cookies across iterations (defaults to false)", "false")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data/connection (default 3000)", "3000")]
	[Parameter("IgnoreCertErrors", typeof(bool), "Allow https regardless of cert status (defaults to true)", "true")]
	[Parameter("FailureStatusCodes", typeof(int[]), "Comma separated list of status codes that are failures causing current test case to stop.", "400,401,402,403,404,405,406,407,408,409,410,411,412,413,414,415,416,417,500,501,502,503,504,505")]
	[Parameter("FaultOnStatusCodes", typeof(int[]), "Comma separated list of status codes that are faults. Defaults to none.", "")]
	[Parameter("Proxy", typeof(string), "HTTP proxy address (http://192.168.1.1). Defaults to none.", "")]
	public class HttpPublisher : Peach.Core.Publishers.BufferedStreamPublisher
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();
		protected override Logger Logger { get { return logger; } }

		public string Url { get; protected set; }
		public string Method { get; protected set; }
		public string Username { get; protected set; }
		public string Password { get; protected set; }
		public string Domain { get; protected set; }
		public string BaseUrl { get; protected set; }
		public string Proxy { get; protected set; }
		public string ClientCertificate { get; protected set; }
		public int[] FaultOnStatusCodes { get; protected set; }
		public int[] FailureStatusCodes { get; protected set; }
		public bool Cookies { get; protected set; }
		public bool CookiesAcrossIterations { get; protected set; }
		public bool IgnoreCertErrors { get; protected set; }

		protected CookieContainer CookieJar = new CookieContainer();
		protected HttpWebResponse Response { get; set; }
		protected string Query { get; set; }
		protected X509Certificate _clientCertificate = null;
		protected WebProxy _proxy = null;

		// Allow access from scripting
		public Dictionary<string, string> Headers = new Dictionary<string, string>();

		protected CredentialCache Credentials;

		public HttpPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
			if (!string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password))
			{
				Uri baseUrl = null;

				// ParameterParser.Parse() will throw if not set on the HttpPublisher
				// But we need to guard for null since it is not set in the WebApiPublisher
				if (!string.IsNullOrEmpty(Url))
				{
					try
					{
						baseUrl = new Uri(Url);
					}
					catch (Exception ex)
					{
						throw new PeachException("The value of parameter 'Url' is not a valid URI.", ex);
					}
				}

				if (!string.IsNullOrWhiteSpace(BaseUrl))
				{
					try
					{
						baseUrl = new Uri(BaseUrl);
					}
					catch (Exception ex)
					{
						throw new PeachException("The value of parameter 'BaseUrl' is not a valid URI.", ex);
					}
				}

				if (baseUrl == null)
					throw new PeachException("The parameter 'BaseUrl' is required when using Username/Password authentication.");

				Credentials = new CredentialCache
				{
					{baseUrl, "Basic", new NetworkCredential(Username, Password)}
				};

				if (!string.IsNullOrWhiteSpace(Domain))
				{
					Credentials.Add(baseUrl, "NTLM", new NetworkCredential(Username, Password, Domain));
					Credentials.Add(baseUrl, "Digest", new NetworkCredential(Username, Password, Domain));
				}

				if (!string.IsNullOrEmpty(ClientCertificate))
				{
					if (!File.Exists(ClientCertificate))
						throw new PeachException(string.Format("Client certificate file '{0}' not found.", ClientCertificate));

					_clientCertificate = X509Certificate.CreateFromCertFile(ClientCertificate);
				}
			}

			if (IgnoreCertErrors)
			{
				ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
			}

			if (!string.IsNullOrEmpty(Proxy))
			{
				_proxy = new WebProxy(Proxy);
			}
		}

		protected static string ReadString(BitwiseStream data)
		{
			data.Seek(0, SeekOrigin.Begin);
			var rdr = new BitReader(data);
			try
			{
				var str = rdr.ReadString(Encoding.UTF8);
				return str;
			}
			catch (Exception ex)
			{
				// Eat up encoding exception.
				throw new SoftException("HTTP Publisher skips test cases with incorrect UTF8", ex);
			}
		}

		protected override Variant OnCall(string method, List<BitwiseStream> args)
		{

			switch (method)
			{
				case "Query":
					Query = ReadString(args[0]);
					break;
				case "Header":
					var key = CleanHeaderValue(ReadString(args[0]));
					var value = ReadString(args[1]);
					Headers[key] = value;
					break;
			}

			return null;
		}

		static readonly char[] InvalidParamChars =
		{ 
			'(', ')', '<', '>', '@', ',', ';', ':', 
			'\\', '"', '\'', '/', '[', ']', '?', '=', 
			'{', '}', ' ', '\t', '\r', '\n'
		};


		/// <summary>
		/// Remove characters not allowed in header fields.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		protected string CleanHeaderValue(string str)
		{
			var sb = new StringBuilder(str.Length);

			foreach (var c in str)
			{
				if (Array.IndexOf(InvalidParamChars, c) == -1 &&
					!((c == '\x007f') || ((c < ' ') && (c != '\t'))))
				{
					sb.Append(c);
				}
			}

			return sb.ToString();
		}

		protected override void OnInput()
		{
			if (Response == null)
				CreateClient(null);

			base.OnInput();
		}

		/// <summary>
		/// Send data
		/// </summary>
		/// <param name="data">Data to send/write</param>
		protected override void OnOutput(BitwiseStream data)
		{
			lock (_clientLock)
			{
				if (_client != null)
					CloseClient();
			}

			CreateClient(data);
		}

		private void CreateClient(BitwiseStream data)
		{
			if (Response != null)
			{
				Response.Close();
				Response = null;
			}

			// Send request with data as body.
			Uri url;

			try
			{
				url = new Uri(Url);
				if (!string.IsNullOrWhiteSpace(Query))
					url = new Uri(Url + "?" + Query);
			}
			catch (UriFormatException ex)
			{
				throw new SoftException(ex);
			}

			SoftException caught = null;
			FaultException fault = null;

			try
			{
				Retry.TimedBackoff(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(Timeout), () =>
				{
					try
					{
						_client = TryCreateClient(url, data);
					}
					catch (FaultException ex)
					{
						fault = ex;
					}
					catch (SoftException ex)
					{
						caught = ex;
					}
					catch (ProtocolViolationException ex)
					{
						// Happens if we try to write data and the method doesn't support it
						caught = new SoftException(ex);
					}
					catch (WebException ex)
					{
						if (ex.Status == WebExceptionStatus.Timeout || ex.Status == WebExceptionStatus.ConnectFailure)
							throw new TimeoutException(ex.Message, ex);

						caught = new SoftException(ex);
					}
				});
			}
			catch (TimeoutException ex)
			{
				caught = new SoftException("Timed out connecting to {0}".Fmt(SafeUrlString(url)), ex);
			}

			if (fault != null)
				throw new FaultException(fault.Fault, fault);

			if (caught != null)
				throw new SoftException(caught);

			_clientName = SafeUrlString(url);

			StartClient();
		}

		private static string SafeUrlString(Uri url)
		{
			return "{0}://{1}:{2}".Fmt(url.Scheme, url.Host, url.Port);
		}

		static bool HeaderCompare(string lhs, string rhs)
		{
			return 0 == string.Compare(lhs, rhs, StringComparison.OrdinalIgnoreCase);
		}

		static void CheckBadChars(string value)
		{
			//First, check for correctly formed multi-line value
			//Second, check for absenece of CTL characters
			var crlf = 0;
			foreach (var t in value)
			{
				var c = (char)(0x000000ff & (uint)t);
				switch (crlf)
				{
					case 0:
						if (c == '\r')
						{
							crlf = 1;
						}
						else if (c == '\n')
						{
							// Technically this is bad HTTP.  But it would be a breaking change to throw here.
							// Is there an exploit?
							crlf = 2;
						}
						else if (c == 127 || (c < ' ' && c != '\t'))
						{
							throw new ArgumentException("Specified value has invalid Control characters.", "value");
						}
						break;

					case 1:
						if (c == '\n')
						{
							crlf = 2;
							break;
						}
						throw new ArgumentException("Specified value has invalid CRLF characters.", "value");

					case 2:
						if (c == ' ' || c == '\t')
						{
							crlf = 0;
							break;
						}
						throw new ArgumentException("Specified value has invalid CRLF characters.", "value");
				}
			}
		}

		protected virtual HttpWebRequest GetRequest(Uri url, BitwiseStream data)
		{
			var request = (HttpWebRequest)WebRequest.Create(url);
			request.Method = Method;
			request.Proxy = _proxy;

			if (_clientCertificate != null)
				request.ClientCertificates.Add(_clientCertificate);

			foreach (var kv in Headers)
			{
				try
				{
					// Mono doesn't do this but Microsoft does.
					// Ensure both behave the same way.
					CheckBadChars(kv.Value);

					if (HeaderCompare(kv.Key, "Accept"))
						request.Accept = kv.Value;
					else if (HeaderCompare(kv.Key, "Connection"))
						request.Connection = kv.Value;
					else if (HeaderCompare(kv.Key, "Content-Type"))
						request.ContentType = kv.Value;
					else if (HeaderCompare(kv.Key, "Date"))
						request.Date = DateTime.Parse(kv.Value);
					else if (HeaderCompare(kv.Key, "Expect"))
						request.Expect = kv.Value;
					else if (HeaderCompare(kv.Key, "If-Modified-Since"))
						request.IfModifiedSince = DateTime.Parse(kv.Value);
					else if (HeaderCompare(kv.Key, "Referer"))
						request.Referer = kv.Value;
					else if (HeaderCompare(kv.Key, "Transfer-Encoding"))
						request.TransferEncoding = kv.Value;
					else if (HeaderCompare(kv.Key, "User-Agent"))
						request.UserAgent = kv.Value;
					else if (!string.IsNullOrWhiteSpace(kv.Key))
						request.Headers[kv.Key] = kv.Value;
				}
				catch (ArgumentException ex)
				{
					throw new SoftException("Unable to set the '{0}' HTTP header to '{1}'.".Fmt(kv.Key, kv.Value), ex);
				}
			}

			if (data != null)
			{
				if (Logger.IsDebugEnabled)
					Logger.Debug("\n\n" + Utilities.HexDump(data));

				using (var sout = request.GetRequestStream())
				{
					data.CopyTo(sout);
				}
			}
			else
			{
				request.ContentLength = 0;
			}

			return request;
		}

		protected virtual void OnResponse(HttpWebResponse response)
		{
		}

		private Stream TryCreateClient(Uri url, BitwiseStream data)
		{
			Logger.Trace("TryCreateClient> {0} {1}", Method, SafeUrlString(url));

			var request = GetRequest(url, data);

			request.Timeout = Timeout;
			request.ServicePoint.Expect100Continue = false;

			if (Cookies)
				request.CookieContainer = CookieJar;

			if (Credentials != null)
				request.Credentials = Credentials;

			WebException exception = null;

			try
			{
				Response = (HttpWebResponse)request.GetResponse();
				OnResponse(Response);
			}
			catch (WebException wex)
			{
				exception = wex;
				Response = (HttpWebResponse) wex.Response;
				OnResponse(Response);

				if (Response == null)
					throw;
			}

			if (FaultOnStatusCodes.Contains((int)Response.StatusCode))
			{
				var sb = new StringBuilder();

				sb.AppendFormat("HTTP/{0} {1} {2}\r\n", Response.ProtocolVersion, (int)Response.StatusCode, Response.StatusDescription);

				for (var i = 0; i < Response.Headers.Count; ++i)
				{
					sb.AppendFormat("{0}: {1}\r\n", Response.Headers.Keys[i], Response.Headers[i]);
				}

				sb.AppendFormat("\r\n");

				try
				{
					var stream = Response.GetResponseStream();

					if (stream != null)
					{
						var ch = Response.CharacterSet;
						if (string.IsNullOrEmpty(ch))
							ch = "utf-8";

						using (var rdr = new StreamReader(stream, System.Text.Encoding.GetEncoding(ch)))
						{
							string line;
							while ((line = rdr.ReadLine()) != null)
								sb.AppendLine(line);
						}
					}
				}
				catch (Exception ex)
				{
					sb.Append("Error reading reponse from server.");
					sb.AppendLine();
					sb.Append(ex.Message);
					sb.AppendLine();
				}

				var fault = new FaultSummary
				{
					Title = "Fault on status code {0} ({1})".Fmt((int)Response.StatusCode, Response.StatusCode),
					Description = sb.ToString(),
					MajorHash = FaultSummary.Hash(Response.StatusCode.ToString()),
					MinorHash = FaultSummary.Hash(string.Format("{0} {1} {2}", request.Method, request.RequestUri, Response.StatusCode)),
					Exploitablity = "Unknown"
				};

				throw new FaultException(fault);
			}

			if (FailureStatusCodes.Contains((int)Response.StatusCode))
			{
				if(exception != null)
					throw new SoftException(string.Format("Failure status code {0} ({1}) found.", (int)Response.StatusCode, Response.StatusCode), exception);

				throw new SoftException(string.Format("Failure status code {0} ({1}) found.", (int)Response.StatusCode, Response.StatusCode));
			}

			return Response.GetResponseStream();
		}

		protected override void OnClose()
		{
			base.OnClose();

			if (Cookies && !CookiesAcrossIterations)
				CookieJar = new CookieContainer();

			if (Response != null)
			{
				Response.Close();
				Response = null;
			}

			Query = null;
			Headers.Clear();
		}
	}
}
