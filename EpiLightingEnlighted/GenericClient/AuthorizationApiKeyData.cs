using System;

namespace PepperDash.Essentials.Plugin.EnlightedLighting
{
    /// <summary>
    /// Class to store ApiKey and ApiKeyUsername
    /// </summary>
    public class AuthorizationApiKeyData
    {
        public string ApiKey { get; set; }
        public string ApiKeyUsername { get; set; }
    }

    /// <summary>
    /// Class to get current timestamp with milliseconds precision
    /// </summary>
    static class CurrentMillis
    {
        private static readonly DateTime Jan1St1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        /// <summary>Get extra long current timestamp</summary>
        public static long Millis { get { return (long)((DateTime.UtcNow - Jan1St1970).TotalMilliseconds); } }
    }
}