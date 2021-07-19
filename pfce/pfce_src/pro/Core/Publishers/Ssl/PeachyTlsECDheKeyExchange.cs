using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Security;

namespace Peach.Pro.Core.Publishers.Ssl
{
	public class PeachyTlsECDheKeyExchange : TlsECDheKeyExchange
	{
		public PeachyTlsECDheKeyExchange(int keyExchange, IList supportedSignatureAlgorithms, int[] namedCurves, byte[] clientECPointFormats, byte[] serverECPointFormats)
			: base(keyExchange, supportedSignatureAlgorithms, namedCurves, clientECPointFormats, serverECPointFormats)
		{
		}

		/// <summary>
		/// Same as base function, but skip KeyUsage checks
		/// </summary>
		/// <param name="serverCertificate"></param>
		public override void ProcessServerCertificate(Certificate serverCertificate)
		{
			if (serverCertificate.IsEmpty)
				throw new TlsFatalAlert(AlertDescription.bad_certificate);

			X509CertificateStructure x509Cert = serverCertificate.GetCertificateAt(0);

			SubjectPublicKeyInfo keyInfo = x509Cert.SubjectPublicKeyInfo;
			try
			{
				this.mServerPublicKey = PublicKeyFactory.CreateKey(keyInfo);
			}
			catch (Exception e)
			{
				throw new TlsFatalAlert(AlertDescription.unsupported_certificate, e);
			}

			if (mTlsSigner == null)
			{
				try
				{
					this.mECAgreePublicKey = TlsEccUtilities.ValidateECPublicKey((ECPublicKeyParameters)this.mServerPublicKey);
				}
				catch (InvalidCastException e)
				{
					throw new TlsFatalAlert(AlertDescription.certificate_unknown, e);
				}
			}
			else
			{
				if (!mTlsSigner.IsValidPublicKey(this.mServerPublicKey))
					throw new TlsFatalAlert(AlertDescription.certificate_unknown);
			}

			// AbstractTlsKeyExchange version is empty, so skip
			//base.ProcessServerCertificate(serverCertificate);
		}
	}
}
