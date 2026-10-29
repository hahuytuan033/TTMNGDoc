using System.Collections.Generic;
using AmobearTTMNG;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TikTokMiniGameDemo
{
    /// <summary>
    /// Scene Demo — Desktop Shortcut / Sidebar Revisit / Share, nhận quà HAI BƯỚC (luồng của pocket-2).
    ///
    /// Shortcut:
    ///   Add Shortcut → popup → Start → <see cref="TikTokShortcut.Add"/> (addShortcut) → nhắc mở lại từ icon.
    ///   Phiên sau mở từ icon: module tự gọi getShortcutMissionReward → canReceiveReward=true → lưu quyền nhận quà
    ///   → <see cref="TikTokShortcut.OnPendingRewardChanged"/> → popup đổi sang "Reward" + nút Claim.
    ///   Claim → <see cref="TikTokShortcut.Claim"/> → <see cref="TikTokShortcut.OnRewardClaimed"/>: demo cộng coin.
    ///
    /// Sidebar Revisit (nút Revisit) — luồng riêng theo doc Sidebar, cùng kiểu hai bước:
    ///   Revisit → popup → Start → <see cref="TikTokSidebar.Start"/> (startEntranceMission) → nhắc vào lại từ sidebar.
    ///   Phiên vào lại: getEntranceMissionReward → quyền nhận quà → popup Claim.
    ///
    /// Share (nút Share) — theo JS SDK Reference mục 6:
    ///   Share → popup → Share → <see cref="TikTokShare.Share"/> (shareAppMessage kèm query). Thành công → quà share
    ///   mỗi ngày một lần → popup Claim. Bạn bè mở game từ tin nhắn → đọc lại query.
    ///
    /// Phần thưởng (<see cref="RewardCoins"/> coin) là của DEMO — game thật tự quyết trong handler OnRewardClaimed.
    /// Các tham chiếu được gán sẵn trong scene Demo.unity.
    /// </summary>
    public class ShortcutDemo : MonoBehaviour
    {
        private const int RewardCoins = 100;
        private const string CoinsKey = "demo_shortcut_coins";

        public Button AddShortcutButton;
        public GameObject Popup;
        public TMP_Text PopupText;
        public TMP_Text PopupDescription;
        public Button StartButton;
        /// <summary>Nền tối của popup — chạm ra ngoài panel để đóng.</summary>
        public Button PopupBackground;
        /// <summary>Nút X ở góc phải trên của panel.</summary>
        public Button PopupCloseButton;
        public TMP_Text StatusText;
        public Button ApiDemoButton;

        public Button RevisitButton;
        public GameObject RevisitPopup;
        public TMP_Text RevisitPopupText;
        public TMP_Text RevisitPopupDescription;
        public Button RevisitStartButton;
        public Button RevisitPopupBackground;
        public Button RevisitPopupCloseButton;

        public Button ShareButton;
        public GameObject SharePopup;
        public TMP_Text SharePopupText;
        public TMP_Text SharePopupDescription;
        /// <summary>Nút "Share" trong popup — bấm mới gọi shareAppMessage (hoặc Claim khi có quà share).</summary>
        public Button ShareStartButton;
        public Button SharePopupBackground;
        public Button SharePopupCloseButton;

        [Header("Editor test")]
        [Tooltip("Chỉ Editor: giả lập phiên được mở từ icon màn hình chính (như launch mode scene_shortcut). Tích khi đang Play = vừa mở lại game từ icon.")]
        public bool SimulateShortcutLaunchInEditor;
        [Tooltip("Chỉ Editor: giả lập phiên được mở từ sidebar trang cá nhân TikTok (như launch mode scene_center). Tích khi đang Play = vừa vào lại game từ sidebar.")]
        public bool SimulateSidebarLaunchInEditor;
        [Tooltip("Chỉ Editor: query giả lập như khi game được mở từ tin nhắn share (vd from=share&level=1). Để trống = mở bình thường.")]
        public string SimulateShareLaunchQueryInEditor;

        // Popup đang ở chế độ Claim (nút chính = Claim) hay chế độ bắt đầu luồng (Start / Share).
        private bool _shortcutClaimMode;
        private bool _sidebarClaimMode;
        private bool _shareClaimMode;

        private void Awake()
        {
#if UNITY_EDITOR
            TikTokShortcut.EditorSimulateShortcutLaunch = SimulateShortcutLaunchInEditor;
            TikTokSidebar.EditorSimulateSidebarLaunch = SimulateSidebarLaunchInEditor;
            TikTokShare.EditorSimulateLaunchQuery = SimulateShareLaunchQueryInEditor;
#endif
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying || StatusText == null)
                return;
            if (TikTokShortcut.EditorSimulateShortcutLaunch != SimulateShortcutLaunchInEditor)
            {
                TikTokShortcut.EditorSimulateShortcutLaunch = SimulateShortcutLaunchInEditor;
                if (SimulateShortcutLaunchInEditor)
                    TikTokShortcut.RefreshReward();
            }
            if (TikTokSidebar.EditorSimulateSidebarLaunch != SimulateSidebarLaunchInEditor)
            {
                TikTokSidebar.EditorSimulateSidebarLaunch = SimulateSidebarLaunchInEditor;
                if (SimulateSidebarLaunchInEditor)
                    TikTokSidebar.RefreshReward();
            }
        }

        [ContextMenu("Reset Shortcut Test Data")]
        private void ResetShortcutTestData()
        {
            TikTokShortcut.ResetLocalLedger();
            TikTokSidebar.ResetLocalLedger();
            TikTokShare.ResetLocalLedger();
            TikTokStorage.DeleteKey(CoinsKey);
            TikTokStorage.Save();
            Debug.Log("[ShortcutDemo] Test data reset: ledgers cleared, coins = 0");
            if (Application.isPlaying)
                StatusText.text = "Coins: 0";
        }
#endif

        private void Start()
        {
            Popup.SetActive(false);
            RevisitPopup.SetActive(false);
            AddShortcutButton.gameObject.SetActive(false);
            RevisitButton.gameObject.SetActive(false);
            SharePopup.SetActive(false);
            ShareButton.gameObject.SetActive(false);
            StatusText.text = $"Coins: {Coins}";

            AddShortcutButton.onClick.AddListener(OpenPopup);
            StartButton.onClick.AddListener(OnStartClicked);
            PopupBackground.onClick.AddListener(() => Popup.SetActive(false));
            PopupCloseButton.onClick.AddListener(() => Popup.SetActive(false));
            RevisitButton.onClick.AddListener(OpenRevisitPopup);
            RevisitStartButton.onClick.AddListener(OnRevisitStartClicked);
            RevisitPopupBackground.onClick.AddListener(() => RevisitPopup.SetActive(false));
            RevisitPopupCloseButton.onClick.AddListener(() => RevisitPopup.SetActive(false));
            ShareButton.onClick.AddListener(OpenSharePopup);
            ShareStartButton.onClick.AddListener(OnShareClicked);
            SharePopupBackground.onClick.AddListener(() => SharePopup.SetActive(false));
            SharePopupCloseButton.onClick.AddListener(() => SharePopup.SetActive(false));
            ApiDemoButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));

            // Bước 2 doc: không hỗ trợ thì ẩn lối vào, không gọi addShortcut.
            TikTokShortcut.CheckSupported(supported =>
            {
                if (this != null)
                    AddShortcutButton.gameObject.SetActive(supported);
            });

            // Doc Sidebar bước 3: canIUse cả hai API, không hỗ trợ thì ẩn lối vào.
            TikTokSidebar.CheckSupported(supported =>
            {
                if (this != null)
                    RevisitButton.gameObject.SetActive(supported);
            });

            // Share: canIUse("shareAppMessage"), không hỗ trợ thì ẩn nút.
            TikTokShare.CheckSupported(supported =>
            {
                if (this != null)
                    ShareButton.gameObject.SetActive(supported);
            });

            // Bạn bè mở game từ tin nhắn share: query có ngay lúc mở (getLaunchOptionsSync) hoặc khi onShow.
            HandleShareQuery(TikTokShare.GetLaunchQuery());
            TikTokShare.OnShareQuery += HandleShareQuery;

            // Phần thưởng do GAME quyết: template chỉ báo "đã Claim".
            TikTokShortcut.OnRewardClaimed += OnShortcutClaimed;
            TikTokSidebar.OnRewardClaimed += OnSidebarClaimed;
            TikTokShare.OnRewardClaimed += OnShareClaimed;

            // Nền tảng xác nhận đủ điều kiện (lúc mở game / OnShow) → hiện popup Claim.
            TikTokShortcut.OnPendingRewardChanged += ShowPendingClaim;
            TikTokSidebar.OnPendingRewardChanged += ShowPendingClaim;
            TikTokShare.OnPendingRewardChanged += ShowPendingClaim;

            // Module tự kiểm tra khi InitSDK xong và mỗi OnShow. Gọi thêm ở đây để chạy cả khi Play thẳng scene Demo
            // (không qua Setup); lần gọi trùng tự bị bỏ qua.
            TikTokShortcut.RefreshReward();
            TikTokSidebar.RefreshReward();
            ShowPendingClaim();
        }

        private void OnDestroy()
        {
            TikTokShare.OnShareQuery -= HandleShareQuery;
            TikTokShortcut.OnRewardClaimed -= OnShortcutClaimed;
            TikTokSidebar.OnRewardClaimed -= OnSidebarClaimed;
            TikTokShare.OnRewardClaimed -= OnShareClaimed;
            TikTokShortcut.OnPendingRewardChanged -= ShowPendingClaim;
            TikTokSidebar.OnPendingRewardChanged -= ShowPendingClaim;
            TikTokShare.OnPendingRewardChanged -= ShowPendingClaim;
        }

        private static int Coins => TikTokStorage.GetInt(CoinsKey, 0);

        private void AddCoins(string message)
        {
            TikTokStorage.SetInt(CoinsKey, Coins + RewardCoins);
            TikTokStorage.Save();
            TikTokVibrate.Long();
            StatusText.text = $"{message} +{RewardCoins} coins\nCoins: {Coins}";
        }

        // Ưu tiên shortcut → sidebar → share; chỉ mở khi chưa có popup nào đang mở.
        private void ShowPendingClaim()
        {
            if (this == null || Popup.activeSelf || RevisitPopup.activeSelf || SharePopup.activeSelf)
                return;
            if (TikTokShortcut.HasPendingReward)
                OpenPopup();
            else if (TikTokSidebar.HasPendingReward)
                OpenRevisitPopup();
            else if (TikTokShare.HasPendingReward)
                OpenSharePopup();
        }

        private static void SetButtonLabel(Button button, string label)
        {
            var text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
                text.text = label;
        }

        // ------------------------------------------------------------ Shortcut

        private void OpenPopup()
        {
            _shortcutClaimMode = TikTokShortcut.HasPendingReward;
            if (_shortcutClaimMode)
            {
                PopupText.text = "Reward";
                PopupDescription.text = $"Thanks for opening the game from your home screen!\nClaim {RewardCoins} coins.";
                SetButtonLabel(StartButton, "Claim");
            }
            else
            {
                // Bước 3 doc: lời dẫn phải nói rõ hành động và điều kiện nhận thưởng.
                PopupText.text = "Add Shortcut";
                PopupDescription.text = TikTokShortcut.HasClaimedThisPeriod
                    ? "You already got the shortcut reward."
                    : $"Add the game to your home screen.\nOpen it from the icon next time to get {RewardCoins} coins!";
                SetButtonLabel(StartButton, "Start");
            }
            StartButton.interactable = true;
            Popup.SetActive(true);
        }

        private void OnStartClicked()
        {
            StartButton.interactable = false;
            if (_shortcutClaimMode)
            {
                // Đóng trước: Claim bắn OnPendingRewardChanged đồng bộ, popup quà kế tiếp (nếu có) cần chỗ để mở.
                Popup.SetActive(false);
                TikTokShortcut.Claim(ok =>
                {
                    if (this != null && !ok)
                        StatusText.text = "Reward already claimed.";
                });
                return;
            }

            // Diagram: addShortcut → success/fail → "Ask user to re-open from the icon". Không phát thưởng.
            TikTokShortcut.Add(ok =>
            {
                if (this == null)
                    return;
                Popup.SetActive(false);
                StatusText.text = ok
                    ? $"Shortcut added! Close the game and open it from the home-screen icon to get {RewardCoins} coins."
                    : "Could not add the shortcut on this device.";
            });
        }

        private void OnShortcutClaimed(string period)
        {
            if (this != null)
                AddCoins("Welcome back from the home screen!");
        }

        // ------------------------------------------------------------ Sidebar Revisit

        private void OpenRevisitPopup()
        {
            _sidebarClaimMode = TikTokSidebar.HasPendingReward;
            if (_sidebarClaimMode)
            {
                RevisitPopupText.text = "Reward";
                RevisitPopupDescription.text = $"Welcome back from the TikTok sidebar!\nClaim {RewardCoins} coins.";
                SetButtonLabel(RevisitStartButton, "Claim");
            }
            else
            {
                // Doc Sidebar bước 2: lời dẫn phải nói rõ phải vào lại từ sidebar trang cá nhân mới nhận được.
                RevisitPopupText.text = "Revisit";
                if (TikTokSidebar.HasClaimedThisPeriod)
                    RevisitPopupDescription.text = "You already got today's sidebar reward.\nCome back tomorrow!";
                else if (TikTokSidebar.IsWaitingForRevisit)
                    RevisitPopupDescription.text = $"Not completed yet.\nOpen the game from your TikTok Profile sidebar to get {RewardCoins} coins.";
                else
                    RevisitPopupDescription.text = $"Go to your TikTok Profile sidebar,\nthen open the game from there to get {RewardCoins} coins!";
                SetButtonLabel(RevisitStartButton, "Start");
            }
            RevisitStartButton.interactable = true;
            RevisitPopup.SetActive(true);
        }

        private void OnRevisitStartClicked()
        {
            RevisitStartButton.interactable = false;
            if (_sidebarClaimMode)
            {
                RevisitPopup.SetActive(false);
                TikTokSidebar.Claim(ok =>
                {
                    if (this != null && !ok)
                        StatusText.text = "Reward already claimed.";
                });
                return;
            }

            // Diagram: startEntranceMission → Guide user to Profile sidebar entry. Không phát thưởng.
            TikTokSidebar.Start(ok =>
            {
                if (this == null)
                    return;
                RevisitPopup.SetActive(false);
                StatusText.text = ok
                    ? $"Now open the game from your TikTok Profile sidebar to get {RewardCoins} coins."
                    : "Sidebar revisit is not available on this device.";
            });
        }

        private void OnSidebarClaimed(string period)
        {
            if (this != null)
                AddCoins("Welcome back from the TikTok sidebar!");
        }

        // ------------------------------------------------------------ Share

        private void OpenSharePopup()
        {
            _shareClaimMode = TikTokShare.HasPendingReward;
            if (_shareClaimMode)
            {
                SharePopupText.text = "Reward";
                SharePopupDescription.text = $"Thanks for sharing!\nClaim {RewardCoins} coins (once a day).";
                SetButtonLabel(ShareStartButton, "Claim");
            }
            else
            {
                SharePopupText.text = "Share";
                SharePopupDescription.text = "Send the game to your TikTok friends\nand invite them to play!";
                SetButtonLabel(ShareStartButton, "Share");
            }
            ShareStartButton.interactable = true;
            SharePopup.SetActive(true);
        }

        private void OnShareClicked()
        {
            ShareStartButton.interactable = false;
            if (_shareClaimMode)
            {
                SharePopup.SetActive(false);
                TikTokShare.Claim(ok =>
                {
                    if (this != null && !ok)
                        StatusText.text = "Reward already claimed.";
                });
                return;
            }

            // query để bạn bè mở game từ tin nhắn này biết nguồn vào. title / imageUrl để null = theo console.
            string query = TikTokShare.ToQueryString(new Dictionary<string, string>
            {
                { "from", "share" },
                { "coins", Coins.ToString() },
            });
            TikTokShare.Share(query, ok =>
            {
                if (this == null)
                    return;
                SharePopup.SetActive(false);
                StatusText.text = ok
                    ? "Shared! Friends who open your message will join the game."
                    : "Could not share on this device.";
                // Thành công lần đầu trong ngày → OnPendingRewardChanged đã bắn lúc popup còn mở; mở lại popup Claim.
                ShowPendingClaim();
            });
        }

        private void OnShareClaimed(string period)
        {
            if (this != null)
                AddCoins("Thanks for sharing!");
        }

        private void HandleShareQuery(Dictionary<string, string> query)
        {
            if (this == null || query == null || !query.TryGetValue("from", out string from) || from != "share")
                return;
            query.TryGetValue("coins", out string coins);
            StatusText.text = $"You joined from a friend's share!\nYour friend has {coins ?? "?"} coins.";
        }
    }
}
