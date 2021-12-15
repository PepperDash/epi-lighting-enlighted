using System;
using PepperDash.Core;

namespace PepperDash.Essentials.Plugin.EnlightedLighting
{
    /// <summary>
    /// Client Response Event Args
    /// </summary>
    public class GenericClientResponseEventArgs : EventArgs
    {
        /// <summary>
        /// Client response code
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Client response content string
        /// </summary>
        public string ContentString { get; set; }
        
        /// <summary>
        /// Client URL Response (path of the request)
        /// </summary>
        public string ResponseUrl { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public GenericClientResponseEventArgs()
        {

        }

        /// <summary>
        /// Constructor overload
        /// </summary>
        /// <param name="code"></param>
        /// <param name="contentString"></param>
        /// /// <param name="responseUrl"></param>
        public GenericClientResponseEventArgs(int code, string contentString, string responseUrl)
        {
            Code = code < 0 ? 0 : code;
            ContentString = string.IsNullOrEmpty(contentString) ? "" : contentString;
            ResponseUrl = responseUrl;
        }
    }
}