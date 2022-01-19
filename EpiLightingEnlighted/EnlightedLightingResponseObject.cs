using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace PepperDash.Essentials.Plugin.EnlightedLighting
{
    // Classes below converted using http://json2csharp.com/	

    /// <summary>
    /// Response device
    /// </summary>
    public class DeviceConfig
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class EnlightedLightingResponseStatus
    {
        [JsonProperty("status")]
        public string Status { get; set; }        
    }
}