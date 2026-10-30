using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace AmobearTTMNG.UI
{
    /// <summary>Allows the Canvas prefab to receive clicks in scenes without an EventSystem.</summary>
    [DisallowMultipleComponent]
    public sealed class TikTokUIInput : MonoBehaviour
    {
        private void OnEnable()
        {
            if (FindObjectOfType<EventSystem>()) return;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var moduleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (moduleType == null)
            {
                Debug.LogError("[TikTok UI] Input System UI module is unavailable. Install/enable the Input System package.", this);
                return;
            }
#else
            var moduleType = typeof(StandaloneInputModule);
#endif
            var go = new GameObject("EventSystem (TikTok UI)");
            SceneManager.MoveGameObjectToScene(go, gameObject.scene);
            go.AddComponent<EventSystem>();
            var module = go.AddComponent(moduleType);
            moduleType.GetMethod("AssignDefaultActions", Type.EmptyTypes)?.Invoke(module, null);
        }
    }
}
