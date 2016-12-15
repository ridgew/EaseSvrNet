using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace EaseServer.Security
{
    /// <summary>
    /// Client X.509 certificate, X.509 chain, and any SSL policy errors encountered
    /// during the SSL stream creation
    /// </summary>
    public class ClientCertificate
    {
        /// <summary>
        /// Client security certificate
        /// </summary>
        public readonly X509Certificate Certificate;

        /// <summary>
        /// Client security certificate chain
        /// </summary>
        public readonly X509Chain Chain;

        /// <summary>
        /// Any SSL policy errors encountered during the SSL stream creation
        /// </summary>
        public readonly SslPolicyErrors SslPolicyErrors;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="certificate">Certificate</param>
        /// <param name="chain">Certificate chain</param>
        /// <param name="sslPolicyErrors">SSL policy errors</param>
        public ClientCertificate(X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            Certificate = certificate;
            Chain = chain;
            SslPolicyErrors = sslPolicyErrors;
        }
    }
}
