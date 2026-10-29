using UnityEngine;

namespace HS.TikTokSDK.Missions
{
    /// <summary>
    /// Entrance Mission — navigates user to TikTok profile.
    /// 
    /// SDK handles:
    ///   - TT.StartEntranceMission() → opens TikTok profile
    ///   - OnApplicationPause detection → user returned
    ///   - TT.GetEntranceMissionReward() → verify mission
    ///   - Reports Success or Failed
    ///
    /// Game handles:
    ///   - Daily/once claim checks
    ///   - Granting rewards (hints, coins, complex data, etc.)
    ///   - UI feedback
    /// 
    /// NOTE: No #if directives — this ships as a precompiled DLL.
    /// Runtime platform checks only.
    /// </summary>
    public class EntranceMission : TikTokMissionBase
    {
        public override MissionType Type => MissionType.EntranceMission;

        /// <summary>Used by TikTokMissions to detect pending return.</summary>
        internal bool PendingReturnCheck { get; private set; } = false;

        protected override void OnExecute()
        {
            if (IsWebGL)
            {
                TikTokSDK.Log($"[Missions] {Type}: Calling TT.StartEntranceMission()...");
                try
                {
                    TTSDK.TT.StartEntranceMission(new TTSDK.TTStartEntranceMissionParam
                    {
                        success = (data) =>
                        {
                            TikTokSDK.Log($"[Missions] {Type}: TikTok profile nav opened. Waiting for return...");
                            PendingReturnCheck = true;
                        },
                        fail = (err) =>
                        {
                            string errMsg = err?.ToString() ?? "Unknown error";
                            TikTokSDK.Log($"[Missions] {Type}: StartEntranceMission FAILED: {errMsg}");
                            CompleteFailed(errMsg);
                        }
                    });
                }
                catch (System.Exception e)
                {
                    TikTokSDK.LogError($"[Missions] {Type}: Exception calling TT API: {e.Message}");
                    CompleteFailed(e.Message);
                }
            }
            else
            {
                // Editor: no TT API, simulate success
                TikTokSDK.Log($"[Missions] {Type}: Editor mode — simulating success.");
                CompleteSuccess();
            }
        }

        /// <summary>Called by TikTokMissions when user returns from TikTok.</summary>
        internal void OnAppResumed()
        {
            PendingReturnCheck = false;
            TikTokSDK.Log($"[Missions] {Type}: User returned! Verifying mission...");
            VerifyMission();
        }

        private void VerifyMission()
        {
            if (IsWebGL)
            {
                TikTokSDK.Log($"[Missions] {Type}: Calling TT.GetEntranceMissionReward()...");
                try
                {
                    TTSDK.TT.GetEntranceMissionReward(new TTSDK.TTGetEntranceMissionRewardParam
                    {
                        success = (result) =>
                        {
                            TikTokSDK.Log($"[Missions] {Type}: Mission verified!");
                            CompleteSuccess();
                        },
                        fail = (error) =>
                        {
                            string errMsg = error?.ToString() ?? "Unknown error";
                            TikTokSDK.LogError($"[Missions] {Type}: Verification FAILED: {errMsg}");
                            CompleteFailed(errMsg);
                        }
                    });
                }
                catch (System.Exception e)
                {
                    TikTokSDK.LogError($"[Missions] {Type}: Exception verifying: {e.Message}");
                    CompleteFailed(e.Message);
                }
            }
            else
            {
                CompleteSuccess();
            }
        }
    }
}
