using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using NLog;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Security;
using Peach.Core;
using Peach.Core.IO;
using Peach.Pro.Core.Publishers.Ssl;

namespace Peach.Pro.Core.Publishers
{
	[Publisher("Ssl")]
	[Parameter("Host", typeof(string), "Hostname to connect to")]
	[Parameter("Port", typeof(ushort), "Port to connect to")]
	[Parameter("VerifyServer", typeof(bool), "Verify the server certificate", "false")]
	[Parameter("Timeout", typeof(int), "How many milliseconds to wait for data (default 3000)", "3000")]
	[Parameter("ConnectTimeout", typeof(int), "Max milliseconds to wait for connection (default 10000)", "10000")]
	[Parameter("Sni", typeof(string), "Sni to use for SSL connection. Will use Host by default", "")]
	[Parameter("ClientCert", typeof(string), "Path to client certificate in PEM format", "")]
	[Parameter("ClientKey", typeof(string), "Path to client private key in PEM format", "")]
	[Parameter("Alpn", typeof(string), "ALPN TLS extension, example value: h2;spdy/3.1;http/1.1", "")]
	public class SslClientPublisher : Peach.Core.Publishers.BufferedStreamPublisher
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		#region BouncyCastle TLS Helper Classes

		// Need class with TlsClient in inheritance chain
		class MyTlsClient : DefaultTlsClient
		{
			private readonly string cert;
			private readonly string key;

			public string Alpn { get; set; }

			public MyTlsClient(string clientCert, string clientKey)
			{
				cert = clientCert;
				key = clientKey;
				Alpn = null;
			}

			public override TlsAuthentication GetAuthentication()
			{
				return new MyTlsAuthentication(cert, key, this.mContext);
			}

			public override int[] GetCipherSuites()
			{
				var baseSuites = base.GetCipherSuites();
				var extraSuites =  new int[]
				{
					CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256,
					CipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA,
					CipherSuite.TLS_DH_RSA_WITH_AES_128_CBC_SHA,
					CipherSuite.TLS_DHE_DSS_WITH_AES_128_CBC_SHA,
					CipherSuite.TLS_DHE_PSK_WITH_AES_128_CBC_SHA,
					CipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA,
					CipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA,
					CipherSuite.TLS_ECDH_RSA_WITH_AES_128_CBC_SHA,
					CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA,
					CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA,
					CipherSuite.TLS_PSK_WITH_AES_128_CBC_SHA,
					CipherSuite.TLS_RSA_PSK_WITH_AES_128_CBC_SHA,
					CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA,
					CipherSuite.TLS_SRP_SHA_DSS_WITH_AES_128_CBC_SHA,
					CipherSuite.TLS_SRP_SHA_RSA_WITH_AES_128_CBC_SHA,
					CipherSuite.TLS_SRP_SHA_WITH_AES_128_CBC_SHA,
					CipherSuite.TLS_DH_DSS_WITH_AES_128_CBC_SHA256,
					CipherSuite.TLS_DH_RSA_WITH_AES_128_CBC_SHA256,
					CipherSuite.TLS_DHE_DSS_WITH_AES_128_CBC_SHA256,
					CipherSuite.TLS_DHE_PSK_WITH_AES_128_CBC_SHA256,
					CipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA256,
					CipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA256,
					CipherSuite.TLS_ECDH_RSA_WITH_AES_128_CBC_SHA256,
					CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256,
					CipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA256,
					CipherSuite.TLS_PSK_WITH_AES_128_CBC_SHA256,
					CipherSuite.TLS_RSA_PSK_WITH_AES_128_CBC_SHA256,
					CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA256,
					CipherSuite.TLS_DHE_PSK_WITH_AES_128_CCM,
					CipherSuite.TLS_DHE_RSA_WITH_AES_128_CCM,
					CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CCM,
					CipherSuite.TLS_PSK_WITH_AES_128_CCM,
					CipherSuite.TLS_RSA_WITH_AES_128_CCM,
					CipherSuite.TLS_DHE_RSA_WITH_AES_128_CCM_8,
					CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CCM_8,
					CipherSuite.TLS_PSK_DHE_WITH_AES_128_CCM_8,
					CipherSuite.TLS_PSK_WITH_AES_128_CCM_8,
					CipherSuite.TLS_RSA_WITH_AES_128_CCM_8,
					CipherSuite.TLS_DH_DSS_WITH_AES_128_GCM_SHA256,
					CipherSuite.TLS_DH_RSA_WITH_AES_128_GCM_SHA256,
					CipherSuite.TLS_DHE_DSS_WITH_AES_128_GCM_SHA256,
					CipherSuite.TLS_DHE_PSK_WITH_AES_128_GCM_SHA256,
					CipherSuite.TLS_DHE_RSA_WITH_AES_128_GCM_SHA256,
					CipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_GCM_SHA256,
					CipherSuite.TLS_ECDH_RSA_WITH_AES_128_GCM_SHA256,
					CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256,
					CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
					CipherSuite.TLS_PSK_WITH_AES_128_GCM_SHA256,
					CipherSuite.TLS_RSA_PSK_WITH_AES_128_GCM_SHA256,
					CipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256,
					CipherSuite.TLS_DH_DSS_WITH_AES_256_CBC_SHA,
					CipherSuite.TLS_DH_RSA_WITH_AES_256_CBC_SHA,
					CipherSuite.TLS_DHE_DSS_WITH_AES_256_CBC_SHA,
					CipherSuite.TLS_DHE_PSK_WITH_AES_256_CBC_SHA,
					CipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA,
					CipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA,
					CipherSuite.TLS_ECDH_RSA_WITH_AES_256_CBC_SHA,
					CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA,
					CipherSuite.TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA,
					CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA,
					CipherSuite.TLS_PSK_WITH_AES_256_CBC_SHA,
					CipherSuite.TLS_RSA_PSK_WITH_AES_256_CBC_SHA,
					CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA,
					CipherSuite.TLS_SRP_SHA_DSS_WITH_AES_256_CBC_SHA,
					CipherSuite.TLS_SRP_SHA_RSA_WITH_AES_256_CBC_SHA,
					CipherSuite.TLS_SRP_SHA_WITH_AES_256_CBC_SHA,
					CipherSuite.TLS_DH_DSS_WITH_3DES_EDE_CBC_SHA,
					CipherSuite.TLS_DH_RSA_WITH_3DES_EDE_CBC_SHA,
					CipherSuite.TLS_DHE_DSS_WITH_3DES_EDE_CBC_SHA,
					CipherSuite.TLS_DHE_PSK_WITH_3DES_EDE_CBC_SHA,
					CipherSuite.TLS_DHE_RSA_WITH_3DES_EDE_CBC_SHA,
					CipherSuite.TLS_ECDH_ECDSA_WITH_3DES_EDE_CBC_SHA,
					CipherSuite.TLS_ECDH_RSA_WITH_3DES_EDE_CBC_SHA,
					CipherSuite.TLS_ECDHE_ECDSA_WITH_3DES_EDE_CBC_SHA,
					CipherSuite.TLS_ECDHE_PSK_WITH_3DES_EDE_CBC_SHA,
					CipherSuite.TLS_ECDHE_RSA_WITH_3DES_EDE_CBC_SHA,
					CipherSuite.TLS_PSK_WITH_3DES_EDE_CBC_SHA,
					CipherSuite.TLS_RSA_PSK_WITH_3DES_EDE_CBC_SHA,
					CipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA,
					CipherSuite.TLS_SRP_SHA_DSS_WITH_3DES_EDE_CBC_SHA,
					CipherSuite.TLS_SRP_SHA_RSA_WITH_3DES_EDE_CBC_SHA,
					CipherSuite.TLS_SRP_SHA_WITH_3DES_EDE_CBC_SHA,
					CipherSuite.DRAFT_TLS_DHE_PSK_WITH_CHACHA20_POLY1305_SHA256,
					CipherSuite.DRAFT_TLS_DHE_RSA_WITH_CHACHA20_POLY1305_SHA256,
					CipherSuite.DRAFT_TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256,
					CipherSuite.DRAFT_TLS_ECDHE_PSK_WITH_CHACHA20_POLY1305_SHA256,
					CipherSuite.DRAFT_TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256,
					CipherSuite.DRAFT_TLS_PSK_WITH_CHACHA20_POLY1305_SHA256,
					CipherSuite.DRAFT_TLS_RSA_PSK_WITH_CHACHA20_POLY1305_SHA256,
					CipherSuite.TLS_DH_DSS_WITH_AES_128_CBC_SHA,
					CipherSuite.TLS_DH_DSS_WITH_AES_256_CBC_SHA256,
					CipherSuite.TLS_DH_RSA_WITH_AES_256_CBC_SHA256,
					CipherSuite.TLS_DHE_DSS_WITH_AES_256_CBC_SHA256,
					CipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA256,
					CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA256,
					CipherSuite.TLS_DHE_PSK_WITH_AES_256_CBC_SHA384,
					CipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA384,
					CipherSuite.TLS_ECDH_RSA_WITH_AES_256_CBC_SHA384,
					CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384,
					CipherSuite.TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA384,
					CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384,
					CipherSuite.TLS_PSK_WITH_AES_256_CBC_SHA384,
					CipherSuite.TLS_RSA_PSK_WITH_AES_256_CBC_SHA384,
					CipherSuite.TLS_DHE_PSK_WITH_AES_256_CCM,
					CipherSuite.TLS_DHE_RSA_WITH_AES_256_CCM,
					CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CCM,
					CipherSuite.TLS_PSK_WITH_AES_256_CCM,
					CipherSuite.TLS_RSA_WITH_AES_256_CCM,
					CipherSuite.TLS_DHE_RSA_WITH_AES_256_CCM_8,
					CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CCM_8,
					CipherSuite.TLS_PSK_DHE_WITH_AES_256_CCM_8,
					CipherSuite.TLS_PSK_WITH_AES_256_CCM_8,
					CipherSuite.TLS_RSA_WITH_AES_256_CCM_8,
					CipherSuite.TLS_DH_DSS_WITH_AES_256_GCM_SHA384,
					CipherSuite.TLS_DH_RSA_WITH_AES_256_GCM_SHA384,
					CipherSuite.TLS_DHE_DSS_WITH_AES_256_GCM_SHA384,
					CipherSuite.TLS_DHE_PSK_WITH_AES_256_GCM_SHA384,
					CipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384,
					CipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_GCM_SHA384,
					CipherSuite.TLS_ECDH_RSA_WITH_AES_256_GCM_SHA384,
					CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384,
					CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384,
					CipherSuite.TLS_PSK_WITH_AES_256_GCM_SHA384,
					CipherSuite.TLS_RSA_PSK_WITH_AES_256_GCM_SHA384,
					CipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384,
					CipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_CBC_SHA,
					CipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_CBC_SHA,
					CipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA,
					CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA,
					CipherSuite.TLS_RSA_WITH_CAMELLIA_128_CBC_SHA,
					CipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_CBC_SHA256,
					CipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_CBC_SHA256,
					CipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA256,
					CipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_128_CBC_SHA256,
					CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA256,
					CipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_128_CBC_SHA256,
					CipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_128_CBC_SHA256,
					CipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_CBC_SHA256,
					CipherSuite.TLS_ECDHE_PSK_WITH_CAMELLIA_128_CBC_SHA256,
					CipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_128_CBC_SHA256,
					CipherSuite.TLS_PSK_WITH_CAMELLIA_128_CBC_SHA256,
					CipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_128_CBC_SHA256,
					CipherSuite.TLS_RSA_WITH_CAMELLIA_128_CBC_SHA256,
					CipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_GCM_SHA256,
					CipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_GCM_SHA256,
					CipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_GCM_SHA256,
					CipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_128_GCM_SHA256,
					CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_GCM_SHA256,
					CipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_128_GCM_SHA256,
					CipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_128_GCM_SHA256,
					CipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_GCM_SHA256,
					CipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_128_GCM_SHA256,
					CipherSuite.TLS_PSK_WITH_CAMELLIA_128_GCM_SHA256,
					CipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_128_GCM_SHA256,
					CipherSuite.TLS_RSA_WITH_CAMELLIA_128_GCM_SHA256,
					CipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_CBC_SHA,
					CipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_CBC_SHA,
					CipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA,
					CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA,
					CipherSuite.TLS_RSA_WITH_CAMELLIA_256_CBC_SHA,
					CipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_CBC_SHA256,
					CipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_CBC_SHA256,
					CipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA256,
					CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA256,
					CipherSuite.TLS_RSA_WITH_CAMELLIA_256_CBC_SHA256,
					CipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_256_CBC_SHA384,
					CipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_256_CBC_SHA384,
					CipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_256_CBC_SHA384,
					CipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_CBC_SHA384,
					CipherSuite.TLS_ECDHE_PSK_WITH_CAMELLIA_256_CBC_SHA384,
					CipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_256_CBC_SHA384,
					CipherSuite.TLS_PSK_WITH_CAMELLIA_256_CBC_SHA384,
					CipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_256_CBC_SHA384,
					CipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_GCM_SHA384,
					CipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_GCM_SHA384,
					CipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_GCM_SHA384,
					CipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_256_GCM_SHA384,
					CipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_GCM_SHA384,
					CipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_256_GCM_SHA384,
					CipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_256_GCM_SHA384,
					CipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_GCM_SHA384,
					CipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_256_GCM_SHA384,
					CipherSuite.TLS_PSK_WITH_CAMELLIA_256_GCM_SHA384,
					CipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_256_GCM_SHA384,
					CipherSuite.TLS_RSA_WITH_CAMELLIA_256_GCM_SHA384,
					CipherSuite.TLS_RSA_WITH_NULL_MD5,
					CipherSuite.TLS_DHE_PSK_WITH_NULL_SHA,
					CipherSuite.TLS_ECDH_ECDSA_WITH_NULL_SHA,
					CipherSuite.TLS_ECDH_RSA_WITH_NULL_SHA,
					CipherSuite.TLS_ECDHE_ECDSA_WITH_NULL_SHA,
					CipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA,
					CipherSuite.TLS_ECDHE_RSA_WITH_NULL_SHA,
					CipherSuite.TLS_PSK_WITH_NULL_SHA,
					CipherSuite.TLS_RSA_PSK_WITH_NULL_SHA,
					CipherSuite.TLS_RSA_WITH_NULL_SHA,
					CipherSuite.TLS_DHE_PSK_WITH_NULL_SHA256,
					CipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA256,
					CipherSuite.TLS_PSK_WITH_NULL_SHA256,
					CipherSuite.TLS_RSA_PSK_WITH_NULL_SHA256,
					CipherSuite.TLS_RSA_WITH_NULL_SHA256,
					CipherSuite.TLS_DHE_PSK_WITH_NULL_SHA384,
					CipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA384,
					CipherSuite.TLS_PSK_WITH_NULL_SHA384,
					CipherSuite.TLS_RSA_PSK_WITH_NULL_SHA384,
					CipherSuite.TLS_DH_anon_WITH_RC4_128_MD5,
					CipherSuite.TLS_RSA_WITH_RC4_128_MD5,
					CipherSuite.TLS_DHE_PSK_WITH_RC4_128_SHA,
					CipherSuite.TLS_ECDH_anon_WITH_RC4_128_SHA,
					CipherSuite.TLS_ECDH_ECDSA_WITH_RC4_128_SHA,
					CipherSuite.TLS_ECDH_RSA_WITH_RC4_128_SHA,
					CipherSuite.TLS_ECDHE_ECDSA_WITH_RC4_128_SHA,
					CipherSuite.TLS_ECDHE_PSK_WITH_RC4_128_SHA,
					CipherSuite.TLS_ECDHE_RSA_WITH_RC4_128_SHA,
					CipherSuite.TLS_PSK_WITH_RC4_128_SHA,
					CipherSuite.TLS_RSA_WITH_RC4_128_SHA,
					CipherSuite.TLS_RSA_PSK_WITH_RC4_128_SHA,
					CipherSuite.TLS_DH_DSS_WITH_SEED_CBC_SHA,
					CipherSuite.TLS_DH_RSA_WITH_SEED_CBC_SHA,
					CipherSuite.TLS_DHE_DSS_WITH_SEED_CBC_SHA,
					CipherSuite.TLS_DHE_RSA_WITH_SEED_CBC_SHA,
					CipherSuite.TLS_RSA_WITH_SEED_CBC_SHA
				};

				var allSuites = new int[baseSuites.Length + extraSuites.Length];
				baseSuites.CopyTo(allSuites, 0);
				extraSuites.CopyTo(allSuites, baseSuites.Length);

				return allSuites;
			}

			public override TlsKeyExchange GetKeyExchange()
			{
				int keyExchangeAlgorithm = TlsUtilities.GetKeyExchangeAlgorithm(mSelectedCipherSuite);

				switch (keyExchangeAlgorithm)
				{
					case KeyExchangeAlgorithm.DH_DSS:
					case KeyExchangeAlgorithm.DH_RSA:
						return CreateDHKeyExchange(keyExchangeAlgorithm);

					case KeyExchangeAlgorithm.DHE_DSS:
					case KeyExchangeAlgorithm.DHE_RSA:
						return CreateDheKeyExchange(keyExchangeAlgorithm);

					case KeyExchangeAlgorithm.ECDH_ECDSA:
					case KeyExchangeAlgorithm.ECDH_RSA:
						return CreateECDHKeyExchange(keyExchangeAlgorithm);

					case KeyExchangeAlgorithm.ECDHE_ECDSA:
					case KeyExchangeAlgorithm.ECDHE_RSA:
						return CreateECDheKeyExchange(keyExchangeAlgorithm);

					case KeyExchangeAlgorithm.RSA:
						return CreateRsaKeyExchange();

					default:
						/*
							* Note: internal error here; the TlsProtocol implementation verifies that the
							* server-selected cipher suite was in the list of client-offered cipher suites, so if
							* we now can't produce an implementation, we shouldn't have offered it!
							*/
						throw new TlsFatalAlert(AlertDescription.internal_error);
				}
			}

			public override IDictionary GetClientExtensions()
			{
				var clientExtensions = base.GetClientExtensions();

				if (!string.IsNullOrEmpty(Alpn))
				{
					clientExtensions = TlsExtensionsUtilities.EnsureExtensionsInitialised(clientExtensions);

					var protocols = Alpn.Split(new [] {';'}, StringSplitOptions.RemoveEmptyEntries);
					var innerStream = new MemoryStream();

					foreach (var s in protocols)
					{
						var buff = Encoding.UTF8.GetBytes(s);
						TlsUtilities.WriteUint8((byte)buff.Length, innerStream);
						innerStream.Write(buff, 0, buff.Length);
					}

					var innerBuff = innerStream.ToArray();
					var stream = new MemoryStream();

					// Write ALPN length
					TlsUtilities.WriteUint16(innerBuff.Length, stream);
					// Write ALPN array
					stream.Write(innerBuff, 0, innerBuff.Length);

					clientExtensions[0x10] = stream.ToArray();
				}

				return clientExtensions;
			}
			
			protected override TlsKeyExchange CreateDHKeyExchange(int keyExchange)
			{
				return new PeachyTlsDHKeyExchange(keyExchange, mSupportedSignatureAlgorithms, null);
			}

			protected override TlsKeyExchange CreateDheKeyExchange(int keyExchange)
			{
				return new PeachyTlsDheKeyExchange(keyExchange, mSupportedSignatureAlgorithms, null);
			}

			protected override TlsKeyExchange CreateECDHKeyExchange(int keyExchange)
			{
				return new PeachyTlsECDHKeyExchange(keyExchange, mSupportedSignatureAlgorithms, mNamedCurves, mClientECPointFormats,
					mServerECPointFormats);
			}

			protected override TlsKeyExchange CreateECDheKeyExchange(int keyExchange)
			{
				return new PeachyTlsECDheKeyExchange(keyExchange, mSupportedSignatureAlgorithms, mNamedCurves, mClientECPointFormats,
					mServerECPointFormats);
			}

			protected override TlsKeyExchange CreateRsaKeyExchange()
			{
				return new PeachyTlsRsaKeyExchange(mSupportedSignatureAlgorithms);
			}
		}

		// Need class to handle certificate auth
		class MyTlsAuthentication : TlsAuthentication
		{
			private readonly string _clientKey;
			private readonly string _clientCert;
			private readonly TlsContext _tlsContext;

			public MyTlsAuthentication(string cert, string key, TlsContext context)
			{
				_clientKey = key;
				_clientCert = cert;
				_tlsContext = context;
			}

			protected Stream DecodePem(string fileName)
			{
				var _pemContentRegex = new Regex(@"-----BEGIN [A-Za-z0-9 ]*-----(.*)-----END [A-Za-z0-9 ]*-----", RegexOptions.Singleline);

				var pem = File.ReadAllText(fileName);
				var match = _pemContentRegex.Match(pem);

				if (!match.Success)
					throw new PeachException(string.Format("Error, invalid PEM file supplied to Ssl publisher. File '{0}'.",
						fileName));

				var b64data = pem.Substring(match.Groups[1].Index, match.Groups[1].Length);

				var bytes = Convert.FromBase64String(b64data);
				var stream = new BitStream();
				var writer = new BitWriter(stream);

				// Add double size header needed by certificate.parse

				writer.BigEndian();
				writer.WriteBits((ulong)bytes.Length + 3, 24);
				writer.WriteBits((ulong)bytes.Length, 24);
				writer.WriteBytes(bytes);

				stream.Position = 0;
				return stream;
			}


			public TlsCredentials GetClientCredentials(CertificateRequest certificateRequest)
			{
				if (string.IsNullOrEmpty(_clientKey) || string.IsNullOrEmpty(_clientKey))
					return null;

				// Load certificate

				var certBytes = DecodePem(_clientCert);
				var clientCert = Certificate.Parse(certBytes);

				// Load private key

				AsymmetricKeyParameter  clientPrivateKey;

				using(var stream = new FileStream(_clientKey, FileMode.Open))
				using (var textReader = new StreamReader(stream))
				{
					var pem = new Org.BouncyCastle.OpenSsl.PemReader(textReader);
					var pemObj = pem.ReadObject();

					if (pemObj is AsymmetricCipherKeyPair)
					{
						var keys = (AsymmetricCipherKeyPair) pemObj;
						clientPrivateKey = keys.Private;
					}
					else if (pemObj is RsaPrivateCrtKeyParameters)
						clientPrivateKey = (AsymmetricKeyParameter)pemObj;
					else if (pemObj is DsaPrivateKeyParameters)
						clientPrivateKey = (AsymmetricKeyParameter)pemObj;
					else if (pemObj is ECPrivateKeyParameters)
						clientPrivateKey = (AsymmetricKeyParameter)pemObj;
					else
						throw new PeachException("Error, ClientKey for SslClientPublisher is in unknown format.");
				}
				
				// Figure out signing method

				byte sigAlg;
				if (clientPrivateKey is RsaKeyParameters)
					sigAlg = SignatureAlgorithm.rsa;
				else if (clientPrivateKey is DsaPrivateKeyParameters)
					sigAlg = SignatureAlgorithm.dsa;
				else if (clientPrivateKey is ECPrivateKeyParameters)
					sigAlg = SignatureAlgorithm.ecdsa;
				else
					throw new PeachException("Error, ClientKey provided is not of a supported type. RSA, DSA or ECDSA keys are supported.");

				SignatureAndHashAlgorithm sigAndHashAlg = null;
				if (certificateRequest.SupportedSignatureAlgorithms != null)
				{
					foreach (SignatureAndHashAlgorithm alg in certificateRequest.SupportedSignatureAlgorithms)
					{
						if (alg.Signature == sigAlg)
						{
							sigAndHashAlg = alg;
							break;
						}
					}

					if (sigAndHashAlg == null)
						throw new PeachException("Error, SSL server does not support signature algorithms compatable with private key.");
				}

				//return null;
				return new DefaultTlsSignerCredentials(_tlsContext, clientCert, clientPrivateKey,
					sigAndHashAlg);
			}

			public void NotifyServerCertificate(Certificate serverCertificate)
			{
				// validate server certificate
			}
		}

		#endregion

		#region AsyncHelper
		class TlsAsyncRead : IAsyncResult
		{
			private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
			protected NLog.Logger Logger { get { return logger; } }

			protected Thread m_thread;
			protected Stream m_stream;
			protected byte[] m_buffer = null;
			protected int m_offset = 0;
			protected int m_count = 0;
			public int m_read_count = 0;
			protected ManualResetEvent m_event = new ManualResetEvent(false);
			protected AsyncCallback _callback;
			//Timer m_timer;

			public TlsAsyncRead(
				Stream stream,
				byte[] buffer,
				int offset,
				int count,
				AsyncCallback asyncCallback,
				object state)
			{
				m_stream = stream;
				m_buffer = buffer;
				m_offset = offset;
				m_count = count;
				_callback = asyncCallback;
			}

			public void Process()
			{
				m_thread = new Thread(new ThreadStart(Run));
				m_thread.Start();
			}

			void bk_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
			{
				Run();
			}

			public void Join()
			{
				if (Thread.CurrentThread == m_thread)
					return;

				m_thread.Join();
			}

			public void Run()
			{
				try
				{
					m_read_count = m_stream.Read(m_buffer, m_offset, m_count);
					//m_event.Set();

					//if (_callback != null)
					//	_callback(this);
				}
				catch (ThreadAbortException)
				{
					m_read_count = 0;
				}
				catch (Exception)
				{
					m_read_count = 0;
				}
				finally
				{
					m_event.Set();

					if (_callback != null)
						_callback(this);
				}
			}

			public object AsyncState
			{
				get { throw new NotImplementedException(); }
			}

			public WaitHandle AsyncWaitHandle
			{
				get { return m_event; }
			}

			public bool CompletedSynchronously
			{
				get { return false; }
			}

			public bool IsCompleted
			{
				get { return m_event.WaitOne(1); }
			}

			public void Cancel()
			{
				m_thread.Abort();
			}
		}

		internal class TlsAsyncWrite : IAsyncResult
		{
			protected ManualResetEvent m_event = new ManualResetEvent(true);

			public TlsAsyncWrite(
				AsyncCallback asyncCallback,
				object state,
				object owner,
				string operationId)
			{
			}

			public object AsyncState
			{
				get { throw new NotImplementedException(); }
			}

			public WaitHandle AsyncWaitHandle
			{
				get { return m_event; }
			}

			public bool CompletedSynchronously
			{
				get { return true; }
			}

			public bool IsCompleted
			{
				get { return true; }
			}
		}

		#endregion

		#region MyTlsStream

		private class MyTlsStream : Stream
		{
			private readonly Stream _stream;

			public MyTlsStream(Stream stream)
			{
				_stream = stream;
			}

			private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

			protected NLog.Logger Logger
			{
				get { return logger; }
			}

			#pragma warning disable 414
			private object _lock = new object();
			#pragma warning restore  414

			private int _readTimeout = -1;
			private int _writeTimeout = -1;

			public override int ReadTimeout
			{
				get { return _readTimeout; }
				set { _readTimeout = value; }
			}

			public override int WriteTimeout
			{
				get { return _writeTimeout; }
				set { _writeTimeout = value; }
			}

			public override bool CanRead
			{
				get { return true; }
			}

			public override bool CanSeek
			{
				get { return false; }
			}

			public override bool CanWrite
			{
				get { return _stream.CanWrite; }
			}

			public override void Flush()
			{
				_stream.Flush();
			}

			public override long Length
			{
				get { return _stream.Length; }
			}

			public override long Position
			{
				get { return _stream.Position; }
				set { _stream.Position = value; }
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				return _stream.Read(buffer, offset, count);
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				return _stream.Seek(offset, origin);
			}

			public override void SetLength(long value)
			{
				_stream.SetLength(value);
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				_stream.Write(buffer, offset, count);
			}

			public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
			{
				Write(buffer, offset, count);
				return new TlsAsyncWrite(callback, state, null, null);
			}

			public override void EndWrite(IAsyncResult asyncResult)
			{
			}

			private TlsAsyncRead m_asyncRead = null;

			public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
			{
				var ar = new TlsAsyncRead(this, buffer, offset, count, callback, state);
				ar.Process();
				m_asyncRead = ar;

				return ar;
			}

			public override int EndRead(IAsyncResult asyncResult)
			{
				asyncResult.AsyncWaitHandle.WaitOne();
				((TlsAsyncRead) asyncResult).Join();
				if (m_asyncRead == asyncResult)
					m_asyncRead = null;

				return ((TlsAsyncRead) asyncResult).m_read_count;
			}

			public override void Close()
			{
				if (m_asyncRead != null)
					m_asyncRead.Cancel();

				_stream.Close();
				base.Close();
			}
		}

		#endregion

		#region Stream Passthru

		class StreamMux : Stream
		{
			private readonly Stream _stream;

			public StreamMux(Stream stream)
			{
				_stream = stream;
			}

			public override void Flush()
			{
				_stream.Flush();
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				return _stream.Seek(offset, origin);
			}

			public override void SetLength(long value)
			{
				_stream.SetLength(value);
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				return _stream.Read(buffer, offset, count);
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				_stream.Write(buffer, offset, count);
			}

			public override bool CanRead
			{
				get { return _stream.CanRead; }
			}

			public override bool CanSeek
			{
				get { return _stream.CanSeek; }
			}

			public override bool CanWrite
			{
				get { return _stream.CanWrite; }
			}

			public override long Length
			{
				get { return _stream.Length; }
			}

			public override long Position
			{
				get { return _stream.Position; }
				set { _stream.Position = value; }
			}
		}

		#endregion

		public bool VerifyServer { get; protected set; }
		public string Host { get; protected set; }
		public string Sni { get; protected set; }
		public int ConnectTimeout { get; protected set; }
		public ushort Port { get; protected set; }
		public string ClientKey { get; protected set; }
		public string ClientCert { get; protected set; }
		public string Alpn { get; protected set; }

		protected TcpClient _tcp = null;
		protected EndPoint _localEp = null;
		protected EndPoint _remoteEp = null;

		private StreamMux _writeStream = null;
		private TlsClientProtocol _tlsClientHandler = null;

		public SslClientPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		protected override void OnOpen()
		{
			base.OnOpen();

			var timeout = ConnectTimeout;
			var sw = new Stopwatch();

			if (_tcp != null || _client != null)
			{
				Logger.Warn("open: Found non-null _tcp or _client object. Cleaning up.");
				ClientShutdown();
			}

			for (int i = 1; _tcp == null; i *= 2)
			{
				try
				{
					// Must build a new client object after every failed attempt to connect.
					// For some reason, just calling BeginConnect again does not work on mono.
					_tcp = new TcpClient();

					sw.Restart();

					var ar = _tcp.BeginConnect(Host, Port, null, null);
					if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeout)))
						throw new TimeoutException();
					_tcp.EndConnect(ar);
				}
				catch (Exception ex)
				{
					sw.Stop();

					if (_tcp != null)
					{
						_tcp.Close();
						_tcp = null;
					}

					timeout -= (int)sw.ElapsedMilliseconds;

					if (timeout > 0)
					{
						int waitTime = Math.Min(timeout, i);
						timeout -= waitTime;

						Logger.Warn("open: Warn, Unable to connect to remote host {0} on port {1}.  Trying again in {2}ms...", Host, Port, waitTime);
						Thread.Sleep(waitTime);
					}
					else
					{
						Logger.Error("open: Error, Unable to connect to remote host {0} on port {1}.", Host, Port);
						throw new SoftException(ex);
					}
				}
			}

			Debug.Assert(_client == null);

			try
			{
				_tlsClientHandler = new TlsClientProtocol(_tcp.GetStream(), new SecureRandom());

				var tlsClient = new MyTlsClient(ClientCert, ClientKey)
				{
					Alpn = Alpn
				};

				_tlsClientHandler.Connect(tlsClient);

			}
			catch (Exception ex)
			{
				Logger.Error("open: Error, Unable to perform TLS connection. {0}.", ex.Message);

				try
				{
					_tlsClientHandler.Close();
					_tlsClientHandler = null;
				}
				catch
				{
					// Ignore anything bouncy castle would throw at us
				}

				Debug.Assert(_tcp != null);

				_tcp.Close();
				_tcp = null;

				throw new SoftException(ex);
			}

			_client = _tlsClientHandler.Stream;
			_localEp = _tcp.Client.LocalEndPoint;
			_remoteEp = _tcp.Client.RemoteEndPoint;
			_clientName = _remoteEp.ToString();

			// .NET includes a helper to allow BeginRead() and BeginWrite()
			// to perform async operations on streams that only expose Read() and Write().
			// Unfortunatley, it only allows one outstanding async operation to be pending
			// meaning if a call to BeginRead() has completed, a call to BeginWrite()
			// will not return until EndRead() is called.
			// To work artound this we just wrap the client stream in another stream
			// object allowing us to have independent async read and write operations.

			// See BeginReadInternal semaphore.Wait() in the Stream.cs reference sources

			_writeStream = new StreamMux(_client);

			StartClient();
		}

		protected override void ClientShutdown()
		{
			try
			{
				if (_client != null)
					_client.Close();
				if (_writeStream != null)
					_writeStream.Close();
				if (_tlsClientHandler != null)
					_tlsClientHandler.Close();
				if (_tcp != null)
					_tcp.Close();
			}
			catch (Exception)
			{
				//ignore
			}
			finally
			{
				// Don't null this out yet! The ReadComplete callback in
				// BufferedStream will check.
				//_client = null;

				_writeStream = null;
				_tlsClientHandler = null;
				_tcp = null;
				_remoteEp = null;
				_localEp = null;
			}
		}

		protected override void ClientClose()
		{
			ClientShutdown();

			_client = null;
		}

		protected override IAsyncResult ClientBeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			if (Logger.IsTraceEnabled) Logger.Trace("ClientBeginWrite> offset: {0} count: {1}", offset, count);
			_sendLen = count;
			return _writeStream.BeginWrite(buffer, offset, count, callback, state);
		}

		protected override int ClientEndWrite(IAsyncResult asyncResult)
		{
			_writeStream.EndWrite(asyncResult);
			return _sendLen;
		}
	}
}
