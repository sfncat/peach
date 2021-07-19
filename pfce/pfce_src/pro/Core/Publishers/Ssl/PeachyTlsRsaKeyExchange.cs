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
	public class PeachyTlsRsaKeyExchange : TlsRsaKeyExchange
	{
		public PeachyTlsRsaKeyExchange(IList supportedSignatureAlgorithms)
			: base(supportedSignatureAlgorithms)
		{
		}

		/// <summary>
		/// Skip keyUsage checks
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

			// Sanity check the PublicKeyFactory
			if (this.mServerPublicKey.IsPrivate)
				throw new TlsFatalAlert(AlertDescription.internal_error);

			this.mRsaServerPublicKey = ValidateRsaPublicKey((RsaKeyParameters)this.mServerPublicKey);

			//TlsUtilities.ValidateKeyUsage(x509Cert, KeyUsage.KeyEncipherment);

			//base.ProcessServerCertificate(serverCertificate);
		}

	}
}
