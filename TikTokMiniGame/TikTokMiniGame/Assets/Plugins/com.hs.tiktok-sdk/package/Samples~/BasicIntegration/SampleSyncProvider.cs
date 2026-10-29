using UnityEngine;

/// <summary>
/// SAMPLE: Minimal ITikTokSyncProvider implementation.
/// Copy this into your project and adapt it to your game's save system.
/// </summary>
public class SampleSyncProvider : MonoBehaviour, HS.TikTokSDK.ITikTokSyncProvider
{
    // ─── Your game's save data ─────────────────────────
    // Replace this with your actual player data class.
    [System.Serializable]
    public class MyPlayerData
    {
        public int level = 1;
        public int coins = 0;
        public bool adsRemoved = false;
        public long lastUpdatedTimestamp = 0;
    }

    public MyPlayerData playerData = new MyPlayerData();

    // ─── ITikTokSyncProvider ───────────────────────────

    public bool IsDirtyForCloud { get; set; }

    public string SerializeForCloud()
    {
        // Serialize your data to JSON for cloud upload
        return JsonUtility.ToJson(playerData);
    }

    public void OnCloudSyncSuccess(long serverTimestamp)
    {
        // Store the server timestamp WITHOUT setting IsDirtyForCloud = true
        playerData.lastUpdatedTimestamp = serverTimestamp;
        // Save locally (your own save mechanism)
        Debug.Log($"[SampleSyncProvider] Cloud sync success. Server timestamp: {serverTimestamp}");
    }

    public void OnCloudDataReceived(string profileJson, long serverTimestamp)
    {
        // Server has newer data — deserialize and overwrite local save
        playerData = JsonUtility.FromJson<MyPlayerData>(profileJson);
        playerData.lastUpdatedTimestamp = serverTimestamp;
        Debug.Log($"[SampleSyncProvider] Received cloud data. Level: {playerData.level}");
    }

    // ─── Registration ──────────────────────────────────

    private void Start()
    {
        // Register with the SDK so it can call our sync methods
        HS.TikTokSDK.TikTokSDK.Instance?.RegisterSyncProvider(this);
    }

    // ─── Example: Mark data as dirty after gameplay ────

    public void OnLevelComplete()
    {
        playerData.level++;
        IsDirtyForCloud = true;
        // Save locally first (instant), cloud sync happens in background
    }
}
