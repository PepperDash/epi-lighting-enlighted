using System;
using System.Net;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Plugin.EnlightedLighting
{
    /// <summary>
    /// Evertz Nucleus Client Class
    /// </summary>
    public class GenericClientHttp : IRestfulComms
    {
        private const string DefaultRequestType = "GET";
        private readonly HttpClient _client;

        private readonly CrestronQueue<Action> _requestQueue = new CrestronQueue<Action>(20);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key"></param>
        /// <param name="controlConfig"></param>
        public GenericClientHttp(string key, EssentialsControlPropertiesConfig controlConfig)
        {
            if (string.IsNullOrEmpty(key) || controlConfig == null)
            {
                Debug.Console(2, Debug.ErrorLogLevel.Error,
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

            Debug.Console(2, this, "{0}", new String('-', 80));
            Debug.Console(2, this, "GenericClient: Key = {0}", Key);
            Debug.Console(2, this, "GenericClient: Host = {0}", Host);
            Debug.Console(2, this, "GenericClient: Port = {0}", Port);
            Debug.Console(2, this, "GenericClient: Username = {0}", Username);
            Debug.Console(2, this, "GenericClient: Password = {0}", Password);
            Debug.Console(2, this, "GenericClient: AuthorizationBase64 = {0}", AuthorizationBase64);

            _client = new HttpClient
            {
                Url = new UrlParser(Host),
                Port = Port,
                UserName = Username,
                Password = Password,
                KeepAlive = false
            };

            Debug.Console(2, this, "clientUrl: {0}", _client.Url.ToString());

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

            Debug.Console(2, "{0}", new String('-', 100));
            Debug.Console(2, this, @"Request:
                url: {0}
                content: {1}
                requestType: {2}",
                request.Url, request.ContentString, request.RequestType);
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

                        OnResponseRecieved(new GenericClientResponseEventArgs(response.Code, response.ContentString));
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

                        OnResponseRecieved(new GenericClientResponseEventArgs(response.Code, response.ContentString));
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
    }
}