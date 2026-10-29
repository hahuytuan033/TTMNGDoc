using UnityEngine;
using HS.TikTokSDK;
using HS.TikTokSDK.Auth;

/// <summary>
/// SAMPLE: Shows how to trigger login and handle the backend response.
/// Copy this into your project and adapt it to your game's loading flow.
///
/// What happens under the hood:
///   1. WebGL: TT.Login() → TikTok returns a temporary auth code
///      Editor: EditorMockLogin() → uses test_open_id (no real TikTok code)
///   2. SDK sends POST /api/login with { game_id, code } + SDK auth headers
///   3. Backend exchanges code with TikTok → gets open_id + access_token
///   4. Backend creates/updates user, generates session_token
///   5. Backend returns: { session_token, open_id, profile_data,
///      last_updated_timestamp, pending_rewards, refund_orders }
///   6. SDK stores session_token + open_id internally
///   7. SDK fires OnLoginSuccess(profileJson, serverTimestamp)
/// </summary>
public class SampleLoginHandler : MonoBehaviour
{
    [Header("UI References (optional)")]
    [Tooltip("Assign a loading panel to show during login")]
    public GameObject loadingPanel;

    [Tooltip("Assign a login-failed panel")]
    public GameObject errorPanel;

    private void Start()
    {
        // ─── Subscribe to auth events BEFORE triggering login ──────
        TikTokAuth.Instance.OnLoginSuccess += HandleLoginSuccess;
        TikTokAuth.Instance.OnLoginFailed  += HandleLoginFailed;

        // ─── Start login automatically ─────────────────────────────
        StartLogin();
    }

    private void OnDestroy()
    {
        // Always unsubscribe to prevent leaks
        if (TikTokAuth.Instance != null)
        {
            TikTokAuth.Instance.OnLoginSuccess -= HandleLoginSuccess;
            TikTokAuth.Instance.OnLoginFailed  -= HandleLoginFailed;
        }
    }

    // ─── Trigger Login ─────────────────────────────────────────────

    public void StartLogin()
    {
        Debug.Log("[SampleLogin] Starting login...");

        if (loadingPanel != null)
            loadingPanel.SetActive(true);

#if UNITY_WEBGL && !UNITY_EDITOR
        // ── PRODUCTION (WebGL on TikTok) ───────────────────────
        // TT.Login() calls the TikTok JS SDK (tt.login / TTMinis.game.login)
        // and returns a temporary auth code on success.
        TT.Login(
            successCallback: (code) =>
            {
                Debug.Log("[SampleLogin] Got TikTok auth code, exchanging...");
                // Send code to YOUR backend (not to TikTok directly)
                // The SDK handles: POST /api/login → backend → TikTok OAuth
                TikTokAuth.Instance.LoginWithCode(code);
            },
            failedCallback: (err) =>
            {
                Debug.LogError($"[SampleLogin] TT.Login failed: {err}");
                HandleLoginFailed($"TikTok login failed: {err}");
            }
        );
#else
        // ── EDITOR / TEST MODE ─────────────────────────────────
        // No real TikTok code — backend detects X-Is-Test header
        // and creates/returns a test user (open_id = test_<device_hash>)
        Debug.Log("[SampleLogin] Editor mode → mock login");
        TikTokAuth.Instance.EditorMockLogin();
#endif
    }

    // ─── Login Success ─────────────────────────────────────────────

    /// <summary>
    /// Called when backend returns 200 OK with session data.
    ///
    /// At this point the SDK has already:
    ///   • Stored session_token + open_id (TikTokSDK.Instance.SessionToken)
    ///   • Applied server-driven SDK config (polling intervals, sync cooldowns)
    ///   • Triggered CheckPendingRewards() for crash recovery
    ///
    /// You receive:
    ///   profileJson      — the player's cloud save data (null if first login)
    ///   serverTimestamp   — Unix timestamp of the last cloud sync
    /// </summary>
    private void HandleLoginSuccess(string profileJson, long serverTimestamp)
    {
        Debug.Log($"[SampleLogin] ✅ Login success!");
        Debug.Log($"  OpenId:    {TikTokSDK.Instance.OpenId}");
        Debug.Log($"  Session:   {TikTokSDK.Instance.SessionToken.Substring(0, 8)}...");
        Debug.Log($"  Timestamp: {serverTimestamp}");

        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        // ── Load cloud save data (if any) ──────────────────────
        if (!string.IsNullOrEmpty(profileJson))
        {
            Debug.Log($"  Cloud data received: {profileJson.Length} chars");

            // If using ITikTokSyncProvider, the SDK calls OnCloudDataReceived
            // automatically. Otherwise, deserialize it yourself:
            //
            // var saveData = JsonUtility.FromJson<MyPlayerData>(profileJson);
            // ApplySaveData(saveData);
        }
        else
        {
            Debug.Log("  No cloud data — first login or no sync provider.");
            // Initialize fresh player data
        }

        // ── Proceed to game ────────────────────────────────────
        // Example: load the main menu scene
        // UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    // ─── Login Failed ──────────────────────────────────────────────

    private void HandleLoginFailed(string error)
    {
        Debug.LogError($"[SampleLogin] ❌ Login failed: {error}");

        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        if (errorPanel != null)
            errorPanel.SetActive(true);

        // Common errors:
        //   "Missing game_id or code"  → SDK misconfigured (check Inspector)
        //   "Invalid game_id"          → game not registered on backend
        //   "TikTok Auth Failed"       → code expired or invalid
        //   "Network error"            → backend unreachable
        //   "Server redirect failed."  → 301 redirect without valid api_url
    }

    // ─── Retry Button (wire to your UI) ────────────────────────────

    public void OnRetryButtonClicked()
    {
        if (errorPanel != null)
            errorPanel.SetActive(false);

        StartLogin();
    }
}
