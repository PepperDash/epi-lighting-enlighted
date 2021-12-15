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
}