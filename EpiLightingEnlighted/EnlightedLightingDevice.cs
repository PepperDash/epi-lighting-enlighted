using System;
using PepperDash.Core;
using Newtonsoft.Json;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using Crestron.SimplSharpPro.DeviceSupport;

namespace PepperDash.Essentials.Plugin.EnlightedLighting
{
    //Would be smart to print out paths being sent to confirm the slashes are needed or not

    //Make a public class of functions to send paths or requests
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

        public void ApplyScene(string path)
        {                        
            //_comms.SendRequest(path, null);
            //Assume case sensitive with requestType
            _comms.SendRequest("Post", path, null);
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
        private EnlightedLightingConfig _config;
        private readonly IRestfulComms _comms;
        private readonly long _pollTimeMs;
        private readonly long _warningTimeoutMs;
        private readonly long _errorTimeoutMs;
        private const long PingInterval = 50000; //50 seconds
        private CTimer _pingTimer;

        public SendLightingApiRequest SendLightingRequest { get; set; }
        public BoolFeedback OnlineFeedback { get; private set; }        
        private bool _online;

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

            OnlineFeedback  = new BoolFeedback(()=> _online);
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
                var joinMap = new EnlightedLightingBridgeJoinMap(joinStart);

                // This adds the join map to the collection on the bridge IF not null
                if (bridge != null) bridge.AddJoinMap(Key, joinMap);

                var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);
                if (customJoins != null)
                {
                    joinMap.SetCustomJoinData(customJoins);
                }

                Debug.Console(0, this, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
                Debug.Console(0, this, "Linking to Bridge Type {0}", GetType().Name);

                // device name to bridge
                trilist.SetString(joinMap.Name.JoinNumber, Name);                                
                trilist.SetSigTrueAction(joinMap.Poll.JoinNumber, SetManualPoll);
                trilist.SetStringSigAction(joinMap.ManualCommand.JoinNumber, SetManualCommand);
                trilist.SetStringSigAction(joinMap.ApplyScene.JoinNumber, SetApplyScene);

                // device online status to bridge
                OnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
                //StatusFeedback.LinkInputSig(trilist.UShortInput[joinMap.Status.JoinNumber]);

                // Do OnlineFeedback fireupdate & create ctimer, start or give it 2 cyles 
                // It's due time is going to be 3 x your poll rate (50 seconds), (timer will expire after 150... s you don't see it flap
                // Everytime you get a repsonse reset the timer. Everytime you get a response trigger onlineFeedback fireupdate (be sure to set online true BEFORE you fire the update)
                // WHen the ctimer expires then set to false and fire update. Check out MC 'controller' EPI. Look in the handleMessage that handles message sfrom websocket. PING/PONG response.
                // 


                // bridge online status 
                // during testing this will never go high
                
                trilist.OnlineStatusChange += (device, args) =>
                {
                    if (!args.DeviceOnLine) return;

                    trilist.SetString(joinMap.Name.JoinNumber, Name);                  
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
            ExtendedDebuggingState = state;
            Debug.Console(0, this, "Extended Debugging: {0}", ExtendedDebuggingState ? "On" : "Off");
        }

        private void _comms_ResponseReceived(object sender, GenericClientResponseEventArgs args)
        {
            try
            {
                Debug.Console(1, this, "Respone Code: {0}", args.Code);
                Debug.Console(0, this, "Response URL: {0}", args.ResponseUrl);
                //If we get response.code 200 then parse
                //Perahps some will help you know if your AUTH failed or if other things fail! Make it helpful.
                //401 is unahtorizied and 403 is forboredden

                ResetPingTimer(); // Reset CTimer with every response
                _online = true;
                OnlineFeedback.FireUpdate();
                
                if (string.IsNullOrEmpty(args.ContentString)) return;

                if (args.Code != 200) return;
                if (args.ResponseUrl.Contains("applyScene"))
                {                   
                    var obj = JsonConvert.DeserializeObject<EnlightedLightingResponseStatus>(args.ContentString);
                    if (obj != null)
                        ParseStatusResponse(obj);
                }
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
                Debug.Console(1, this, Debug.ErrorLogLevel.None, "Reponse from HTTPS:  {0}", responseObj);                
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
        public void SendText(string cmd)
        {
            if (string.IsNullOrEmpty(cmd))
                return;

            if (_comms != null) _comms.SendRequest(cmd, string.Empty);
        }

        /// <summary>
        /// Sent custom command using GET response type
        /// </summary>
        /// <param name="cmd">Path of custom command</param>
        public void SetManualCommand(string cmd)
        {
            if (string.IsNullOrEmpty(cmd))
                return;

            if (_comms != null) _comms.SendRequest(cmd, string.Empty);
        }

        /// <summary>
        /// Trigger SendText method to Manually poll device version
        /// </summary>
        public void SetManualPoll()
        {
            //Custom command used to poll device
            _comms.SendRequest("Get", "/ems/api/org/em/v1/energy", null);
        }

        /// <summary>
        /// Apply lighting scene, send path as parameter
        /// </summary>
        /// <param name="path">Path of URL, requires forward slash prefix</param>
        public void SetApplyScene(string path)
        {
            _comms.SendRequest("Post", path, null);            
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
            _online = false;
            OnlineFeedback.FireUpdate();
        }
    }   
}
