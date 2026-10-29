using System;
using System.Collections.Generic;
using UnityEngine;

namespace HS.TikTokSDK.Missions
{
    /// <summary>
    /// Central manager for TikTok missions. FULLY OPTIONAL — does nothing
    /// unless the game explicitly registers missions.
    ///
    /// This follows the same pattern as TikTokSync: if no missions are
    /// registered, this component sits idle with zero overhead.
    ///
    /// Usage (in game's Start/Awake):
    ///   var missions = TikTokMissions.Instance;
    ///   if (missions == null) return; // not in scene — skip
    ///
    ///   // Register only the missions this game needs
    ///   missions.Register(new EntranceMission { RewardId = "gift", RewardAmount = 5 });
    ///   missions.Register(new ShortcutMission { RewardId = "bookmark", RewardAmount = 10 });
    ///
    ///   // Subscribe to results
    ///   missions.OnMissionResult += HandleResult;
    ///
    ///   // Execute when user clicks a button
    ///   missions.Execute(MissionType.EntranceMission);
    /// </summary>
    public class TikTokMissions : MonoBehaviour
    {
        public static TikTokMissions Instance { get; private set; }

        // ─── Events ──────────────────────────────────────────
        /// <summary>
        /// Fired when any mission completes (success, failure, or already-claimed).
        /// Only fires if missions are registered.
        /// </summary>
        public event Action<MissionResult> OnMissionResult;

        // ─── Registry ────────────────────────────────────────
        private readonly Dictionary<MissionType, ITikTokMission> _missions
            = new Dictionary<MissionType, ITikTokMission>();

        /// <summary>True if any missions have been registered.</summary>
        public bool HasMissions => _missions.Count > 0;

        // ─── Lifecycle ──────────────────────────────────────
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                TikTokSDK.Log("[Missions] Manager ready. No missions registered yet — waiting for game to register.");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // ─── Public API ─────────────────────────────────────

        /// <summary>
        /// Register a mission. Only registered missions can be executed.
        /// The game decides which missions to enable.
        ///
        /// Example:
        ///   TikTokMissions.Instance.Register(new EntranceMission { RewardId = "gift", RewardAmount = 5 });
        /// </summary>
        public void Register(ITikTokMission mission)
        {
            _missions[mission.Type] = mission;
            TikTokSDK.Log($"[Missions] Registered: {mission.Type}");
        }

        /// <summary>
        /// Execute a mission by type. Does nothing if the type is not registered.
        /// Results are delivered via the <see cref="OnMissionResult"/> event.
        /// </summary>
        public void Execute(MissionType type)
        {
            if (!_missions.TryGetValue(type, out var mission))
            {
                TikTokSDK.LogWarning($"[Missions] Cannot execute — {type} is not registered. Call Register() first.");
                return;
            }

            if (mission.IsProcessing)
            {
                TikTokSDK.LogWarning($"[Missions] {type} is already processing.");
                return;
            }

            mission.Execute(OnMissionCompleted);
        }

        /// <summary>
        /// Check if a specific mission type has been registered.
        /// </summary>
        public bool IsRegistered(MissionType type)
        {
            return _missions.ContainsKey(type);
        }

        /// <summary>
        /// Check if a specific mission is currently in progress.
        /// </summary>
        public bool IsProcessing(MissionType type)
        {
            return _missions.TryGetValue(type, out var mission) && mission.IsProcessing;
        }

        /// <summary>
        /// Check if any mission is currently in progress.
        /// </summary>
        public bool IsAnyProcessing()
        {
            foreach (var mission in _missions.Values)
            {
                if (mission.IsProcessing) return true;
            }
            return false;
        }

        // ─── OnApplicationPause forwarding ──────────────────
        /// <summary>
        /// Forwards pause/resume events to missions that need it.
        /// Only active when an EntranceMission is registered and pending.
        /// </summary>
        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused || _missions.Count == 0) return;

            // Forward to EntranceMission if it's waiting for user return
            if (_missions.TryGetValue(MissionType.EntranceMission, out var mission)
                && mission is EntranceMission entrance
                && entrance.PendingReturnCheck)
            {
                entrance.OnAppResumed();
            }
        }

        // ─── Internal ───────────────────────────────────────
        private void OnMissionCompleted(MissionResult result)
        {
            OnMissionResult?.Invoke(result);
        }
    }
}
