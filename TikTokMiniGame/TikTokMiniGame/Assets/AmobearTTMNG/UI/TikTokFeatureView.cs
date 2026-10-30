using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AmobearTTMNG.UI
{
    public enum TikTokUIFeature { Shortcut, Sidebar, Share }

    public sealed class TikTokFeatureView : MonoBehaviour
    {
        public TikTokUIFeature Feature;
        public Button EntryButton;
        public GameObject Popup;
        public TMP_Text Title;
        public TMP_Text Description;
        public Button ActionButton;
        public Button BackgroundButton;
        public Button CloseButton;
        [TextArea] public string RewardDescription = "Your reward is ready. Tap Claim to receive it.";
        [Tooltip("Connect a game reward method here before enabling Claim. Do not also grant this reward through the module's global event.")]
        public UnityEvent OnRewardClaimed = new UnityEvent();
        [Tooltip("Query sent when sharing; no demo wallet information is included.")]
        public string ShareQuery = "from=share";
        public string ShareTitle;
        public string ShareImageUrl;

        private string authoredTitle, authoredDescription, authoredAction;
        private TMP_Text actionLabel;
        private readonly List<GameObject> guide = new List<GameObject>();
        private Vector2 descriptionSize;
        private bool cached;
        internal bool Supported;
        internal bool Busy;
        internal bool ClaimMode;

        public bool HasPendingReward => Feature == TikTokUIFeature.Shortcut ? TikTokShortcut.HasPendingReward
            : Feature == TikTokUIFeature.Sidebar ? TikTokSidebar.HasPendingReward : TikTokShare.HasPendingReward;

        public bool HasValidRewardHandler()
        {
            if (OnRewardClaimed == null) return false;
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(this);
            var calls = serialized.FindProperty("OnRewardClaimed.m_PersistentCalls.m_Calls");
#endif
            for (int i = 0; i < OnRewardClaimed.GetPersistentEventCount(); i++)
            {
                var target = OnRewardClaimed.GetPersistentTarget(i);
                string method = OnRewardClaimed.GetPersistentMethodName(i);
                if (target != null && OnRewardClaimed.GetPersistentListenerState(i) != UnityEventCallState.Off
                    && !string.IsNullOrEmpty(method))
                {
#if UNITY_EDITOR
                    var call = calls.GetArrayElementAtIndex(i);
                    var mode = (PersistentListenerMode)call.FindPropertyRelative("m_Mode").enumValueIndex;
                    Type argument = null;
                    switch (mode)
                    {
                        case PersistentListenerMode.Int: argument = typeof(int); break;
                        case PersistentListenerMode.Float: argument = typeof(float); break;
                        case PersistentListenerMode.String: argument = typeof(string); break;
                        case PersistentListenerMode.Bool: argument = typeof(bool); break;
                        case PersistentListenerMode.Object:
                            argument = Type.GetType(call.FindPropertyRelative("m_Arguments.m_ObjectArgumentAssemblyTypeName").stringValue, false) ?? typeof(UnityEngine.Object);
                            break;
                    }
                    var resolved = UnityEventBase.GetValidMethodInfo(target, method, argument == null ? Type.EmptyTypes : new[] { argument });
                    if (resolved != null && resolved.ReturnType == typeof(void)) return true;
#else
                    // Serialized listener signatures are checked in the Editor/Validate Project.
                    var methods = target.GetType().GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    foreach (var candidate in methods)
                        if (candidate.Name == method && candidate.ReturnType == typeof(void) && candidate.GetParameters().Length <= 1)
                            return true;
#endif
                }
            }
            return false;
        }

        public bool HasReferences => EntryButton && Popup && Title && Description && ActionButton && BackgroundButton && CloseButton;

        internal void CacheContent()
        {
            if (cached || !HasReferences) return;
            cached = true;
            authoredTitle = Title.text;
            authoredDescription = Description.text;
            actionLabel = ActionButton.GetComponentInChildren<TMP_Text>(true);
            authoredAction = actionLabel ? actionLabel.text : "Start";
            descriptionSize = Description.rectTransform.sizeDelta;
            var parent = Description.transform.parent;
            if (parent != Title.transform.parent)
                foreach (Transform child in parent)
                    if (child != Description.transform && child.gameObject.activeSelf) guide.Add(child.gameObject);
        }

        internal void Show()
        {
            CacheContent();
            ClaimMode = HasPendingReward;
            Title.text = ClaimMode ? "Reward" : authoredTitle;
            Description.text = ClaimMode
                ? (HasValidRewardHandler() ? RewardDescription : "Reward is pending. The game has not configured its reward handler yet.")
                : authoredDescription;
            if (actionLabel) actionLabel.text = ClaimMode ? "Claim" : authoredAction;
            foreach (var item in guide) if (item) item.SetActive(!ClaimMode);
            Description.rectTransform.sizeDelta = ClaimMode
                ? new Vector2(descriptionSize.x, Mathf.Max(descriptionSize.y, Description.GetPreferredValues(Description.text, Description.rectTransform.rect.width, 0).y))
                : descriptionSize;
            ActionButton.interactable = !Busy && (ClaimMode ? HasValidRewardHandler() : Supported);
            Popup.SetActive(true);
            Popup.transform.SetAsLastSibling();
        }

        internal void ShowResult(string message)
        {
            Description.text = message;
            foreach (var item in guide) if (item) item.SetActive(false);
            Description.rectTransform.sizeDelta = new Vector2(descriptionSize.x,
                Mathf.Max(descriptionSize.y, Description.GetPreferredValues(message, Description.rectTransform.rect.width, 0).y));
            ActionButton.interactable = !Busy && Supported;
        }
    }
}
