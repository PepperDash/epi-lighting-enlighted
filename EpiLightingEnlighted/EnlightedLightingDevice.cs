using System;
using PepperDash.Core;
using Newtonsoft.Json;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using Crestron.SimplSharpPro.DeviceSupport;

namespace PepperDash.Essentials.Plugin.EnlightedLighting
{
    
    // Public class of functions to send paths or requests
    public class SendLightingApiRequest
    {        
        /// <summary>
        /// Local copy of the IRestfulComms client
        /// </summary>
        private readonly IRestfulComms _comms;

        /// <summary>
        /// Constructor which includes copy of the Generic HTTP Client
        /// </summary>
        /// <param name="comms">Local copy of the IRestfulComms client</param>
        public SendLightingApiRequest(IRestfulComms comms)
        {
            _comms = comms;
        }
    }

    /// <summary>
    /// Create the bridgeable device
    /// </summary>
	public class EnlightedLightingDevice : EssentialsBridgeableDevice
    {
        /// <summary>
        /// Store the config locally
        /// </summary>
        private readonly EnlightedLightingConfig _config;
        private readonly IRestfulComms _comms;
        private readonly long _pollTimeMs;
        private readonly long _warningTimeoutMs;
        private readonly long _errorTimeoutMs;
        private const long PingInterval = 50000; //50 seconds        
        private const string GetOrgDetails = "/ems/api/org/company";
        private const string GetAllCampuses = "/ems/api/org/campus/list/1";
        private const string GetAllBuildings = "/ems/api/org/building/list/1";
        private const string GetAllFloors = "/ems/api/org/floor/list";
        private CTimer _pingTimer;
        private EnlightedLightingBridgeJoinMap _joinMap { get; set; }

        public SendLightingApiRequest SendLightingRequest { get; set; }
        public BoolFeedback OnlineFeedback { get; private set; }        
        private bool _deviceOnline;

        /// <summary>
        /// Tracks name debugging state
        /// </summary>
        public bool ExtendedDebuggingState;

	    #region Device Constructor

	    /// <summary>
	    /// Plugin device constructor for devices that need IBasicCommunication
	    /// </summary>
	    /// <param name="key"></param>
	    /// <param name="name"></param>
	    /// <param name="config"></param>
	    /// <param name="client"></param>
	    public EnlightedLightingDevice(string key, string name, EnlightedLightingConfig config, IRestfulComms client)
	        : base(key, name)
	    {
	        Debug.Console(0, this, "Constructing new Enlighted Lighting plugin instance using key: '{0}', name: '{1}'", key,
	            name);            

            OnlineFeedback  = new BoolFeedback(()=> _deviceOnline);
	        StartPingTImer(); // Start CTimer
	        
	        _config = config;
	        _pollTimeMs = (config.PollTimeMs > 0) ? config.PollTimeMs : 60000;
	        _warningTimeoutMs = (config.WarningTimeoutMs > 0) ? config.WarningTimeoutMs : 180000;
	        _errorTimeoutMs = (config.ErrorTimeoutMs > 0) ? config.ErrorTimeoutMs : 300000;            

	        // device communications
	        _comms = client;
	        if (_comms == null)
	        {
	            Debug.Console(0, this, Debug.ErrorLogLevel.Error, "Failed to construct GenericClient using method '{0}'",
	                config.Control.Method);
	            return;
	        }            

            SendLightingRequest = new SendLightingApiRequest(_comms);

            //Taking config values and getting them to the client
            _comms.AuthorizationApiKeyData.ApiKey = config.ApiKey;	        
            _comms.AuthorizationApiKeyData.ApiKeyUsername = config.ApiKeyUsername;
            _comms.AuthorizationApiKeyData.HeaderUsesApiKey = config.HeaderUsesApiKey;

	        _comms.ResponseReceived += _comms_ResponseReceived;

	        DeviceManager.AddDevice(_comms);

	        //OnlineFeedback = new BoolFeedback(() => true);	// false > _commsMonitor.IsOnline
	        //StatusFeedback = new IntFeedback(() => 2);		// 0 > (int)_commsMonitor.Status

	        Debug.Console(0, "{0}", new String('-', 100));
	    }
	    #endregion


        #region Overrides of EssentialsBridgeableDevice
        /// <summary>
        /// Links plugin device to the EISC bridge Post Activation
        /// Link to API method replaces bridge class, this method will be called by the bridge directly
        /// When using EiscApiAdvanced your JSON type must be "type": "eiscApiAdvanced";
        /// </summary>
        /// <param name="trilist"></param>
        /// <param name="joinStart"></param>
        /// <param name="joinMapKey"></param>
        /// <param name="bridge"></param>
        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            if (trilist == null)
            {
                Debug.Console(0, "{0}", new String('-', 100));
                Debug.Console(0, this, "LinkToApi(trilist-null joinStart-{1}, joinMapKey-{2}, bridge-[null: {3}])", joinStart, joinMapKey, bridge == null);
                Debug.Console(0, this, "LinkToApi failed to link trilist, {0} bridge will not be function", Key);
                Debug.Console(0, "{0}", new String('-', 100));
                return;
            }

            try
            {
                _joinMap = new EnlightedLightingBridgeJoinMap(joinStart);

                // Add the join map to the collection on the bridge if not null
                if (bridge != null) bridge.AddJoinMap(Key, _joinMap);

                var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);
                if (customJoins != null)
                {
                    _joinMap.SetCustomJoinData(customJoins);
                }

                Debug.Console(0, this, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
                Debug.Console(0, this, "Linking to Bridge Type {0}", GetType().Name);

                // Device name to bridge
                trilist.SetString(_joinMap.Name.JoinNumber, Name);
                trilist.SetSigTrueAction(_joinMap.PrintAllInfo.JoinNumber, PrintInformation);
                trilist.SetStringSigAction(_joinMap.GetCustomPath.JoinNumber, GetCustomPath);
                trilist.SetStringSigAction(_joinMap.PostCustomPath.JoinNumber, PostCustomPath);                
                trilist.SetUShortSigAction(_joinMap.Scene.JoinNumber, SetApplySceneWithId);                
                           

                // Device online status to bridge
                OnlineFeedback.LinkInputSig(trilist.BooleanInput[_joinMap.IsOnline.JoinNumber]);
                
                trilist.OnlineStatusChange += (device, args) =>
                {
                    if (!args.DeviceOnLine) return;

                    trilist.SetString(_joinMap.Name.JoinNumber, Name);                  
                };
            }
            catch (Exception e)
            {
                Debug.Console(0, this, "LinkToApi Exception: {0}", e.Message);
                Debug.Console(0, this, "LinkToApi Stack Trace: {0}", e.StackTrace);
                if (e.InnerException != null) Debug.Console(0, this, "LinkToApi Inner Exception: {0}", e.InnerException);
            }
        }
        #endregion                 

        /// <summary>
        /// Sets name debugging state
        /// </summary>
        /// <param name="state"></param>
        public void SetExtendedDebuggingState(bool state)
        {
            //ExtendedDebuggingState = state;
            Debug.Console(0, this, "Extended Debugging: {0}", state ? "On" : "Off");
        }

        private void _comms_ResponseReceived(object sender, GenericClientResponseEventArgs args)
        {
            try
            {
                Debug.Console(1, this, "Response Code: {0}", args.Code);
                Debug.Console(1, this, "Response URL: {0}", args.ResponseUrl);
                //If we get response.code 200 then parse
                //Perahps some will help you know if your AUTH failed or if other things fail! Make it helpful.
                //401 is unahtorizied and 403 is forboredden

                ResetPingTimer(); // Reset CTimer with every response
                _deviceOnline = true;
                OnlineFeedback.FireUpdate();
                
                if (string.IsNullOrEmpty(args.ContentString)) return;

                switch (args.Code)
                {
                    case 200: // Ok, valid response, request successful
                        return;                        
                    case 302:
                        Debug.Console(1, this, Debug.ErrorLogLevel.Error, "Moved temporarily, user not authenticated, URL redirection");
                        break;
                    case 401:
                        Debug.Console(1, this, Debug.ErrorLogLevel.Error, "Request not completed, lacks valid authentication credentials for requested resource");
                        break;
                    case 403:
                        Debug.Console(1, this, Debug.ErrorLogLevel.Error, "Request forbidden, permission denied, no access to the user");
                        break;
                    case 404:
                        Debug.Console(1, this, Debug.ErrorLogLevel.Error, "API not valid or not found");
                        break;
                    case 405:
                        Debug.Console(1, this, Debug.ErrorLogLevel.Error, "Method not allowed: Server understood request but method not supported by target resource");
                        break;
                    case 406:
                        Debug.Console(1, this, Debug.ErrorLogLevel.Error, "Not acceptable, server cannot produce response from given request");
                        break;
                    case 407:
                        Debug.Console(1, this, Debug.ErrorLogLevel.Error, "Request not applied, lacks valid authentication credentials for proxy server between browser and server to access requested resource");
                        break;
                    case 408:
                        Debug.Console(1, this, Debug.ErrorLogLevel.Error, "API received after time expired, API canceled");
                        break;
                }

                if (!args.ResponseUrl.Contains("applyScene")) return;
                var obj = JsonConvert.DeserializeObject<EnlightedLightingResponseStatus>(args.ContentString);
                if (obj != null)
                    ParseStatusResponse(obj);
            }
            catch (Exception e)
            {
                Debug.Console(1, this, Debug.ErrorLogLevel.Error, "_comms_ResponseReceived Exception: {0}", e.Message);
                Debug.Console(2, this, Debug.ErrorLogLevel.Error, "_comms_ResponseReceived Stack Trace: {0}", e.StackTrace);
                if (e.InnerException != null)
                    Debug.Console(1, Debug.ErrorLogLevel.Error, "_comms_ResponseReceived Inner Exception: {0}",
                        e.InnerException);
            }
        }

        /// <summary>
        /// Determine data sets to pull out of response (CODE and STRING are done for you)
        /// </summary>
        /// <param name="responseObj">Reponse from device</param>
        private void ParseStatusResponse(EnlightedLightingResponseStatus responseObj)
        {
            if (responseObj == null) return;
            
            try
            {
                // if(responseObj.Status == 0){} 
                
                // There really isn't anything to parse from the device. The EM is not rememebering
                // the last scene called, only the lighting levels per load which we are not interested in parsing or saving.
                // Since the response is already being printed into console if/when debug is active we get the status response
                // and can view the status via the API with no need to print it here.
                Debug.Console(1, this, Debug.ErrorLogLevel.None, "Reponse from device:  {0}", responseObj);                
            }
            catch (Exception e)
            {
                Debug.Console(1, this, Debug.ErrorLogLevel.Error, "ParseResponse Exception: {0}", e.Message);
                Debug.Console(2, this, Debug.ErrorLogLevel.Error, "ParseResponse Stack Trace: {0}", e.StackTrace);
                if (e.InnerException != null) Debug.Console(1, this, Debug.ErrorLogLevel.Error, "ParseResponse Inner Exception: {0}", e.InnerException);
            }
        }

        /// <summary>
        /// Send text to device
        /// </summary>
        /// <param name="cmd">Path to send to device</param>
        public void PostCustomPath(string cmd)
        {
            if (string.IsNullOrEmpty(cmd))
                return;

            if (_comms != null) _comms.SendRequest("Post", cmd, string.Empty);
        }

        /// <summary>
        /// Send custom command using GET request type
        /// </summary>
        /// <param name="cmd">Path of custom command</param>
        public void GetCustomPath(string cmd)
        {
            if (string.IsNullOrEmpty(cmd))
                return;

            if (_comms != null) _comms.SendRequest("Get", cmd, string.Empty);
        }

        /// <summary>
        /// Apply lighting scene using scene ID and virtualSwitchIdentifer
        /// </summary>
        /// <param name="sceneId">Path of URL, requires forward slash prefix</param>
        public void SetApplySceneWithId(ushort sceneId)
        {
            try
            {
                var dictionaryKeyIndex = "scene" + sceneId;
                Debug.Console(2, this, Debug.ErrorLogLevel.Error, "SetApplySceneWithIndex: {0}", dictionaryKeyIndex);
                EnlightedLightingSceneIo sceneOjbect;
                var found = _config.SceneDictionary.TryGetValue(dictionaryKeyIndex, out sceneOjbect);

                if (!found) Debug.Console(2, this, Debug.ErrorLogLevel.Error, "SetApplySceneWithIndex: Variable from SceneDictionary not found");
                if (sceneId == 0) { return; }
                var sTemp = string.Format("/ems/api/org/switch/v1/op/applyScene/{0}/{1}?time=0", _config.VirtualSwitchIdentifier, sceneOjbect.SceneId);
                _comms.SendRequest("Post", sTemp, string.Empty);
            }
            catch (Exception e)
            {
                Debug.Console(2, this, Debug.ErrorLogLevel.Error, "SetApplySceneWithIndex: InnerException: {0} Message: {1} StackTrace: {2}", e.InnerException, e.Message, e.StackTrace);
            }
            
        }

        /// <summary>
        /// Manually poll device using SendRequest method
        /// </summary>
        public void SetManualPoll()
        {
            //Custom command used to poll device
            _comms.SendRequest("Get", "/ems/api/org/em/v1/energy", string.Empty);
        }

        private void ResetPingTimer()
        {
            // This tells us we're online with the API and getting pings
            _pingTimer.Reset(PingInterval);
        }

        private void StartPingTImer()
        {
            StopPingTimer();
            _pingTimer = new CTimer(PingTimerCallback, null, PingInterval);
        }

        private void StopPingTimer()
        {
            if (_pingTimer == null)
            {
                return;
            }

            _pingTimer.Stop();
            _pingTimer.Dispose();
            _pingTimer = null;
        }

        private void PingTimerCallback(object o)
        {
            Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Ping timer expired");
            _deviceOnline = false;
            OnlineFeedback.FireUpdate();
            SetManualPoll();
            StartPingTImer();
        }

        private void PrintInformation()
        {
            GetCustomPath(GetOrgDetails);
            GetCustomPath(GetAllCampuses);
            GetCustomPath(GetAllBuildings);
            GetCustomPath(GetAllFloors);            
            // Use the 'Get Switch Groups' command to retrieve Switch IDs based on switch names - "/ems/api/org/switchgroups/list/{property}/{pid}"
            // Use the 'Get Switch Scenes' command to retrieve scene IDs for each switch - "/ems/api/org/switch/v1/getSwitchScenes/{floor_id}/{switch_name}"
        }
    }   
}
