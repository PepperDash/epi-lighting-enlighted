using System;
using PepperDash.Core;
using Newtonsoft.Json;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;
using System.Collections.Generic;
using PepperDash.Essentials.Core.Bridges;
using Crestron.SimplSharpPro.DeviceSupport;


namespace PepperDash.Essentials.Plugin.EnlightedLighting
{
	/// <summary>
	/// Plugin device template for third party devices that use IBasicCommunication
	/// </summary>
	/// <remarks>
	/// Rename the class to match the device plugin being developed.
	/// </remarks>
	/// <example>
	/// "EssentialsPluginDeviceTemplate" renamed to "SamsungMdcDevice"
	/// </example>
	public class EnlightedLightingDevice : EssentialsBridgeableDevice
    {
        /// <summary>
        /// It is often desirable to store the config
        /// </summary>
        private EnlightedLightingConfig _config;

        private readonly IRestfulComms _comms;

        private readonly long _pollTimeMs;
        private readonly long _warningTimeoutMs;
        private readonly long _errorTimeoutMs;

		/// <summary>
		/// Plugin device constructor for devices that need IBasicCommunication
		/// </summary>
		/// <param name="key"></param>
		/// <param name="name"></param>
		/// <param name="config"></param>
		/// <param name="comms"></param>
        public EnlightedLightingDevice(string key, string name, EnlightedLightingConfig config, IRestfulComms client)
			: base(key, name)
		{
            Debug.Console(0, this, "Constructing new Enlighted Lighting plugin instance using key: '{0}', name: '{1}'", key, name);

			// TODO [X] Update the constructor as needed for the plugin device being developed

			_config = config;

            _pollTimeMs = (config.PollTimeMs > 0) ? config.PollTimeMs : 60000;
            _warningTimeoutMs = (config.WarningTimeoutMs > 0) ? config.WarningTimeoutMs : 180000;
            _errorTimeoutMs = (config.ErrorTimeoutMs > 0) ? config.ErrorTimeoutMs : 300000;

            // device communications
            _comms = client;
            if (_comms == null)
            {
                Debug.Console(0, this, Debug.ErrorLogLevel.Error, "Failed to construct GenericClient using method '{0}'", config.Control.Method);
                return;
            }

            _comms.ResponseReceived += _comms_ResponseReceived;

            DeviceManager.AddDevice(_comms);

            //OnlineFeedback = new BoolFeedback(() => true);	// false > _commsMonitor.IsOnline
            //StatusFeedback = new IntFeedback(() => 2);		// 0 > (int)_commsMonitor.Status

            Debug.Console(0, "{0}", new String('-', 100));
        }

        /// <summary>
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

                // device online status to bridge
                //OnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
                //StatusFeedback.LinkInputSig(trilist.UShortInput[joinMap.Status.JoinNumber]);

                // bridge online status 
                // during testing this will never go high
                // TODO [ ]  evaluate switcher in field to see if there is a poll that can be used to signal OnlineStatus back to SIMPL
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

        /// <summary>
        /// Tracks name debugging state
        /// </summary>
        public bool ExtendedDebuggingState;

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

                if (string.IsNullOrEmpty(args.ContentString)) return;


                var obj = JsonConvert.DeserializeObject<EnlightedLightingResponseObject>(args.ContentString);
                if (obj != null) { }
                    //ParseSessionData(obj);
                //what data sets do you need to pull out of the response  - So far JKD pulled out CODE and STRING. If you need more, do more.
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

        private void ParseResponse(EnlightedLightingResponseObject obj)
        {
            if (obj == null) return;

            try
            {

                //Debug.Console(1, this, "ParseResponse: code-{0}, messgae-{1} | dataCount-{2}", obj.);

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
        /// <param name="cmd"></param>
        public void SendText(string cmd)
        {
            if (string.IsNullOrEmpty(cmd))
                return;

            if (_comms != null) _comms.SendRequest(cmd, string.Empty);
        }

        /// <summary>
        /// Poll 
        /// Response message includes number of active sessions
        /// </summary>
        public void Poll()
        {
            //SendText(BreakawayIsEnabled
                //? string.Format("api/v1/GET/bsessions")
                //: string.Format("api/v1/GET/sessions"));
        }
    }
}
