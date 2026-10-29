namespace HS.TikTokSDK.Missions
{
    /// <summary>
    /// Shortcut Mission — shows native TikTok "Add Shortcut" dialog.
    /// 
    /// SDK handles:
    ///   - TT.AddShortcut() → native in-app dialog
    ///   - TT.GetShortcutMissionReward() → verify mission
    ///   - Reports Success or Failed
    ///
    /// Game handles:
    ///   - Once/daily claim checks
    ///   - Granting rewards
    ///   - UI feedback
    /// 
    /// NOTE: No #if directives — this ships as a precompiled DLL.
    /// Runtime platform checks only.
    /// </summary>
    public class ShortcutMission : TikTokMissionBase
    {
        public override MissionType Type => MissionType.ShortcutMission;

        protected override void OnExecute()
        {
            if (IsWebGL)
            {
                TikTokSDK.Log($"[Missions] {Type}: Calling TT.AddShortcut()...");
                try
                {
                    TTSDK.TT.AddShortcut(
                        csCallback: (success) =>
                        {
                            TikTokSDK.Log($"[Missions] {Type}: TT.AddShortcut result: {success}");
                            if (success)
                                VerifyMissionReward();
                            else
                                CompleteFailed("User cancelled");
                        }
                    );
                }
                catch (System.Exception e)
                {
                    TikTokSDK.LogError($"[Missions] {Type}: Exception calling TT API: {e.Message}");
                    CompleteFailed(e.Message);
                }
            }
            else
            {
                TikTokSDK.Log($"[Missions] {Type}: Editor mode — simulating success.");
                CompleteSuccess();
            }
        }

        private void VerifyMissionReward()
        {
            if (IsWebGL)
            {
                TikTokSDK.Log($"[Missions] {Type}: Calling TT.GetShortcutMissionReward()...");
                try
                {
                    TTSDK.TT.GetShortcutMissionReward(new TTSDK.GetShortcutMissionRewardParam
                    {
                        Success = (result) =>
                        {
                            if (result.CanReceiveReward)
                            {
                                TikTokSDK.Log($"[Missions] {Type}: CanReceiveReward = true!");
                                CompleteSuccess();
                            }
                            else
                            {
                                TikTokSDK.Log($"[Missions] {Type}: CanReceiveReward = false.");
                                CompleteFailed("Mission not complete");
                            }
                        },
                        Fail = (error) =>
                        {
                            string errMsg = error?.ToString() ?? "Unknown error";
                            TikTokSDK.LogError($"[Missions] {Type}: GetShortcutMissionReward FAILED: {errMsg}");
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
