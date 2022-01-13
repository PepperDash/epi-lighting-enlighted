using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash.Essentials.Plugin.EnlightedLighting
{
    public class AuthorizationApiKeyData
    {
        public bool HeaderUsesApiKey { get; set; }
        public string ApiKey { get; set; }
        public string ApiKeyUsername { get; set; }
    }

    /// <summary>Class to get current timestamp with enough precision</summary>
    static class CurrentMillis
    {
        private static readonly DateTime Jan1St1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        /// <summary>Get extra long current timestamp</summary>
        public static long Millis { get { return (long)((DateTime.UtcNow - Jan1St1970).TotalMilliseconds); } }
    }
}