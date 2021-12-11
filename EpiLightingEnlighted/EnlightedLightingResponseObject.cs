using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace PepperDash.Essentials.Plugin.EnlightedLighting
{
    // Classes below were converted using http://json2csharp.com/	

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

    public class EnlightedLightingResponseObject
    {
        //[JsonProperty("meta")]
        //public MetaConfig Meta { get; set; }

        //[JsonProperty("data")]
        //public List<DataConfig> Data { get; set; }
    }
}