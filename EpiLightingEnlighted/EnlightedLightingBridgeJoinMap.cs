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

		// TODO [X] Add digital joins below plugin being developed	

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

        [JoinName("HeaderUsesApiKey")]
        public JoinDataComplete HeaderUsesApiKey = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 10,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                Description = "Utilize API Key within HTTPS Requests",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
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

		// TODO [X] Add analog joins below plugin being developed

        [JoinName("Status")]
        public JoinDataComplete Status = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                Description = "Enlighted Lighting Status",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });

		#endregion


		#region Serial

		// TODO [X] Add serial joins below plugin being developed

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

        [JoinName("ApiKeyUsername")]
        public JoinDataComplete ApiKeyUsername = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 2,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                Description = "Username associated to API Key",
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
                Description = "Send command manually (do not include header or delimeter)",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("APIKey")]
        public JoinDataComplete ApiKey = new JoinDataComplete(
            new JoinData()
            {
                JoinNumber = 3,
                JoinSpan = 1
            },
            new JoinMetadata()
            {
                Description = "API Key associated to Username",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
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