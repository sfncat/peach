using System;
using System.Collections;

namespace Org.BouncyCastle.Crypto.Tls.Tests
{
    public class TlsTestConfig
    {
        public static readonly bool DEBUG = false;

        /**
         * Client does not authenticate, ignores any certificate request
         */
        public const int CLIENT_AUTH_NONE = 0;

        /**
         * Client will authenticate if it receives a certificate request
         */
        public const int CLIENT_AUTH_VALID = 1;

        /**
         * Client will authenticate if it receives a certificate request, with an invalid certificate
         */
        public const int CLIENT_AUTH_INVALID_CERT = 2;

        /**
         * Client will authenticate if it receives a certificate request, with an invalid CertificateVerify signature
         */
        public const int CLIENT_AUTH_INVALID_VERIFY = 3;

        /**
         * Server will not request a client certificate
         */
        public const int SERVER_CERT_REQ_NONE = 0;

        /**
         * Server will request a client certificate but receiving one is optional
         */
        public const int SERVER_CERT_REQ_OPTIONAL = 1;

        /**
         * Server will request a client certificate and receiving one is mandatory
         */
        public const int SERVER_CERT_REQ_MANDATORY = 2;

        /**
         * Configures the client authentication behaviour of the test client. Use CLIENT_AUTH_* constants.
         */
        public int clientAuth = CLIENT_AUTH_VALID;

        /**
         * If not null, and TLS 1.2 or higher is negotiated, selects a fixed signature/hash algorithm to
         * be used for the CertificateVerify signature (if one is sent).
         */
        public SignatureAndHashAlgorithm clientAuthSigAlg = null;

        /**
         * If not null, and TLS 1.2 or higher is negotiated, selects a fixed signature/hash algorithm to
         * be _claimed_ in the CertificateVerify (if one is sent), independently of what was actually used.
         */
        public SignatureAndHashAlgorithm clientAuthSigAlgClaimed = null;

        /**
         * Configures the minimum protocol version the client will accept. If null, uses the library's default.
         */
        public ProtocolVersion clientMinimumVersion = null;

        /**
         * Configures the protocol version the client will offer. If null, uses the library's default.
         */
        public ProtocolVersion clientOfferVersion = null;

        /**
         * Configures whether the client will indicate version fallback via TLS_FALLBACK_SCSV.
         */
        public bool clientFallback = false;

        /**
         * Configures whether a (TLS 1.2+) client will send the signature_algorithms extension in ClientHello.
         */
        public bool clientSendSignatureAlgorithms = true;

        /**
         * If not null, and TLS 1.2 or higher is negotiated, selects a fixed signature/hash algorithm to
         * be used for the ServerKeyExchange signature (if one is sent).
         */
        public SignatureAndHashAlgorithm serverAuthSigAlg = null;

        /**
         * Configures whether the test server will send a certificate request.
         */
        public int serverCertReq = SERVER_CERT_REQ_OPTIONAL;

        /**
         * If TLS 1.2 or higher is negotiated, configures the set of supported signature algorithms in the
         * CertificateRequest (if one is sent). If null, uses a default set.
         */
        public IList serverCertReqSigAlgs = null;

        /**
         * Configures the maximum protocol version the server will accept. If null, uses the library's default.
         */
        public ProtocolVersion serverMaximumVersion = null;

        /**
         * Configures the minimum protocol version the server will accept. If null, uses the library's default.
         */
        public ProtocolVersion serverMinimumVersion = null;

        /**
         * Configures the connection end that a fatal alert is expected to be raised. Use ConnectionEnd.* constants.
         */
        public int expectFatalAlertConnectionEnd = -1;

        /**
         * Configures the type of fatal alert expected to be raised. Use AlertDescription.* constants.
         */
        public int expectFatalAlertDescription = -1;

        public void ExpectClientFatalAlert(byte alertDescription)
        {
            this.expectFatalAlertConnectionEnd = ConnectionEnd.client;
            this.expectFatalAlertDescription = alertDescription;
        }

        public void ExpectServerFatalAlert(byte alertDescription)
        {
            this.expectFatalAlertConnectionEnd = ConnectionEnd.server;
            this.expectFatalAlertDescription = alertDescription;
        }
    }
}
