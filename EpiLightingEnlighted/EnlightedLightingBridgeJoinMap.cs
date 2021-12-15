using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Plugin.EnlightedLighting
{
	/// <summary>
	/// Plugin device Bridge Join Map
	/// </summary>
	/// <remarks>
	/// Rename the class to match the device plugin being developed.  Reference Essentials JoinMaps, if one exists for the device plugin being developed
	/// </remarks>
	/// <see cref="PepperDash.Essentials.Core.Bridges"/>
	/// <example>
	/// "EssentialsPluginBridgeJoinMapTemplate" renamed to "SamsungMdcBridgeJoinMap"
	/// </example>
	public class EnlightedLightingBridgeJoinMap : JoinMapBaseAdvanced
	{

		#region Digital	

        [JoinName("IsOnline")]
        public JoinDataComplete IsOnline = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                Description = "Device Online",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("Poll")]
        public JoinDataComplete Poll = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 11,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                Description = "Manual Poll Device",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("QueryListOperations")]
        public JoinDataComplete QueryListOperations = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 12,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                Description = "Query Operations List",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

		#endregion


		#region Analog		

        //[JoinName("Status")]
        //public JoinDataComplete Status = new JoinDataComplete(
        //    new JoinData()
        //    {
        //        JoinNumber = 1,
        //        JoinSpan = 1
        //    },
        //    new JoinMetadata()
        //    {
        //        Description = "Enlighted Lighting Status",
        //        JoinCapabilities = eJoinCapabilities.ToSIMPL,
        //        JoinType = eJoinType.Analog
        //    });

		#endregion


		#region Serial		

        [JoinName("Name")]
        public JoinDataComplete Name = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                Description = "Enlighted Lighting Device Name",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("ManualCommand")]
        public JoinDataComplete ManualCommand = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 2,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                Description = "Send command path manually (do not include header or delimeter)",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("ApplyScene")]
        public JoinDataComplete ApplyScene = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 3,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                Description = "Send command path manually (do not include header or delimeter)",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            });

		#endregion

		/// <summary>
		/// Plugin device BridgeJoinMap constructor
		/// </summary>
		/// <param name="joinStart">This will be the join it starts on the EISC bridge</param>
		public EnlightedLightingBridgeJoinMap(uint joinStart)
			: base(joinStart, typeof(EnlightedLightingBridgeJoinMap))
		{
		}
	}
}