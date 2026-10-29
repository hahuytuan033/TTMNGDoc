using System;
using System.Collections.Generic;
using TTSDK;
using TTSDK.UNBridgeLib.LitJson;
using UnityEngine;
using UnityEngine.UI;

namespace TikTokMiniGameDemo
{
    /// <summary>
    /// MonoBehaviour đầu vào của Demo. Xây dựng một danh sách dạng accordion cho các nút API TT.*
    /// khi chạy, liên kết từng nút với một trình xử lý và ghi kết quả vào bảng log.
    /// Bản thân UI được tạo bởi <see cref="EditorTools.DemoSceneBuilder"/>.
    /// </summary>
    public class DemoMainMenu : MonoBehaviour
    {
        // -----------------------------------------------------------------
        // Các tham chiếu Inspector — được kết nối tự động bởi DemoSceneBuilder khi nó xây dựng lại scene.
        // -----------------------------------------------------------------

        /// <summary>Container chứa ButtonTemplate + tất cả các nút danh mục/nút con được tạo ra.</summary>
        public RectTransform ButtonContainer;
        /// <summary>Văn bản tiêu đề ở trên cùng.</summary>
        public Text TitleText;
        /// <summary>Nội dung văn bản của bảng log (bộ đệm cuộn).</summary>
        public Text LogText;
        /// <summary>ScrollRect bọc <see cref="LogText"/>; chúng ta ghim nó ở dưới cùng mỗi khi thêm log mới.</summary>
        public ScrollRect LogScroll;
        /// <summary>Xóa bộ đệm log.</summary>
        public Button ClearLogButton;
        /// <summary>Nút "Init SDK" lớn ở trên cùng — phải được nhấn đầu tiên.</summary>
        public Button InitSdkButton;
        /// <summary>Văn bản trạng thái nhỏ dưới tiêu đề hiển thị trạng thái đã khởi tạo / chưa khởi tạo.</summary>
        public Text StatusText;

        /// <summary>
        /// Phiên bản package SDK được ghi nhận bởi <see cref="EditorTools.DemoSceneBuilder"/> khi
        /// xây dựng lại scene bằng cách đọc <c>Assets/Plugins/com.tiktok.minigame/package.json</c>.
        /// </summary>
        public string PackageVersion = "?";

        // -----------------------------------------------------------------
        // Trạng thái runtime.
        // -----------------------------------------------------------------

        private readonly List<string> _logBuffer = new List<string>();
        private const int MaxLogLines = 200;
        private bool _sdkInited;

        // Lưu trữ các listener handles để chúng ta có thể Off (hủy đăng ký) sau này. Nếu không có những biến này,
        // các API Off* không thể tìm thấy callback tương ứng để xóa bỏ (TT đăng ký qua so sánh delegate).
        private OnNetworkStatusChangeCallback _networkStatusCb;
        private OnNetworkWeakChangeCallback _networkWeakCb;
        private OnFeedStatusChangeCallback _feedStatusCb;

        // =================================================================
        // Vòng đời / Lifecycle
        // =================================================================

        private void Start()
        {
            // Tiêu đề hiển thị phiên bản *package* SDK (file unitypackage mà bạn import),
            // được ghi sẵn bởi DemoSceneBuilder. Chúng ta ưu tiên dùng nó thay vì TT.TTSDKVersion vì
            // TT.TTSDKVersion theo dõi bản revision nội bộ của mã nguồn SDK (ví dụ 6.3.9),
            // trong khi các nhà phát triển (CP) thường ghi nhớ theo phiên bản package đã phát hành (ví dụ 1.1.1).
            TitleText.text = $"TikTok Mini Game SDK Demo  ·  v{PackageVersion}";
            // Nếu đi qua scene Setup thì SDK đã được TikTokBootstrap init sẵn.
            _sdkInited = AmobearTTMNG.TikTokBootstrap.IsReady;
            UpdateStatus();

            ClearLogButton.onClick.AddListener(ClearLog);
            InitSdkButton.onClick.AddListener(InitSdk);

            BuildFeatureButtons();
            Log($"Platform: {Application.platform}");
            Log("Press [Init SDK] to start. Tap a category to expand its TT.* API buttons.");
        }

        // =================================================================
        // Xây dựng UI
        // =================================================================

        /// <summary>
        /// Định nghĩa toàn bộ cây danh mục/API. Để thêm một API TT.* mới, chỉ cần thêm một dòng.
        /// </summary>
        private void BuildFeatureButtons()
        {
            AddCategory("System",
                ("System Info", DoGetSystemInfo),
                ("Container Version", DoContainerVersion),
                ("Launch Options", DoLaunchOptions),
                ("Clean File Cache", DoCleanFileCache));

            AddCategory("Lifecycle",
                ("Register App Show/Hide", DoRegisterAppLifecycle),
                ("Set Before-Exit Listener", DoSetBeforeExit));

            AddCategory("Share",
                ("Share App Message", DoShare));

            AddCategory("Mission",
                ("Start Entrance Mission", DoStartEntranceMission),
                ("Get Entrance Mission Reward", DoGetEntranceMissionReward));

            AddCategory("Ads",
                ("Rewarded Video Ad", DoRewardedAd),
                ("Interstitial Ad", DoInterstitialAd));

            AddCategory("Network",
                ("Get Network Type", DoGetNetworkType),
                ("On NetworkStatus Change", DoOnNetworkStatusChange),
                ("Off NetworkStatus Change", DoOffNetworkStatusChange),
                ("On NetworkWeak Change", DoOnNetworkWeakChange),
                ("Off NetworkWeak Change", DoOffNetworkWeakChange));

            AddCategory("Device",
                ("Vibrate Short", DoVibrateShort),
                ("Vibrate Long", DoVibrateLong),
                ("Set FPS 30", () => SetFps(30)),
                ("Set FPS 60", () => SetFps(60)));

            AddCategory("Keyboard",
                ("Show Keyboard", DoShowKeyboard),
                ("Hide Keyboard", DoHideKeyboard));

            AddCategory("Shortcut",
                ("Can I Use Shortcut", DoCanIUseShortcut),
                ("Add Shortcut", DoAddShortcut),
                ("Get Shortcut Mission Reward", DoGetShortcutMissionReward),
                ("Refresh Shortcut Reward", DoRefreshShortcutReward),
                ("Claim Shortcut Reward", DoClaimShortcutReward),
                ("Reset Local Shortcut Ledger", DoResetShortcutLedger));

            AddCategory("Template Modules",
                ("Storage Probe", DoStorageProbe),
                ("File Write + Read", DoFileWriteRead),
                ("Safe Area", DoSafeArea),
                ("Network (module)", DoNetworkModule),
                ("Vibrate Short (module)", AmobearTTMNG.TikTokVibrate.Short),
                ("Vibrate Long (module)", AmobearTTMNG.TikTokVibrate.Long));

            AddCategory("Storage",
                ("PlayerPrefs ++counter", DoPlayerPrefs),
                ("Save<DemoSaving>", DoSaveTyped),
                ("Load<DemoSaving>", DoLoadTyped),
                ("Delete<DemoSaving>", DoDeleteTyped),
                ("Clear All Savings", DoClearAllSavings),
                ("Saving Disk Size", DoSavingDiskSize));

            AddCategory("Feed",
                ("On Feed Status Change", DoOnFeedStatusChange),
                ("Off Feed Status Change", DoOffFeedStatusChange));

            AddCategory("Analytics",
                ("Report Event", DoReportEvent),
                ("Report Scene", DoReportScene));
        }

        /// <summary>
        /// Thêm một header danh mục + một danh sách các nút con được thu gọn. Bấm vào header để mở rộng/thu gọn.
        /// </summary>
        private void AddCategory(string title, params (string label, Action handler)[] items)
        {
            var subButtons = new List<GameObject>();
            bool expanded = false;
            string baseLabel = title;

            // Nút Header sử dụng màu xám (slate) đặc trưng để tạo độ tương phản với các nút con màu xanh.
            var headerBtn = SpawnButton();
            headerBtn.name = $"Header_{title}";
            var headerText = headerBtn.GetComponentInChildren<Text>();
            headerText.text = $"▶  {baseLabel}";
            headerText.fontStyle = FontStyle.Bold;
            var hcolors = headerBtn.colors;
            hcolors.normalColor = new Color(0.28f, 0.32f, 0.40f);
            hcolors.highlightedColor = new Color(0.36f, 0.40f, 0.48f);
            hcolors.pressedColor = new Color(0.22f, 0.26f, 0.32f);
            hcolors.selectedColor = new Color(0.28f, 0.32f, 0.40f);
            headerBtn.colors = hcolors;
            headerBtn.onClick.AddListener(() =>
            {
                expanded = !expanded;
                headerText.text = (expanded ? "▼  " : "▶  ") + baseLabel;
                foreach (var go in subButtons) go.SetActive(expanded);
                // ContentSizeFitter + VerticalLayoutGroup sẽ tính toán lại layout ở frame tiếp theo.
                // ForceUpdateCanvases buộc cập nhật lại chiều cao của cuộn ngay lập tức để giữ thanh cuộn ở đúng vị trí mà không bị giật.
                Canvas.ForceUpdateCanvases();
            });

            foreach (var item in items)
            {
                var btn = SpawnButton();
                btn.name = $"Btn_{item.label}";
                btn.GetComponentInChildren<Text>().text = $"   {item.label}"; // thụt lề để thể hiện phân cấp giao diện
                var label = item.label;
                var handler = item.handler;
                btn.onClick.AddListener(() => SafeInvoke(label, handler));
                btn.gameObject.SetActive(false); // mặc định là thu gọn
                subButtons.Add(btn.gameObject);
            }
        }

        /// <summary>
        /// Tạo một bản sao của ButtonTemplate (luôn là child 0 của <see cref="ButtonContainer"/>) và
        /// trả về instance Button mới được tạo.
        /// </summary>
        private Button SpawnButton()
        {
            var prefab = ButtonContainer.GetChild(0).gameObject;
            var go = Instantiate(prefab, ButtonContainer);
            go.SetActive(true);
            return go.GetComponent<Button>();
        }

        /// <summary>
        /// Chạy một trình xử lý API được bọc trong try/catch để đảm bảo nếu một lời gọi SDK gặp lỗi
        /// cũng không làm hỏng menu. Lưu ý: TT.InitSDK ném lỗi bất đồng bộ qua SynchronizationContext,
        /// vì vậy try/catch ở đây KHÔNG bắt được ngoại lệ của InitSDK — việc đó được xử lý riêng.
        /// </summary>
        private void SafeInvoke(string label, Action action)
        {
            Log($"▶ {label}");
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                Log($"  ✗ Exception: {e.Message}");
            }
        }

        // =================================================================
        // Khởi tạo SDK
        // =================================================================

        /// <summary>
        /// Gọi <c>TT.InitSDK</c>. Phải thành công trước khi có thể gọi bất kỳ API nào phụ thuộc vào
        /// container (Ads / Share / Shortcut). Trong Unity Editor (với SDK >= 1.1.1, DLL dành cho editor
        /// sẽ tự động tải và gọi thành công qua bản giả lập Mock); trong bản build WebGL thì nó sẽ giao tiếp với container thật.
        /// </summary>
        private void InitSdk()
        {
            if (_sdkInited)
            {
                Log("SDK already inited.");
                return;
            }
            Log("▶ Init SDK");
            try
            {
                TT.InitSDK((code, env) =>
                {
                    // code == 0 là thành công; các giá trị khác 0 là lỗi do SDK trả về.
                    _sdkInited = (code == 0);
                    Log($"  ← InitSDK code={code} containerEnv={(env != null ? "ok" : "null")}");
                    UpdateStatus();
                });
            }
            catch (Exception e)
            {
                // Với SDK < 1.1.1 InitSDK sẽ ném lỗi đồng bộ trong Editor; chúng ta vẫn sẽ
                // bắt lỗi đó và hiển thị cho người dùng.
                Log($"  ✗ InitSDK threw: {e.Message}");
                Log("  Tip: TT.InitSDK requires the editor-flavoured dll (SDK ≥ 1.1.1) or a real WebGL container.");
                UpdateStatus();
            }
        }

        private void UpdateStatus()
        {
            StatusText.text = _sdkInited
                ? "<color=#7CFC00>SDK inited</color>"
                : "<color=#FFAA33>SDK not inited</color>";
        }

        // =================================================================
        // Trình xử lý API — mỗi phân vùng tương ứng 1:1 với một danh mục trong BuildFeatureButtons.
        // =================================================================

        #region System / Container

        /// <summary>
        /// <c>TT.GetSystemInfo</c> — thông tin thiết bị / host / ngôn ngữ. Hỗ trợ gọi trước InitSDK ở phiên bản v6.5+.
        /// </summary>
        private void DoGetSystemInfo()
        {
            var info = TT.GetSystemInfo();
            if (info == null)
            {
                Log("  ← GetSystemInfo returned null");
                return;
            }
            Log($"  brand={info.brand} model={info.model} platform={info.platform}");
            Log($"  system={info.system} lang={info.language} sdkVersion={info.sdkVersion}");
            Log($"  screen={info.screenWidth}x{info.screenHeight} hostName={info.hostName} hostVersion={info.hostVersion}");
        }

        /// <summary>
        /// <c>TT.GetContainerVersion</c> + hằng số phiên bản. Thường dùng để kiểm tra tính tương thích.
        /// </summary>
        private void DoContainerVersion()
        {
            Log($"  ← ContainerVersion={TT.GetContainerVersion()} InContainerEnv={TT.InContainerEnv}");
            Log($"  TTSDKVersion={TT.TTSDKVersion} GameVersion={TT.GameVersion} GamePublishVersion={TT.GamePublishVersion}");
        }

        /// <summary>
        /// <c>TT.GetLaunchOptionsSync</c> — các tham số (scene, path, query) được truyền cho game khi khởi chạy.
        /// </summary>
        private void DoLaunchOptions()
        {
            var opts = TT.GetLaunchOptionsSync();
            if (opts == null) { Log("  ← LaunchOptions null"); return; }
            int queryCount = opts.Query == null ? 0 : opts.Query.Count;
            Log($"  ← scene={opts.Scene} path={opts.Path} query.count={queryCount}");
        }

        /// <summary>
        /// <c>TT.CleanAllFileCache</c> — dọn dẹp thư mục cache do SDK quản lý.
        /// </summary>
        private void DoCleanFileCache()
        {
            TT.CleanAllFileCache(ok => Log($"  ← CleanAllFileCache ok={ok}"));
        }

        #endregion

        #region Lifecycle

        private bool _lifecycleRegistered;
        /// <summary>
        /// Đăng ký theo dõi <c>TT.GetAppLifeCycle().OnShow / OnHide</c> để nhận sự kiện chạy nền/hiển thị lại.
        /// </summary>
        private void DoRegisterAppLifecycle()
        {
            var lc = TT.GetAppLifeCycle();
            if (lc == null) { Log("  ← GetAppLifeCycle returned null"); return; }
            if (_lifecycleRegistered) { Log("  Lifecycle handlers already registered."); return; }
            lc.OnShow += dict => Log($"  ← App OnShow, paramCount={(dict == null ? 0 : dict.Count)}");
            lc.OnHide += () => Log("  ← App OnHide");
            _lifecycleRegistered = true;
            Log("  ← Registered OnShow / OnHide");
        }

        private bool _beforeExitSet;
        /// <summary>
        /// Chặn thao tác thoát của người dùng. Trả về <c>true</c> để ngăn chặn hành động thoát mặc định
        /// (bạn sẽ phải tự gọi logic thoát của riêng mình), hoặc <c>false</c> để cho phép container đóng game.
        /// </summary>
        private void DoSetBeforeExit()
        {
            if (_beforeExitSet) { Log("  before-exit already set"); return; }
            var lc = TT.GetAppLifeCycle();
            if (lc == null) { Log("  ← GetAppLifeCycle returned null"); return; }
            lc.SetOnBeforeExitAppListener(() =>
            {
                Log("  ← BeforeExitApp invoked (returning false → default exit)");
                return false;
            });
            _beforeExitSet = true;
            Log("  ← BeforeExitApp listener set");
        }

        #endregion

        #region Share

        /// <summary>
        /// <c>TT.ShareAppMessage</c> — mở bảng chia sẻ của container. <c>TemplateType=1</c>
        /// là dạng template "thẻ" tiêu chuẩn; <c>Path</c>/<c>Query</c> là link deep-link trỏ ngược về game.
        /// </summary>
        private void DoShare()
        {
            TT.ShareAppMessage(new ShareAppMessageParam
            {
                Title = "TikTok Mini Game Demo",
                Subtitle = "Try the SDK in your browser",
                ImageUrl = "",
                TemplateType = 1,
                Path = "demo/main",
                Query = "src=demo",
                Success = () => Log("  ← Share success"),
                Fail = err => Log($"  ← Share fail: {err?.ErrMsg}"),
                Complete = () => Log("  ← Share complete"),
            });
        }

        #endregion

        #region Mission

        /// <summary>
        /// <c>TT.StartEntranceMission</c> — khởi chạy quy trình nhiệm vụ "lần đầu tiên truy cập"
        /// (ví dụ: tặng thưởng 1 lần cho người chơi truy cập từ một kênh được chỉ định).
        /// </summary>
        private void DoStartEntranceMission()
        {
            TT.StartEntranceMission(new TTStartEntranceMissionParam
            {
                success = r => Log($"  ← StartEntranceMission is_sufficient={r?.is_sufficient}"),
                fail = err => Log($"  ← StartEntranceMission fail: {err?.ErrMsg}"),
                complete = r => Log("  ← StartEntranceMission complete"),
            });
        }

        /// <summary>
        /// <c>TT.GetEntranceMissionReward</c> — kiểm tra / nhận phần thưởng cho nhiệm vụ đang kích hoạt.
        /// </summary>
        private void DoGetEntranceMissionReward()
        {
            TT.GetEntranceMissionReward(new TTGetEntranceMissionRewardParam
            {
                success = r => Log($"  ← GetEntranceMissionReward canReceiveReward={r?.canReceiveReward}"),
                fail = err => Log($"  ← GetEntranceMissionReward fail: {err?.ErrMsg}"),
                complete = r => Log("  ← GetEntranceMissionReward complete"),
            });
        }

        #endregion

        #region Ads

        // Label khai báo trong Resources/TikTokAdsConfig (Placement ID copy từ TikTok Developer Portal).
        private const string DemoRewardedLabel = "rw_demo";
        private const string DemoInterstitialLabel = "it_demo";

        /// <summary>
        /// <see cref="AmobearTTMNG.Ads.TikTokAds.ShowVideoReward"/> → <c>TT.CreateRewardedVideoAd</c> theo Placement ID của
        /// label. Callback chỉ chạy khi xem HẾT; còn lại báo qua OnRewardUnavailable.
        /// </summary>
        private void DoRewardedAd()
        {
            void Unavailable(string label, string reason) => Log($"  ← Rewarded '{label}' unavailable: {reason}");
            AmobearTTMNG.Ads.TikTokAds.OnRewardUnavailable -= Unavailable;
            AmobearTTMNG.Ads.TikTokAds.OnRewardUnavailable += Unavailable;
            Log($"  → TikTokAds.ShowVideoReward('{DemoRewardedLabel}') configured={AmobearTTMNG.Ads.TikTokAds.IsConfigured(DemoRewardedLabel, AmobearTTMNG.Ads.TikTokAdType.Rewarded)}");
            AmobearTTMNG.Ads.TikTokAds.ShowVideoReward(() => Log("  ← Rewarded watched to the end → GRANT reward"), DemoRewardedLabel);
        }

        /// <summary>
        /// <see cref="AmobearTTMNG.Ads.TikTokAds.ShowInterstitial"/> → <c>TT.CreateInterstitialAd</c>. Callback luôn chạy;
        /// trong 15s đầu / chưa đủ 30s từ lần trước thì bị chặn (xem log lý do).
        /// </summary>
        private void DoInterstitialAd()
        {
            void Skipped(string label, string reason) => Log($"  ← Interstitial '{label}' skipped: {reason}");
            AmobearTTMNG.Ads.TikTokAds.OnInterstitialUnavailable -= Skipped;
            AmobearTTMNG.Ads.TikTokAds.OnInterstitialUnavailable += Skipped;
            Log($"  → TikTokAds.ShowInterstitial('{DemoInterstitialLabel}')");
            AmobearTTMNG.Ads.TikTokAds.ShowInterstitial(() => Log("  ← Interstitial flow continued"), DemoInterstitialLabel);
        }

        #endregion

        #region Network

        /// <summary>
        /// <c>TT.GetNetWorkType</c> — kiểm tra tình trạng kết nối mạng hiện tại một lần.
        /// </summary>
        private void DoGetNetworkType()
        {
            TT.GetNetWorkType(new GetNetworkTypeParam
            {
                Success = r => Log($"  ← GetNetWorkType: {r?.NetworkType}"),
                Fail = err => Log($"  ← GetNetWorkType fail: {err?.ErrMsg}"),
            });
        }

        /// <summary>
        /// <c>TT.OnNetworkStatusChange</c> — đăng ký nhận các sự kiện bật/tắt kết nối mạng.
        /// </summary>
        private void DoOnNetworkStatusChange()
        {
            if (_networkStatusCb != null) { Log("  status listener already on"); return; }
            _networkStatusCb = r => Log($"  ← NetworkStatus changed: IsConnected={r?.IsConnected} type={r?.NetworkType}");
            TT.OnNetworkStatusChange(_networkStatusCb);
            Log("  ← OnNetworkStatusChange registered");
        }

        /// <summary>Hủy đăng ký tương ứng với hàm On.</summary>
        private void DoOffNetworkStatusChange()
        {
            if (_networkStatusCb == null) { Log("  no status listener to remove"); return; }
            TT.OffNetworkStatusChange(_networkStatusCb);
            _networkStatusCb = null;
            Log("  ← OffNetworkStatusChange done");
        }

        /// <summary>
        /// <c>TT.OnNetworkWeakChange</c> — đăng ký nhận sự kiện "mạng yếu" (phát hiện giật lag do mạng).
        /// </summary>
        private void DoOnNetworkWeakChange()
        {
            if (_networkWeakCb != null) { Log("  weak listener already on"); return; }
            _networkWeakCb = r => Log($"  ← NetworkWeak changed: WeakNet={r?.WeakNet} type={r?.NetworkType}");
            TT.OnNetworkWeakChange(_networkWeakCb);
            Log("  ← OnNetworkWeakChange registered");
        }

        /// <summary>Hủy đăng ký tương ứng với hàm On.</summary>
        private void DoOffNetworkWeakChange()
        {
            if (_networkWeakCb == null) { Log("  no weak listener to remove"); return; }
            TT.OffNetworkWeakChange(_networkWeakCb);
            _networkWeakCb = null;
            Log("  ← OffNetworkWeakChange done");
        }

        #endregion

        #region Device

        /// <summary><c>TT.VibrateShort</c> — rung ngắn ~15ms.</summary>
        private void DoVibrateShort()
        {
            TT.VibrateShort(new VibrateShortParam
            {
                Success = _ => Log("  ← VibrateShort ok"),
                Fail = err => Log($"  ← VibrateShort fail: {err?.ErrMsg}"),
                Complete = () => { },
            });
        }

        /// <summary><c>TT.VibrateLong</c> — rung dài ~400ms.</summary>
        private void DoVibrateLong()
        {
            TT.VibrateLong(new VibrateLongParam
            {
                Success = _ => Log("  ← VibrateLong ok"),
                Fail = err => Log($"  ← VibrateLong fail: {err?.ErrMsg}"),
                Complete = () => { },
            });
        }

        /// <summary>
        /// <c>TT.SetPreferredFramesPerSecond</c> — gửi gợi ý FPS mục tiêu cho engine và container.
        /// </summary>
        private void SetFps(int fps)
        {
            TT.SetPreferredFramesPerSecond(fps);
            Log($"  ← target FPS = {fps}");
        }

        #endregion

        #region Keyboard

        /// <summary>
        /// <c>TT.ShowKeyboard</c> — hiển thị bàn phím ảo (chỉ hỗ trợ WebGL). Gắn các
        /// callback khi gõ phím / xác nhận / ẩn bàn phím qua <c>TT.GetTTKeyboard()</c> nếu bạn cần.
        /// </summary>
        private void DoShowKeyboard()
        {
            TT.ShowKeyboard(
                new TTKeyboard.ShowKeyboardOptions
                {
                    maxLength = 100,
                    multiple = false,
                    confirmHold = false,
                    defaultValue = "hello",
                    confirmType = "done",
                },
                () => Log("  ← ShowKeyboard ok"),
                err => Log($"  ← ShowKeyboard fail: {err}"));
        }

        /// <summary><c>TT.HideKeyboard</c> — ẩn bàn phím ảo.</summary>
        private void DoHideKeyboard()
        {
            TT.HideKeyboard(
                () => Log("  ← HideKeyboard ok"),
                err => Log($"  ← HideKeyboard fail: {err}"));
        }

        #endregion

        #region Shortcut

        /// <summary>
        /// <c>TT.AddShortcut</c> — yêu cầu người dùng thêm shortcut game vào màn hình chính.
        /// </summary>
        private void DoAddShortcut()
        {
            TT.AddShortcut(ok => Log($"  ← AddShortcut: {ok} (chưa phát thưởng — cần mở lại từ icon)"));
        }

        /// <summary>
        /// <c>CanIUse.AddShortcut</c> / <c>CanIUse.GetShortcutMissionReward</c> — client có hỗ trợ không.
        /// </summary>
        private void DoCanIUseShortcut()
        {
            Log($"  ← CanIUse AddShortcut={CanIUse.AddShortcut} GetShortcutMissionReward={CanIUse.GetShortcutMissionReward}");
        }

        /// <summary>
        /// <c>TT.GetShortcutMissionReward</c> — phiên hiện tại có được mở từ icon màn hình chính không.
        /// Gọi ngay sau AddShortcut trong cùng phiên luôn ra false; test bằng launch mode <c>scene_shortcut</c>.
        /// </summary>
        private void DoGetShortcutMissionReward()
        {
            TT.GetShortcutMissionReward(new GetShortcutMissionRewardParam
            {
                Success = r => Log($"  ← GetShortcutMissionReward canReceiveReward={r?.CanReceiveReward}"),
                Fail = err => Log($"  ← GetShortcutMissionReward fail: {err?.ErrorCode} {err?.ErrMsg}"),
                Complete = () => Log("  ← GetShortcutMissionReward complete"),
            });
        }

        /// <summary>
        /// <see cref="AmobearTTMNG.TikTokShortcut.RefreshReward"/> — hỏi lại nền tảng; đủ điều kiện thì lưu quyền nhận quà.
        /// </summary>
        private void DoRefreshShortcutReward()
        {
            AmobearTTMNG.TikTokShortcut.RefreshReward();
            Log($"  ← Refresh: pending={AmobearTTMNG.TikTokShortcut.HasPendingReward} waiting={AmobearTTMNG.TikTokShortcut.IsWaitingForRevisit} claimed={AmobearTTMNG.TikTokShortcut.HasClaimedThisPeriod}");
        }

        /// <summary>
        /// <see cref="AmobearTTMNG.TikTokShortcut.Claim"/> — bước 2: người chơi bấm Claim. Game cộng thưởng trong OnRewardClaimed.
        /// </summary>
        private void DoClaimShortcutReward()
        {
            AmobearTTMNG.TikTokShortcut.Claim(ok =>
                Log($"  ← Claim ok={ok} claimedThisPeriod={AmobearTTMNG.TikTokShortcut.HasClaimedThisPeriod}"));
        }

        private void DoResetShortcutLedger()
        {
            AmobearTTMNG.TikTokShortcut.ResetLocalLedger();
            Log("  ← Local shortcut ledger reset (trạng thái phía nền tảng không reset được)");
        }

        #endregion

        #region Template Modules

        private void DoStorageProbe()
        {
            const string k = "demo_storage_counter";
            int next = AmobearTTMNG.TikTokStorage.GetInt(k, 0) + 1;
            AmobearTTMNG.TikTokStorage.SetInt(k, next);
            AmobearTTMNG.TikTokStorage.Save();
            Log($"  ← TikTokStorage healthy={AmobearTTMNG.TikTokStorage.IsHealthy} fallback={AmobearTTMNG.TikTokStorage.IsFallback} container={AmobearTTMNG.TikTokStorage.IsContainerStorage} counter={next}");
        }

        private void DoFileWriteRead()
        {
            const string path = "demo/hello.txt";
            string content = $"hello {DateTime.UtcNow:O}";
            bool wrote = AmobearTTMNG.TikTokFileSystem.WriteText(path, content);
            string read = AmobearTTMNG.TikTokFileSystem.ReadText(path);
            Log($"  ← FileSystem available={AmobearTTMNG.TikTokFileSystem.IsAvailable} write={wrote} read='{read}' (Editor luôn false/null)");
        }

        private void DoSafeArea()
        {
            AmobearTTMNG.TikTokSafeArea.Refresh();
            Log($"  ← SafeArea resolved={AmobearTTMNG.TikTokSafeArea.IsResolved} top={AmobearTTMNG.TikTokSafeArea.NormalizedTop:F3} topCapsule={AmobearTTMNG.TikTokSafeArea.NormalizedTopWithCapsule:F3} bottom={AmobearTTMNG.TikTokSafeArea.NormalizedBottom:F3} | {AmobearTTMNG.TikTokSafeArea.Describe()}");
        }

        private void DoNetworkModule()
        {
            AmobearTTMNG.TikTokNetwork.Refresh();
            Log($"  ← {AmobearTTMNG.TikTokNetwork.Describe()}");
        }

        #endregion

        #region Storage

        /// <summary>
        /// Class POCO demo dùng để minh họa <c>TT.Save&lt;T&gt;</c>. Mọi class có tag [Serializable] đều dùng được.
        /// </summary>
        [Serializable]
        private class DemoSaving
        {
            public string Name = "demo";
            public int Counter;
            public long SavedAt;
        }

        /// <summary>
        /// <c>TT.PlayerPrefs</c> — hệ thống lưu trữ key/value đa nền tảng. Có API giống hệt
        /// <see cref="UnityEngine.PlayerPrefs"/>; trên WebGL thì SDK sẽ định tuyến nó lưu
        /// vào bộ nhớ lưu trữ bền vững của container.
        /// </summary>
        private void DoPlayerPrefs()
        {
            const string k = "demo_counter";
            int prev = TT.PlayerPrefs.GetInt(k, 0);
            int next = prev + 1;
            TT.PlayerPrefs.SetInt(k, next);
            TT.PlayerPrefs.Save();
            Log($"  ← PlayerPrefs[{k}] {prev} → {next}");
        }

        /// <summary>
        /// <c>TT.Save&lt;T&gt;</c> — serialize một object và lưu vào slot của SDK.
        /// Mỗi loại dữ liệu chiếm một slot trừ khi bạn chỉ định rõ <c>saveName</c>.
        /// </summary>
        private void DoSaveTyped()
        {
            var data = new DemoSaving
            {
                Counter = UnityEngine.Random.Range(1, 9999),
                SavedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            bool ok = TT.Save(data);
            Log($"  ← Save<DemoSaving> ok={ok} counter={data.Counter}");
        }

        /// <summary><c>TT.LoadSaving&lt;T&gt;</c> — đọc lại dữ liệu đã lưu từ Save.</summary>
        private void DoLoadTyped()
        {
            var data = TT.LoadSaving<DemoSaving>();
            if (data == null) { Log("  ← Load<DemoSaving> null"); return; }
            Log($"  ← Load<DemoSaving> name={data.Name} counter={data.Counter} savedAt={data.SavedAt}");
        }

        /// <summary><c>TT.DeleteSaving&lt;T&gt;</c> — xóa dữ liệu trong một slot.</summary>
        private void DoDeleteTyped()
        {
            TT.DeleteSaving<DemoSaving>();
            Log("  ← Delete<DemoSaving>");
        }

        /// <summary><c>TT.ClearAllSavings</c> — xóa sạch tất cả các slot lưu trữ.</summary>
        private void DoClearAllSavings()
        {
            TT.ClearAllSavings();
            Log("  ← ClearAllSavings");
        }

        /// <summary><c>TT.GetSavingDiskSize</c> — tổng số byte trên ổ đĩa được sử dụng bởi các dữ liệu lưu trữ.</summary>
        private void DoSavingDiskSize()
        {
            long size = TT.GetSavingDiskSize();
            Log($"  ← SavingDiskSize={size} bytes");
        }

        #endregion

        #region Feed

        /// <summary>
        /// <c>TT.OnFeedStatusChange</c> — đăng ký nhận các sự kiện vào/thoát trạng thái Feed (chơi trực tiếp).
        /// </summary>
        private void DoOnFeedStatusChange()
        {
            if (_feedStatusCb != null) { Log("  feed listener already on"); return; }
            _feedStatusCb = r => Log($"  ← FeedStatus changed: {r?.Type}");
            TT.OnFeedStatusChange(_feedStatusCb);
            Log("  ← OnFeedStatusChange registered");
        }

        /// <summary>Hủy đăng ký tương ứng với hàm On.</summary>
        private void DoOffFeedStatusChange()
        {
            if (_feedStatusCb == null) { Log("  no feed listener to remove"); return; }
            TT.OffFeedStatusChange(_feedStatusCb);
            _feedStatusCb = null;
            Log("  ← OffFeedStatusChange done");
        }

        #endregion

        #region Analytics

        /// <summary>
        /// <c>TT.ReportEvent</c> — sự kiện tùy chỉnh kèm tham số JSON bất kỳ.
        /// </summary>
        private void DoReportEvent()
        {
            TT.ReportEvent(new ReportEventParam
            {
                eventName = "demo_button_pressed",
                @params = new JsonData
                {
                    ["src"] = "main_menu",
                    ["ts"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                },
                success = () => Log("  ← ReportEvent success"),
                fail = () => Log("  ← ReportEvent fail"),
                complete = () => { },
            });
        }

        /// <summary>
        /// <c>TT.ReportScene</c> — ghi nhận chuyển cảnh/trang để phân tích phễu (funnel analytics).
        /// </summary>
        private void DoReportScene()
        {
            var p = new JsonData
            {
                ["scene"] = "main_menu",
                ["ts"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            TT.ReportScene(p,
                r => Log("  ← ReportScene success"),
                (code, msg) => Log($"  ← ReportScene fail code={code} msg={msg}"),
                () => { });
        }

        #endregion

        // =================================================================
        // Bảng log
        // =================================================================

        /// <summary>
        /// Thêm một dòng vào cuộn log + Console Unity + kéo thanh cuộn xuống tận cùng.
        /// </summary>
        private void Log(string msg)
        {
            Debug.Log($"[DemoMenu] {msg}");
            _logBuffer.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");
            if (_logBuffer.Count > MaxLogLines)
            {
                // Bỏ bớt các mục cũ nhất để giới hạn bộ nhớ log.
                _logBuffer.RemoveRange(0, _logBuffer.Count - MaxLogLines);
            }
            LogText.text = string.Join("\n", _logBuffer);
            // Bắt buộc render lại layout để ScrollRect tính được chiều cao nội dung mới trước khi gán
            // verticalNormalizedPosition — nếu không việc ghim xuống đáy sẽ bị trễ một frame.
            Canvas.ForceUpdateCanvases();
            LogScroll.verticalNormalizedPosition = 0f;
        }

        private void ClearLog()
        {
            _logBuffer.Clear();
            LogText.text = string.Empty;
        }
    }
}
