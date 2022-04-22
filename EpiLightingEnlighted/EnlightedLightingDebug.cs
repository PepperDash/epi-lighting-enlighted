using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash.Essentials.Plugin.EnlightedLighting
{
    public class EnlightedLightingDebug
    {
        // public props
        public uint DebugInfo = 0;
        public uint DebugWarn = 1;
        public uint DebugVerbose = 2;

        /// <summary>
        /// Replaces current 'DebugInfo' value. Default value is 0.
        /// </summary>
        /// <param name="level">Integer level of DebugInfo</param>
        public void SetDebugInfo(uint level)
        {
          DebugInfo = level;
        }

        /// <summary>
        /// Replaces 'SetDebugWarn' value. Default value is 1. 
        /// </summary>
        /// <param name="level"></param>
        public void SetDebugWarn(uint level)
        {
          DebugWarn = level;
        }

        /// <summary>
        /// Replaces 'SetDebugVerbose' value. Default value is 2.
        /// </summary>
        /// <param name="level"></param>
        public void SetDebugVerbose(uint level)
        {
          DebugVerbose = level;
        }

        /// <summary>
        /// Set all debug values (DebugInfo, DebugWarn, and DebugVerbose) to level value provided
        /// </summary>
        /// <param name="level">Integer value all Debug values are to be set to</param>
        public void SetDebugAll(uint level)
        {
          DebugInfo = level;
          DebugWarn = level;
          DebugVerbose = level;
        }

        /// <summary>
        /// Resets all Debug (DebugInfo = 0, DebugWarn = 1, and DebugVerbose = 2) values to default. 
        /// </summary>
        public void ResetDebugAll() {
          DebugInfo = 0;
          DebugWarn = 1;
          DebugVerbose = 2;
        }
    }
}