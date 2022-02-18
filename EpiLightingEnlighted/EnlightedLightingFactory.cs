using System;
using PepperDash.Core;
using System.Collections.Generic;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Plugin.EnlightedLighting
{
	/// <summary>
	/// Plugin device factory for devices that use IBasicCommunication
	/// </summary>
	/// <remarks>
	/// Rename the class to match the device plugin being developed
	/// </remarks>
	/// <example>
	/// "EssentialsPluginFactoryTemplate" renamed to "MyDeviceFactory"
	/// </example>
    public class EnlightedLightingFactory : EssentialsPluginDeviceFactory<EnlightedLightingDevice>
    {
		/// <summary>
		/// Plugin device factory constructor
		/// </summary>
		/// <remarks>
		/// Update the MinimumEssentialsFrameworkVersion & TypeNames as needed when creating a plugin
		/// </remarks>
		/// <example>
 		/// Set the minimum Essentials Framework Version
		/// <code>
		/// MinimumEssentialsFrameworkVersion = "1.6.4;
        /// </code>
		/// In the constructor we initialize the list with the typenames that will build an instance of this device
        /// <code>
		/// TypeNames = new List<string>() { "SamsungMdc", "SamsungMdcDisplay" };</string>
        /// </code>
		/// </example>
        public EnlightedLightingFactory()
        {
            // Set the minimum Essentials Framework Version
			MinimumEssentialsFrameworkVersion = "1.9.7";

            // In the constructor we initialize the list with the typenames that will build an instance of this device
			//Update the TypeNames for the plugin being developed
            TypeNames = new List<string>() { "enlightedlighting" };
        }

        /// <summary>
        /// Builds and returns an instance of EssentialsPluginDeviceTemplate
        /// </summary>
        /// <param name="dc"></param>
        /// <returns></returns>
        public override EssentialsDevice BuildDevice(PepperDash.Essentials.Core.Config.DeviceConfig dc)
        {
            Debug.Console(1, "{0}", new String('-', 100));
            Debug.Console(1, "[{0}] Factory Attempting to create new device from type: '{1}'", dc.Key, dc.Type);

            try
            {
                // get the plugin device properties configuration object & check for null
                var propertiesConfig = dc.Properties.ToObject<EnlightedLightingConfig>();
                if (propertiesConfig == null)
                {
                    Debug.Console(0, "[{0}] Unable to get configuration. Please check configuration.", dc.Key);
                    return null;
                }

                IRestfulComms client;

                switch (propertiesConfig.Control.Method)
                {
                    case eControlMethod.Http:
                        client = new GenericClientHttp(string.Format("{0}-http", dc.Key), propertiesConfig.Control);
                        break;
                    case eControlMethod.Https:
                        client = new GenericClientHttps(string.Format("{0}-https", dc.Key), propertiesConfig.Control);
                        break;
                    default:
                        Debug.Console(0, "[{0}] Control method '{1}' NOT supported. Please check configuration", dc.Key, propertiesConfig.Control.Method);
                        Debug.Console(0, "{0}", new String('-', 100));

                        return null;
                }

                return new EnlightedLightingDevice(dc.Key, dc.Name, propertiesConfig, client);
            }
            catch (Exception ex)
            {
                Debug.Console(0, "[{0}] Factory BuildDevice Exception: {1}", dc.Key, ex.Message);
                Debug.Console(0, "[{0}] Factory BuildDevice Stack Trace: {1}", dc.Key, ex.StackTrace);
                if (ex.InnerException != null) Debug.Console(0, "[{0}] Factory BuildDevice InnerException: {1}", dc.Key, ex.InnerException);
                Debug.Console(0, "{0}", new String('-', 100));
                return null;
            }
        }
    }
}
          