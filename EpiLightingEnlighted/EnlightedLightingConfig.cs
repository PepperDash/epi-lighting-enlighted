﻿using System.Collections.Generic;
using Newtonsoft.Json;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Plugin.EnlightedLighting
{
	/// <summary>
	/// Plugin device configuration object
	/// </summary>
	/// <remarks>
	/// Rename the class to match the device plugin being created
	/// </remarks>
	/// <example>
	/// "EssentialsPluginConfigObjectTemplate" renamed to "SamsungMdcConfig"
	/// </example>
	[ConfigSnippet("\"properties\":{\"control\":{}")]
	public class EnlightedLightingConfig
	{
		/// <summary>
        /// Essentials control properties configuration object
		/// </summary>
		[JsonProperty("control")]
		public EssentialsControlPropertiesConfig Control { get; set; }

		/// <summary>
		/// Serializes the poll time value
		/// </summary>
		/// <remarks>
		/// </remarks>
		/// <value>
		/// PollTimeMs property gets/sets the value as a long
		/// </value>
		/// <example>
		/// <code>
		/// "properties": {
		///		"polltimeMs": 30000
		/// }
		/// </code>
		/// </example>
		[JsonProperty("pollTimeMs")]
		public long PollTimeMs { get; set; }

		/// <summary>
		/// Serializes the warning timeout value
		/// </summary>
		/// <remarks>
		/// </remarks>
		/// <value>
		/// WarningTimeoutMs property gets/sets the value as a long
		/// </value>
		/// <example>
		/// <code>
		/// "properties": {
		///		"warningTimeoutMs": 180000
		/// }
		/// </code>
		/// </example>
		[JsonProperty("warningTimeoutMs")]
		public long WarningTimeoutMs { get; set; }

		/// <summary>
		/// Serializes the error timeout value
		/// </summary>
		/// /// <remarks>
		/// </remarks>
		/// <value>
		/// ErrorTimeoutMs property gets/sets the value as a long
		/// </value>
		/// <example>
		/// <code>
		/// "properties": {
		///		"errorTimeoutMs": 300000
		/// }
		/// </code>
		/// </example>
		[JsonProperty("errorTimeoutMs")]
		public long ErrorTimeoutMs { get; set; }

        /// <summary>
        /// Serializes collection apiKey property
        /// </summary>
        /// /// <remarks>
        /// </remarks>
        /// <value>
        /// ApiKey property gets/sets the value as a string
        /// </value>
        /// <example>
        /// <code>
        /// "properties": {
        ///		"apiKey": a55sdfasdf90878#8sd!df&sdf
        /// }
        /// </code>
        /// </example>
        [JsonProperty("apiKey")]
        public string ApiKey { get; set; }

        /// <summary>
        /// Determines if HTTPS header uses API key with time stamp
        /// </summary>
        /// /// <remarks>
        /// </remarks>
        /// <value>
        /// true/false
        /// </value>
        /// <example>
        /// <code>
        /// "properties": {
        ///		"headerUsesApiKey": true
        /// }
        /// </code>
        /// </example>
        [JsonProperty("headerUsesApiKey")]
        public bool HeaderUsesApiKey { get; set; }

        /// <summary>
        /// Serializes username as part of HTTPS header
        /// </summary>
        /// <remarks>
        /// Must match Enlighted Lighting Energy Manager (EM) authentication
        /// </remarks>
        /// <value>
        /// String of username
        /// </value>
        /// <example>
        /// <code>
        /// "properties": {
        ///		"apiKeyUsername": myUsername
        /// }
        /// </code>
        /// </example>
        [JsonProperty("apiKeyUsername")]
        public string ApiKeyUsername { get; set; }

		/// <summary>
		/// Example dictionary of objects
		/// </summary>
		/// <remarks>
		/// This is an example collection configuration object.  This should be modified or deleted as needed for the plugin being built.
		/// </remarks>
		/// <example>
		/// <code>
		/// "properties": {
		///		"presets": {
		///			"preset1": {
		///				"enabled": true,
		///				"name": "Preset 1"
		///			}
		///		}
		/// }
		/// </code>
		/// </example>
		/// <example>
		/// <code>
		/// "properties": {
		///		"inputNames": {
		///			"input1": "Input 1",
		///			"input2": "Input 2"		
		///		}
		/// }
		/// </code>
		/// </example>
		[JsonProperty("DeviceDictionary")]
		public Dictionary<string, EnlightedLightingIo> DeviceDictionary { get; set; }

		/// <summary>
		/// Constuctor
		/// </summary>
		/// <remarks>
		/// If using a collection you must instantiate the collection in the constructor
		/// to avoid exceptions when reading the configuration file 
		/// </remarks>
		public EnlightedLightingConfig()
		{
			DeviceDictionary = new Dictionary<string, EnlightedLightingIo>();
		}
	}

    /// <summary>
    /// Enlighted lighting IO configuration object
    /// </summary>
	public class EnlightedLightingIo
	{
		/// <summary>
		/// Serializes collection name property
		/// </summary>
		/// <remarks>
		/// This is an example collection of configuration objects.  This can be modified or deleted as needed for the plugin being built.
		/// </remarks>
		[JsonProperty("name")]
		public string Name { get; set; }

		/// <summary>
		/// Serializes collection value property
		/// </summary>
		/// <remarks>
		/// This is an example collection of configuration objects.  This can be modified or deleted as needed for the plugin being built.
		/// </remarks>
		[JsonProperty("id")]
		public string Id { get; set; }

                /// <summary>
        /// Constructor
        /// </summary>
        public EnlightedLightingIo()
        {
            Name = "";
            Id = "";
        }
	}
}