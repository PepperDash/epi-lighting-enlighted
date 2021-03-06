using System;
using System.Text;
using PepperDash.Core;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharp.Net.Https;
using Crestron.SimplSharp.Cryptography;
using SHA1 = System.Security.Cryptography.SHA1;
using RequestType = Crestron.SimplSharp.Net.Https.RequestType;


namespace PepperDash.Essentials.Plugin.EnlightedLighting
{
    /// <summary>
    /// HTTPS Client Class
    /// </summary>
    public class GenericClientHttps : IRestfulComms
    {
        private const string DefaultRequestType = "GET";
        private readonly HttpsClient _client;
        private readonly EnlightedLightingDebug _enlightedDebug = new EnlightedLightingDebug();
        private readonly CrestronQueue<Action> _requestQueue = new CrestronQueue<Action>(20);
        private const bool HeaderUsesApiKey = true;

        /// <summary>
        /// Custom Authorization with ApiKeyData
        /// </summary>
        public AuthorizationApiKeyData AuthorizationApiKeyData { get; set; }

        /// <summary>
        /// Client host address
        /// </summary>
        public string Host { get; private set; }

        /// <summary>
        /// Client port
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Client username
        /// </summary>
        public string Username { get; private set; }

        /// <summary>
        /// Client password
        /// </summary>
        public string Password { get; private set; }

        /// <summary>
        /// Base64 authorization
        /// </summary>
        public string AuthorizationBase64 { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key"></param>
        /// <param name="controlConfig"></param>
        public GenericClientHttps(string key, EssentialsControlPropertiesConfig controlConfig)
        {
            Key = key;
            AuthorizationApiKeyData = new AuthorizationApiKeyData();

            Host = (controlConfig.TcpSshProperties.Port >= 1 && controlConfig.TcpSshProperties.Port <= 65535)
                ? String.Format("https://{0}:{1}", controlConfig.TcpSshProperties.Address,
                    controlConfig.TcpSshProperties.Port)
                : String.Format("https://{0}", controlConfig.TcpSshProperties.Address);
            Port = (controlConfig.TcpSshProperties.Port >= 1 && controlConfig.TcpSshProperties.Port <= 65535)
                ? controlConfig.TcpSshProperties.Port
                : 443;
            Username = controlConfig.TcpSshProperties.Username ?? "";
            Password = controlConfig.TcpSshProperties.Password ?? "";
            AuthorizationBase64 = EncodeBase64(Username, Password);

            Debug.Console(_enlightedDebug.DebugVerbose, this, "{0}", new String('-', 80));
            Debug.Console(_enlightedDebug.DebugVerbose, this, "GenericClient: Key = {0}", Key);
            Debug.Console(_enlightedDebug.DebugVerbose, this, "GenericClient: Host = {0}", Host);
            Debug.Console(_enlightedDebug.DebugVerbose, this, "GenericClient: Port = {0}", Port);
            Debug.Console(_enlightedDebug.DebugVerbose, this, "GenericClient: Username = {0}", Username);
            Debug.Console(_enlightedDebug.DebugVerbose, this, "GenericClient: Password = {0}", Password);
            Debug.Console(_enlightedDebug.DebugVerbose, this, "GenericClient: AuthorizationBase64 = {0}", AuthorizationBase64);     

            _client = new HttpsClient
            {
                Url = new UrlParser(Host),
                UserName = Username,
                Password = Password,
                KeepAlive = false,
                HostVerification = false,
                PeerVerification = false,
                Verbose = true                
            };
            
            Debug.Console(_enlightedDebug.DebugVerbose, this, "_clientUrl: {0}", _client.Url.ToString());
            Debug.Console(_enlightedDebug.DebugVerbose, this, "{0}", new String('-', 80));           
        }

        #region IRestfulComms Members
        /// <summary>
        /// Implements IKeyed interface
        /// </summary>
        public string Key { get; private set; }
        /// <summary>
        /// Client response event
        /// </summary>
        public event EventHandler<GenericClientResponseEventArgs> ResponseReceived;

        /// <summary>
        /// Sends request to the client
        /// </summary>
        /// <param name="requestType">ENUM of HTTP request type. Example: Get, Post, or Put</param>
        /// <param name="path"></param>
        /// <param name="content"></param>
        public void SendRequest(string requestType, string path, string content)
        {
            if (requestType.Length < 1) { return; }
            if (path.Length < 1) { return; }

            var request = new HttpsClientRequest
            {
                RequestType = (RequestType)Enum.Parse(typeof(RequestType), requestType, true),
                Url = new UrlParser(string.Format("{0}{1}", Host, path)),
                ContentString = content
            };
            
            request.Header.SetHeaderValue("Accept", "application/json");
            request.Header.SetHeaderValue("Content-Type", "application/json");

            if (HeaderUsesApiKey)
            {
                // TimeStamp in Ms
                var unixTimeStampMs = CurrentMillis.Millis;                
                // Calculate authorization code
                var hash = GetApiKey(AuthorizationApiKeyData.ApiKeyUsername, AuthorizationApiKeyData.ApiKey, unixTimeStampMs.ToString());    
                // Do not include colon character after each header value as character will be entered via the 'SetHeaderValue' function 
                // Header values are case sensitive                
                request.Header.SetHeaderValue("ApiKey", AuthorizationApiKeyData.ApiKeyUsername);
                request.Header.SetHeaderValue("Authorization", hash);
                request.Header.SetHeaderValue("ts", unixTimeStampMs.ToString());
            }
            else if (!string.IsNullOrEmpty(AuthorizationBase64)) { request.Header.SetHeaderValue("Authorization", AuthorizationBase64); }

            Debug.Console(_enlightedDebug.DebugVerbose, "{0}", new String('-', 100));
            Debug.Console(_enlightedDebug.DebugVerbose, this, @"Request:
                url: {0}
                path: {1}
                content: {2}
                requestType: {3}
                requestHeader: {4}",
                request.Url, path, request.ContentString, request.RequestType, request.Header);
            Debug.Console(_enlightedDebug.DebugVerbose, "{0}", new String('-', 100));
            
            try
            {
                if (_client.ProcessBusy)
                {
                    _requestQueue.Enqueue(() => _client.DispatchAsync(request, (response, error) =>
                    {
                        if (response == null)
                        {                            
                            Debug.Console(_enlightedDebug.DebugWarn, this, "Response is null, error: {0}", error);
                            return;
                        }                        

                        OnResponseRecieved(new GenericClientResponseEventArgs(response.Code, response.ContentString, response.ResponseUrl));
                    }));
                }
                else
                {
                    _client.DispatchAsync(request, (response, error) =>
                    {
                        if (response == null)
                        {
                            Debug.Console(_enlightedDebug.DebugWarn, this, "Response is null, error: {0}", error);
                            return;
                        }

                        OnResponseRecieved(new GenericClientResponseEventArgs(response.Code, response.ContentString, response.ResponseUrl));
                    });
                }
            }
            catch (Exception e)
            {
                if (e.Message.Contains("No Endpoint"))
                {
                    Debug.Console(_enlightedDebug.DebugWarn, this, Debug.ErrorLogLevel.Error, "No endpoint");
                }
                else if (e.Message.Contains("Invalid URI:"))
                {
                    Debug.Console(_enlightedDebug.DebugWarn, this, Debug.ErrorLogLevel.Error, "Invalid URI");
                }
                else
                {
                    Debug.Console(_enlightedDebug.DebugWarn, this, Debug.ErrorLogLevel.Error, "SendRequest Exception: {0}", e.Message);
                    Debug.Console(_enlightedDebug.DebugVerbose, this, Debug.ErrorLogLevel.Error, "SendRequest Stack Trace: {0}", e.StackTrace);
                    if (e.InnerException != null) Debug.Console(_enlightedDebug.DebugWarn, this, Debug.ErrorLogLevel.Error, "SendRequest Inner Exception: {0}", e.InnerException);
                }
            }
        }

        /// <summary>
        /// Sends or queues request to the client
        /// </summary>
        /// <param name="request">String request</param>
        /// <param name="contentString"></param>
        public void SendRequest(string request, string contentString)
        {
            if (string.IsNullOrEmpty(request))
            {
                Debug.Console(_enlightedDebug.DebugVerbose, this, Debug.ErrorLogLevel.Error, "SendRequest: Request is null or empty");
                return;
            }
            SendRequest(DefaultRequestType, request, contentString);
        }
        #endregion

        /// <summary>
        /// Callback when receiving responses, checks queue if additional messages need to be sent
        /// </summary>
        /// <param name="args"></param>
        private void OnResponseRecieved(GenericClientResponseEventArgs args)
        {
            Debug.Console(_enlightedDebug.DebugVerbose, this, "OnResponseRecieved: args.Code = {0}, args.ContentString = {1}", args.Code, args.ContentString);
            CheckRequestQueue();

            var handler = ResponseReceived;
            if (handler == null) { return; } // If null no one is listening
            handler(this, args);
        }

        /// <summary>
        /// Checks request queue and issues next request
        /// </summary>
        private void CheckRequestQueue()
        {
            Debug.Console(_enlightedDebug.DebugVerbose, this, "CheckRequestQueue: _requestQueue.Count = {0}", _requestQueue.Count);
            var nextRequest = _requestQueue.TryToDequeue();
            Debug.Console(_enlightedDebug.DebugVerbose, this, "CheckRequestQueue: _requestQueue.TryToDequeue was {0}",
                (nextRequest == null) ? "unsuccessful" : "successful");
            if (nextRequest != null)
            {
                nextRequest();
            }
        }

        /// <summary>
        /// Encodes username and password, returning a Base64 encoded string 
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        private string EncodeBase64(string username, string password)
        {
            if (string.IsNullOrEmpty(username)) return "";

            try
            {
                var base64String = Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(string.Format("{0}:{1}", username, password)));
                return string.Format("Basic {0}", base64String);
            }
            catch (Exception ex)
            {
                Debug.Console(_enlightedDebug.DebugVerbose, this, Debug.ErrorLogLevel.Error, "EncodeBase64 Exception:\r{0}", ex);
                return "";
            }
        }

        /// <summary>
        /// Calculate ApiKey and return ApiKey hash string
        /// </summary>
        /// <param name="apiKey">String of ApiKey associated to Username</param>
        /// <param name="username">String  Username of associated ApiKey</param>
        /// <param name="timeStampMs">String of dtae-time in MiliSeconds</param>
        /// <returns></returns>
        private static string GetApiKey(string username, string apiKey, string timeStampMs)
        {
            // Create object used for calculating SHA1 
            var hashCalculator = new SHA1CryptoServiceProvider();
            // Create string that combines all three substrings
            var sha1StringCombined = username + apiKey + timeStampMs;
            // Calculate the hash with encoding into Bytes
            var hashbytes = hashCalculator.ComputeHash(Encoding.UTF8.GetBytes(sha1StringCombined));            
            // Convert Bytes to a string                                  
            var hashBytesConverted =  BitConverter.ToString(hashbytes);
            // Remove dash characters from bitConverter and lower all alpha characters
            hashBytesConverted = (hashBytesConverted.Replace("-", "")).ToLower();
            return hashBytesConverted;
        }
    }
}