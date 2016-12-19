using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;

namespace EaseServerAPI.Authentication
{
    /// <summary>
    /// The "basic" authentication scheme is based on the model that the
    /// client must authenticate itself with a user-ID and a password for
    /// each realm.  The realm value should be considered an opaque string
    /// which can only be compared for equality with other realms on that
    /// server. The server will service the request only if it can validate
    /// the user-ID and password for the protection space of the Request-URI.
    /// There are no optional authentication parameters.
    /// </summary>
    public class BasicAuthentication : AuthenticationModule
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicAuthentication"/> class.
        /// </summary>
        /// <param name="authenticator">Delegate used to provide information used during authentication.</param>
        /// <param name="authenticationRequiredHandler">Delegate used to determine if authentication is required (may be null).</param>
        public BasicAuthentication(AuthenticationHandler authenticator, AuthenticationRequiredHandler authenticationRequiredHandler)
            : base(authenticator, authenticationRequiredHandler)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicAuthentication"/> class.
        /// </summary>
        /// <param name="authenticator">Delegate used to provide information used during authentication.</param>
        public BasicAuthentication(AuthenticationHandler authenticator)
            : base(authenticator)
        {
        }

        /// <summary>
        /// Create a response that can be sent in the WWW-Authenticate header.
        /// </summary>
        /// <param name="realm">Realm that the user should authenticate in</param>
        /// <param name="options">Not used in basic auth</param>
        /// <returns>A correct auth request.</returns>
        public override string CreateResponse(string realm, object[] options)
        {
            if (string.IsNullOrEmpty(realm))
                throw new ArgumentNullException("realm");

            return "Basic realm=\"" + realm + "\"";
        }

        /// <summary>
        /// An authentication response have been received from the web browser.
        /// Check if it's correct
        /// </summary>
        /// <param name="authenticationHeader">Contents from the Authorization header</param>
        /// <param name="realm">Realm that should be authenticated</param>
        /// <param name="httpVerb">GET/POST/PUT/DELETE etc.</param>
        /// <param name="options">Not used in basic auth</param>
        /// <returns>Authentication object that is stored for the request. A user class or something like that.</returns>
        /// <exception cref="ArgumentException">if authenticationHeader is invalid</exception>
        /// <exception cref="ArgumentNullException">If any of the paramters is empty or null.</exception>
        public override object Authenticate(string authenticationHeader, string realm, string httpVerb, object[] options)
        {
            if (string.IsNullOrEmpty(authenticationHeader))
                throw new ArgumentNullException("realm");
            if (string.IsNullOrEmpty(realm))
                throw new ArgumentNullException("realm");
            if (string.IsNullOrEmpty(httpVerb))
                throw new ArgumentNullException("httpVerb");

            /*
             * To receive authorization, the client sends the userid and password,
      separated by a single colon (":") character, within a base64 [7]
      encoded string in the credentials.*/
            authenticationHeader = authenticationHeader.Remove(0, 6);
            string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authenticationHeader));
            int pos = decoded.IndexOf(':');
            if (pos == -1)
                return null;

            string ourPw = decoded.Substring(pos + 1, decoded.Length - pos - 1);
            string pw = ourPw;
            object state;
            CheckAuthentication(realm, decoded.Substring(0, pos), ref pw, out state);

            return ourPw == pw ? state : null;
        }

        /// <summary>
        /// name used in http request.
        /// </summary>
        public override string Name
        {
            get { return "basic"; }
        }
    }

    /// <summary>
    /// Implements HTTP Digest authentication. It's more secure than Basic auth since password is 
    /// encrypted with a "key" from the server. 
    /// </summary>
    /// <remarks>
    /// Keep in mind that the password is encrypted with MD5. Use a combination of SSL and digest auth to be secure.
    /// </remarks>
    public class DigestAuthentication : AuthenticationModule
    {
        static readonly Dictionary<string, DateTime> _nonces = new Dictionary<string, DateTime>();
        private static Timer _timer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DigestAuthentication"/> class.
        /// </summary>
        /// <param name="authenticator">Delegate used to provide information used during authentication.</param>
        /// <param name="authenticationRequiredHandler">Delegate used to determine if authentication is required (may be null).</param>
        public DigestAuthentication(AuthenticationHandler authenticator, AuthenticationRequiredHandler authenticationRequiredHandler)
            : base(authenticator, authenticationRequiredHandler)
        {
			TokenIsHA1 = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DigestAuthentication"/> class.
        /// </summary>
        /// <param name="authenticator">Delegate used to provide information used during authentication.</param>
        public DigestAuthentication(AuthenticationHandler authenticator)
            : base(authenticator)
        {
        }

        /// <summary>
        /// Used by test classes to be able to use hardcoded values
        /// </summary>
        public static bool DisableNonceCheck;

        /// <summary>
        /// name used in http request.
        /// </summary>
        public override string Name
        {
            get { return "digest"; }
        }

        /// <summary>
        /// An authentication response have been received from the web browser.
        /// Check if it's correct
        /// </summary>
        /// <param name="authenticationHeader">Contents from the Authorization header</param>
        /// <param name="realm">Realm that should be authenticated</param>
        /// <param name="httpVerb">GET/POST/PUT/DELETE etc.</param>
        /// <param name="options">First option: true if username/password is correct but not cnonce</param>
        /// <returns>
        /// Authentication object that is stored for the request. A user class or something like that.
        /// </returns>
        /// <exception cref="ArgumentException">if authenticationHeader is invalid</exception>
        /// <exception cref="ArgumentNullException">If any of the paramters is empty or null.</exception>
        public override object Authenticate(string authenticationHeader, string realm, string httpVerb, object[] options)
        {
            lock (_nonces)
            {
                if (_timer == null)
                    _timer = new Timer(ManageNonces, null, 15000, 15000);
            }

            if (!authenticationHeader.StartsWith("Digest", true, CultureInfo.CurrentCulture))
                return null;

            bool staleNonce;
            if (options.Length > 0)
                staleNonce = (bool)options[0];
            else staleNonce = false;

            NameValueCollection reqInfo = Decode(authenticationHeader, Encoding.UTF8);
            if (!IsValidNonce(reqInfo["nonce"]) && !DisableNonceCheck)
                return null;

            string username = reqInfo["username"];
            string password = string.Empty;
            object state;

            if (!CheckAuthentication(realm, username, ref password, out state))
                return null;

            string HA1;
            if (!TokenIsHA1)
            {
                string A1 = String.Format("{0}:{1}:{2}", username, realm, password);
                HA1 = GetMD5HashBinHex2(A1);
            }
            else
                HA1 = password;

            string A2 = String.Format("{0}:{1}", httpVerb, reqInfo["uri"]);
            string HA2 = GetMD5HashBinHex2(A2);
            string hashedDigest = Encrypt(HA1, HA2, reqInfo["qop"],
                                          reqInfo["nonce"], reqInfo["nc"], reqInfo["cnonce"]);

            if (reqInfo["response"] == hashedDigest && !staleNonce)
                return state;

            return null;
        }

        /// <summary>
        /// Gets or sets whether the token supplied in <see cref="AuthenticationHandler"/> is a
        /// HA1 generated string.
        /// </summary>
        public bool TokenIsHA1 { get; set; }

        /// <summary>
        /// Encrypts parameters into a Digest string
        /// </summary>
        /// <param name="realm">Realm that the user want to log into.</param>
        /// <param name="userName">User logging in</param>
        /// <param name="password">Users password.</param>
        /// <param name="method">HTTP method.</param>
        /// <param name="uri">Uri/domain that generated the login prompt.</param>
        /// <param name="qop">Quality of Protection.</param>
        /// <param name="nonce">"Number used ONCE"</param>
        /// <param name="nc">Hexadecimal request counter.</param>
        /// <param name="cnonce">"Client Number used ONCE"</param>
        /// <returns>Digest encrypted string</returns>
        public static string Encrypt(string realm, string userName, string password, string method, string uri, string qop, string nonce, string nc, string cnonce)
        {
            string A1 = String.Format("{0}:{1}:{2}", userName, realm, password);
            string HA1 = GetMD5HashBinHex2(A1);
            string A2 = String.Format("{0}:{1}", method, uri);
            string HA2 = GetMD5HashBinHex2(A2);

            string unhashedDigest;
            if (qop != null)
            {
                unhashedDigest = String.Format("{0}:{1}:{2}:{3}:{4}:{5}",
                                               HA1,
                                               nonce,
                                               nc,
                                               cnonce,
                                               qop,
                                               HA2);
            }
            else
            {
                unhashedDigest = String.Format("{0}:{1}:{2}",
                                               HA1,
                                               nonce,
                                               HA2);
            }

            return GetMD5HashBinHex2(unhashedDigest);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ha1">Md5 hex encoded "userName:realm:password", without the quotes.</param>
        /// <param name="ha2">Md5 hex encoded "method:uri", without the quotes</param>
        /// <param name="qop">Quality of Protection</param>
        /// <param name="nonce">"Number used ONCE"</param>
        /// <param name="nc">Hexadecimal request counter.</param>
        /// <param name="cnonce">Client number used once</param>
        /// <returns></returns>
        protected virtual string Encrypt(string ha1, string ha2, string qop, string nonce, string nc, string cnonce)
        {
            string unhashedDigest;
            if (qop != null)
            {
                unhashedDigest = String.Format("{0}:{1}:{2}:{3}:{4}:{5}",
                                               ha1,
                                               nonce,
                                               nc,
                                               cnonce,
                                               qop,
                                               ha2);
            }
            else
            {
                unhashedDigest = String.Format("{0}:{1}:{2}",
                                               ha1,
                                               nonce,
                                               ha2);
            }

            return GetMD5HashBinHex2(unhashedDigest);
        }
        private static void ManageNonces(object state)
        {
            lock (_nonces)
            {
                foreach (KeyValuePair<string, DateTime> pair in _nonces)
                {
                    if (pair.Value >= DateTime.Now)
                        continue;

                    _nonces.Remove(pair.Key);
                    return;
                }
            }
        }


        /// <summary>
        /// Create a response that can be sent in the WWW-Authenticate header.
        /// </summary>
        /// <param name="realm">Realm that the user should authenticate in</param>
        /// <param name="options">First options specifies if true if username/password is correct but not cnonce.</param>
        /// <returns>A correct auth request.</returns>
        /// <exception cref="ArgumentNullException">If realm is empty or null.</exception>
        public override string CreateResponse(string realm, object[] options)
        {
            string nonce = GetCurrentNonce();

            StringBuilder challenge = new StringBuilder("Digest realm=\"");
            challenge.Append(realm);
            challenge.Append("\"");
            challenge.Append(", nonce=\"");
            challenge.Append(nonce);
            challenge.Append("\"");
            challenge.Append(", opaque=\"" + Guid.NewGuid().ToString().Replace("-", string.Empty) + "\"");
            challenge.Append(", stale=");

            if (options.Length > 0)
                challenge.Append((bool)options[0] ? "true" : "false");
            else
                challenge.Append("false");

            challenge.Append(", algorithm=MD5");
            challenge.Append(", qop=auth");

            return challenge.ToString();
        }

        /// <summary>
        /// Decodes authorization header value
        /// </summary>
        /// <param name="buffer">header value</param>
        /// <param name="encoding">Encoding that the buffer is in</param>
        /// <returns>All headers and their values if successful; otherwise null</returns>
        /// <example>
        /// NameValueCollection header = DigestAuthentication.Decode("response=\"6629fae49393a05397450978507c4ef1\",\r\nc=00001", Encoding.ASCII);
        /// </example>
        /// <remarks>Can handle lots of whitespaces and new lines without failing.</remarks>
        public static NameValueCollection Decode(string buffer, Encoding encoding)
        {
            if (string.Compare(buffer.Substring(0, 7), "Digest ", true) == 0)
                buffer = buffer.Remove(0, 7).Trim(' ');

            NameValueCollection values = new NameValueCollection();
            int step = 0;
            bool inQuote = false;
            string name = string.Empty;
            int start = 0;
            for (int i = start; i < buffer.Length; ++i)
            {
                char ch = buffer[i];
                if (ch == '"')
                    inQuote = !inQuote;

                //find start of name
                switch (step)
                {
                    case 0:
                        if (!char.IsWhiteSpace(ch))
                        {
                            if (!char.IsLetterOrDigit(ch) && ch != '"')
                                return null;
                            start = i;
                            ++step;
                        }
                        break;
                    case 1:
                        if (char.IsWhiteSpace(ch) || ch == '=')
                        {
                            if (start == -1)
                                return null;
                            name = buffer.Substring(start, i - start);
                            start = -1;
                            ++step;
                        }
                        else if (!char.IsLetterOrDigit(ch) && ch != '"')
                            return null;
                        break;
                    case 2:
                        if (!char.IsWhiteSpace(ch) && ch != '=')
                        {
                            start = i;
                            ++step;
                        }
                        break;
                }
                // find end of value
                if (step == 3)
                {
                    if (inQuote)
                        continue;

                    if (ch == ',' || char.IsWhiteSpace(ch) || i == buffer.Length - 1)
                    {
                        if (start == -1)
                            return null;

                        int stop = i;
                        if (buffer[start] == '"')
                        {
                            ++start;
                            --stop;
                        }
                        if (i == buffer.Length - 1 || (i == buffer.Length - 2 && buffer[buffer.Length - 1] == '"'))
                            ++stop;

                        values.Add(name.ToLower(), buffer.Substring(start, stop - start));
                        name = string.Empty;
                        start = -1;
                        step = 0;
                    }
                }
            }

            return values.Count == 0 ? null : values;
        }

        /// <summary>
        /// Gets the current nonce.
        /// </summary>
        /// <returns></returns>
        protected virtual string GetCurrentNonce()
        {
            string nonce = Guid.NewGuid().ToString().Replace("-", string.Empty);
            lock (_nonces)
                _nonces.Add(nonce, DateTime.Now.AddSeconds(30));

            return nonce;
        }

        /// <summary>
        /// Gets the Md5 hash bin hex2.
        /// </summary>
        /// <param name="toBeHashed">To be hashed.</param>
        /// <returns></returns>
        public static string GetMD5HashBinHex2(string toBeHashed)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] result = md5.ComputeHash(Encoding.ASCII.GetBytes(toBeHashed));

            StringBuilder sb = new StringBuilder();
            foreach (byte b in result)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        /// <summary>
        /// determines if the nonce is valid or has expired.
        /// </summary>
        /// <param name="nonce">nonce value (check wikipedia for info)</param>
        /// <returns>true if the nonce has not expired.</returns>
        protected virtual bool IsValidNonce(string nonce)
        {
            lock (_nonces)
            {
                if (_nonces.ContainsKey(nonce))
                {
                    if (_nonces[nonce] < DateTime.Now)
                    {
                        _nonces.Remove(nonce);
                        return false;
                    }

                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Delegate used to let authentication modules authenticate the user name and password.
    /// </summary>
    /// <param name="realm">Realm that the user want to authenticate in</param>
    /// <param name="userName">User name specified by client</param>
    /// <param name="token">Can either be user password or implementation specific token.</param>
    /// <param name="login">object that will be stored in a session variable called <see cref="AuthenticationModule.AuthenticationTag"/> if authentication was successful.</param>
    /// <exception cref="ForbiddenException">throw forbidden exception if too many attempts have been made.</exception>
    /// <remarks>
    /// <para>
    /// Use <see cref="DigestAuthentication.TokenIsHA1"/> to specify that the token is a HA1 token. (MD5 generated
    /// string from realm, user name and password); Md5String(userName + ":" + realm + ":" + password);
    /// </para>
    /// </remarks>
    public delegate void AuthenticationHandler(string realm, string userName, ref string token, out object login);

    /// <summary>
    /// Let's you decide on a system level if authentication is required.
    /// </summary>
    /// <param name="request">HTTP request from client</param>
    /// <returns>true if user should be authenticated.</returns>
    /// <remarks>throw <see cref="ForbiddenException"/> if no more attempts are allowed.</remarks>
    /// <exception cref="ForbiddenException">If no more attempts are allowed</exception>
    public delegate bool AuthenticationRequiredHandler(HttpRequest request);

    /// <summary>
    /// Authentication modules are used to implement different
    /// kind of HTTP authentication.
    /// </summary>
    public abstract class AuthenticationModule
    {
        private readonly AuthenticationHandler _authenticator;
        private readonly AuthenticationRequiredHandler _authenticationRequiredHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationModule"/> class.
        /// </summary>
        /// <param name="authenticator">Delegate used to provide information used during authentication.</param>
        /// <param name="authenticationRequiredHandler">Delegate used to determine if authentication is required (may be null).</param>
        protected AuthenticationModule(AuthenticationHandler authenticator, AuthenticationRequiredHandler authenticationRequiredHandler)
        {
            Check.Require(authenticator, "authenticator");
            _authenticationRequiredHandler = authenticationRequiredHandler;
            _authenticator = authenticator;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationModule"/> class.
        /// </summary>
        /// <param name="authenticator">Delegate used to provide information used during authentication.</param>
        protected AuthenticationModule(AuthenticationHandler authenticator)
            : this(authenticator, null)
        {
        }

        /// <summary>
        /// name used in HTTP request.
        /// </summary>
        public abstract string Name
        { get; }

        /// <summary>
        /// Tag used for authentication.
        /// </summary>
        public const string AuthenticationTag = "__authtag";

        /// <summary>
        /// Create a response that can be sent in the WWW-Authenticate header.
        /// </summary>
        /// <param name="realm">Realm that the user should authenticate in</param>
        /// <param name="options">Array with optional options.</param>
        /// <returns>A correct authentication request.</returns>
        /// <exception cref="ArgumentNullException">If realm is empty or null.</exception>
        public abstract string CreateResponse(string realm, params object[] options);

        /// <summary>
        /// An authentication response have been received from the web browser.
        /// Check if it's correct
        /// </summary>
        /// <param name="authenticationHeader">Contents from the Authorization header</param>
        /// <param name="realm">Realm that should be authenticated</param>
        /// <param name="httpVerb">GET/POST/PUT/DELETE etc.</param>
        /// <param name="options">options to specific implementations</param>
        /// <returns>Authentication object that is stored for the request. A user class or something like that.</returns>
        /// <exception cref="ArgumentException">if <paramref name="authenticationHeader"/> is invalid</exception>
        /// <exception cref="ArgumentNullException">If any of the parameters is empty or null.</exception>
        public abstract object Authenticate(string authenticationHeader, string realm, string httpVerb,
                                            params object[] options);

        /// <summary>
        /// Used to invoke the authentication delegate that is used to lookup the user name/realm.
        /// </summary>
        /// <param name="realm">Realm (domain) that user want to authenticate in</param>
        /// <param name="userName">User name</param>
        /// <param name="password">Password used for validation. Some implementations got password in clear text, they are then sent to client.</param>
        /// <param name="login">object that will be stored in the request to help you identify the user if authentication was successful.</param>
        /// <returns>true if authentication was successful</returns>
        protected bool CheckAuthentication(string realm, string userName, ref string password, out object login)
        {
            _authenticator(realm, userName, ref password, out login);
            return true;
        }

        /// <summary>
        /// Determines if authentication is required.
        /// </summary>
        /// <param name="request">HTTP request from browser</param>
        /// <returns>true if user should be authenticated.</returns>
        /// <remarks>throw <see cref="ForbiddenException"/> from your delegate if no more attempts are allowed.</remarks>
        /// <exception cref="ForbiddenException">If no more attempts are allowed</exception>
        public bool AuthenticationRequired(HttpRequest request)
        {
            return _authenticationRequiredHandler != null && _authenticationRequiredHandler(request);
        }
    }

    /// <summary>
    /// Small design by contract implementation.
    /// </summary>
    public static class Check
    {
        /// <summary>
        /// Check whether a parameter is empty.
        /// </summary>
        /// <param name="value">Parameter value</param>
        /// <param name="parameterOrErrorMessage">Parameter name, or error description.</param>
        /// <exception cref="ArgumentException">value is empty.</exception>
        public static void NotEmpty(string value, string parameterOrErrorMessage)
        {
            if (!string.IsNullOrEmpty(value))
                return;

            if (parameterOrErrorMessage.IndexOf(' ') == -1)
                throw new ArgumentException("'" + parameterOrErrorMessage + "' cannot be empty.", parameterOrErrorMessage);

            throw new ArgumentException(parameterOrErrorMessage);
        }

        /// <summary>
        /// Checks whether a parameter is null.
        /// </summary>
        /// <param name="value">Parameter value</param>
        /// <param name="parameterOrErrorMessage">Parameter name, or error description.</param>
        /// <exception cref="ArgumentNullException">value is null.</exception>
        public static void Require(object value, string parameterOrErrorMessage)
        {
            if (value != null)
                return;

            if (parameterOrErrorMessage.IndexOf(' ') == -1)
                throw new ArgumentNullException("'" + parameterOrErrorMessage + "' cannot be null.", parameterOrErrorMessage);

            throw new ArgumentNullException(parameterOrErrorMessage);

        }

        /// <summary>
        /// Checks whether a parameter is null.
        /// </summary>
        /// <param name="minValue"></param>
        /// <param name="value">Parameter value</param>
        /// <param name="parameterOrErrorMessage">Parameter name, or error description.</param>
        /// <exception cref="ArgumentException">value is null.</exception>
        public static void Min(int minValue, object value, string parameterOrErrorMessage)
        {
            if (value != null)
                return;

            if (parameterOrErrorMessage.IndexOf(' ') == -1)
                throw new ArgumentException("'" + parameterOrErrorMessage + "' must be at least " + minValue + ".", parameterOrErrorMessage);

            throw new ArgumentException(parameterOrErrorMessage);

        }
    }
}