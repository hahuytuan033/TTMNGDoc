using System;
using UnityEngine;

namespace HS.TikTokSDK.Missions
{
    /// <summary>
    /// Base class for TikTok missions. Provides shared utilities:
    /// platform detection, processing guard, and result broadcasting.
    /// 
    /// The SDK only handles TT API calls + lifecycle.
    /// Claim guards, rewards, business logic = game's responsibility.
    /// </summary>
    public abstract class TikTokMissionBase : ITikTokMission
    {
        // ─── Interface ──────────────────────────────────────
        public abstract MissionType Type { get; }
        public bool IsProcessing => _isProcessing;

        // ─── State ──────────────────────────────────────────
        protected bool _isProcessing = false;
        protected Action<MissionResult> _onResult;

        // ─── Helpers ────────────────────────────────────────
        protected bool IsWebGL => Application.platform == RuntimePlatform.WebGLPlayer;

        // ─── Execute (template method) ──────────────────────
        public void Execute(Action<MissionResult> onResult)
        {
            if (_isProcessing)
            {
                TikTokSDK.LogWarning($"[Missions] {Type}: Already processing. Please wait.");
                return;
            }

            _isProcessing = true;
            _onResult = onResult;
            TikTokSDK.Log($"[Missions] {Type}: Starting...");

            OnExecute();
        }

        /// <summary>
        /// Override this to implement the mission-specific TT API flow.
        /// Call <see cref="CompleteSuccess"/> or <see cref="CompleteFailed"/> when done.
        /// </summary>
        protected abstract void OnExecute();

        // ─── Result helpers ─────────────────────────────────

        /// <summary>TT API confirmed mission success.</summary>
        protected void CompleteSuccess()
        {
            Complete(new MissionResult
            {
                Type = this.Type,
                Status = MissionStatus.Success
            });
        }

        /// <summary>TT API returned error or user cancelled.</summary>
        protected void CompleteFailed(string error = null)
        {
            Complete(new MissionResult
            {
                Type = this.Type,
                Status = MissionStatus.Failed,
                Error = error
            });
        }

        private void Complete(MissionResult result)
        {
            _isProcessing = false;
            TikTokSDK.Log($"[Missions] {Type}: Completed → {result.Status}" +
                (result.Error != null ? $" ({result.Error})" : ""));
            _onResult?.Invoke(result);
            _onResult = null;
        }
    }
}
