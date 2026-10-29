namespace HS.TikTokSDK.Missions
{
    /// <summary>
    /// Contract for all TikTok missions. Implement this to add a new mission type.
    /// Each mission handles its own TT API calls, claim guards, and Editor simulation.
    /// </summary>
    public interface ITikTokMission
    {
        /// <summary>The type of this mission (used for routing results).</summary>
        MissionType Type { get; }

        /// <summary>True when this mission is currently in progress.</summary>
        bool IsProcessing { get; }

        /// <summary>
        /// Start the mission flow.
        /// Implementations should handle:
        ///   - Platform check (WebGL vs Editor)
        ///   - TT API calls
        ///   - Claim guards (daily/once)
        ///   - Broadcasting result via callback
        /// </summary>
        /// <param name="onResult">Callback with mission result when flow completes.</param>
        void Execute(System.Action<MissionResult> onResult);
    }
}
