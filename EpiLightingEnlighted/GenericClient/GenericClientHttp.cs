using System;
using PepperDash.Core;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;
using Crestron.SimplSharp.Net.Http;

namespace PepperDash.Essentials.Plugin.EnlightedLighting
{
    /// <summary>
    /// HTTP Client Class
    /// </summary>
    public class GenericClientHttp : IRestfulComms
    {
        private const string DefaultRequestType = "GET";
        private readonly HttpClient _client;        

        private readonly CrestronQueue<Action> _requestQueue = new CrestronQueue<Action>(20);
        private readonly EnlightedLightingDebug _enlightedDebug = new EnlightedLightingDebug();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key"></param>
        /// <param name="controlConfig"></param>
        public GenericClientHttp(string key, EssentialsControlPropertiesConfig controlConfig)
        {
            if (string.IsNullOrEmpty(key) || controlConfig == null)
            {
                Debug.Console(_enlightedDebug.DebugVerbose, Debug.ErrorLogLevel.Error,
                    "GenericClient key or host is null or empty, failed to instantiate client");
                return;
            }            

            Key = key;

            Host = (controlConfig.TcpSshProperties.Port >= 1 && controlConfig.TcpSshProperties.Port <= 65535)
                ? String.Format("http://{0}:{1}", controlConfig.TcpSshProperties.Address, controlConfig.TcpSshProperties.Port)
                : String.Format("http://{0}", controlConfig.TcpSshProperties.Address);
            Port = (controlConfig.TcpSshProperties.Port >= 1 && controlConfig.TcpSshProperties.Port <= 65535)
                ? controlConfig.TcpSshProperties.Port
                : 80;
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

            _client = new HttpClient
            {
                Url = new UrlParser(Host),
                Port = Port,
                UserName = Username,
                Password = Password,
                KeepAlive = false
            };

            Debug.Console(_enlightedDebug.DebugVerbose, this, "clientUrl: {0}", _client.Url.ToString());

            Debug.Console(_enlightedDebug.DebugVerbose, this, "{0}", new String('-', 80));
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

        public AuthorizationApiKeyData AuthorizationApiKeyData { get; set; }

        #region IRestfulComms Members

        /// <summary>
        /// Implements IKeyed interface
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// Sends request to the client
        /// </summary>
        /// <param name="requestType"></param>
        /// <param name="path"></param>
        /// <param name="content"></param>
        public void SendRequest(string requestType, string path, string content)
        {
            var request = new HttpClientRequest
            {
                RequestType = (RequestType)Enum.Parse(typeof(RequestType), requestType, true),
                Url = new UrlParser(String.Format("{0}/{1}", _client.Url, path)),
                ContentString = content
            };

            request.Header.SetHeaderValue("Content-Type", "application/json");

            if (!string.IsNullOrEmpty(AuthorizationBase64))
            {
                request.Header.SetHeaderValue("Authorization", AuthorizationBase64);
            }

            Debug.Console(_enlightedDebug.DebugVerbose, "{0}", new String('-', 100));
            Debug.Console(_enlightedDebug.DebugVerbose, this, @"Request:
                url: {0}
                content: {1}
                requestType: {2}",
                request.Url, request.ContentString, request.RequestType);
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
                Debug.Console(_enlightedDebug.DebugWarn, this, Debug.ErrorLogLevel.Error, "SendRequest Exception: {0}", e.Message);
                Debug.Console(_enlightedDebug.DebugVerbose, this, Debug.ErrorLogLevel.Error, "SendRequest Stack Trace: {0}", e.StackTrace);
                if (e.InnerException != null) Debug.Console(_enlightedDebug.DebugWarn, this, Debug.ErrorLogLevel.Error, "SendRequest Inner Exception: {0}", e.InnerException);
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
            if (handler == null)
            {
                return;
            }

            handler(this, args);
        }

        // Checks request queue and issues next request
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

        // Encodes username and password, returning a Base64 encoded string
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
    }
}