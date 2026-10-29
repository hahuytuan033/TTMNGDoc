namespace HS.TikTokSDK.Missions
{
    // ─── Mission Types ──────────────────────────────────────
    public enum MissionType
    {
        EntranceMission,    // Gift / Follow Profile
        ShortcutMission,    // Bookmark / Add to Home Screen
        ShareMission,       // Share to feed (future)
        // Add new mission types here
    }

    // ─── Mission Status ─────────────────────────────────────
    public enum MissionStatus
    {
        /// <summary>TikTok mission completed successfully. Game should grant reward.</summary>
        Success,
        /// <summary>TikTok API error or user cancelled the action.</summary>
        Failed
    }

    // ─── Mission Result ─────────────────────────────────────
    /// <summary>
    /// Result of a TikTok mission attempt.
    /// The SDK only reports success/fail from the TT API.
    /// Claim guards, rewards, and business logic are the game's responsibility.
    /// </summary>
    public struct MissionResult
    {
        public MissionType Type;
        public MissionStatus Status;
        public string Error;

        /// <summary>True if the TikTok mission was completed successfully.</summary>
        public bool IsSuccess => Status == MissionStatus.Success;
    }
}
