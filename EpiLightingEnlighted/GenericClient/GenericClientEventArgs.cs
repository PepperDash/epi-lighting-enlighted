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
        public GenericClientResponseEventArgs(int code, string contentString)
        {
            Code = code < 0 ? 0 : code;
            ContentString = string.IsNullOrEmpty(contentString) ? "" : contentString;
        }
    }
}