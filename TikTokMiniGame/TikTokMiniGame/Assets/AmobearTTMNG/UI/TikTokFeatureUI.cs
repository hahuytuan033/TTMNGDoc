using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace AmobearTTMNG.UI
{
    [DisallowMultipleComponent]
    public sealed class TikTokFeatureUI : MonoBehaviour
    {
        public List<TikTokFeatureView> Features = new List<TikTokFeatureView>();
        public RectTransform PopupsRoot;
        public bool AutoOpenClaim = true;
        private static TikTokFeatureUI owner;
        private readonly HashSet<TikTokUIFeature> dismissed = new HashSet<TikTokUIFeature>();
        private readonly Dictionary<TikTokFeatureView, UnityAction[]> listeners = new Dictionary<TikTokFeatureView, UnityAction[]>();
        private TikTokFeatureView opened;
        private int generation;
        private bool initialized;

        private void OnEnable()
        {
            if (owner != null && owner != this)
            {
                Debug.LogWarning("[TikTok UI] Another feature UI is already active. This controller is disabled.", this);
                enabled = false;
                return;
            }
            owner = this;
            int token = ++generation;
            foreach (var view in Features.Where(v => v))
            {
                if (!view.HasReferences) { Debug.LogError("[TikTok UI] Missing references: " + view.Feature, view); continue; }
                view.CacheContent();
                view.Popup.SetActive(false);
                view.EntryButton.gameObject.SetActive(false);
                view.Busy = false;
                UnityAction show = () => Open(view);
                UnityAction action = () => Act(view);
                UnityAction close = ClosePopup;
                view.EntryButton.onClick.AddListener(show);
                view.ActionButton.onClick.AddListener(action);
                view.BackgroundButton.onClick.AddListener(close);
                view.CloseButton.onClick.AddListener(close);
                listeners[view] = new[] { show, action, close };
            }
            TikTokShortcut.OnPendingRewardChanged += PendingChanged;
            TikTokSidebar.OnPendingRewardChanged += PendingChanged;
            TikTokShare.OnPendingRewardChanged += PendingChanged;
#if UNITY_EDITOR
            InitializeViews(token);
#else
            TikTokBootstrap.WhenSdkReady(() => { if (Alive(token)) InitializeViews(token); });
#endif
        }

        private bool Alive(int token) => this != null && isActiveAndEnabled && generation == token;

        private void InitializeViews(int token)
        {
            initialized = true;
            foreach (var view in Features.Where(v => v && v.HasReferences))
            {
                Action<bool> supported = ok =>
                {
                    if (!Alive(token) || !view) return;
                    view.Supported = ok;
                    view.EntryButton.gameObject.SetActive(ok || view.HasPendingReward);
                };
                switch (view.Feature)
                {
                    case TikTokUIFeature.Shortcut: TikTokShortcut.CheckSupported(supported); break;
                    case TikTokUIFeature.Sidebar: TikTokSidebar.CheckSupported(supported); break;
                    case TikTokUIFeature.Share: TikTokShare.CheckSupported(supported); break;
                }
            }
            TikTokShortcut.RefreshReward();
            TikTokSidebar.RefreshReward();
            PendingChanged();
        }

        private void OnDisable()
        {
            ++generation;
            initialized = false;
            TikTokShortcut.OnPendingRewardChanged -= PendingChanged;
            TikTokSidebar.OnPendingRewardChanged -= PendingChanged;
            TikTokShare.OnPendingRewardChanged -= PendingChanged;
            foreach (var pair in listeners)
            {
                var view = pair.Key;
                if (!view) continue;
                if (view.EntryButton) view.EntryButton.onClick.RemoveListener(pair.Value[0]);
                if (view.ActionButton) view.ActionButton.onClick.RemoveListener(pair.Value[1]);
                if (view.BackgroundButton) view.BackgroundButton.onClick.RemoveListener(pair.Value[2]);
                if (view.CloseButton) view.CloseButton.onClick.RemoveListener(pair.Value[2]);
                if (view.Popup) view.Popup.SetActive(false);
            }
            listeners.Clear();
            opened = null;
            if (owner == this) owner = null;
        }

        public void Open(TikTokFeatureView view)
        {
            if (!initialized || !view || !view.HasReferences || !Features.Contains(view)) return;
            if (opened && opened.Busy) return;
            if (opened && opened != view) opened.Popup.SetActive(false);
            opened = view;
            view.Show();
        }

        public void ClosePopup()
        {
            if (!opened) return;
            dismissed.Add(opened.Feature);
            opened.Popup.SetActive(false);
            opened = null;
            PendingChanged();
        }

        private void PendingChanged()
        {
            if (!initialized || !isActiveAndEnabled) return;
            foreach (var view in Features.Where(v => v && v.HasReferences))
            {
                if (!view.HasPendingReward) dismissed.Remove(view.Feature);
                view.EntryButton.gameObject.SetActive(view.Supported || view.HasPendingReward);
            }
            if (opened && !opened.Busy && opened.ClaimMode != opened.HasPendingReward) opened.Show();
            if (!AutoOpenClaim || opened) return;
            var next = Features.Where(v => v && v.HasReferences && v.HasPendingReward && !v.Busy && !dismissed.Contains(v.Feature))
                .OrderBy(v => v.Feature).FirstOrDefault();
            if (next) Open(next);
        }

        public void Act(TikTokFeatureView view)
        {
            if (!initialized || !view || opened != view || view.Busy) return;
            // Check the ledger again at click time, never infer reward from a button press or share success alone.
            if (view.ClaimMode)
            {
                if (!view.HasValidRewardHandler())
                {
                    Debug.LogWarning("[TikTok UI] Connect OnRewardClaimed in Inspector before Claim.", view);
                    view.Show();
                    return;
                }
                view.Busy = true;
                view.ActionButton.interactable = false;
                // Capture the reward event independently of UI subscriptions: a successful asynchronous Claim
                // must still deliver its reward if the controller is disabled while the server responds.
                var reward = view.OnRewardClaimed;
                bool delivered = false;
                int token = generation;
                Action<bool> done = ok =>
                {
                    if (ok && !delivered)
                    {
                        delivered = true;
                        try { reward.Invoke(); } catch (Exception e) { Debug.LogException(e); }
                    }
                    if (!Alive(token) || !view) return;
                    view.Busy = false;
                    if (!ok && opened == view)
                    {
                        view.Show();
                        view.ShowResult(view.HasPendingReward ? "Reward could not be claimed. Please try again."
                            : "This reward is no longer available to claim.");
                        view.ActionButton.interactable = view.ClaimMode && view.HasValidRewardHandler();
                        return;
                    }
                    if (opened == view) ClosePopup();
                    PendingChanged();
                };
                switch (view.Feature)
                {
                    case TikTokUIFeature.Shortcut: TikTokShortcut.Claim(done); break;
                    case TikTokUIFeature.Sidebar: TikTokSidebar.Claim(done); break;
                    case TikTokUIFeature.Share: TikTokShare.Claim(done); break;
                }
                return;
            }
            if (!view.Supported) return;
            view.Busy = true;
            view.ActionButton.interactable = false;
            int version = generation;
            Action<bool> completed = ok =>
            {
                if (!Alive(version) || !view) return;
                view.Busy = false;
                if (view.HasPendingReward)
                {
                    dismissed.Remove(view.Feature);
                    if (opened == view) view.Show();
                }
                else if (opened == view)
                    view.ShowResult(!ok ? "This action could not be completed. Please try again."
                        : view.Feature == TikTokUIFeature.Shortcut ? "Open the game again from your home-screen shortcut to check your reward."
                        : view.Feature == TikTokUIFeature.Sidebar ? "Open the game again from your TikTok Profile sidebar to check your reward."
                        : "Shared successfully.");
                PendingChanged();
            };
            switch (view.Feature)
            {
                case TikTokUIFeature.Shortcut: TikTokShortcut.Add(completed); break;
                case TikTokUIFeature.Sidebar: TikTokSidebar.Start(completed); break;
                case TikTokUIFeature.Share: TikTokShare.Share(view.ShareQuery, completed, title: string.IsNullOrWhiteSpace(view.ShareTitle) ? null : view.ShareTitle, imageUrl: string.IsNullOrWhiteSpace(view.ShareImageUrl) ? null : view.ShareImageUrl); break;
            }
        }
    }
}
