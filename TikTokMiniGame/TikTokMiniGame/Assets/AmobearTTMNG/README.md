# AmobearTTMNG — Unity SDK trong Assets

Code dùng chung nằm trực tiếp trong `Assets/AmobearTTMNG`: `Runtime/`, `Editor/`, `UI/`, `Demo/` và `TTSDK~/` (bộ cài TikTok).
Unity 2022.3; cần packages `com.unity.ugui` và `com.unity.textmeshpro`.

## Cài vào game khác

Submodule phải trỏ tới **repo SDK-only đã export**, nhánh `sdk-assets`. Không trỏ nhánh project/tài liệu của TTMNGDoc vào Assets.

```powershell
powershell -NoProfile -File tools/add-ttmng.ps1 -GameProject D:/Games/MyGame -Url <SDK_REPOSITORY_URL> -Ref sdk-assets
```

Hoặc từ thư mục project game, sau khi cài hai Unity packages ở trên:

```bash
git submodule add -b sdk-assets <SDK_REPOSITORY_URL> Assets/AmobearTTMNG
```

Mở Unity, chấp nhận import TTSDK rồi chạy `AmobearTTMNG/Setup Project` và `Validate Project`.
Setup thêm `TikTokBootstrap` trực tiếp vào scene đầu tiên đang bật trong Build Settings, để `nextScene` trống.
Không tạo scene/Camera mới hoặc thay đổi thứ tự build. Chạy lại tái sử dụng bootstrap đã có.
Nếu chưa có scene được bật, Setup báo lỗi để bạn thêm scene game trước. Scene có thay đổi chưa lưu sẽ được hỏi lưu.
Game vẫn chạy trong scene hiện tại trong lúc SDK khởi tạo; code cần SDK phải đợi `TikTokBootstrap.WhenReady(...)`.
Không khai báo `com.amobear.ttmng` trong manifest.json và không giữ bản cũ trong Packages (trùng assembly/GUID).
Demo có sẵn, nhưng không tự thêm vào Build Settings của game. Muốn chạy demo, dùng menu `AmobearTTMNG/Demo/Add Demo Scenes to Build`.
Nếu chữ Demo thiếu, import TMP Essential Resources qua menu Window/TextMeshPro.

Hai đường dẫn TTSDK bắt buộc giữ nguyên: `Assets/Plugins/com.tiktok.minigame` và `Assets/Editor/StarkBuilderSetting.asset`.
Cấu hình của từng game được tạo ở `Assets/AmobearTTMNGConfig` ngoài submodule để không chia sẻ App ID/Placement ID.
Setup giữ lại cấu hình Resources hợp lệ đã có, không tự ghi đè ID.
Validate Project kiểm tra đủ **Ads (Rewarded + Interstitial), Revisit Sidebar, Shortcut và Share**:
- Thiếu/đa cấu hình Ads, thiếu placement hợp lệ, label trùng, ID dùng cho hai loại, thời gian không hợp lệ → Error (chặn Build and Validate).
- Tìm lời gọi API trong source của assembly Player dưới Assets, loại SDK/Editor/Demo đi kèm, Plugins và Samples. Không thấy lời gọi → Warning, vì alias/DLL/reflection có thể không được nhận diện.
- Có lời gọi chỉ là bằng chứng tĩnh, không xác nhận nút UI đã nối, scene build có dùng code hoặc tính năng chạy trên thiết bị.
- Báo cáo luôn kèm checklist test máy thật cho ads, sidebar, shortcut và share; không tự xác nhận trạng thái Portal hay phần thưởng.
Commit submodule pointer, `.gitmodules`, meta thư mục, manifest/lock và các cấu hình được sinh vào repo game.

## Setup nhanh button và popup trong scene game

### Cách nhanh nhất: kéo Canvas vào scene

Kéo **`Assets/AmobearTTMNG/UI/Prefabs/TikTokFeatureCanvas.prefab`** từ Project vào Hierarchy của scene game (đặt ở scene root). Đã có sẵn Canvas Overlay, CanvasScaler 1080×1920, GraphicRaycaster, controller, ba button và ba popup nối đầy đủ. Không cần chạy Setup Feature UI/Connect hoặc gán OnClick. Khi chạy, prefab tái sử dụng EventSystem đang có; nếu thiếu, tự tạo EventSystem với input module theo Input Handling. Canvas mặc định sorting order 100, có thể chỉnh trong Inspector.

Chọn `TikTokFeatureCanvas` để chỉnh giao diện và gán **OnRewardClaimed** của game. Button mở hướng dẫn hoạt động sẵn; Claim chỉ bật sau khi có handler thưởng hợp lệ. Bootstrap SDK vẫn được cài bằng Setup Project tại scene đầu, độc lập với Canvas. Prefab không tự cấp coin hoặc khởi tạo thêm bootstrap. Lưu scene/override ở repo game để không sửa asset gốc trong submodule.

### Nếu muốn dùng Canvas có sẵn

Kéo `UI/Prefabs/TikTokFeatureUI.prefab` vào Canvas của game. Prefab chứa controller, ba button và ba popup đã nối sẵn; không chứa Camera, Canvas, EventSystem hay wallet Demo. Chọn `AmobearTikTokUI` để gán `OnRewardClaimed` cho từng tính năng. Canvas phải có GraphicRaycaster và scene cần EventSystem/input module hoạt động. Inspector báo thiếu các phần này và thiếu bootstrap. Có thể dùng nút **Select complete TikTokFeatureUI prefab** trong cửa sổ Setup để tìm nhanh asset.

Chỉnh layout, nội dung và hàm thưởng trên **instance trong scene của game**, không sửa prefab gốc bên trong submodule. Prefab instance giữ các override của game; các thuộc tính chưa override sẽ theo bản prefab SDK khi cập nhật. Không cần thêm OnClick thủ công: controller đăng ký/hủy listener lúc enable/disable.

### Game đã có button/popup: Connect / Repair

Mở **AmobearTTMNG → Setup Feature UI → Kết nối UI có sẵn / Repair References**. Chọn Canvas/parent (hoặc controller hiện có), tick tính năng cần nối và kéo entry button, popup, Title/Description TMP, action, background-close và close button vào các ô. Mỗi popup cần đủ các thành phần này; với UI custom dùng tên khác, gán trực tiếp.

**Suggest missing references** chỉ điền ô trống khi tìm được đúng một đối tượng khớp tên bên trong popup đã chọn; không sửa scene. Kiểm tra các ô rồi bấm **Connect / Repair**. Công cụ giữ nguyên layout, nội dung, UnityEvent thưởng và OnClick có sẵn; cảnh báo nếu button đã có callback Inspector để dev kiểm tra tránh gọi API hai lần. Không gán một button/popup cho nhiều tính năng, không nối qua scene khác.

Connect chỉ thêm controller/view còn thiếu và cập nhật các tham chiếu được chọn; bỏ tick không xóa tính năng. Có Undo/Redo, không tự lưu scene hoặc đổi Build Settings. Trong Inspector controller, **Repair References** mở lại cùng cửa sổ với dữ liệu hiện có; **Select Button / Select Popup** chọn đúng object để sửa giao diện. Nếu đang dùng UI copy từ Demo, dùng Connect thay vì chỉ copy Canvas — Canvas Demo không chứa controller xử lý các nút.

### Tạo UI mẫu theo từng tính năng

1. Mở scene muốn đặt UI và đặt scene đó **active** nếu đang mở additive.
2. Chọn Canvas hoặc RectTransform con làm nơi đặt nhóm button. Với nhiều Canvas, phải chọn rõ Canvas/parent.
3. Mở **AmobearTTMNG → Setup Feature UI**, chọn Shortcut, Revisit Sidebar, Share và tùy chọn **Tự mở popup Claim** (mặc định bật cả ba và tự mở).
4. Bấm **Add to Current Scene**. Nếu chưa có Canvas, công cụ tạo Overlay Canvas tham chiếu 1080×1920; giữ nguyên CanvasScaler có sẵn. EventSystem hiện hữu được tái sử dụng; nếu thiếu, tạo input module theo Input Handling.
5. Chọn `AmobearTikTokUI`. Trong Inspector, gán hàm cấp thưởng của game vào **OnRewardClaimed** của từng tính năng, sửa **RewardDescription**; Share có query/title/image URL riêng.
6. Chỉnh button dưới nhóm `AmobearTikTokUI`, popup dưới `AmobearTikTokPopups` trên Canvas, rồi tự lưu scene.

Ví dụ: kéo component quản lý thưởng của game vào OnRewardClaimed → chọn hàm `public void GrantShortcutReward()`. SDK không quyết định loại quà/số lượng và không cộng 100 coin Demo. Handler phải tồn tại và không ở trạng thái Off; thiếu handler thì Claim bị khóa, quyền nhận quà vẫn giữ nguyên. Chỉ callback Claim thành công từ module mới gọi event. Không đồng thời cấp cùng phần thưởng qua event toàn cục của module để tránh cộng hai lần. Với Claim bất đồng bộ, giữ đối tượng nhận thưởng sống đến khi callback hoàn tất.

UI chờ SDK khởi tạo, kiểm tra hỗ trợ trước khi bật nút. Popup hướng dẫn dùng Start/Share; khi đủ điều kiện chuyển sang Claim. Các Claim chờ được mở lần lượt **Shortcut → Sidebar → Share**. Đóng popup sẽ không tự bật lại ngay; vẫn mở lại bằng nút tính năng. Lỗi thao tác/Claim hiển thị để thử lại.

Setup chỉ sửa scene active, hỗ trợ Undo/Redo, không tự lưu, không đổi Build Settings và không chạy Setup Project. Chạy lại chỉ thêm tính năng còn thiếu; bỏ tick không xóa UI cũ, không đổi layout/nội dung/event đã chỉnh. UI được sao chép vào scene nên cập nhật submodule không tự ghi đè UI đã tùy chỉnh. Không dùng công cụ trong Play Mode hoặc Prefab Stage.

Chạy **Setup Project** riêng nếu game chưa có bootstrap tại scene đầu. **Validate Project** nhận diện UI trong scene build, kể cả prefab instance, kiểm tra tham chiếu, handler và controller trùng. Chỉ bật một controller TikTok UI khi load additive. Kết quả kiểm tra tĩnh không thay thế kiểm thử trên TikTok thật.

`UI/` chứa prefab, script, font, giấy phép font, shader và hình hướng dẫn dùng chung; công cụ export bao gồm toàn bộ thư mục này. Cần cài UGUI/TMP và import TMP Essential Resources nếu Unity yêu cầu. Nếu game dùng assembly riêng, thêm reference `AmobearTTMNG.UI` khi truy cập component bằng code.

## Cập nhật và clone

Unity tự kiểm tra cập nhật sau khi mở project (khoảng 20 giây) và mỗi 30 phút khi không Play/compile.
SDK phải là một Git checkout riêng tại Assets/AmobearTTMNG; máy cần Git và quyền truy cập origin (VPN nếu cần).
Trình kiểm tra dùng nhánh submodule trong `.gitmodules` (ưu tiên override ở Git config); nếu không cấu hình thì dùng nhánh mặc định của origin.
Popup hiện hash hiện tại/mới và nội dung tối đa 100 commit, đồng thời ghi Console log. Nút **Update SDK** chỉ fast-forward
khi SDK không có thay đổi local. Không tự commit/push repo game, không tự chạy Setup và không cập nhật các plugin khác.
Sau Update: chờ compile, chạy Validate, test game rồi commit con trỏ submodule. Chạy Setup nếu bản mới cần áp dụng thay đổi scene/config.
Menu **AmobearTTMNG/Check for SDK Updates** cho phép kiểm tra ngay hoặc mở lại thông báo đã đóng.
Mất mạng/thiếu quyền chỉ ghi cảnh báo; không chặn mở game. Batch mode không kiểm tra tự động.
Các project dùng bản SDK cũ chưa có trình kiểm tra phải cập nhật thủ công một lần để nhận tính năng này.

```bash
git clone --recurse-submodules <GAME_REPOSITORY_URL>
# Với clone có sẵn:
git submodule update --init --recursive
# Nâng SDK sau khi đã kiểm tra thay đổi local:
git -C Assets/AmobearTTMNG fetch origin
git -C Assets/AmobearTTMNG checkout <SDK_COMMIT_OR_TAG>
git add Assets/AmobearTTMNG
```

Không commit cấu hình riêng của game vào repo SDK. Bản dev có thể có thư mục test bổ sung, bản export chỉ lấy code, demo và bộ cài.

## 2. Module

| Module | Dùng để | Ghi chú |
|---|---|---|
| `TikTokBootstrap` | InitSDK, `WhenReady`, `WhenSdkReady`, `OnShow`/`OnHide`/`OnShowWithParams` | Xem mục 2.1. OnHide tự `TikTokStorage.Save()`. |
| `TikTokStorage` | Key-value bền (`tt.setStorageSync`) | Probe ghi→đọc lúc khởi động, hỏng thì fallback PlayerPrefs. Chuỗi rỗng = xoá key. |
| `TikTokFileSystem` | Ghi/đọc file trong container | `persistentDataPath` / C# file IO KHÔNG bền trên TikTok. Editor trả false/null. |
| `TikTokAds` + `TikTokAdsConfig` | Rewarded / Interstitial theo label | Gọi `TT.CreateRewardedVideoAd` / `TT.CreateInterstitialAd` với Placement ID trong config. Chặn sẵn luật 15s / 30s và ad chồng nhau. |
| `TikTokShortcut` | Desktop Shortcut Revisit | Nhận quà hai bước. Mặc định nhận **một lần**. |
| `TikTokSidebar` | Sidebar Revisit | Nhận quà hai bước. Mặc định **mỗi ngày một lần**. |
| `TikTokShare` | `shareAppMessage` + đọc query khi bạn bè mở | Quà share tuỳ chọn, mỗi ngày một lần. |
| `TikTokSafeArea` / `SafeAreaFitter` | Vùng an toàn + vùng nút capsule | WebGL `Screen.safeArea` luôn trả toàn màn hình — phải dùng module này. |
| `TikTokNetwork` | Loại mạng, mất mạng, mạng yếu | `OnChanged` replay-on-subscribe. |
| `TikTokVibrate` | Rung ngắn / dài | Chặn gọi dồn 0.1s; `Enabled = false` khi người chơi tắt rung. |

### 2.1. InitSDK chậm

`TikTokBootstrap` chờ InitSDK tối đa 8s rồi cho game chạy tiếp ở chế độ không SDK; callback vẫn có thể đến muộn.

- `WhenReady(ok => ...)`: trả lời NGAY khi init có kết quả — sau timeout là `false`. Dùng cho lệnh người chơi bấm.
- `WhenSdkReady(() => ...)`: chạy khi SDK sẵn sàng, **kể cả thành công sau timeout**. Dùng cho việc lúc khởi động cần
  SDK (preload, gắn listener). Các module trong package đã dùng nó: thưởng Shortcut / Sidebar được hỏi lại, ads
  preload, mạng, safe area đều tự chạy lại.
- `CheckSupported` (Shortcut / Sidebar / Share) có thể gọi callback **hai lần** (`false` rồi `true`) — UI bật / tắt
  nút theo giá trị mới nhất.

---

## 3. Nhận quà Shortcut / Sidebar / Share (template chỉ làm luồng, GAME quyết phần thưởng)

```csharp
using AmobearTTMNG;

// 1. Lối vào
TikTokShortcut.CheckSupported(ok => shortcutButton.SetActive(ok));
shortcutButton.onClick → TikTokShortcut.Add(ok => ShowHint("Mở lại game từ icon để nhận quà"));

// 2. Module tự hỏi nền tảng lúc mở game + mỗi OnShow. Đủ điều kiện → quyền nhận quà được LƯU NGAY.
TikTokShortcut.OnPendingRewardChanged += () => claimPopup.SetActive(TikTokShortcut.HasPendingReward);

// 3. Người chơi bấm Claim → event → game cấp quà của mình.
claimButton.onClick → TikTokShortcut.Claim();
TikTokShortcut.OnRewardClaimed += period => Wallet.Add(500);
```

- Lúc mở scene, kiểm `HasPendingReward` (quà có thể còn treo từ phiên trước).
- Đổi chu kỳ: `TikTokShortcut.ClaimPeriod = () => DateTime.UtcNow.ToString("yyyyMMdd");`
- **Chống nhận trùng:** mặc định chỉ có sổ cục bộ (chặn trùng trên một máy). Doc Shortcut / Sidebar của TikTok yêu cầu
  backend ghi sổ theo `open_id + mission + kỳ`; package chưa có backend chung — game có server thì gán
  `ServerClaim = (period, done) => { /* gọi server, done(true) chỉ lần đầu */ };`
- Sidebar / Share dùng API giống hệt (`TikTokSidebar.Start`, `TikTokShare.Share`).
- Test: DevTool launch mode `scene_shortcut` / `scene_center`. Icon thật chỉ mở **bản đã phát hành**.
  Editor: tick `Simulate … In Editor` trên `ShortcutDemo` khi đang Play.

## 4. Quảng cáo (theo https://developers.tiktok.com/docs/en/in-app-ads)

**Lấy Placement ID (mỗi game một bộ riêng):**
1. Organization đã bật + được duyệt **IAA** (Monetization → Feature enablement, chỉ admin; duyệt 1–2 ngày).
2. Developer Portal → app → **Operation → Monetization → In-App Ads (IAAs)** → bật **Ad placements**.
3. **Add ad placement** → đặt tên, chọn **Rewarded** hoặc **Interstitial** → **Add**.
4. Chuyển placement sang **Active** (mặc định inactive → lỗi `20003`).
5. Copy **Placement ID** → điền vào `Resources/TikTokAdsConfig.asset`:

| Label (game dùng) | Type | Placement ID |
|---|---|---|
| `rw_revive` | Rewarded | `<copy từ Portal>` |
| `it_level_end` | Interstitial | `<copy từ Portal>` |

**Gọi trong game:**
```csharp
using AmobearTTMNG.Ads;
TikTokAds.ShowVideoReward(() => GiveReward(), "rw_revive");        // chỉ chạy khi xem HẾT
TikTokAds.OnRewardUnavailable += (label, reason) => ShowToast("Chưa có quảng cáo, thử lại sau");
TikTokAds.ShowInterstitial(() => ContinueFlow(), "it_level_end");  // LUÔN chạy đúng một lần
TikTokAds.IsAdsActive = () => !Profile.NoAds;                      // Remove Ads chỉ tắt interstitial
```

- Interstitial chỉ đặt ở chỗ nghỉ tự nhiên (hết màn, chuyển scene) — KHÔNG giữa lúc đang chơi, không hẹn giờ bật ngang.
- Module tự chặn: ad chồng nhau (`11006`), interstitial trong **15s** đầu và cách nhau **< 30s** (`11004`). Tăng
  `InterstitialIntervalSeconds` trong config nếu muốn thưa hơn — không hạ dưới 30.
- Rewarded instance được tạo sẵn khi SDK init (`PreloadRewarded`); mỗi instance show một lần rồi tạo lại.
- Label chưa khai báo / sai loại / Placement ID rỗng → rewarded báo unavailable, interstitial bỏ qua (xem log `[Ads]`).
- Editor: hiển thị mock UI của TTSDK để chọn xem hết / đóng sớm / lỗi; bật `TikTokAds.EditorSimulateNoFill` để test nhánh không có ad.
- Test thật: DevTool → **tắt Enable IAA Mock**, bật Show vConsole để xem log. Doanh thu chỉ xem được sau khi game lên chính thức.
- Không có banner / app-open.

Mã lỗi chính thức của TikTok (Error Code Reference, phần IAA):

| Mã | Nghĩa | Xử lý |
|---|---|---|
| `20001` / `20002` / `20003` / `20004` / `20007` | Placement không tồn tại / không thuộc game / chưa Active / IAA chưa bật / sai loại | Sửa trên Developer Portal (Monetization → In-App Ads) |
| `30001` (init) | Gọi cùng ad unit quá dày | Giảm tần suất gọi |
| `11003` | Instance đã show rồi | Mỗi instance chỉ show một lần — tạo mới |
| `11004` | Interstitial chưa đủ khoảng cách | Quy định: không show trong **15s** đầu sau khi module ads khởi động, cách nhau **≥ 30s** |
| `11005` | Interstitial vượt số lần hiển thị | Giảm tần suất |
| `11006` | Đang có ad khác hiện | Gọi show tuần tự |
| `30001` (show) | Người chơi đóng rewarded trước khi xem hết | Không thưởng, cho chơi tiếp |
| `50000` + `inner_error_code` | Lỗi hệ thống (50001 client key sai, 50008 container đang pre-render...) | Gửi support kèm inner code |

Khi duyệt bản phát hành (Acceptance Testing), checklist của TikTok có: **rewarded ad, IAP, Silent Login, Home Screen
Shortcut, Revisit From Profile**. Template không có IAP / login — lúc Submit for review ghi chú cho reviewer: "game chỉ
có quảng cáo, không có tài khoản / IAP, dữ liệu lưu cục bộ". Quà shortcut phải **chỉ nhận được một lần**. Test quảng cáo thật thì tắt **Enable IAA Mock** trong DevTool.

---

## 5. Test trên máy thật (không tự động được)

- **Quét QR không vào được** (`ttmg dev` kẹt "connecting" / timeout): tài khoản quét phải nằm trong **Test User** của
  app; điện thoại và máy dev cùng mạng / cùng dải IP (Wi-Fi khách của công ty thường cách ly thiết bị); tắt VPN chế độ
  Global / TUN, nếu cần proxy thì `ttmg config set proxy http://127.0.0.1:<port>` thay vì VPN toàn hệ thống; firewall
  phải cho **Node.js** nhận kết nối ở cả mạng Private và Public (phát hotspot từ điện thoại là mạng Public); máy có
  nhiều card mạng (VM, dây + Wi-Fi) thì IP trên QR có thể sai.
- Mở lần đầu, mở lần hai, ra nền rồi quay lại, mạng, UI trên máy đích. Ghi dữ liệu → thoát → mở lại → còn dữ liệu.
- Shortcut: DevTool launch mode `scene_shortcut`. Icon thật trên màn hình chính chỉ mở **bản đã phát hành** — bản
  preview / debug không mở được từ icon.
- Sidebar: DevTool launch mode `scene_center`, rồi luồng thật: vào bình thường → Revisit → vào lại từ sidebar trang
  cá nhân.
- Quảng cáo thật: tắt **Enable IAA Mock** trong DevTool.

## 6. Khi báo lỗi cho TikTok

Gửi một lần: ClientKey, Unity version, TikTok Unity SDK version, cấu trúc thư mục `tt-minigame`, dung lượng gói,
có bật Wasm split không, TikTok App version, thiết bị + OS, SystemInfo, lỗi đầu tiên, build log, các bước tái hiện,
video. Các module đã log kèm `platform / system / tiktok version` (`TikTokShortcut.DeviceInfo()`).

Khi `game.json.wasmFuncCount` ≥ 80000 → xem Unity Wasm Code Splitting Guide.
