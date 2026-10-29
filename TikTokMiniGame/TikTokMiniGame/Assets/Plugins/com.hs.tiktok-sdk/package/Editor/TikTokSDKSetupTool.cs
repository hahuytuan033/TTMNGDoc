#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using HS.TikTokSDK;
using HS.TikTokSDK.Auth;
using HS.TikTokSDK.IAP;
using HS.TikTokSDK.Sync;

namespace HS.TikTokSDK.Editor
{
    /// <summary>
    /// One-click SDK setup tool. Creates a persistent GameObject with all SDK
    /// components pre-configured and ready to use.
    /// </summary>
    public class TikTokSDKSetupTool
    {
        [MenuItem("IAP TikTok Setup/Setup SDK in Scene", false, 10)]
        public static void SetupSDK()
        {
            string sdkName = "HS_TikTokSDK";

            // Check if it already exists
            GameObject existing = GameObject.Find(sdkName);
            if (existing != null)
            {
                Selection.activeGameObject = existing;
                Debug.LogWarning("[HS.TikTokSDK] SDK GameObject already exists in the scene!");
                return;
            }

            // Create the SDK root GameObject
            GameObject sdkObj = new GameObject(sdkName);

            // Core config
            var core = sdkObj.AddComponent<TikTokSDK>();
            core.sdkKey = "";
            core.gameId = "";

            // Auth
            sdkObj.AddComponent<TikTokAuth>();

            // IAP
            sdkObj.AddComponent<TikTokIAP>();

            // Sync
            var sync = sdkObj.AddComponent<TikTokSync>();
            sync.syncIntervalSeconds = 10f;

            // Select it in the hierarchy
            Selection.activeGameObject = sdkObj;

            // Mark scene dirty so the change is saved
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("<b>[HS.TikTokSDK] SDK setup complete!</b> Enter your SDK Key and Game ID in the Inspector.");
        }
    }
}
#endif
