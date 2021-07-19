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
	public class PeachyTlsDheKeyExchange : TlsDheKeyExchange
	{
		public PeachyTlsDheKeyExchange(int keyExchange, IList supportedSignatureAlgorithms, DHParameters dhParameters)
			: base(keyExchange, supportedSignatureAlgorithms, dhParameters)
		{
		}

		/// <summary>
		/// Skip keyusage checks
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
					this.mDHAgreePublicKey = TlsDHUtilities.ValidateDHPublicKey((DHPublicKeyParameters)this.mServerPublicKey);
					this.mDHParameters = ValidateDHParameters(mDHAgreePublicKey.Parameters);
				}
				catch (InvalidCastException e)
				{
					throw new TlsFatalAlert(AlertDescription.certificate_unknown, e);
				}

				//TlsUtilities.ValidateKeyUsage(x509Cert, KeyUsage.KeyAgreement);
			}
			else
			{
				if (!mTlsSigner.IsValidPublicKey(this.mServerPublicKey))
				{
					throw new TlsFatalAlert(AlertDescription.certificate_unknown);
				}

				//TlsUtilities.ValidateKeyUsage(x509Cert, KeyUsage.DigitalSignature);
			}

			//base.ProcessServerCertificate(serverCertificate);
		}
	}
}
