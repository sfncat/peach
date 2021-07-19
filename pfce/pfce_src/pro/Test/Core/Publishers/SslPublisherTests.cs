using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NUnit.Framework;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.IO.Pem;
using Peach.Core;
using Peach.Core.Test;
using Encoding = Peach.Core.Encoding;

namespace Peach.Pro.Test.Core.Publishers
{
	internal class PeachTlsServer : DefaultTlsServer
	{
		private static X509CertificateStructure LoadCertificate(string fileName)
		{
			var pem = LoadPemFile(fileName);

			if (pem.Type.EndsWith("CERTIFICATE"))
				return X509CertificateStructure.GetInstance(pem.Content);

			throw new ArgumentException("doesn't specify a valid certificate", "fileName");
		}

		private static AsymmetricKeyParameter LoadPrivateKey(string fileName)
		{
			var pem = LoadPemFile(fileName);

			if (pem.Type.EndsWith("RSA PRIVATE KEY"))
			{
				var rsa = RsaPrivateKeyStructure.GetInstance(pem.Content);

				return new RsaPrivateCrtKeyParameters(
					rsa.Modulus,
					rsa.PublicExponent,
					rsa.PrivateExponent,
					rsa.Prime1,
					rsa.Prime2,
					rsa.Exponent1,
					rsa.Exponent2,
					rsa.Coefficient);
			}

			if (pem.Type.EndsWith("PRIVATE KEY"))
				return PrivateKeyFactory.CreateKey(pem.Content);

			throw new ArgumentException("doesn't specify a valid private key", "fileName");
		}

		private static PemObject LoadPemFile(string fileName)
		{
			using (var rdr = new StreamReader(fileName))
			{
				return new PemReader(rdr).ReadPemObject();
			}
		}

		internal static string Fingerprint(X509CertificateStructure c)
		{
			var der = c.GetEncoded();
			var sha1 = DigestUtilities.CalculateDigest("SHA256", der);
			var fp = new StringBuilder();

			fp.Append(sha1[0].ToString("X2"));

			for (var i = 1; i < sha1.Length; ++i)
			{
				fp.Append(":");
				fp.Append(sha1[i].ToString("X2"));
			}

			return fp.ToString();
		}

		private readonly X509CertificateStructure _caCert;
		private readonly X509CertificateStructure _serverCert;
		private readonly AsymmetricKeyParameter _serverKey;
		private readonly SecureRandom _secureRandom;

		public PeachTlsServer(string caCert, string serverCert, string serverKey)
		{
			_caCert = LoadCertificate(caCert);
			_serverCert = LoadCertificate(serverCert);
			_serverKey = LoadPrivateKey(serverKey);
			_secureRandom = new SecureRandom();
		}

		public SecureRandom SecureRandom
		{
			get { return _secureRandom; }
		}

		public override void NotifyAlertRaised(byte alertLevel, byte alertDescription, string message, Exception cause)
		{
			TextWriter output = (alertLevel == AlertLevel.fatal) ? Console.Error : Console.Out;
			output.WriteLine("TLS server raised alert: " + AlertLevel.GetText(alertLevel)
				+ ", " + AlertDescription.GetText(alertDescription));
			if (message != null)
			{
				output.WriteLine("> " + message);
			}
			if (cause != null)
			{
				output.WriteLine(cause);
			}
		}

		public override void NotifyAlertReceived(byte alertLevel, byte alertDescription)
		{
			var output = alertLevel == AlertLevel.fatal ? Console.Error : Console.Out;

			output.WriteLine("TLS server received alert: {0}, {1}", AlertLevel.GetText(alertLevel), AlertDescription.GetText(alertDescription));
		}

		protected override int[] GetCipherSuites()
		{
			var suites = new[]
			{
					CipherSuite.DRAFT_TLS_DHE_PSK_WITH_CHACHA20_POLY1305_SHA256,
					CipherSuite.DRAFT_TLS_DHE_RSA_WITH_CHACHA20_POLY1305_SHA256,
					CipherSuite.DRAFT_TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256,
					CipherSuite.DRAFT_TLS_ECDHE_PSK_WITH_CHACHA20_POLY1305_SHA256,
					CipherSuite.DRAFT_TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256,
					CipherSuite.DRAFT_TLS_PSK_WITH_CHACHA20_POLY1305_SHA256,
					CipherSuite.DRAFT_TLS_RSA_PSK_WITH_CHACHA20_POLY1305_SHA256,
			};

			return base.GetCipherSuites().Concat(suites).ToArray();
		}

		protected override ProtocolVersion MaximumVersion
		{
			get { return ProtocolVersion.TLSv12; }
		}

		public override ProtocolVersion GetServerVersion()
		{
			var serverVersion = base.GetServerVersion();

			Console.WriteLine("TLS server negotiated {0}", serverVersion);

			return serverVersion;
		}

		public override CertificateRequest GetCertificateRequest()
		{
			var certificateTypes = new[]
			{
				ClientCertificateType.rsa_sign,
				ClientCertificateType.dss_sign,
				ClientCertificateType.ecdsa_sign
			};

			var serverSigAlgs = TlsUtilities.IsSignatureAlgorithmsExtensionAllowed(mServerVersion)
				? TlsUtilities.GetDefaultSupportedSignatureAlgorithms()
				: null;

			var certificateAuthorities = new ArrayList
			{
				_caCert.Subject
			};

			return new CertificateRequest(certificateTypes, serverSigAlgs, certificateAuthorities);
		}

		public override void NotifyClientCertificate(Certificate clientCertificate)
		{
			Console.WriteLine("TLS server received client certificate chain of length " + clientCertificate.Length);

			foreach (var entry in clientCertificate.GetCertificateList())
			{
				Console.WriteLine("    fingerprint:SHA-256 {0} ({1})", Fingerprint(entry), entry.Subject);
			}
		}

		protected override TlsEncryptionCredentials GetRsaEncryptionCredentials()
		{
			return new DefaultTlsEncryptionCredentials(
				mContext,
				new Certificate(new[] { _serverCert, _caCert }),
				_serverKey);
		}

		protected override TlsSignerCredentials GetRsaSignerCredentials()
		{
			SignatureAndHashAlgorithm signatureAndHashAlgorithm = null;

			if (mSupportedSignatureAlgorithms != null)
			{
				foreach (SignatureAndHashAlgorithm alg in mSupportedSignatureAlgorithms)
				{
					if (alg.Signature == SignatureAlgorithm.rsa)
					{
						signatureAndHashAlgorithm = alg;
						break;
					}
				}

				if (signatureAndHashAlgorithm == null)
					return null;
			}

			return new DefaultTlsSignerCredentials(
				mContext,
				new Certificate(new[] { _serverCert, _caCert }),
				_serverKey,
				signatureAndHashAlgorithm);
		}
	}

	internal class TlsListener : IDisposable
	{
		private readonly TempDirectory _tmp;
		private readonly TcpListener _listener;
		private readonly PeachTlsServer _server;

		private TcpClient _tcp;
		private TlsServerProtocol _protocol;
		private bool _timeClose = false;

		public TlsListener(IPEndPoint localEp, bool timeClose = false)
		{
			_timeClose = timeClose;
			_tmp = new TempDirectory();
			_listener = new TcpListener(localEp);

			var caCert = Path.Combine(_tmp.Path, "ca.pem");
			var serverCert = Path.Combine(_tmp.Path, "server-cert.pem");
			var serverKey = Path.Combine(_tmp.Path, "server-key.pem");

			File.WriteAllText(caCert, @"-----BEGIN CERTIFICATE-----
MIIDZzCCAh+gAwIBAgIEUqKcyzANBgkqhkiG9w0BAQsFADAjMSEwHwYDVQQDExhC
b3VuY3lDYXN0bGUgVExTIFRlc3QgQ0EwHhcNMTMxMjA3MDM1ODAzWhcNMzMxMjAy
MDM1ODAzWjAjMSEwHwYDVQQDExhCb3VuY3lDYXN0bGUgVExTIFRlc3QgQ0EwggFS
MA0GCSqGSIb3DQEBAQUAA4IBPwAwggE6AoIBMQDMhzecH5G7Hux5I8B4ftDYKQfB
EpGBlFB2Yvbn3JIbtEpnY3utJokWGdbTY5oXn8amSRZFP9ZJlBDPrAyop//UfuJ0
A1n2wDiFHUcPMc1Dg67uH44fGib59tnOV4a0w4xF18FVgPH++2Vy/ZY/VjSAIfMd
U3nznh1p744dsEjTqj4euJjcy9CCvpW7A0i0ZuXztkkNZvcVnskCrvuHKshAZoPo
dtIW1G66evZQCGQIJHLyASAifQFe1c8VlJ8U4Z5zQeJe26DjMRF5IrYJWl43IFYr
DfFC4x+9EnVKdE2g95D9mTkWAwX8/y5eWzPBj7uauLdc36CPfJcn6Q0shGxMbn+O
j2/mrF8cq9hXBe0cuRLH8F7k6wGxzVzx4wizMysCKJoXYVnEw9AP3uSZqXNDAgMB
AAGjQzBBMA8GA1UdEwEB/wQFMAMBAf8wDwYDVR0PAQH/BAUDAwcEADAdBgNVHQ4E
FgQU9mOvr6Pi7+4O5bKxRihqeCkyHukwDQYJKoZIhvcNAQELBQADggExAKyMiFmj
YxzjXpQBD5dvRI7xZn79vH3lo13XRBXj/sbPXDXWIv21iLfutXn/RGGsq8piPXHG
5UY3cpZR6gOq6QO1dJ91K0ViAJBFQdkhhtfbhqGY4jvj0vGO6zenG/WrjH26nCT7
8S4L6ZoF6Y0EfQXluP50vEitTaZ6x/rung9h2JQ8rYKiRRVCA+tgBWK/CNhQ9LXy
k3GU0mKLik0AkEFS17C0NWePIPEs/Kxv9iTEFacAN9wVHjZcMYnYtWaPNX0LWV8s
2V2DMJxrmgCEcoXgJxlyEmvyqwpjB+2AiIQVIuWcwPqgBQoKHThT2zJcXV+bMhMs
6cGvaIdvPxttduQsP349GcmUIlV6zFJq+HcMjfa8hZNIkuGBpUzdRQnu1+vYTkwz
eVOPEIBZLzg9e2k=
-----END CERTIFICATE-----");

			File.WriteAllText(serverCert, @"-----BEGIN CERTIFICATE-----
MIIDmjCCAlKgAwIBAgIEUqKc4DANBgkqhkiG9w0BAQsFADAjMSEwHwYDVQQDExhC
b3VuY3lDYXN0bGUgVExTIFRlc3QgQ0EwHhcNMTMxMjA3MDM1ODI0WhcNMzMxMjAy
MDM1ODI0WjAjMSEwHwYDVQQDExhCb3VuY3lDYXN0bGUgVGVzdCBTZXJ2ZXIwggFS
MA0GCSqGSIb3DQEBAQUAA4IBPwAwggE6AoIBMQCzL883ng/tmQfbcUeO2Bm7OnIZ
rzj5hk4zeyeKR6brrSj3RaxOq4wy1c14BA8YAVSm7ZDGjjXqiCiWqq1NdgnP2qyn
94O/OU0Ik3scpvkWDbweIJx0zHYBHTeqUaTEjdawI/EWxfzfOvPzBlK+s7uONLX9
Z8lbW9iZ76SS1hyD3T7mknTmEQAjAVT+aH0qdVFV2cg0JwKp2fCkDV8A9cvCo1h1
GiVpzNpAUjWaXxhs6AKd5/O7F5K32rP/tOQEhNW8F/cAfl9QbpR7M3GZrzmjtvU/
hAk2JYYDu7CHuIAGkhHvv3kWTx5s5JQP2Vn2KqjQLNcEMCLAl7e7NIsEhOUXTFvK
94f4guPuwqlrQKRX7nJnUKYHjbqWW64GVjFuLbB8CA3xqt9C2dOhuX+b4KWtAgMB
AAGjdjB0MAwGA1UdEwEB/wQCMAAwEwYDVR0lBAwwCgYIKwYBBQUHAwEwDwYDVR0P
AQH/BAUDAwegADAdBgNVHQ4EFgQUlILGBt8EzhYtlNrOPrhfwi0s6bkwHwYDVR0j
BBgwFoAU9mOvr6Pi7+4O5bKxRihqeCkyHukwDQYJKoZIhvcNAQELBQADggExAMQd
MLOlWKWJxh6IP7sRlWKjpWZyu43eSOfvEpVljW8VaRxJC8UdhpFxsXS6Ml7wEMUC
BkVNHGMxho/GJMXUBV7OsQSv0et1o45bmkN+KKisSVReSgcj6Drp/BRcUcybPtcJ
aDW1txh/suHWppVmtkIkZIF/3IR2qFekDdCLoluiEOvbNn3YjUnQLm6Eo0pBxgpb
W5MF3/19UckP1sLrs5vFk1dtDBZ/agpI9I0psv+6OsjosvrdpjIPHjwmoZ+oYtKc
4Q30vzLCVtGGyzXWBZ+Z6AbmZpJPDQtul522XKE2vE8GA3+X/RXVAZB8a86DWtzq
J1O6D+KOyA9zwe1CO+VJ5fMkjSNXY6WDzEXqyKEBP8tkkvSByiM546CXtNDbEwBe
PtYQf223mpK56XTFq4k=
-----END CERTIFICATE-----");

			File.WriteAllText(serverKey, @"-----BEGIN RSA PRIVATE KEY-----
MIIFfAIBAAKCATEAsy/PN54P7ZkH23FHjtgZuzpyGa84+YZOM3snikem660o90Ws
TquMMtXNeAQPGAFUpu2Qxo416ogolqqtTXYJz9qsp/eDvzlNCJN7HKb5Fg28HiCc
dMx2AR03qlGkxI3WsCPxFsX83zrz8wZSvrO7jjS1/WfJW1vYme+kktYcg90+5pJ0
5hEAIwFU/mh9KnVRVdnINCcCqdnwpA1fAPXLwqNYdRolaczaQFI1ml8YbOgCnefz
uxeSt9qz/7TkBITVvBf3AH5fUG6UezNxma85o7b1P4QJNiWGA7uwh7iABpIR7795
Fk8ebOSUD9lZ9iqo0CzXBDAiwJe3uzSLBITlF0xbyveH+ILj7sKpa0CkV+5yZ1Cm
B426lluuBlYxbi2wfAgN8arfQtnTobl/m+ClrQIDAQABAoIBMAEZ6kKNFNbIeBqT
24dTDIUsj+DCbGPsR86wsG5FOgFRa96K0qcAS53nBAXDynKFl6igJcXs4WfLiRnN
QGcgob+cIgM6pT8rDnlZT/291pH9mD/MwnyHDwak7C7dGXrRojWQl1TGZqjJS0f5
enaN5lUnXwROZqsxUYohFkmtj1dpjxTqUGZ0In1JdG2mDmunMgjkJ/UqtCp0QFCp
xtd1u5/913RBFC+n/CJl1+TnNQ4WNDRtkHZfJpVp+dOldQr028CJU3C5u1JB9ped
JK+dywUFlqH1Tdk2v5CQV5esvbFpXX7FIlHuId+JOwFyvkR5+7NmyQ2uAz/Yeg8K
ItdRhNQCJpYPlPH9Yb1JooEhPYrQX6oz/q2R3qiFdtc1cMZW3KCtIALZk30lW/o+
B4EiysECgZkA1cYcdgNKy5K1O/IOyhr1eYK2us738Z3A57gC1y+m58D02dzmwacg
HKZI9Cz98L8yEyORkdxpg86xi+NIcdU15BXXioFgMI5ZSfNVBcO4rv8G/QCxEWDr
gnXe7FYuLlOKBbVXx7fJ0IXRUVbUlH5ZqE2HlFbSdPJmlaKtbxxyT7xhhWkf/MuI
hRn+MFvbXvC+JqL+FS141kECgZkA1pS74hc7H5blq0J51E8EDtgU5m770VlQKhbW
Z1D0oXVfB19DD3SX4xu22/6XlKXLlQDx4ssxVw11VIkd9WhkIrqq4U644XL5xXmf
PMsR+fG2o+Y7MY4TNy+4qcuOK17n6R2pxR4zQoVnZs/qL4s5jPKMsC76C4Udfxfh
yup0eFEJ+jPQdWYWQ6uX3UF0rA5x0Tb200aKbG0CgZkAkwaoeHoXLR//yfTXOyWD
g0jliGHkoabQEA681WcOsgJB5L1LcBETwuCS+G0hUj0NoaAq9FjVsTOtZPqyzqfH
YtGq5rXIhFzDCFt1NHvCP4ljMwsQvVUdZSLQaVd0d6Q5H2fzsYa0JNiEeB7yIhcs
btaz0tBL+ubkqzGxeuPjsvdrUyhUObd6c6DG9FeY7xlAjq43djVKEIECgZhITde9
SDyo2UzMV1r72iAw7EimmPELSsADXqyiJZo4qXb64fOTyqK/aQBFwtTKxs8Bh076
L6ORhLxrXsSUg7dyKFoaD0+mz/ovu1qXvolxIix7r8F0Yj5BUzgzJp7iKFmWqGMj
Q5jcKl18PETZ/lzHDJexajLhHNqij6aKnFPgktX80+bDGEIaTUCf0kWBEGDzsUSc
TmGoRQKBmQDVFQ6EAOXXNp2bMLZ78qnxmS+NZPijBDeVu1cXMYdRSBoldrtSHKUM
NFjPfesz4IRbEePK5s0vgM88QCDaspy8aLj//gh9YuqijbHOfhvMUP6MqzU/jJN7
MDAxcGbcoFqdWTP1HB9qeX91UNRwN+2/xfO6SrLbfTvG4v/sntr8YfVYya2EuAa9
qLk0TB3QXaoHknsz7EhRnw==
-----END RSA PRIVATE KEY-----");

			_server = new PeachTlsServer(caCert, serverCert, serverKey);
		}

		public void Start()
		{
			_listener.Start();
			_listener.BeginAcceptTcpClient(OnAccept, null);
		}

		public void Dispose()
		{
			_listener.Stop();
			_tmp.Dispose();
		}

		private void OnAccept(IAsyncResult ar)
		{
			try
			{
				_tcp = _listener.EndAcceptTcpClient(ar);
			}
			catch (ObjectDisposedException)
			{
				return;
			}

			_protocol = new TlsServerProtocol(_tcp.GetStream(), _server.SecureRandom);
			_protocol.Accept(_server);

			if (_timeClose)
			{
				var buf = Encoding.ASCII.GetBytes("Hello World");

				_protocol.Stream.Write(buf, 0, buf.Length);

				System.Threading.Thread.Sleep(10);

				_protocol.Close();
				_tcp.Close();
			}

			_listener.BeginAcceptTcpClient(OnAccept, null);
		}
	}

	[Quick]
	[Peach]
	[TestFixture]
	class SslPublisherTests
	{
		[Test]
		[Ignore("Need to finish implementation")]
		public void Test()
		{
			const string xml = @"
<Peach>
	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel name='DM'>
					<String value='Hello World' />
				</DataModel>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='Ssl'>
			<Param name='Host' value='127.0.0.1' />
			<Param name='Port' value='6789' />
		</Publisher>
	</Test>
</Peach>
";

			using (var l = new TlsListener(new IPEndPoint(IPAddress.Any, 6789)))
			{
				var dom = DataModelCollector.ParsePit(xml);
				var cfg = new RunConfiguration { singleIteration = true };
				var e = new Engine(null);

				l.Start();

				// Changing the time TlsListener calls sleep before doing _protocol.Close()
				// causes a variety of different exceptions and messages.

				 Assert.Throws<PeachException>(() => e.startFuzzing(dom, cfg));
			}
		}

		[Test]
		[Ignore("Requires openssl to exist and be running")]
		public void TestOpenssl()
		{
			// openssl req -x509 -newkey rsa:2048 -keyout key.pem -out cert.pem -days 365 -nodes
			// openssl s_server -key key.pem -cert cert.pem -accept 44330 -www

			const string xml = @"
<Peach>
	<StateModel name='SM' initialState='Initial'>
		<State name='Initial'>
			<Action type='output'>
				<DataModel name='DM'>
					<String value='GET / HTTP/1.0\r\n\r\n' />
				</DataModel>
			</Action>
			<Action type='input'>
				<DataModel name='DM'>
					<String />
				</DataModel>
			</Action>
		</State>
	</StateModel>

	<Test name='Default'>
		<StateModel ref='SM' />
		<Publisher class='Ssl'>
			<Param name='Host' value='127.0.0.1' />
			<Param name='Port' value='44330' />
		</Publisher>
	</Test>
</Peach>
";

			var dom = DataModelCollector.ParsePit(xml);
			var cfg = new RunConfiguration { singleIteration = true };
			var e = new Engine(null);

			e.startFuzzing(dom, cfg);

			var dm = dom.tests[0].stateModel.states[0].actions[1].dataModel;

			Console.WriteLine(dm.InternalValue.BitsToString());
		}
	}
}
