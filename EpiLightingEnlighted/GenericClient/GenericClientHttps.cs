using System;
using System.Globalization;
using System.Net;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharp.Net.Https;
using PepperDash.Core;
using PepperDash.Core.WebApi.Presets;
using PepperDash.Essentials.Core;
using RequestType = Crestron.SimplSharp.Net.Https.RequestType;
using Crestron.SimplSharp.Cryptography;
using SHA1 = System.Security.Cryptography.SHA1;

namespace PepperDash.Essentials.Plugin.EnlightedLighting
{
    /// <summary>
    /// HTTPS Client Class
    /// </summary>
    public class GenericClientHttps : IRestfulComms
    {
        private const string DefaultRequestType = "GET";
        private readonly HttpsClient _client;               

        private readonly CrestronQueue<Action> _requestQueue = new CrestronQueue<Action>(20);        

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key"></param>
        /// <param name="controlConfig"></param>
        public GenericClientHttps(string key, EssentialsControlPropertiesConfig controlConfig)
        {
            Key = key;

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

            Debug.Console(2, this, "{0}", new String('-', 80));
            Debug.Console(2, this, "GenericClient: Key = {0}", Key);
            Debug.Console(2, this, "GenericClient: Host = {0}", Host);
            Debug.Console(2, this, "GenericClient: Port = {0}", Port);
            Debug.Console(2, this, "GenericClient: Username = {0}", Username);
            Debug.Console(2, this, "GenericClient: Password = {0}", Password);
            Debug.Console(2, this, "GenericClient: AuthorizationBase64 = {0}", AuthorizationBase64);     

            _client = new HttpsClient
            {
                Url = new UrlParser(Host),
                UserName = Username,
                Password = Password,
                KeepAlive = false,
                HostVerification = false,
                PeerVerification = false
            };

            AuthorizationApiKeyData = new AuthorizationApiKeyData();

            Debug.Console(2, this, "_clientUrl: {0}", _client.Url.ToString());

            Debug.Console(2, this, "{0}", new String('-', 80));           
        }

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
        /// Custom Authorization with ApiKeyData
        /// </summary>
        public AuthorizationApiKeyData AuthorizationApiKeyData { get; set; }

        #region IRestfulComms Members

        /// <summary>
        /// Implements IKeyed interface
        /// </summary>
        public string Key { get; private set; }

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
                Url = new UrlParser(string.Format("{0}/{1}", Host, path)),
                ContentString = content
            };

            request.Header.SetHeaderValue("Content-Type", "application/json");

            if (AuthorizationApiKeyData.HeaderUsesApiKey)
            {
                //Get property of class that has the ApiKey from config
                //Get property of class that has the Api Username from config

                //Get Millisecond TimeStamp from EPOCH time [https://www.epochconverter.com/]
                var unixTimeStampMs = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                Debug.Console(1, this, "_client UnixTimeStamp: {0}", unixTimeStampMs.ToString());
                //Calculate authorization code
                var hash = GetApiKey(AuthorizationApiKeyData.ApiKey, AuthorizationApiKeyData.ApiKeyUsername, unixTimeStampMs.ToString());
                Debug.Console(1, this, "_client ApiKey Hash: {0}", hash);
                //NOTE: Do not include colon character after each header value, as character will be entered via the 'SetHeaderValue' function 
                //NOTE: Header values are case sensitive
                request.Header.SetHeaderValue("ApiKey", AuthorizationApiKeyData.ApiKey);
                request.Header.SetHeaderValue("Authorization", hash);
                request.Header.SetHeaderValue("ts", unixTimeStampMs.ToString());
            }

            else if (!string.IsNullOrEmpty(AuthorizationBase64))
            {
                request.Header.SetHeaderValue("Authorization", AuthorizationBase64);
                //request.Header.SetHeaderValue("Authorization:", )
            }

            Debug.Console(2, "{0}", new String('-', 100));
            Debug.Console(2, this, @"Request:
                url: {0}
                path: {1}
                content: {2}
                requestType: {3}",
                request.Url, path, request.ContentString, request.RequestType);
            Debug.Console(2, "{0}", new String('-', 100));

            try
            {
                if (_client.ProcessBusy)
                {
                    _requestQueue.Enqueue(() => _client.DispatchAsync(request, (response, error) =>
                    {
                        if (response == null)
                        {                            
                            Debug.Console(1, this, "_client.Display: response is null, error: {0}", error);
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
                            Debug.Console(1, this, "_client.Display: response is null, error: {0}", error);
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
                    Debug.Console(1, this, Debug.ErrorLogLevel.Error, "SessionManager: No endpoint");
                }
                else if (e.Message.Contains("Invalid URI:"))
                {
                    Debug.Console(1, this, Debug.ErrorLogLevel.Error, "SessionManager: Invalid URI");
                }
                else
                {
                    Debug.Console(1, this, Debug.ErrorLogLevel.Error, "SendRequest Exception: {0}", e.Message);
                    Debug.Console(2, this, Debug.ErrorLogLevel.Error, "SendRequest Stack Trace: {0}", e.StackTrace);
                    if (e.InnerException != null) Debug.Console(1, this, Debug.ErrorLogLevel.Error, "SendRequest Inner Exception: {0}", e.InnerException);
                }
            }
        }

        /// <summary>
        /// Client response event
        /// </summary>
        public event EventHandler<GenericClientResponseEventArgs> ResponseReceived;

        /// <summary>
        /// Sends OR queues a request to the client
        /// </summary>
        /// <param name="request"></param>
        /// <param name="contentString"></param>
        public void SendRequest(string request, string contentString)
        {
            if (string.IsNullOrEmpty(request))
            {
                Debug.Console(2, this, Debug.ErrorLogLevel.Error, "SendRequest: request is null or empty");
                return;
            }

            SendRequest(DefaultRequestType, request, contentString);
        }

        #endregion

        private void OnResponseRecieved(GenericClientResponseEventArgs args)
        {
            Debug.Console(2, this, "OnResponseRecieved: args.Code = {0}, args.ContentString = {1}", args.Code, args.ContentString);

            CheckRequestQueue();

            var handler = ResponseReceived;
            if (handler == null)
            {
                return;
            }

            handler(this, args);
        }

        // checks request queue and issues next request
        private void CheckRequestQueue()
        {
            Debug.Console(2, this, "CheckRequestQueue: _requestQueue.Count = {0}", _requestQueue.Count);
            var nextRequest = _requestQueue.TryToDequeue();
            Debug.Console(2, this, "CheckRequestQueue: _requestQueue.TryToDequeue was {0}",
                (nextRequest == null) ? "unsuccessful" : "successful");
            if (nextRequest != null)
            {
                nextRequest();
            }
        }

        // encodes username and password, returning a Base64 encoded string
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
                Debug.Console(2, this, Debug.ErrorLogLevel.Error, "EncodeBase64 Exception:\r{0}", ex);
                return "";
            }
        }

        /// <summary>
        /// Calculate ApiKey and return ApiKey hash string
        /// </summary>
        /// <param name="apiKey">String of ApiKey associated to Username</param>
        /// <param name="username">String  Username of associated ApiKey</param>
        /// <param name="timeStampMs">String of EPOCH time in MiliSeconds</param>
        /// <returns></returns>
        private static string GetApiKey(string apiKey, string username, string timeStampMs)
        {
            //Create object used for calculating SHA1 
            var hashCalculator = new SHA1CryptoServiceProvider();
            //Create string that combines all three substrings
            var sha1StringCombined = username + apiKey + timeStampMs;
            //Calculate the hash with encoding into Bytes
            var hashbytes = hashCalculator.ComputeHash(Encoding.UTF8.GetBytes(sha1StringCombined));            
            //Convert Bytes to a string
            return BitConverter.ToString(hashbytes);
        }
    }
}